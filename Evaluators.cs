using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal static class Evaluators {
        internal static int RareRelicsAvailable() {
            var classRelics = 0;
            switch (Save.state.character) {
                case PlayerCharacter.Ironclad: {
                    classRelics = 3;
                    break;
                }
                case PlayerCharacter.Silent: {
                    classRelics = 3;
                    break;
                }
                case PlayerCharacter.Defect: {
                    classRelics = 1;
                    break;
                }
                case PlayerCharacter.Watcher: {
                    classRelics = 2;
                    break;
                }
            }
            return 25 + classRelics;
        }
        public static float ExpectedGoldFromRandomRelic() {
            return 1f / 6f * (1f / RareRelicsAvailable()) * 300f;
        }

        public static bool CardRemovesSkills(Card card) {
            return card.id switch {
                "Second Wind" => true,
                _ => false,
            };
        }
        public static float PermanentDeckSize() {
            var hasSkillRemover = Save.state.cards.Any(CardRemovesSkills);
            return Save.state.cards.Select(x => {
                if (x.cardType == CardType.Power) {
                    return 0f;
                }
                if (x.id == "Purity") {
                    return x.upgrades > 0 ? -5f : -3f;
                }
                if (x.tags.TryGetValue(Tags.NonPermanent.ToString(), out var value)) {
                    return 1 - value;
                }
                if (x.cardType == CardType.Skill && hasSkillRemover && !CardRemovesSkills(x)) {
                    return 0f;
                }
                return 1f;
            }).Sum();
        }

        public static float CostOfNonPermanent() {
            var hasSkillRemover = Save.state.cards.Any(CardRemovesSkills);
            return Save.state.cards.Select(x => {
                if (x.cardType == CardType.Power) {
                    return x.intCost;
                }
                if (x.id == "Purity") {
                    return -3f;
                }
                if (x.tags.TryGetValue(Tags.NonPermanent.ToString(), out var value)) {
                    if (x.tags.TryGetValue(Tags.ExhaustCost.ToString(), out var cost)) {
                        return cost;
                    }
                    return x.intCost;
                }
                if (hasSkillRemover && CardRemovesSkills(x)) {
                    return x.intCost * 2.5f;
                }
                return 0f;
            }).Sum();
        }

        public static float ExtraPerFightEnergy() {
            return 0f;
        }

        public static float PerTurnEnergy() {
            return 3f;
        }

        public static IEnumerable<Card> GetCardDrawCards() {
            return Save.state.cards.Where(x => x.tags.ContainsKey(Tags.CardDraw.ToString()) && (x.intCost != -1));
        }

        public static bool HasCalmEnter() {
            return Save.state.cards.Any(x => x.id == "InnerPeace" || x.id == "FearNoEvil");
        }

        public static int TurnOneEnergy() {
            return 3;
        }

        public static void CardRewardDamageStats(Path path, int nodeIndex, out float totalDamage, out float totalCost) {
            //path.expectedCardRewards[nodeIndex]
            totalCost = 0;
            totalDamage = 0;
        }

        public static float RandomPotionHealthValue() {
            return 10f;
        }

        public static float GetPotionHealthValue(string potionId, float expectedHealth) {
            var potionValue = RandomPotionHealthValue();
            switch (potionId) {
                case "Ancient Potion": {
                    potionValue = 0f;
                    break;
                }
                case "Fruit Juice": {
                    return 5f;
                }
            }
            var healthFactor = Lerp.Inverse(10f, 30f, expectedHealth);
            var healthEfficiencyMultiplier = Lerp.From(1f/3f, 1f, healthFactor);
            return potionValue * healthEfficiencyMultiplier;
        }

        public static float GetCurrentEffectiveHealth() {
            var literalHealth = Save.state.current_health;
            var effectiveHealth = literalHealth * 1f;
            if (Save.state.potions != null) {
                foreach (var potion in Save.state.potions) {
                    if (potion.Equals("Potion Slot")) {
                        continue;
                    }
                    effectiveHealth += GetPotionHealthValue(potion, literalHealth);
                }
            }
            return effectiveHealth;
        }

        public static float GetHealth(int floor) {
            var path = Evaluation.Active.Path;
            if (path == null) {
                return GetCurrentEffectiveHealth();
            }
            var index = path.FloorNumToPathIndex(floor);
            if (index == -1) {
                return GetCurrentEffectiveHealth();
            }
            return path.expectedHealth[index];
        }

        public static string ChooseBestUpgrade(out float bestValue, Path path = null, int index = -1) {
            var numPriorUpgrades = path == null ? 0 : (int)Math.Round(path.expectedUpgrades[index]);
            ChooseBestAndWorstUpgrade(numPriorUpgrades, out var bestId, out bestValue, out var worstId, out var worstValue);
            return bestId;
        }

        public static void ChooseBestAndWorstUpgrade(int numPriorUpgrades, out string bestId, out float bestValue, out string worstId, out float worstValue) {
            var unupgradedCards = Enumerable.Range(0, Save.state.cards.Count).Select(x => (Card: Save.state.cards[x], Index: x)).Where(x => x.Card.upgrades == 0 && x.Card.cardType != CardType.Curse).Select(x => {
                var currentValue = EvaluationFunctionReflection.GetCardEvalFunctionCached(x.Card.id)(x.Card, x.Index);
                var upgradeValue = EvaluationFunctionReflection.GetUpgradeFunctionCached(x.Card.id)(x.Card, x.Index, currentValue);
                return (Card: x.Card, Val: upgradeValue - currentValue);
            }).OrderByDescending(x => x.Val);
            if (numPriorUpgrades >= unupgradedCards.Count()) {
                bestId = null;
                bestValue = 0f;
                worstId = null;
                worstValue = 0f;
                return;
            }
            var anticipatedSelection = unupgradedCards.Skip(numPriorUpgrades).First();
            var worstSelection = unupgradedCards.LastOrDefault();
            bestId = anticipatedSelection.Card.id;
            bestValue = anticipatedSelection.Val;
            worstId = worstSelection.Card.id;
            worstValue = worstSelection.Val;
        }

        public static bool EligibleToSeeRelic(Relic r) {
            // TODO: have we seen this?
            return
                !Save.state.relics.Contains(r.id) &&
                r.rarity != Rarity.Basic &&
                r.rarity != Rarity.Special;
        }

        public static void ChooseBestRelic(out string bestRelic, out float bestValue, out float averageValue, out int count) {
            var hasEvaluation = Evaluation.Active != null;
            var relics = Database.instance.relics.Where(EligibleToSeeRelic).Select(x => {
                if (hasEvaluation) {
                    var value = EvaluationFunctionReflection.GetRelicEvalFunctionCached(x.id)(x);
                    return (Relic: x, Val: value);
                }
                else {
                    var value = x.bias;
                    return (Relic: x, Val: value);
                }
            }).OrderByDescending(x => x.Val);
            bestRelic = relics.First().Relic.id;
            bestValue = relics.First().Val;
            averageValue = relics.Average(x => x.Val);
            count = relics.Count();
        }

        public static int FloorToAct(int floorNum) {
            return floorNum <= 17 ? 1 : (floorNum <= 34 ? 2 : (floorNum <= 51 ? 3 : 4));
        }
        public static int ActToFirstFloor(int actNum) {
            return 1 + ((actNum - 1) * 17) + (actNum == 4 ? 1 : 0);
        }
        public static int FloorsIntoAct(int floorNum) {
            var act = FloorToAct(floorNum);
            var actStart = ActToFirstFloor(act);
            return floorNum - actStart;
        }
        public static int LastFloorThisAct(int actNum) {
            return actNum switch {
                4 => 55,
                _ => ActToFirstFloor(actNum + 1) - 1
            };
        }
        public static float PercentGameOver(int floorNum) {
            return floorNum / FightSimulator.FLOORS_IN_GAME;
        }

        public static readonly int BASE_RARE_CHANCE = 3;
        public static readonly int BASE_UNCOMMON_CHANCE = 37;

        public static void UpdateCardRarityChances(float[] cardsOfRarity, float cardBlizz, float remainingRewards) {
            // FIXME: this doesn't work very well.  It ends up with fewer cards of rarity than the total cards???
            float marginalProbability = MathF.Min(1f, remainingRewards - 1);
            var rareChance = (BASE_RARE_CHANCE + cardBlizz) / 100f;
            var uncommonChance = (BASE_UNCOMMON_CHANCE + cardBlizz) / 100f;
            if (rareChance + uncommonChance > 1f) {
                uncommonChance -= (rareChance + uncommonChance) - 1f;
            }
            var commonChance = 1f - (rareChance + uncommonChance);

            cardsOfRarity[0] += commonChance * marginalProbability;
            cardsOfRarity[1] += uncommonChance * marginalProbability;
            cardsOfRarity[2] += rareChance * marginalProbability;

            remainingRewards -= 1f;
            if (remainingRewards <= 0f) {
                return;
            }

            var commonBlizz = Math.Min(40, cardBlizz + 1);
            var initialBlizz = 5;
            // This should be a slight overestimate, since the average cardBlizz is very unlikely to be 40
            var averageBlizz = (commonBlizz * commonChance) + (cardBlizz * uncommonChance) + (initialBlizz * rareChance);
            UpdateCardRarityChances(cardsOfRarity, averageBlizz, remainingRewards);
        }

        public static float ChanceOfSpecificCard(Color color, Rarity rarity) {
            // This code doesn't handle duplicates correctly.  The abstraction used here is mostly incompatible with that feature, and the math gets hard
            var populationSize = Database.instance.cards.Where(x => x.cardColor == color && x.cardRarity == rarity).Count();
            var hitDensity = 1f / populationSize;
            return hitDensity;
        }

        public static float ChanceOfSpecificRelic(PlayerCharacter character, Rarity rarity) {
            var populationSize = Database.instance.relics.Where(x => x.forCharacter.Is(character) && x.rarity.Is(rarity)).Count();
            var hitDensity = 1f / populationSize;
            return hitDensity;
        }

        public static float ChanceOfSpecificCardInReward(Color color, Rarity rarity, float expectedCardRewards) {
            var cardBlizzRandomizer = Save.state.card_random_seed_randomizer;
            var cardsOfRarity = new float[] {
                0f,
                0f,
                0f,
            };
            // FIXME: you don't always get 3 cards
            UpdateCardRarityChances(cardsOfRarity, cardBlizzRandomizer, expectedCardRewards * 3f);

            var cardsOfThisRarity = 0f;
            switch (rarity) {
                case Rarity.Common: {
                    cardsOfThisRarity = cardsOfRarity[0];
                    break;
                }
                case Rarity.Uncommon: {
                    cardsOfThisRarity = cardsOfRarity[1];
                    break;
                }
                case Rarity.Rare: {
                    cardsOfThisRarity = cardsOfRarity[2];
                    break;
                }
                default: {
                    throw new System.NotImplementedException();
                }
            }

            var hitDensity = ChanceOfSpecificCard(color, rarity);
            return hitDensity * cardsOfThisRarity;
        }

        public static float ChanceOfAppearingInShop(Color color, Rarity rarity, CardType cardType) {
            switch (color) {
                case Color.Red:
                case Color.Green:
                case Color.Blue:
                case Color.Purple: {
                    var population = Database.instance.cards.Where(x => x.cardColor == color && x.cardType == cardType).Count();
                    if (cardType == CardType.Power) {
                        return 1f / population;
                    }
                    else {
                        return (1f / population) + (1f / (population - 1));
                    }
                }
                case Color.Colorless: {
                    var population = Database.instance.cards.Where(x => x.cardColor == color && x.cardRarity == rarity).Count();
                    return 1f / population;
                }
                default: {
                    throw new System.NotImplementedException();
                }
            }
        }

        public static float IsEnoughToBuyCard(float expectedGold, Rarity rarity) {
            var minCost = rarity switch {
                Rarity.Common => 50f,
                Rarity.Uncommon => 74f,
                Rarity.Rare => 149f,
            };
            var maxCost = rarity switch {
                Rarity.Common => 61f,
                Rarity.Uncommon => 91f,
                Rarity.Rare => 182f,
            };
            return Lerp.Inverse(minCost, maxCost, expectedGold);
        }

        public static bool ShouldRemoveStrikeBeforeDefend() {
            if (Save.state.act_num == 1 && Save.state.boss == "Hexaghost") {
                return false;
            }
            return true;
        }
        public static readonly float NOB_REMOVE_BIAS = 1f;
        public static readonly float GUARDIAN_REMOVE_BIAS = .3f;
        public static float ThreatRemoveBias(Card card) {
            foreach (var threat in Evaluation.Active.Path.Threats) {
                switch (threat.Key) {
                    case "Gremlin Nob": {
                        if (card.type.Equals("Skill")) {
                            return NOB_REMOVE_BIAS * threat.Value;
                        }
                        break;
                    }
                    case "The Guardian": {
                        if (card.type.Equals("Attack")) {
                            return GUARDIAN_REMOVE_BIAS * threat.Value;
                        }
                        break;
                    }
                }
            }
            return 0f;
        }
        public static int CardRemoveTarget(Color color = Color.Any) {
            var validIndicies = Enumerable.Range(0, Save.state.cards.Count)
                .Where(x => Save.state.cards[x].cardColor.Is(color) &&
                    !Save.state.cards[x].tags.ContainsKey(Tags.Unpurgeable.ToString()));
            var bestIndex = validIndicies.OrderBy(i => {
                var card = Save.state.cards[i];
                var value = EvaluationFunctionReflection.GetCardEvalFunctionCached(card.id)(card, i);
                value -= ThreatRemoveBias(card);
                return value;
            }).First();
            return bestIndex;
        }
        public static float ExpectedFightsInFutureActs() {
            var actFourFights = 2f;
            var normalActFights = 8.5f;
            return Save.state.act_num switch {
                1 => actFourFights + (normalActFights * 2f),
                2 => actFourFights + (normalActFights * 1f),
                3 => actFourFights + (normalActFights * 0f),
                4 => 0f,
                _ => throw new System.NotImplementedException()
            };
        }
        public static float ExpectedFightsAfter(Path path, int floorIndex) {
            float fights = 0f;
            for (int i = floorIndex + 1; i < path.nodes.Length; i++) {
                fights += path.nodes[i].nodeType switch {
                    NodeType.Fight => 1,
                    NodeType.Elite => 1,
                    NodeType.MegaElite => 1,
                    _ => 0,
                };
            }
            return fights + ExpectedFightsInFutureActs();
        }
        public static readonly float LOW_VALUE_UPGRADE = 0.5f;
        public static readonly float HIGH_VALUE_UPGRADE = 1.2f;
        public static readonly float CHANCE_OF_HIGH_VALUE_UPGRADE_PER_REWARD = .2f;
        public static float UpgradePowerMultiplier(Path path, int floorIndex) {
            var bestUpgrade = ChooseBestUpgrade(out var deltaValue, path, floorIndex);
            if (bestUpgrade == null) {
                return 1f;
            }
            // This view of deck power is intended to capture the positive things the deck can do, and how they can change
            // The points we get are for scoring, so we massage them a bit
            var deckPower = Enumerable.Range(0, Save.state.cards.Count).Select(x =>
                EvaluationFunctionReflection.GetCardEvalFunctionCached(Save.state.cards[x].id)(Save.state.cards[x], x)
            ).Where(x => x > 0).Sum() + 10f;
            var expectedCardRewards = path.expectedCardRewards[floorIndex];
            var upgradeHits = CHANCE_OF_HIGH_VALUE_UPGRADE_PER_REWARD * expectedCardRewards;
            var newCardUpgradeValue = Lerp.From(LOW_VALUE_UPGRADE, HIGH_VALUE_UPGRADE, upgradeHits / (upgradeHits + 1f));
            var availableValueFraction = deltaValue / deckPower;
            var newValueFraction = newCardUpgradeValue / deckPower;
            return 1f + MathF.Max(availableValueFraction, newValueFraction);
        }

        public static int NormalFutureActsLeft() {
            return Math.Max(0, 3 - Save.state.act_num);
        }

        public static float CurrentInfiniteQuality() {
            var abilityScore =
                (Save.state.infiniteDoesDamage ? .3f : 0f) +
                (Save.state.infiniteBlockPerCard > 2f ? .5f : 0f) +
                (Save.state.infiniteBlockPerCard > 0f ? .2f : 0f);
            var energyToClear = CostOfNonPermanent() - ExtraPerFightEnergy();
            var clearCostPenalty = .5f * (1f - (1f / (1f + (energyToClear / PerTurnEnergy() * 2f))));
            var speedPenalty = Save.state.earliestInfinite switch {
                1 => 0f,
                2 => 0.02f,
                3 => 0.1f,
                4 => 0.3f,
                _ => 0.5f,
            };
            var drawsAfterClear = Save.state.infiniteMaxSize - 5;
            var redrawPenalty = .2f * (1f - (1f / (1f + (drawsAfterClear / 8))));
            var clogPenalty = .3f * (1f - (1f / (1f + ((Save.state.cards.Count() - 12) / 15f))));
            var practicality = (1f - speedPenalty) *
                (1f - clearCostPenalty) *
                (1f - redrawPenalty) *
                (1f - clogPenalty);
            return abilityScore * practicality;
        }
        public static string BestCopyTarget() {
            return "Adaptation";
        }

        public static bool Is(this Color a, Color b) {
            if (a == Color.Any || b == Color.Any) {
                return true;
            }
            return a == b;
        }
        public static bool IsRandomable(this Rarity r) {
            return r switch {
                Rarity.Common => true,
                Rarity.Uncommon => true,
                Rarity.Rare => true,
                _ => false,
            };
        }
        public static bool Is(this Rarity a, Rarity b) {
            return (a == b) || (a == Rarity.Randomable && b.IsRandomable()) || (b == Rarity.Randomable && a.IsRandomable());
        }
        public static bool Is(this PlayerCharacter a, PlayerCharacter b) {
            return (a == b) || (a == PlayerCharacter.Any || b == PlayerCharacter.Any);
        }

        public static void BestAndWorstRewardResults(Color color, out Card bestCard, out float bestValue, out float worstValue, out float averageValue, Rarity rarity = Rarity.Randomable) {
            bestValue = float.MinValue;
            worstValue = float.MaxValue;
            bestCard = null;
            Card worstCard;
            float totalValue = 0f;
            int qualifyingCards = 0;
            foreach (var card in Database.instance.cards) {
                if (card.cardColor.Is(color) && card.cardRarity.Is(rarity)) {
                    qualifyingCards++;
                    var cardScore = EvaluationFunctionReflection.GetCardEvalFunctionCached(card.id)(card, -1);
                    totalValue += cardScore;
                    if (cardScore > bestValue) {
                        bestValue = cardScore;
                        bestCard = card;
                    }
                    if (cardScore < worstValue) {
                        worstValue = cardScore;
                        worstCard = card;
                    }
                }
            }
            averageValue = totalValue / qualifyingCards;
        }

        public static void RandomRelicValue(PlayerCharacter forCharacter, out Relic bestRelic, out float bestValue, out float worstValue, Rarity rarity = Rarity.Randomable) {
            bestValue = float.MinValue;
            worstValue = float.MaxValue;
            bestRelic = null;
            foreach (var relic in Database.instance.relics) {
                if (relic.forCharacter.Is(forCharacter) && relic.rarity.Is(rarity)) {
                    var cardScore = EvaluationFunctionReflection.GetRelicEvalFunctionCached(relic.id)(relic);
                    if (cardScore > bestValue) {
                        bestValue = cardScore;
                        bestRelic = relic;
                    }
                    if (cardScore < worstValue) {
                        worstValue = cardScore;
                    }
                }
            }
        }

        public static Color ToColor(this PlayerCharacter character) {
            switch (Save.state.character) {
                case PlayerCharacter.Ironclad: {
                    return Color.Red;
                }
                case PlayerCharacter.Silent: {
                    return Color.Green;
                }
                case PlayerCharacter.Defect: {
                    return Color.Blue;
                }
                case PlayerCharacter.Watcher: {
                    return Color.Purple;
                }
                default: {
                    throw new NotImplementedException();
                }
            }
        }

        public static float NormalElites(PlayerCharacter character, int act) {
            switch (character) {
                case PlayerCharacter.Ironclad: {
                    return act switch {
                        1 => 2.4f,
                        2 => 3f,
                        3 => 3f,
                        _ => -1f,
                    };
                }
                case PlayerCharacter.Silent: {
                    return act switch {
                        1 => 1.5f,
                        2 => 2.2f,
                        3 => 3.1f,
                        _ => -1f,
                    };
                }
                case PlayerCharacter.Defect: {
                    return act switch {
                        1 => 2.8f,
                        2 => 1.5f,
                        3 => 3f,
                        _ => -1f,
                    };
                }
                case PlayerCharacter.Watcher: {
                    return act switch {
                        1 => 2.5f,
                        2 => .5f,
                        3 => 3.5f,
                        _ => -1f,
                    };
                }
                default: {
                    throw new System.NotImplementedException();
                }
            }
        }
        public static float EstimateOverallPower() {
            var defensivePower = FightSimulator.EstimateDefensivePower();
            var damage = FightSimulator.EstimateDamagePerTurn();
            var normalDamage = FightSimulator.NormalDamageForFloor(Save.state.floor_num);
            var offensivePower = damage / normalDamage;
            var overallPower = defensivePower * offensivePower;
            return overallPower;
        }

        public static float DesiredElites(float overallPower, int forAct) {
            var normalElites = NormalElites(Save.state.character, forAct);
            // If your overallPower == 1f, you're right on track, so interpolation = 1
            var interpolation = Lerp.Inverse(.75f, 1.25f, overallPower) * 2f;
            var deltaElites = 1.5f * (interpolation - .5f) * 2f;
            return normalElites + deltaElites;
        }

        public static float DesiredFights(float overallPower) {
            var normalFights = 3.5f;
            // If your overallPower == 1f, you're right on track, so interpolation = 1
            var interpolation = Lerp.InverseUncapped(.75f, 1.25f, overallPower) * 2f;
            var deltaFights = 3f * (interpolation - .5f) * 2f;
            return MathF.Round(normalFights + deltaFights);
        }

        public static float DesiredShops(float estimatedBeginningGold) {
            return estimatedBeginningGold switch {
                > 450f => 2f,
                < 80f => 0f,
                _ => 1f,
            };
        }
        public static bool WantsEarlyShop(float estimatedBeginningGold) {
            return estimatedBeginningGold > 250f;
        }
        public static IEnumerable<string> GetEligibleEventNames(Path path, int i) {
            return Database.instance.events.Where(x => x.eligible).Select(x => x.name);
        }
        public static float AverageTransformValue(out string averageCardId) {
            var bestRemoveOfColor = CardRemoveTarget();
            return Save.state.GetTransformValue(Save.state.cards[bestRemoveOfColor].cardColor, out averageCardId, bestRemoveOfColor);
        }
        public static float AverageTransformValue(Color color, out string averageCardId, int removeIndex = -1) {
            averageCardId = "";
            if (removeIndex == -1) {
                removeIndex = CardRemoveTarget(color);
            }
            var removeTarget = Save.state.cards[removeIndex];
            Save.state.cards.RemoveAt(removeIndex);

            var validTransforms = Database.instance.cards.Where(x => x.cardColor == color).ToArray();
            var cardValues = new float[validTransforms.Count()];
            for (int i = 0; i < validTransforms.Length; i++) {
                var added = Save.state.AddCardById(validTransforms[i].id);
                cardValues[i] = EvaluationFunctionReflection.GetCardEvalFunctionCached(validTransforms[i].id)(Save.state.cards[added], added);
                Save.state.cards.RemoveAt(added);
            }
            var average = cardValues.Average();
            var bestDelta = float.MaxValue;
            for (int i = 0; i < validTransforms.Length; i++) {
                var localDelta = Math.Abs(cardValues[i] - average);
                if (localDelta < bestDelta) {
                    bestDelta = localDelta;
                    averageCardId = validTransforms[i].id;
                }
            }

            Save.state.cards.Insert(removeIndex, removeTarget);
            return average;
        }
        public static float EstimateFutureAddedCards() {
            var floorsLeft = 55f - Save.state.floor_num;
            return 17f * (floorsLeft / 55f);
        }
        // Roughly: https://www.wolframalpha.com/input?i=x%2F%28x%2B10%29+from+0+to+30
        public static float PercentAllGreen(Evaluation evaluation) {
            var projectedCardAdds = Evaluators.EstimateFutureAddedCards();
            var cardsByUpgradeWeight = Save.state.cards.Select(x => x.upgradePowerMultiplier).Concat(Enumerable.Range(0, (int)projectedCardAdds).Select(x => 1.3f)).OrderByDescending(x => x);
            var totalWeight = cardsByUpgradeWeight.Sum();
            var denominatorOffset = totalWeight / 2f;
            var endOfAct = evaluation.Path.nodes.Length - 1;
            var futureUpgrades = endOfAct >= 0 ? evaluation.Path.expectedUpgrades[endOfAct] : 0;
            var presentUpgrades = Save.state.cards.Select(x => x.upgrades).Sum();
            var fullUpgrades = futureUpgrades + presentUpgrades;
            var upgradeWeight = cardsByUpgradeWeight.Take((int)fullUpgrades).Sum();
            upgradeWeight += (fullUpgrades - (int)fullUpgrades) * cardsByUpgradeWeight.Skip((int)fullUpgrades).First();
            return upgradeWeight / (upgradeWeight + denominatorOffset);
        }
        public static float AverageCardsPerTurn() {
            return 5f;
        }
        public static readonly float LIQUID_MEMORIES_VALUE_PER_EXTRA_ENERGY = .3f;
        public static float ExtraCardValue(Card c, float value, int index) {
            if (c.bottled && c.tags.TryGetValue(Tags.BottleEquity.ToString(), out var bottleValue)) {
                value += bottleValue;
            }
            if (Save.state.potions?.Any(x => x.Equals("LiquidMemories")) == true && c.intCost != int.MaxValue && !c.tags.ContainsKey(Tags.NonPermanent.ToString())) {
                var extraCost = c.intCost - 1;
                value += LIQUID_MEMORIES_VALUE_PER_EXTRA_ENERGY * extraCost;
            }
            value += c.bias;
            if (c.upgrades > 0) {
                value = EvaluationFunctionReflection.GetUpgradeFunctionCached(c.id)(c, index, value);
            }
            return value;
        }
        public static float ExtraCardUpgradeValue(Card c, float value) {
            value *= c.upgradePowerMultiplier;
            value += c.upgradeBias;
            return value;
        }
        public static float EstimateUsesPerFight(Card c) {
            if (c.tags.ContainsKey(Tags.NonPermanent.ToString())) {
                return .8f;
            }
            return 2.2f;
        }
        public static float CardNeedFitFactor(Card c) {
            var deckWeakness = new Weakness();
            var cardAddressesWeakness = new Weakness(c);
            return deckWeakness * cardAddressesWeakness;
        }
        public static readonly float TARGET_WIN_RATE = .5f;
        public static readonly float ESTIMATE_ACCURACY_WINDOW = 15;
        // https://www.wolframalpha.com/input?i=sigmoid%28x*.3%29+from+0+to+30
        public static readonly float FUTURE_FLOOR_SIGMOID_FACTOR = 0.3f;
        public static float AdjustChanceOfDeath(float chance, int floorsInFuture, float defaultDeathChance) {
            var sigmoidFactor = floorsInFuture * FUTURE_FLOOR_SIGMOID_FACTOR;
            var t = PumpernickelMath.Sigmoid(sigmoidFactor);
            return Lerp.From(chance, defaultDeathChance, t);
        }
        public static float DefaultChanceOfDeath(int floorsLeft) {
            return 1f - MathF.Pow(TARGET_WIN_RATE, 1f / floorsLeft);
        }
        public static float EstimateChanceToWin(float[] ChanceOfDeath) {
            var defaultChanceOfDeath = DefaultChanceOfDeath(ChanceOfDeath.Length);
            var adjustedChances = Enumerable.Range(0, ChanceOfDeath.Length).Select(i => AdjustChanceOfDeath(ChanceOfDeath[i], i, defaultChanceOfDeath));
            return adjustedChances.Aggregate(1f, (s, d) => s * (1f - d));
        }
        public static int PoolBegins(NodeType pool, bool easyPool, int act) {
            var firstFloor = ActToFirstFloor(act);
            switch (pool) {
                case NodeType.Fight: {
                    return easyPool ? firstFloor : firstFloor + (act == 1 ? 4 : 3);
                }
                case NodeType.Elite:
                case NodeType.MegaElite: {
                    return firstFloor + 7;
                }
                default: {
                    throw new NotImplementedException();
                }
            }
        }
        public static readonly float MAX_OLD_POOL_BONUS = 1.2f;
        public static float PoolAge(NodeType pool, bool easyPool, int floor) {
            var poolStartFloor = PoolBegins(pool, easyPool, FloorToAct(floor));
            if (floor < poolStartFloor) {
                var gameFraction = (floor * 1f - poolStartFloor) / floor;
                return 1f + gameFraction;
            }
            return Lerp.From(1f, MAX_OLD_POOL_BONUS, (floor * 1f - poolStartFloor) / (floor + 3));
        }
        public static float MaxHealing() {
            return 0f;
        }
        public static void ReorderOptions(List<RewardOption> rewardOptions) {
            var cardIndex = rewardOptions.FirstIndexOf(x => x.values.Contains("Membership Card"));
            if (cardIndex != -1) {
                var cardOption = rewardOptions[cardIndex];
                rewardOptions.RemoveAt(cardIndex);
                rewardOptions.Insert(0, cardOption);
            }
        }
        public static bool ShouldConsiderSkippingGold() {
            return false;
        }
        public static bool ShouldConsiderSkippingRelic() {
            return false;
        }
        public static bool ShouldConsiderSkippingPotion() {
            return Save.state.EmptyPotionSlots() == 0;
        }
        public static void SkipUnpalatableOptions(List<RewardOption> rewardOptions) {
            if (!ShouldConsiderSkippingGold()) {
                foreach (var option in rewardOptions) {
                    if (option.rewardType == RewardType.Gold && option.cost == 0) {
                        option.skippable = false;
                    }
                }
            }
            if (!ShouldConsiderSkippingRelic()) {
                foreach (var option in rewardOptions) {
                    if (option.rewardType == RewardType.Relic && option.cost == 0) {
                        option.skippable = false;
                    }
                }
            }
            if (!ShouldConsiderSkippingPotion()) {
                foreach (var option in rewardOptions) {
                    if (option.rewardType == RewardType.Potion && option.cost == 0) {
                        option.skippable = false;
                    }
                }
            }
        }
        public static bool FireChoicesValid(FireChoice[] fireChoices) {
            if (Save.state.relics.Contains("Coffee Dripper") && fireChoices.Contains(FireChoice.Rest)) {
                return false;
            }
            return true;
        }
        public static float EstimatedHealingPerFight() {
            var found = 0f;
            if (Save.state.relics.Contains("Burning Blood")) {
                found += 6f;
            }
            return found;
        }
        public static int WingedBootsChargesLeft() {
            var wingedBootsIndex = Save.state.relics.IndexOf("WingedGreaves");
            if (wingedBootsIndex < 0) {
                return 0;
            }
            if (wingedBootsIndex >= Save.state.relic_counters.Count) {
                // You just got it
                return 3;
            }
            return Save.state.relic_counters[wingedBootsIndex];
        }
    }
}
