using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace ProjectPumpernickle {
    public interface IRewardStatisticsGroup {
        RewardOutcomeStatistics Evaluate();
    }
    public class RewardOutcomeStatistics {
        public float rewardOutcomeMean;
        public float rewardOutcomeStd;
        public float chosenValue;
        public void Build<T>(IEnumerable<T> options, Func<T, float> scoreFn, Func<T, float> countFn, Func<T, bool> chosen) {
            var optionArray = options.ToArray();
            var scores = optionArray.Select(scoreFn).ToArray();
            var counts = optionArray.Select(countFn).ToArray();
            Build(scores, counts);
            var chosenIndicies = Enumerable.Range(0, optionArray.Length).Where(x => chosen(optionArray[x]));
            if (chosenIndicies.Any()) {
                chosenValue = chosenIndicies.Select(x => scores[x]).Average();
            }
        }
        public void Build(float[] scores, float[] counts = null) {
            if (counts == null) {
                counts = Enumerable.Repeat(1f, scores.Length).ToArray();
            }
            if (!counts.Any(x => x > 0)) {
                return;
            }
            var popSize = counts.Sum();
            rewardOutcomeMean = Enumerable.Range(0, scores.Length).Select(x => scores[x] * counts[x] / popSize).Sum();
            chosenValue = rewardOutcomeMean;
            var averageCount = counts.Where(x => x > 0).Average();
            var variance = 0.0;
            for (int i = 0; i < scores.Length; i++) {
                if (counts[i] <= 0f) {
                    continue;
                }
                variance += Math.Pow(scores[i] - rewardOutcomeMean, 2f) * counts[i] / popSize;
            }
            rewardOutcomeStd = (float)Math.Sqrt(variance);
        }
        public float ChanceToWin(Evaluation evaluation) {
            // Assume that the average run gets points linearly per floor,
            // reaches 100 points at the end of the game, wins half the time
            // with a standard deviation of 15 points
            // https://www.wolframalpha.com/input?i=normal+distribution+mean+%3D+100+standard+deviation+%3D+15
            var lastFloor = Evaluators.LastFloorThisAct(4);
            var nominalPointsPerFloor = 100f / lastFloor;
            var nominalStdPerFloor = 15f / lastFloor;
            var averageCurrentScore = evaluation.InternalScore - chosenValue + rewardOutcomeMean;
            var observedPointsPerFloor = Save.state.floor_num == 0 ? nominalPointsPerFloor : averageCurrentScore / Save.state.floor_num;
            var floorsLeft = lastFloor - Save.state.floor_num;
            var projectedPointsAdded = 0f;
            var projectedStdAdded = nominalStdPerFloor * floorsLeft;
            for (int i = 0; i < floorsLeft; i++) {
                var t = Lerp.Inverse(0, 10, i);
                projectedPointsAdded += Lerp.From(observedPointsPerFloor, nominalPointsPerFloor, t);
            }
            var projectedScore = averageCurrentScore + projectedPointsAdded;
            var projectedDeviation = rewardOutcomeStd + projectedStdAdded;
            var deviationsAboveHundred = (projectedScore - 100f) / projectedDeviation;
            return PumpernickelMath.Sigmoid(deviationsAboveHundred);
        }
        public static RewardOutcomeStatistics operator *(RewardOutcomeStatistics r, float f) {
            r.chosenValue *= f;
            r.rewardOutcomeMean *= f;
            r.rewardOutcomeStd *= MathF.Sqrt(f);
            return r;
        }
    }
    public class AddCardStatisticsGroup : IRewardStatisticsGroup {
        public Color color;
        public Rarity rarity;
        public string cardId { get; protected set; }
        public AddCardStatisticsGroup(Color color, Rarity rarity) {
            this.color = color;
            this.rarity = rarity;
            cardId = Evaluators.AverageRandomCard(color, rarity);
        }
        protected static float evaluateCard(Card card) {
            var added = Save.state.AddCardById(card.id);
            var r = EvaluationFunctionReflection.GetCardEvalFunctionCached(card.id)(Save.state.cards[added], added);
            Save.state.cards.RemoveAt(added);
            return r;
        }
        public RewardOutcomeStatistics Evaluate() {
            var r = new RewardOutcomeStatistics();
            r.Build(
                Database.instance.cards.Where(x => x.cardColor.Is(color) && x.cardRarity.Is(rarity)),
                evaluateCard,
                x => 1f, // This should probably support rarity
                x => x.id.Equals(cardId)
            );
            return r;
        }
    }
    public class ChooseCardsStatisticsGroup : IRewardStatisticsGroup {
        public Color color;
        public string cardId { get; protected set; }
        protected float[] cardRarityAppearances;
        protected float[] cardShopAppearances;
        public ChooseCardsStatisticsGroup(float[] cardRarityAppearances = null, float[] cardShopAppearances = null, Color color = Color.Eligible, Rarity rarity = Rarity.Randomable) {
            this.color = color;
            this.cardRarityAppearances = cardRarityAppearances;
            if (cardRarityAppearances == null) {
                this.cardRarityAppearances = Evaluators.ExpectedCardRewardAppearances(1f, useCurrentRandomizer: true);
            }
            this.cardShopAppearances = cardShopAppearances;
            // This is assuming a kinda bad outcome.  This is the average card, not the average card you would choose
            cardId = Evaluators.AverageRandomCard(color, rarity);
        }
        public static float ReshapeMeanBySelecting(RewardOutcomeStatistics scoreStats, float numPicks) {
            // The score stats are the number of points added by adding a certain card multiplied by
            // the number of that card we expect to see.  Therefore, the number of total points we 
            // expect to gain is both the sum of that distribution and the mean of these stats
            // times the number of cards.  We want to alter that distribution because we don't pick
            // cards randomly, and can't pick more than one per reward.
            // https://www.wolframalpha.com/input?i=plot+e%5E%28-x%5E2%29+vs+e%5E%28-%28x-1%29%5E2%29+%2F+5.5
            // This assumes 3 cards!!!
            return ((scoreStats.rewardOutcomeMean + scoreStats.rewardOutcomeStd) / 5.5f) * numPicks;
        }
        public RewardOutcomeStatistics Evaluate() {
            var r = new RewardOutcomeStatistics();
            var cardScores = Scoring.CardScoreProvider(cardRarityAppearances, cardShopAppearances, color).ToArray();
            var totalRewards = cardScores.Select(x => x.expectedSeen).Sum() / 3f;
            r.Build(cardScores, x => x.score, x => x.expectedSeen, x => x.cardId.Equals(cardId));
            r.rewardOutcomeMean = ReshapeMeanBySelecting(r, totalRewards);
            return r;
        }
    }
    public class CursedTomeRewardGroup : IRewardStatisticsGroup {
        public static readonly string CHOSEN = "Necronomicon";
        public static readonly string[] possibleRelics = new string[] {
            "Necronomicon",
            "Nilry's Codex",
            "Enchiridion",
        };
        protected static float evaluateCard(Relic relic) {
            return EvaluationFunctionReflection.GetRelicEvalFunctionCached(relic.id)(relic);
        }
        public RewardOutcomeStatistics Evaluate() {
            var r = new RewardOutcomeStatistics();
            r.Build(
                Database.instance.relics.Where(x => possibleRelics.Contains(x.id)),
                evaluateCard,
                x => 1f,
                x => x.id.Equals(CHOSEN)
            );
            return r;
        }
    }
    public class BossSwapStatisicsGroup : IRewardStatisticsGroup {
        public static string ASSUMED_SWAP = "Black Star";
        protected static float evaluateRelic(Relic relic) {
            return EvaluationFunctionReflection.GetRelicEvalFunctionCached(relic.id)(relic);
        }
        public RewardOutcomeStatistics Evaluate() {
            var r = new RewardOutcomeStatistics();
            r.Build(
                Database.instance.relics.Where(x => x.rarity == Rarity.Boss),
                evaluateRelic,
                x => 1f,
                x => x.id.Equals(ASSUMED_SWAP)
            );
            return r;
        }
    }
    public class AddRelicsStatisticsGroup : IRewardStatisticsGroup {
        protected float[] foundRelicsByRarity;
        protected float[] shopRelicsByRarity;
        public string relicId { get; protected set; }
        public AddRelicsStatisticsGroup(float[] foundRelicsByRarity = null, float[] shopRelicsByRarity = null) {
            if (foundRelicsByRarity == null) {
                foundRelicsByRarity = Scoring.RelicRarityDistribution(1f, shop: false);
            }
            this.foundRelicsByRarity = foundRelicsByRarity;
            if (shopRelicsByRarity == null) {
                shopRelicsByRarity = new float[4];
            }
            this.shopRelicsByRarity = shopRelicsByRarity;
            relicId = Evaluators.AverageRandomRelic(foundRelicsByRarity, shopRelicsByRarity);
        }
        public RewardOutcomeStatistics Evaluate() {
            var foundScores = Scoring.RelicScoreProvider(foundRelicsByRarity).ToArray();
            var foundStats = new RewardOutcomeStatistics();
            var boughtScores = Scoring.RelicScoreProvider(shopRelicsByRarity).ToArray();
            var boughtStats = new RewardOutcomeStatistics();
            var totalFound = foundRelicsByRarity.Sum();
            var totalBought = shopRelicsByRarity.Sum();
            foundStats.Build(foundScores, x => x.score, x => x.expectedFound, x => x.relicId.Equals(relicId));
            boughtStats.Build(boughtScores, x => x.score, x => x.expectedFound, x => x.relicId.Equals(relicId));
            foundStats *= totalFound;
            boughtStats.chosenValue *= totalBought;
            boughtStats.rewardOutcomeMean = ChooseCardsStatisticsGroup.ReshapeMeanBySelecting(boughtStats, totalBought);
            boughtStats.rewardOutcomeStd *= MathF.Sqrt(totalBought);
            var r = new RewardOutcomeStatistics();
            r.rewardOutcomeMean = foundStats.rewardOutcomeMean + boughtStats.rewardOutcomeMean;
            r.rewardOutcomeStd = MathF.Sqrt(MathF.Pow(foundStats.rewardOutcomeMean, 2f) + MathF.Pow(boughtStats.rewardOutcomeMean, 2f));
            r.chosenValue = foundStats.chosenValue + boughtStats.chosenValue;
            return r;
        }
    }
    public class AddCommonRelicStatisicsGroup : AddRelicsStatisticsGroup {
        public static string ASSUMED_ADD = "Orichalcum";
        public AddCommonRelicStatisicsGroup() : base(new float[] { 1f, 0f, 0f, 0f }, new float[] { 0f, 0f, 0f, 0f }) {
            relicId = ASSUMED_ADD;
        }
    }
    public class AddRareRelicStatisicsGroup : AddRelicsStatisticsGroup {
        public static string ASSUMED_ADD = "Ginger";
        public AddRareRelicStatisicsGroup() : base(new float[] { 0f, 0f, 1f, 0f }, new float[] { 0f, 0f, 0f, 0f }) {
            relicId = ASSUMED_ADD;
        }
    }
    public class RandomUpgradeStatisticsGroup : IRewardStatisticsGroup {
        public int numUpgrades;
        public List<int> assumedUpgrades;
        public RandomUpgradeStatisticsGroup(int numUpgrades) {
            this.numUpgrades = numUpgrades;
            var numCards = Save.state.cards.Count;
            var skipCount = (numCards - numUpgrades) / 2;
            assumedUpgrades = Enumerable.Range(0, numCards)
                .OrderBy(x => Save.state.cards[x].upgradeBias)
                .Skip(skipCount)
                .Take(numUpgrades)
                .ToList();
        }
        protected static float evaluateUpgrade(int upgradeCardIndex) {
            var card = Save.state.cards[upgradeCardIndex];
            var isUpgradable = card.Upgradable();
            if (isUpgradable) {
                card.upgrades++;
            }
            else {
                card.upgrades--;
            }
            var scoreDelta = Scoring.DeepEvaluationScoreDelta();
            if (isUpgradable) {
                card.upgrades--;
                return scoreDelta;
            }
            else {
                card.upgrades++;
                return -scoreDelta;
            }
        }
        public RewardOutcomeStatistics Evaluate() {
            var r = new RewardOutcomeStatistics();
            var relevantCardIndicies = Enumerable.Range(0, Save.state.cards.Count).Where(x => Save.state.cards[x].Upgradable());
            if (Save.state.upgraded != null) {
                // This double-counts searing blow
                relevantCardIndicies = relevantCardIndicies.Concat(Save.state.upgraded);
            }
            r.Build(
                relevantCardIndicies,
                evaluateUpgrade,
                x => 1f,
                x => assumedUpgrades.Contains(x)
            );
            r.rewardOutcomeMean *= numUpgrades;
            r.rewardOutcomeStd *= numUpgrades;
            r.chosenValue *= numUpgrades;
            return r;
        }
    }
}
