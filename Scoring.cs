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
                globalRule.Apply(evaluation);
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
        public static float DeepEvaluationScoreDelta() {
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
            var cardQualityDelta = cardQuality - Evaluation.Active.InternalScores[(byte)ScoreReason.DeckQuality];
            var relicQualityDelta = relicQuality - Evaluation.Active.InternalScores[(byte)ScoreReason.RelicQuality];
            return cardQualityDelta + relicQualityDelta;
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
            var scoreDelta = DeepEvaluationScoreDelta();
            if (addedIndex != -1) {
                Save.state.cards.RemoveAt(addedIndex);
                pointDelta += scoreDelta;
            }
            else {
                Save.state.cards.Insert(removedIndex, removed);
                pointDelta -= scoreDelta;
            }
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
        public static readonly float LAST_CHANCE_TO_PICK_VALUE = 0.25f;
        public static IEnumerable<CardScore> CardScoreProvider(float[] cardRarityAppearances, float[] cardShopAppearances = null, Color colorLimit = Color.Eligible, Rarity rarityLimit = Rarity.Randomable) {
            var path = Evaluation.Active.Path;
            if (cardShopAppearances == null) {
                cardShopAppearances = new float[6];
            }

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
                if (Save.state.ChoosingNow(card.id) && expectedFound < 2f) {
                    var chanceToSee = expectedFound / 2f;
                    Evaluation.Active.SetScore(ScoreReason.LastChanceToPick, LAST_CHANCE_TO_PICK_VALUE * (1f - chanceToSee));
                }
                // This is slightly wrong because multiplies of cards generally aren't as good
                // This is why we don't include hypothetical points for cards that are available now
                yield return new CardScore(card.id, ScoreValueOfCard(card) * expectedFound);
            }
        }
        // Cards aren't good until you build a good deck
        // Relics are good immediately
        public static readonly float FUTURE_CARD_CURRENT_POINT_MIN = 0.25f;
        public static readonly float FUTURE_RELIC_CURRENT_POINT_MIN = 0.5f;
        protected static void ScoreFutureCardValue(Evaluation evaluation) {
            var path = evaluation.Path;
            var rewards = Enumerable.Range(1, 4).Select(actNum => {
                var index = Path.FloorNumToPathIndex(Evaluators.LastFloorThisAct(actNum));
                if (index < 0) {
                    return 0f;
                }
                return path.expectedCardRewards[index];
            }).ToArray();
            var cardRarityAppearances = Enumerable.Range(1, 4).Select(actNum => {
                var beginningOfAct = actNum == 1 ? 0 : rewards[actNum - 2];
                var endOfAct = rewards[actNum - 1];
                var isCurrentAct = actNum == Save.state.act_num;
                return Evaluators.ExpectedCardRewardAppearances(endOfAct - beginningOfAct, isCurrentAct);
            }).Sum();
            var cardShopAppearances = new float[6];
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
            var stats = new ChooseCardsStatisticsGroup(cardRarityAppearances, cardShopAppearances);
            var outcome = stats.Evaluate();
            var cardRewardExpectedTotalValue = outcome.rewardOutcomeMean;
            var currentValueMultiplier = Lerp.From(FUTURE_CARD_CURRENT_POINT_MIN, 1f, Save.state.floor_num / 55f);
            evaluation.SetScore(ScoreReason.CardReward, cardRewardExpectedTotalValue * currentValueMultiplier);
        }
        protected static float ScoreValueOfRelic(Relic relicAdded) {
            var pointDelta = 0f;
            Save.state.relics.Add(relicAdded.id);
            var addedIndex = Save.state.relics.Count - 1;
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
            Save.state.relics.RemoveAt(addedIndex);
            pointDelta += cardQuality - Evaluation.Active.InternalScores[(byte)ScoreReason.DeckQuality];
            pointDelta += relicQuality - Evaluation.Active.InternalScores[(byte)ScoreReason.RelicQuality];
            return pointDelta;
        }
        public struct RelicScore {
            public string relicId;
            public float score;
            public RelicScore(string cardId, float score) {
                this.relicId = cardId;
                this.score = score;
            }
        }
        public static IEnumerable<RelicScore> RelicScoreProvider(float[] relicsByRarity, Rarity rarityLimit = Rarity.Randomable) {
            var path = Evaluation.Active.Path;
            var character = Save.state.character;

            var hitDensity = new float[4];
            for (int i = 0; i < 4; i++) {
                var rarity = i switch {
                    0 => Rarity.Common,
                    1 => Rarity.Uncommon,
                    2 => Rarity.Rare,
                    3 => Rarity.Shop,
                };
                hitDensity[i] = Evaluators.ChanceOfSpecificRelic(Save.state.character, rarity);
            }

            foreach (var relic in Database.instance.relics) {
                var color = relic.forCharacter;
                var rarity = relic.rarity;
                if (!character.Is(color)) {
                    continue;
                }
                if (!rarityLimit.Is(rarity)) {
                    continue;
                }
                // FIXME: we should skip relics you've seen
                var rarityIndex = rarity switch {
                    Rarity.Common => 0,
                    Rarity.Uncommon => 1,
                    Rarity.Rare => 2,
                    Rarity.Shop => 3,
                };
                var chanceToFind = relicsByRarity[rarityIndex];
                var expectedFound = chanceToFind * hitDensity[rarityIndex];
                yield return new RelicScore(relic.id, ScoreValueOfRelic(relic) * expectedFound);
            }
        }
        public static float[] RelicRarityDistribution(float numRelics, bool shop) {
            if (!shop) {
                return new float[] { numRelics * .5f, numRelics * .32f, numRelics * .18f, 0f };
            }
            else {
                return new float[] { numRelics * .5f * 2f / 3f, numRelics * .32f * 2f / 3f, numRelics * .18f * 2f / 3f, numRelics * 1f / 3f };
            }
        }
        public static void ScoreFutureRelicValue(Evaluation evaluation) {
            var path = evaluation.Path;
            var floorsTillEndOfAct = Evaluators.LastFloorThisAct(Save.state.act_num) - Save.state.floor_num;
            var rewardRelics = path.expectedRewardRelics[floorsTillEndOfAct];
            var shopRelics = path.expectedShopRelics[floorsTillEndOfAct];
            var rewardRelicsByRarity = RelicRarityDistribution(rewardRelics, shop: false);
            var shopRelicsByRarity = RelicRarityDistribution(shopRelics, shop: true);
            if (rewardRelicsByRarity.Sum() + shopRelicsByRarity.Sum() > 0) {
                var stats = new AddRelicsStatisticsGroup(rewardRelicsByRarity, shopRelicsByRarity);
                var outcome = stats.Evaluate();
                var relicTotalEV = outcome.rewardOutcomeMean;
                var currentValueMultiplier = Lerp.From(FUTURE_RELIC_CURRENT_POINT_MIN, 1f, Save.state.floor_num / 55f);
                evaluation.SetScore(ScoreReason.FutureRelics, relicTotalEV * currentValueMultiplier);
            }
        }
        public static void ScorePath(Evaluation evaluation) {
            // This doesn't ever award points for future acts to avoid perverse incentives
            var path = evaluation.Path;
            var floorsTillEndOfAct = Evaluators.LastFloorThisAct(Save.state.act_num) - Save.state.floor_num;

            evaluation.SetScore(ScoreReason.Upgrades, 40f * Evaluators.UpgradeValueProportion(evaluation));

            evaluation.SetScore(ScoreReason.Key, Save.state.has_sapphire_key ? .5f : 0);

            var effectiveHealth = Evaluators.GetCurrentEffectiveHealth();
            evaluation.SetScore(ScoreReason.CurrentEffectiveHealth, effectiveHealth / 30f);

            // This does incorporate future act points, because otherwise we might misbehave
            var goldBrought = path.ExpectedGoldBroughtToShops();
            evaluation.SetScore(ScoreReason.BringGoldToShop, goldBrought.Select(PointsForShop).Sum());

            var wingedBootChargesLeft = Math.Max(Evaluators.WingedBootsChargesLeft(), 0) - path.jumps;
            var actsLeft = Math.Max(3 - Save.state.act_num, 0);
            evaluation.SetScore(ScoreReason.WingedBootsCharges, wingedBootChargesLeft * actsLeft * 0.03f);
            evaluation.SetScore(ScoreReason.WingedBootsFlexibility, (wingedBootChargesLeft * actsLeft >= 1) ? 1.5f : 0f);

            path.AddEventScore(evaluation);

            if (Save.state.act_num < 3 && (Save.state.has_emerald_key || path.hasMegaElite)) {
                evaluation.SetScore(ScoreReason.EarlyMegaElite, .2f);
            }

            if (Save.state.act_num == 3 && !Save.state.has_emerald_key && path.hasMegaElite != true) {
                evaluation.SetScore(ScoreReason.MissingKey, float.MinValue);
            }
            if (Save.state.act_num == 3 && !Save.state.has_sapphire_key && !path.ContainsGuaranteedChest()) {
                evaluation.SetScore(ScoreReason.MissingKey, float.MinValue);
            }

            if (Save.state.badBottle) {
                evaluation.SetScore(ScoreReason.BadBottle, -5f);
            }
        }
        public static void ScoreBasedOnEvaluation(Evaluation evaluation) {
            // Order is important here
            // card and relic evaluations can depend on global rules
            // future card and relic evaluations depend on current evaluations heavily
            EvaluateGlobalRules(evaluation);
            ScorePath(evaluation);
            var totalCardScore = 0f;
            for (int i = 0; i < Save.state.cards.Count; i++) {
                var card = Save.state.cards[i];
                var cardValue = EvaluationFunctionReflection.GetCardEvalFunctionCached(card.id)(card, i);
                card.evaluatedScore = cardValue;
                totalCardScore += cardValue;
            }
            evaluation.SetScore(ScoreReason.DeckQuality, totalCardScore);
            var totalRelicScore = 0f;
            for (int i = 0; i < Save.state.relics.Count; i++) {
                var relicId = Save.state.relics[i];
                // TODO: fix setup relics
                var relic = Database.instance.relicsDict[relicId];
                var relicScore = EvaluationFunctionReflection.GetRelicEvalFunctionCached(relic.id)(relic);
                totalRelicScore += relicScore;
            }
            evaluation.SetScore(ScoreReason.RelicQuality, totalRelicScore);
            ScoreFutureCardValue(evaluation);
            ScoreFutureRelicValue(evaluation);
        }
        public static void ScoreBasedOnStatistics(Evaluation evaluation) {
            var stats = evaluation.RewardStats ?? new RewardOutcomeStatistics();
            var winChance = stats.ChanceToWin(evaluation);
            evaluation.SetScore(ScoreReason.MeanCorrection, -(stats.chosenValue - stats.rewardOutcomeMean));
            evaluation.SetScore(ScoreReason.WinChance, winChance * 1f);
            evaluation.SetScore(ScoreReason.Variance, -stats.rewardOutcomeStd / 5f);
        }
        public static readonly float LN101 = 4.61512051684126f;
        public static void ScoreBasedOnOffRamp(Evaluation evaluation) {
            var offRamp = evaluation.OffRamp?.Path ?? evaluation.Path;
            // this has the potential to provide "phantom" points, where you plan a really ambitious path, and then chicken out when the off-ramp disappears
            // but that's kinda the right way to play the game

            // If you have a low chance to survive, we want to incentivise marginal survival chance highly
            // https://www.wolframalpha.com/input?i=10+-+110x%5E2+from+0+to+1
            // rewards [10, -100]
            var deathChance = 1f - offRamp.chanceToSurviveAct;
            var survivalScore = 10f - 110f * MathF.Pow(deathChance, 2f);
            evaluation.SetScore(ScoreReason.ActSurvival, survivalScore);
        }
    }
}
