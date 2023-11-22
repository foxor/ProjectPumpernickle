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

        public static string ChooseBestUpgrade(Path path = null, int index = -1) {
            var numPriorUpgrades = path == null ? 0 : (int)path.expectedUpgrades.Take(index).Sum();
            var unupgradedCards = Enumerable.Range(0, Save.state.cards.Count).Select(x => (Card: Save.state.cards[x], Index: x)).Where(x => x.Card.upgrades == 0).Select(x => {
                var upgradeValue = EvaluationFunctionReflection.GetUpgradeHealthPerFightFunctionCached(x.Card.id)(x.Card, x.Index);
                return (Card: x.Card, Val: upgradeValue);
            }).OrderByDescending(x => x.Val);
            if (numPriorUpgrades >= unupgradedCards.Count()) {
                return null;
            }
            return unupgradedCards.Skip(numPriorUpgrades).First().Card.id;
        }

        public static int FloorToAct(int floorNum) {
            return floorNum <= 17 ? 1 : (floorNum <= 34 ? 2 : (floorNum <= 51 ? 3 : 4));
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

        public static float UpgradeValue(Path path, int floorIndex) {
            var bestUpgrade = ChooseBestUpgrade(path, floorIndex);
            if (bestUpgrade == null) {
                return 0f;
            }
            var bestHealthPerFight = Database.instance.cardsDict[bestUpgrade].upgradeHealthPerFight;
            var expectedFightsAfter = ExpectedFightsAfter(path, floorIndex);
            var numPriorUpgrades = path.expectedUpgrades.Take(floorIndex).Sum();
            var totalMissingUpgrades = Save.state.cards.Where(x => x.upgrades == 0).Count() - numPriorUpgrades;
            // healthPerFight * expectedFightsAfter would be the answer if we were to never get the selected upgrade
            // for the rest of the game.  In reality, the next upgrade we get would be this one, and for many of the
            // fights for the rest of the game, we'd be lacking the second best upgrade, and the upgrades would get worse
            // and worse.  The value of an upgrade is proportional to the area under the curve of missing upgrade
            // values for the rest of the game.
            var totalMissingHealth = 0f;
            foreach (var expectedUpgrades in path.ExpectedUpgradesDuringFights()) {
                if (expectedUpgrades < totalMissingUpgrades) {
                    var missingHealthPerFight = bestHealthPerFight * MathF.Pow(.8f, expectedUpgrades);
                    totalMissingHealth += missingHealthPerFight;
                }
            }
            return totalMissingHealth;
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
    }
}
