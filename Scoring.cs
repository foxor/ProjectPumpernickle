using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ProjectPumpernickle {
    public interface IGlobalRule {
        public bool ShouldApply { get; }
        public void Apply(Evaluation evaluation);
    }
    internal class Scoring {
        public static readonly float VALUE_PER_NET_SAVING = 0.25f;

        protected static IGlobalRule[] GlobalRules;
        static Scoring() {
            var globalRuleTypes = typeof(Scoring).Assembly.GetTypes().Where(x => typeof(IGlobalRule).IsAssignableFrom(x) && typeof(IGlobalRule) != x);
            GlobalRules = globalRuleTypes.Select(x => (IGlobalRule)Activator.CreateInstance(x)).ToArray();
        }
        public static void EvaluateGlobalRules(Evaluation evaluation) {
            foreach (var globalRule in GlobalRules) {
                if (globalRule.ShouldApply) {
                    globalRule.Apply(evaluation);
                }
            }
        }
        public static readonly float MAX_SHOP_VALUE = 5f;
        public static float PointsForShop(float goldBrought) {
            if (goldBrought < 180f) {
                return 0f;
            }
            // https://www.wolframalpha.com/input?i=1+-+%281+%2F+%281+%2B+%28%28x+-+180%29+%2F+360%29%29%29+from+0+to+1000
            var shopValue = 1f - (1f / (1 + ((goldBrought - 180f) / 360f)));
            return (float)shopValue * MAX_SHOP_VALUE;
        }
        public static readonly float FIND_HUNTED_CARD_BONUS = 3f;
        protected static float ScoreValueOfCard(Card cardAdded) {
            var pointDelta = 0f;
            int addedIndex = -1;
            int removedIndex = -1;
            Card removed = null;
            // If we're adding this card now, future copies will be worth less
            // We want to avoid that having an impact on score, because it would
            // disincentivise picking good cards we might see later.
            // To compensate, we assign future copies the same score as the first one
            // like we do when we're not choosing the card now
            if (Save.state.ChoosingNow(cardAdded.id)) {
                removedIndex = Save.state.cards.FirstIndexOf(x => x.id.Equals(cardAdded.id));
                removed = Save.state.cards[removedIndex];
                Save.state.cards.RemoveAt(removedIndex);
            }
            else {
                addedIndex = Save.state.AddCardById(cardAdded.id);
            }
            if (Save.state.huntingCards.Contains(cardAdded.id)) {
                pointDelta += FIND_HUNTED_CARD_BONUS;
            }
            var cardQuality = 0f;
            var relicQuality = 0f;
            for (int i = 0; i < Save.state.cards.Count; i++) {
                var card = Save.state.cards[i];
                var cardValue = EvaluationFunctionReflection.GetCardEvalFunctionCached(card.id)(card, i);
                cardQuality += cardValue;
            }
            for (int i = 0; i < Save.state.relics.Count; i++) {
                var relicId = Save.state.relics[i];
                // TODO: fix setup relics
                var relic = Database.instance.relicsDict[relicId];
                relicQuality += EvaluationFunctionReflection.GetRelicEvalFunctionCached(relic.id)(relic);
            }
            if (addedIndex != -1) {
                Save.state.cards.RemoveAt(addedIndex);
                pointDelta += cardQuality - Evaluation.Active.InternalScores[(byte)ScoreReason.DeckQuality];
            }
            else {
                Save.state.cards.Insert(removedIndex, removed);
                pointDelta += -cardQuality + Evaluation.Active.InternalScores[(byte)ScoreReason.DeckQuality];
            }
            pointDelta += relicQuality - Evaluation.Active.InternalScores[(byte)ScoreReason.RelicQuality];
            return pointDelta;
        }
        public struct CardScore {
            public string cardId;
            public float score;
            public CardScore(string cardId, float score) {
                this.cardId = cardId;
                this.score = score;
            }
        }
        public static IEnumerable<CardScore> CardScoreProvider(float[] cardRarityAppearances, Color colorLimit = Color.Eligible, Rarity rarityLimit = Rarity.Randomable) {
            var path = Evaluation.Active.Path;
            var cardShopAppearances = new float[6];
            var hitDensity = new float[6];
            for (int i = 0; i < 6; i++) {
                var rarity = (i % 3) switch {
                    0 => Rarity.Common,
                    1 => Rarity.Uncommon,
                    2 => Rarity.Rare
                };
                var color = (i / 3) switch {
                    0 => Save.state.character.ToColor(),
                    1 => Color.Colorless,
                };
                if (rarity == Rarity.Common && color == Color.Colorless) {
                    continue;
                }
                hitDensity[i] = Evaluators.ChanceOfSpecificCard(color, rarity);
                var densityOfRarity = Evaluators.DensityOfRarity(color, rarity);
                var classCardsPerShop = 5;
                var seenPerShop = color == Color.Colorless ? 1 : (classCardsPerShop * densityOfRarity);
                var previousFloorShopChance = 0f;
                float previousFloorGold = Save.state.gold;
                for (int n = 0; n < path.expectedShops.Length; n++) {
                    var marginalShopChance = path.expectedShops[n] - previousFloorShopChance;
                    if (marginalShopChance > 0f) {
                        cardShopAppearances[i] += Evaluators.IsEnoughToBuyCard(previousFloorGold, rarity) * marginalShopChance * seenPerShop;
                    }
                    previousFloorShopChance = path.expectedShops[n];
                    previousFloorGold = path.expectedGold[n];
                }
            }

            foreach (var card in Database.instance.cards) {
                var color = card.cardColor;
                var rarity = card.cardRarity;
                if (!colorLimit.Is(color)) {
                    continue;
                }
                if (!rarityLimit.Is(rarity)) {
                    continue;
                }
                if (card.cardType == CardType.Status || card.cardType == CardType.Curse) {
                    continue;
                }
                var rarityOffset = rarity switch {
                    Rarity.Common => 0,
                    Rarity.Uncommon => 1,
                    Rarity.Rare => 2,
                };
                var chanceToFind = 0f;
                if (color == Color.Colorless) {
                    chanceToFind += cardShopAppearances[3 + rarityOffset];
                }
                else {
                    chanceToFind += cardShopAppearances[0 + rarityOffset];
                    chanceToFind += cardRarityAppearances[rarityOffset];
                }
                var densityIndex = (color == Color.Colorless ? 3 : 0) + rarityOffset;
                var expectedFound = chanceToFind * hitDensity[densityIndex];
                // This is slightly wrong because multiplies of cards generally aren't as good
                // This is why we don't include hypothetical points for cards that are available now
                yield return new CardScore(card.id, ScoreValueOfCard(card) * expectedFound);
            }
        }
        public static readonly float FUTURE_CARD_CURRENT_POINT_MIN = 0.25f;
        protected static void ScoreFutureCardValue(Evaluation evaluation) {
            var rewards = Enumerable.Range(1, 4).Select(actNum => {
                var index = Path.FloorNumToPathIndex(Evaluators.LastFloorThisAct(actNum));
                if (index < 0) {
                    return 0f;
                }
                return evaluation.Path.expectedCardRewards[index];
            }).ToArray();
            var cardRarityAppearances = Enumerable.Range(1, 4).Select(actNum => {
                var beginningOfAct = actNum == 1 ? 0 : rewards[actNum - 2];
                var endOfAct = rewards[actNum - 1];
                var isCurrentAct = actNum == Save.state.act_num;
                return Evaluators.ExpectedCardRewardAppearances(endOfAct - beginningOfAct, isCurrentAct);
            }).Sum();
            var stats = new ChooseCardStatisticsGroup(cardRarityAppearances, Color.Eligible, Rarity.Randomable);
            var outcome = stats.Evaluate();
            var cardRewardExpectedTotalValue = outcome.rewardOutcomeMean;
            var currentValueMultiplier = Lerp.From(FUTURE_CARD_CURRENT_POINT_MIN, 1f, Save.state.floor_num / 55f);
            evaluation.AddScore(ScoreReason.CardReward, cardRewardExpectedTotalValue * currentValueMultiplier);
        }
        public static void ScorePath(Evaluation evaluation) {
            // This doesn't ever award points for future acts to avoid perverse incentives
            var path = evaluation.Path;
            var floorsTillEndOfAct = Evaluators.LastFloorThisAct(Save.state.act_num) - Save.state.floor_num;


            evaluation.AddScore(ScoreReason.Upgrades, 20f * Evaluators.PercentAllGreen(evaluation));

            var expectedRelics = path.expectedRewardRelics[floorsTillEndOfAct];
            // Should we take an average?
            evaluation.AddScore(ScoreReason.RelicCount, expectedRelics);

            evaluation.AddScore(ScoreReason.Key, Save.state.has_sapphire_key ? .5f : 0);

            var effectiveHealth = Evaluators.GetCurrentEffectiveHealth();
            evaluation.AddScore(ScoreReason.CurrentEffectiveHealth, effectiveHealth / 30f);

            // This does incorporate future act points, because otherwise we might misbehave
            var goldBrought = path.ExpectedGoldBroughtToShops();
            evaluation.AddScore(ScoreReason.BringGoldToShop, goldBrought.Select(PointsForShop).Sum());

            var wingedBootChargesLeft = Math.Max(Evaluators.WingedBootsChargesLeft(), 0) - path.jumps;
            var actsLeft = Math.Max(3 - Save.state.act_num, 0);
            evaluation.AddScore(ScoreReason.WingedBootsCharges, wingedBootChargesLeft * actsLeft * 0.03f);
            evaluation.AddScore(ScoreReason.WingedBootsFlexibility, (wingedBootChargesLeft * actsLeft >= 1) ? 1.5f : 0f);

            path.AddEventScore(evaluation);

            if (Save.state.act_num < 3 && (Save.state.has_emerald_key || path.hasMegaElite)) {
                evaluation.AddScore(ScoreReason.EarlyMegaElite, 2f);
            }

            if (Save.state.act_num == 3 && !Save.state.has_emerald_key && path.hasMegaElite != true) {
                evaluation.AddScore(ScoreReason.MissingKey, float.MinValue);
            }
            if (Save.state.act_num == 3 && !Save.state.has_sapphire_key && !path.ContainsGuaranteedChest()) {
                evaluation.AddScore(ScoreReason.MissingKey, float.MinValue);
            }

            if (Save.state.badBottle) {
                evaluation.AddScore(ScoreReason.BadBottle, -5f);
            }
        }
        public static void ScoreBasedOnEvaluation(Evaluation evaluation) {
            for (int i = 0; i < Save.state.cards.Count; i++) {
                var card = Save.state.cards[i];
                var cardValue = EvaluationFunctionReflection.GetCardEvalFunctionCached(card.id)(card, i);
                evaluation.AddScore(ScoreReason.DeckQuality, cardValue);
            }
            for (int i = 0; i < Save.state.relics.Count; i++) {
                var relicId = Save.state.relics[i];
                // TODO: fix setup relics
                var relic = Database.instance.relicsDict[relicId];
                evaluation.AddScore(ScoreReason.RelicQuality, EvaluationFunctionReflection.GetRelicEvalFunctionCached(relic.id)(relic));
            }
            EvaluateGlobalRules(evaluation);
            ScorePath(evaluation);
            ScoreFutureCardValue(evaluation);
        }
        public static void ScoreBasedOnStatistics(Evaluation evaluation) {
            var stats = evaluation.RewardStats ?? new RewardOutcomeStatistics();
            var winChance = stats.ChanceToWin(evaluation);
            evaluation.SetScore(ScoreReason.MeanCorrection, -(stats.chosenValue - stats.rewardOutcomeMean));
            evaluation.SetScore(ScoreReason.WinChance, winChance * 1f);
            evaluation.SetScore(ScoreReason.Variance, -stats.rewardOutcomeStd / 5f);
        }
        public static void ScoreBasedOnOffRamp(Evaluation evaluation) {
            var offRamp = evaluation.OffRamp?.Path ?? evaluation.Path;
            // this has the potential to provide "phantom" points, where you plan a really ambitious path, and then chicken out when the off-ramp disappears
            // but that's kinda the right way to play the game
            evaluation.SetScore(ScoreReason.ActSurvival, 10f * offRamp.chanceToSurviveAct);
        }
    }
}
