using System;
using System.Collections.Generic;
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
        public void Build<T>(IEnumerable<T> options, Func<T, float> scoreFn, Func<T, bool> chosen) {
            var optionArray = options.ToArray();
            var scores = optionArray.Select(scoreFn).ToArray();
            Build(scores);
            var chosenIndex = optionArray.FirstIndexOf(chosen);
            chosenValue = scores[chosenIndex];
        }
        public void Build(float[] scores) {
            rewardOutcomeMean = scores.Average();
            chosenValue = rewardOutcomeMean;
            var variance = 0.0;
            for (int i = 0; i < scores.Length; i++) {
                variance += Math.Pow(scores[i] - rewardOutcomeMean, 2f);
            }
            rewardOutcomeStd = (float)Math.Sqrt(variance / scores.Length);
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
                x => x.id.Equals(cardId)
            );
            return r;
        }
    }
    public class ChooseCardStatisticsGroup : IRewardStatisticsGroup {
        public Color color;
        public Rarity rarity;
        public string cardId { get; protected set; }
        protected float[] cardRarityAppearances;
        public ChooseCardStatisticsGroup(float[] cardRarityAppearances, Color color, Rarity rarity) {
            this.color = color;
            this.rarity = rarity;
            this.cardRarityAppearances = cardRarityAppearances;
            // This is assuming a kinda bad outcome.  This is the average card, not the average card you would choose
            cardId = Evaluators.AverageRandomCard(color, rarity);
        }
        protected static float ReshapeMeanBySelecting(RewardOutcomeStatistics scoreStats, float numCards) {
            // The score stats are the number of points added by adding a certain card multiplied by
            // the number of that card we expect to see.  Therefore, the number of total points we 
            // expect to gain is both the sum of that distribution and the mean of these stats
            // times the number of cards.  We want to alter that distribution because we don't pick
            // cards randomly, and can't pick more than one per reward.
            // https://www.wolframalpha.com/input?i=plot+e%5E%28-x%5E2%29+vs+e%5E%28-%28x-1%29%5E2%29+%2F+5.5
            // This assumes 3 cards!!!
            return ((scoreStats.rewardOutcomeMean + scoreStats.rewardOutcomeStd) / 5.5f) * numCards;
        }
        public RewardOutcomeStatistics Evaluate() {
            var r = new RewardOutcomeStatistics();
            var cardScores = Scoring.CardScoreProvider(cardRarityAppearances, color, rarity).ToArray();
            r.Build(cardScores, x => x.score, x => x.cardId.Equals(cardId));
            r.rewardOutcomeMean = ReshapeMeanBySelecting(r, cardScores.Length);
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
                x => x.id.Equals(ASSUMED_SWAP)
            );
            return r;
        }
    }
}
