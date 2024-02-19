using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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

        public static int ExtraPerFightEnergy() {
            return 0;
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

        public static float GetPotionHealthValue(string potionId, float literalHealth) {
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
            var healthFactor = Lerp.Inverse(10f, 30f, literalHealth);
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
            var path = Evaluation.Active?.Path;
            if (path == null) {
                return GetCurrentEffectiveHealth();
            }
            var index = Path.FloorNumToPathIndex(floor);
            if (index == -1) {
                return GetCurrentEffectiveHealth();
            }
            return path.expectedHealth[index];
        }
        public static float ExpectedCardPickRate() {
            return 0.4f;
        }
        public static string ChooseBestUpgrade(out float bestValue, Path path = null, int index = -1) {
            var numPriorUpgrades = path == null ? 0 : (int)Math.Round(path.expectedUpgrades[index]);
            var numCardsAdded = path.expectedCardRewards[index] * ExpectedCardPickRate();
            ChooseBestAndWorstUpgrade(numPriorUpgrades, numCardsAdded, out var bestId, out bestValue, out var worstId, out var worstValue);
            return bestId;
        }

        public static void ChooseBestAndWorstUpgrade(int numPriorUpgrades, float numAddedCards, out string bestId, out float bestValue, out string worstId, out float worstValue) {
            var unupgradedCards = Enumerable.Range(0, Save.state.cards.Count).Select(x => (Card: Save.state.cards[x], Index: x)).Where(x => x.Card.upgrades == 0 && x.Card.cardType != CardType.Curse).Select(x => {
                var currentValue = EvaluationFunctionReflection.GetCardEvalFunctionCached(x.Card.id)(x.Card, x.Index);
                var upgradeValue = EvaluationFunctionReflection.GetUpgradeFunctionCached(x.Card.id)(x.Card, x.Index, currentValue);
                return (Card: x.Card, Val: upgradeValue - currentValue);
            }).OrderByDescending(x => x.Val);
            var remainingUpgrades = unupgradedCards.Skip(numPriorUpgrades);
            if (!remainingUpgrades.Any()) {
                bestId = null;
                worstId = null;
                var futureAddUpgrades = unupgradedCards.Count() - numPriorUpgrades + numAddedCards;
                if (futureAddUpgrades > 0f) {
                    bestValue = 1.5f * 1.3f;
                    worstValue = 0.8f * 1.2f;
                }
                else {
                    bestValue = 0f;
                    worstValue = 0f;
                }
                return;
            }
            var anticipatedSelection = remainingUpgrades.First();
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
        public static bool IsFirstFloorOfAnAct(int floorNum) {
            return FloorsIntoAct(floorNum) == 0;
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
            float marginalProbability = MathF.Min(1f, remainingRewards);
            var rareChance = MathF.Max(0f, (BASE_RARE_CHANCE - cardBlizz) / 100f);
            var uncommonChance = MathF.Max(0f, (BASE_UNCOMMON_CHANCE - cardBlizz) / 100f);
            if (rareChance + uncommonChance > 1f) {
                uncommonChance = 1f - rareChance;
            }
            var commonChance = 1f - (rareChance + uncommonChance);

            cardsOfRarity[0] += commonChance * marginalProbability;
            cardsOfRarity[1] += uncommonChance * marginalProbability;
            cardsOfRarity[2] += rareChance * marginalProbability;

            remainingRewards -= 1f;
            if (remainingRewards <= 0f) {
                return;
            }

            var commonBlizz = cardBlizz - 1;
            var initialBlizz = 5;
            var averageBlizz = (commonBlizz * commonChance) + (cardBlizz * uncommonChance) + (initialBlizz * rareChance);
            UpdateCardRarityChances(cardsOfRarity, averageBlizz, remainingRewards);
        }

        public static float ChanceOfSpecificCard(Color color, Rarity rarity) {
            // This code doesn't handle duplicates correctly.  The abstraction used here is mostly incompatible with that feature, and the math gets hard
            var populationSize = Database.instance.cards.Where(x => x.cardColor == color && x.cardRarity.Is(rarity)).Count();
            var hitDensity = 1f / populationSize;
            return hitDensity;
        }

        public static float ChanceOfSpecificRelic(PlayerCharacter character, Rarity rarity) {
            // FIXME: this shouldn't count relics you've seen
            var populationSize = Database.instance.relics.Where(x => x.forCharacter.Is(character) && x.rarity.Is(rarity)).Count();
            var hitDensity = 1f / populationSize;
            return hitDensity;
        }

        public static float[] ExpectedCardRewardAppearances(float expectedCardRewards, bool useCurrentRandomizer) {
            // AbstractDungeon static initializer is the source of the 5
            var cardBlizzRandomizer = useCurrentRandomizer ? Save.state.card_random_seed_randomizer : 5;
            var cardsOfRarity = new float[] {
                0f,
                0f,
                0f,
            };
            // FIXME: you don't always get 3 cards
            UpdateCardRarityChances(cardsOfRarity, cardBlizzRandomizer, expectedCardRewards * 3f);

            return cardsOfRarity;
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
        public static float FutureUpgradePowerMultiplier(Path path, int floorIndex) {
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
        public static float UpgradePowerGutFeeling(int cardIndex) {
            var card = Save.state.cards[cardIndex];
            var cardValue = card.bias;
            var upgradeValue = ExtraCardUpgradeValue(card, card.bias);
            var upgradePower = upgradeValue - cardValue;
            var deckPower = Lerp.From(10f, 60f, Save.state.floor_num / 55f);
            return 1f + (upgradePower / deckPower);
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
            if (a == Color.Eligible || b == Color.Eligible) {
                var other = a == Color.Eligible ? b : a;
                return Save.state.character.ToColor() == other || other == Color.Colorless;
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
            var scaling = FightSimulator.EstimatePastScalingPerTurn();
            var damage = FightSimulator.EstimateDamagePerTurn(scaling);
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
        public static readonly string[] AverageCardOptions = new string[] {
            "Metallicize",
            "Dagger Throw",
            "Bullet Time",
        };
        public static string AverageRandomCard(Color color, Rarity rarity) {
            return AverageCardOptions.Select(x => Database.instance.cardsDict[x]).Where(x => {
                return x.cardRarity.Is(rarity) && x.cardColor.Is(color);
            }).Select(x => x.id).First();
        }
        public static readonly string[] AverageRelicOptions = new string[] {
            "Orichalcum",
            "InkBottle",
            "Ginger",
            "Champion Belt",
        };
        public static string AverageRandomRelic(float[] foundRelicRarities, float[] shopRelicRarities) {
            return AverageRelicOptions.Select(x => Database.instance.relicsDict[x]).Where(x => {
                var rarityIndex = x.rarity switch {
                    Rarity.Common => 0,
                    Rarity.Uncommon => 1,
                    Rarity.Rare => 2,
                    Rarity.Shop => 3,
                    _ => -1
                };
                if (rarityIndex == -1) {
                    return false;
                }
                return foundRelicRarities[rarityIndex] > 0f || shopRelicRarities[rarityIndex] > 0f;
            }).Select(x => x.id).First();
        }
        public static float EstimateFutureAddedCards() {
            var floorsLeft = 55f - Save.state.floor_num;
            return 17f * (floorsLeft / 55f);
        }
        public static IEnumerable<float> ExpectedFutureUpgradePowerMultipliers(float projectedCardAdds) {
            var lastCard = (int)projectedCardAdds + 0.999f;
            for (int i = 0; i < lastCard; i++) {
                var t = i / (lastCard + 5f);
                yield return Lerp.From(1.3f, 2f, t);
            }
        }
        public static float PowerMultiplierToMarginalDeckPowerImpact(float powerMultiplier) {
            // Power multipliers are relative to the absence of a card
            // When you draw a card, it isn't ever no card, therefore we want to model
            // the increase in relative power gained by a specific multiplier verses
            // a replacement card.
            // x - 1 would be the power multiplier relative to the existing card, the marginal value
            // is a gut feeling based on the quality of a mid-game deck's replacement card to
            // the card being upgraded based on the upgrade multiplier above normal replacement rate.
            var bonus = MathF.Max(0f, powerMultiplier - 1.3f);
            var marginalGutFeelingPower = powerMultiplier - 1f;
            return marginalGutFeelingPower + (bonus * 2f);
        }
        // Roughly: https://www.wolframalpha.com/input?i=x%2F%28x%2B10%29+from+0+to+30
        public static float UpgradeValueProportion(Evaluation evaluation) {
            var projectedCardAdds = Evaluators.EstimateFutureAddedCards();
            var futureAddPowerMultipliers = ExpectedFutureUpgradePowerMultipliers(projectedCardAdds);
            var cardsByPower = Save.state.cards
                .Select(x => x.upgradePowerMultiplier)
                .Where(x => x > 1)
                .Concat(futureAddPowerMultipliers)
                .Select(PowerMultiplierToMarginalDeckPowerImpact)
                .OrderByDescending(x => x);
            var totalPower = cardsByPower.Sum();
            var endOfAct = evaluation.Path.nodes.Length - 1;
            var futureUpgrades = endOfAct >= 0 ? evaluation.Path.expectedUpgrades[endOfAct] : 0;
            var presentUpgrades = Save.state.cards.Select(x => x.upgrades).Sum();
            var fullUpgrades = futureUpgrades + presentUpgrades;
            var gainedPower = cardsByPower.Take((int)fullUpgrades).Sum();
            gainedPower += (fullUpgrades - (int)fullUpgrades) * cardsByPower.Skip((int)fullUpgrades).First();
            var missingPower = totalPower - gainedPower;
            var denominator = (totalPower * .3f) + (missingPower * 1.5f);
            Assert.Break(1309); // now good
            Assert.Break(6772); // now picked
            return gainedPower / denominator;
        }
        public static float AverageCardsPerTurn() {
            return 3.5f;
        }
        public static float AverageCardsPerFight() {
            return 15f;
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
            var effectiveValue = MathF.Max(value, 1.5f);
            value += effectiveValue * (c.upgradePowerMultiplier - 1f);
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
            if (Save.state.GetCurrentNode().nodeType == NodeType.Chest && !Save.state.has_sapphire_key) {
                return true;
            }
            return false;
        }
        public static bool ShouldConsiderSkippingPotion() {
            return Save.state.EmptyPotionSlots() == 0;
        }
        public static bool ShouldConsiderSkippingKey() {
            return false;
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
            if (!ShouldConsiderSkippingKey()) {
                foreach (var option in rewardOptions) {
                    if (option.rewardType == RewardType.Key && option.cost == 0) {
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
        public static bool ShouldConsiderRemovingNonCurse() {
            return !Save.state.cards.Any(x => x.cardType == CardType.Curse && !x.tags.ContainsKey(Tags.Unpurgeable.ToString()));
        }
        public static bool ShouldConsiderRemovingNonBasic() {
            return !Save.state.cards.Any(x => x.cardRarity == Rarity.Basic);
        }
        public static IEnumerable<int> ReasonableRemoveTargets(int maxCt = 1) {
            List<string> unupgradedRemoves = new List<string>();
            List<string> upgradedRemoves = new List<string>();
            var shouldConsiderNonCurse = ShouldConsiderRemovingNonCurse();
            var shouldConsiderNonBasic = ShouldConsiderRemovingNonBasic();
            for (int i = 0; i < Save.state.cards.Count; i++) {
                var card = Save.state.cards[i];
                if (card.cardRarity != Rarity.Basic && !shouldConsiderNonBasic) {
                    continue;
                }
                if (card.cardType == CardType.Curse && !shouldConsiderNonCurse) {
                    continue;
                }
                var relevantList = (card.upgrades == 0 ? unupgradedRemoves : upgradedRemoves);
                var considered = relevantList.Where(x => x.Equals(card.id)).Count();
                if (considered >= maxCt && card.id != "Searing Blow") {
                    continue;
                }
                relevantList.Add(card.id);
                yield return i;
            }
        }
        public static IEnumerable<int> ReasonableUpgradeTargets() {
            List<string> considered = new List<string>();
            for (int i = 0; i < Save.state.cards.Count; i++) {
                var card = Save.state.cards[i];
                if (card.cardType == CardType.Curse) {
                    continue;
                }
                if (card.upgrades > 0 && card.id != "Searing Blow") {
                    continue;
                }
                var hasConsidered = considered.Contains(card.id);
                if (hasConsidered && card.id != "Searing Blow") {
                    continue;
                }
                considered.Add(card.id);
                yield return i;
            }
        }
        public static int MaxCost(Card c) {
            if (Save.state.relics.Contains("Snecko Eye")) {
                return 3;
            }
            if (c.intCost == int.MaxValue) {
                return (int)(PerTurnEnergy() + 0.9999f);
            }
            return c.intCost;
        }
        public static int MinCost(Card c) {
            if (Save.state.relics.Contains("Snecko Eye")) {
                return 0;
            }
            if (c.intCost == int.MaxValue) {
                return 0;
            }
            return c.intCost;
        }
        public static IEnumerable<int> NecronomiconTargets() {
            return Enumerable.Range(0, Save.state.cards.Count)
                .Where(x => Save.state.cards[x].cardType == CardType.Attack && MaxCost(Save.state.cards[x]) >= 2);
        }
        public static float AverageCost(Card c) {
            if (Save.state.relics.Contains("Snecko Eye")) {
                return 1.5f;
            }
            else if (c.intCost != int.MaxValue) {
                return c.intCost;
            }
            else {
                return 3f;
            }
        }
        public static float DensityOfRarity(Color color, Rarity rarity) {
            // Does this matter?
            // If so, pre-compute it on library load
            return 1f/3f;
        }
        public static int PercentHealthHeal(float pct) {
            var missing = Save.state.max_health - Save.state.current_health;
            var attempted = PercentHealthDamage(pct);
            return Math.Min(missing, attempted);
        }
        public static int PercentHealthDamage(float pct) {
            var attempted = (int)(Save.state.max_health * pct + 0.9999);
            return attempted;
        }
    }
}
