using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class Evaluators {
        protected static int RareRelicsAvailable() {
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

        public static float GetPotionHealthValue(string potionId) {
            return 10f;
        }

        public static float GetEffectiveHealth() {
            var literalHealth = Save.state.current_health;
            var effectiveHealth = literalHealth * 1f;
            foreach (var potion in Save.state.potions) {
                if (potion.Equals("Potion Slot")) {
                    continue;
                }
                effectiveHealth += GetPotionHealthValue(potion);
            }
            return effectiveHealth;
        }

        public static string ChooseBestUpgrade(Path path, int index) {
            var numPriorUpgrades = path.fireChoices.Take(index).Where(x => x == FireChoice.Upgrade).Count();
            var unupgradedCards = Enumerable.Range(0, Save.state.cards.Count).Select(x => (Card: Save.state.cards[x], Index: x)).Where(x => x.Card.upgrades == 0).Select(x => {
                var upgradeValue = EvaluationFunctionReflection.GetUpgradeFunctionCached(x.Card.id)(x.Card, x.Index);
                return (Card: x.Card, Val: upgradeValue);
            }).OrderByDescending(x => x.Val);
            return unupgradedCards.Skip(numPriorUpgrades).First().Card.id;
        }

        public static int FloorToAct(int floorNum) {
            return floorNum <= 17 ? 1 : (floorNum <= 34 ? 2 : (floorNum <= 51 ? 3 : 4));
        }
    }
}
