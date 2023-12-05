using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal static class Evaluators {
        internal static int RareRelicsAvailable() {
            var classRelics = 0;
            switch (PumpernickelSaveState.instance.character) {
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

        public static int PermanentDeckSize() {
            return Save.state.cards.Select(x => {
                if (x.cardType == CardType.Power) {
                    return 0;
                }
                if (x.id == "Purity") {
                    return x.upgrades > 0 ? -5 : -3;
                }
                if (x.tags?.ContainsKey(Tags.NonPermanent.ToString()) == true) {
                    return 0;
                }
                return 1;
            }).Sum();
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

        public static float GetPotionHealthValue(string potionId, float expectedHealth) {
            var potionValue = 10f;
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

        public static float GetEffectiveHealth() {
            var literalHealth = Save.state.current_health;
            var effectiveHealth = literalHealth * 1f;
            foreach (var potion in Save.state.potions) {
                if (potion.Equals("Potion Slot")) {
                    continue;
                }
                effectiveHealth += GetPotionHealthValue(potion, literalHealth);
            }
            return effectiveHealth;
        }

        public static string ChooseBestUpgrade(out float bestPowerMultiplier, Path path = null, int index = -1) {
            var numPriorUpgrades = path == null ? 0 : (int)path.expectedUpgrades.Take(index).Sum();
            var unupgradedCards = Enumerable.Range(0, Save.state.cards.Count).Select(x => (Card: Save.state.cards[x], Index: x)).Where(x => x.Card.upgrades == 0).Select(x => {
                var upgradeValue = EvaluationFunctionReflection.GetUpgradePowerMultiplierFunctionCached(x.Card.id)(x.Card, x.Index);
                return (Card: x.Card, Val: upgradeValue);
            }).OrderByDescending(x => x.Val);
            if (numPriorUpgrades >= unupgradedCards.Count()) {
                bestPowerMultiplier = 0f;
                return null;
            }
            var anticipatedSelection = unupgradedCards.Skip(numPriorUpgrades).First();
            bestPowerMultiplier = anticipatedSelection.Val;
            return anticipatedSelection.Card.id;
        }

        public static int FloorToAct(int floorNum) {
            return floorNum <= 17 ? 1 : (floorNum <= 34 ? 2 : (floorNum <= 51 ? 3 : 4));
        }
        public static int ActToFirstFloor(int actNum) {
            return actNum switch {
                1 => 0,
                2 => 18,
                3 => 35,
                4 => 52
            };
        }
        public static int FloorsIntoAct(int floorNum) {
            var act = FloorToAct(floorNum);
            var actStart = ActToFirstFloor(act);
            return floorNum - actStart;
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

        public static int CardRemoveTarget() {
            var basics = Save.state.cards.Where(x => x.cardRarity == Rarity.Basic && x.upgrades == 0);
            if (basics.Any()) {
                var toRemove = ShouldRemoveStrikeBeforeDefend() ? "Strike" : "Defend";
                var firstRemoves = basics.Where(x => x.name.Equals(toRemove));
                if (firstRemoves.Any()) {
                    return Save.state.cards.IndexOf(firstRemoves.First());
                }
                else {
                    return Save.state.cards.IndexOf(basics.First());
                }
            }
            var upgradedBasics = Save.state.cards.Where(x => x.cardRarity == Rarity.Basic);
            if (upgradedBasics.Any()) {
                var toRemove = ShouldRemoveStrikeBeforeDefend() ? "Strike" : "Defend";
                var firstRemoves = upgradedBasics.Where(x => x.name.Equals(toRemove));
                if (firstRemoves.Any()) {
                    return Save.state.cards.IndexOf(firstRemoves.First());
                }
                else {
                    return Save.state.cards.IndexOf(upgradedBasics.First());
                }
            }
            throw new System.NotImplementedException("Card remove too complex");
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

        public static float UpgradeHealthSaved(Path path, int floorIndex) {
            var bestUpgrade = ChooseBestUpgrade(out var powerMultiplier, path, floorIndex);
            if (bestUpgrade == null) {
                return 0f;
            }
            var cardCount = Save.state.cards.Count;
            var deckPowerMultiplierForCard = (cardCount - 1f + powerMultiplier) / cardCount;
            var upgradePath = Path.Copy(path);
            upgradePath.SimulateHealthEvolution(deckPowerMultiplierForCard, floorIndex);
            var totalHealthSaved = 0f;
            for (int i = 1; i < upgradePath.expectedHealth.Length; i++) {
                var loss = path.expectedHealth[i - 1] - path.expectedHealth[i];
                var lossWithUpgrade = upgradePath.expectedHealth[i - 1] - upgradePath.expectedHealth[i];
                if (loss > 0f) {
                    totalHealthSaved += (loss - lossWithUpgrade);
                }
            }
            return totalHealthSaved;
        }

        public static float LiftValue(Path path, int i) {
            if (!Save.state.relics.Contains("Girya")) {
                return float.MinValue;
            }
            // FIXME: this math needs to be better, it's health not points
            return 3.5f;
        }

        public static float RestValue(Path path, int i) {
            var healthGained = MathF.Min(Save.state.max_health * .3f, Save.state.max_health - path.expectedHealth[i]);
            //if (chance of gain health elsewhere) {
            //    healthGained *= 1 - chance rest unecessary;
            //}
            return healthGained;
        }

        public static int NormalFutureActsLeft() {
            return Math.Max(0, 3 - Save.state.act_num);
        }

        public static float InfiniteQuality() {
            return MathF.Min(1f, MathF.Max(0f,
                (Save.state.infiniteDoesDamage ? .6f : 0f) +
                (Save.state.infiniteBlocks ? .2f : 0f) +
                (Save.state.infiniteBlockPerCard > 2f ? .5f : 0f) +
                (Save.state.infiniteDrawPositive ? .1f : 0f) +
                (Save.state.infiniteEnergyPositive ? .1f : 0f) +
                (Save.state.earliestInfinite * -.3f)
            ));
        }
        public static string BestCopyTarget() {
            return "Adaptation";
        }

        public static bool Is(this Color a, Color b) {
            return (a == b);
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

        public static void RandomCardValue(Color color, out Card bestCard, out float bestValue, out float worstValue, Rarity rarity = Rarity.Randomable) {
            bestValue = float.MinValue;
            worstValue = float.MaxValue;
            bestCard = null;
            foreach (var card in Database.instance.cards) {
                if (card.cardColor.Is(color) && card.cardRarity.Is(rarity)) {
                    var cardScore = EvaluationFunctionReflection.GetCardEvalFunctionCached(card.id)(card, -1);
                    if (cardScore > bestValue) {
                        bestValue = cardScore;
                        bestCard = card;
                    }
                    if (cardScore < worstValue) {
                        worstValue = cardScore;
                    }
                }
            }
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
            return normalFights + deltaFights;
        }

        public static float DesiredShops(float estimatedBeginningGold) {
            return estimatedBeginningGold switch {
                > 380f => 2f,
                < 80f => 0f,
                _ => 1f,
            };
        }

        public static bool WantsEarlyShop(float estimatedBeginningGold) {
            return estimatedBeginningGold > 250f;
        }


        public static float ExpectedCardRemovesAvailable(Path path) {
            // TODO
            return 5f;
        }

        public static float CardRemovePoints(Path path) {
            if (Save.state.character == PlayerCharacter.Watcher) {
                var anticipatedEndGameDeckSize = Evaluators.PermanentDeckSize() - ExpectedCardRemovesAvailable(path) + (Evaluators.HasCalmEnter() ? 0 : 1);
                if (anticipatedEndGameDeckSize > 8) {
                    return .2f;
                }
                // Allocate 20 pumpernickel points to getting 5 removes on watcher going for infinite
                return path.ExpectedPossibleCardRemoves() / 5f * 20f;
            }
            // TODO
            return .2f;
        }

        public static void DamageStatsPerCardReward(int byTurn, float cardsInDeck, out float damage, out float cost) {
            // Assumes unupgraded
            switch (Save.state.character) {
                case PlayerCharacter.Watcher: {
                    break;
                }
                default: {
                    throw new NotImplementedException();
                }
            }
            damage = 0;
            cost = 0;
        }
    }
}
