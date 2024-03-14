using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class RelicPickFunctions {
        public static void EmptyCage(string parameters, RewardContext context) {
            var removals = parameters.Trim().Split(",").Select(x => int.Parse(x.Trim()));
            context.cardsRemoved = removals.Select(x => Save.state.cards[x]).ToList();
            context.removedCardIndicies = removals.ToList();
            foreach (var removal in removals) {
                context.description.Add("Remove " + Save.state.cards[removal].Descriptor());
                Save.state.cards.RemoveAt(removal);
            }
        }
        public static void Astrolabe(string parameters, RewardContext context) {
        }
        public static void BottledLightning(string parameters, RewardContext context) {
            var target = parameters.Trim().Split(",").Select(x => int.Parse(x.Trim())).Single();
            var selfIndex = Save.state.relics.FirstIndexOf(x => x.Equals("Bottled Lightning"));
            Save.state.relic_counters[selfIndex] = target;
        }
        public static void BottledTornado(string parameters, RewardContext context) {
            var target = parameters.Trim().Split(",").Select(x => int.Parse(x.Trim())).Single();
            var selfIndex = Save.state.relics.FirstIndexOf(x => x.Equals("Bottled Tornado"));
            Save.state.relic_counters[selfIndex] = target;
        }
        public static void BottledFlame(string parameters, RewardContext context) {
            var target = parameters.Trim().Split(",").Select(x => int.Parse(x.Trim())).Single();
            var selfIndex = Save.state.relics.FirstIndexOf(x => x.Equals("Bottled Flame"));
            Save.state.relic_counters[selfIndex] = target;
        }
    }
    internal class RelicOptionSplitFunctions {
        public static readonly string[] MultiOptionRelics = new string[] {
            "EmptyCage",
            "Astrolabe",
            "BottledLightning",
            "BottledTornado",
            "BottledFlame",
        };
        public static IEnumerable<RewardOptionPart> EmptyCage() {
            var reasonableRemoveTarget = Evaluators.ReasonableRemoveTargets(2).ToArray();
            for (int i = 0; i < reasonableRemoveTarget.Length; i++) {
                for (int j = i + 1; j < reasonableRemoveTarget.Length; j++) {
                    yield return new RewardOptionPart() {
                        value = "Empty Cage: " + j + ", " + i,
                    };
                }
            }
        }
        public static IEnumerable<RewardOptionPart> Astrolabe() {
            var reasonableRemoveTarget = Evaluators.ReasonableRemoveTargets(3).ToArray();
            for (int i = 0; i < reasonableRemoveTarget.Length; i++) {
                for (int j = i + 1; j < reasonableRemoveTarget.Length; j++) {
                    for (int k = j + 1; k < reasonableRemoveTarget.Length; k++) {
                        yield return new RewardOptionPart() {
                            value = "Astrolabe: " + j + ", " + i + ", " + k,
                        };
                    }
                }
            }
        }
        public static IEnumerable<RewardOptionPart> BottledLightning() {
            for (int i = 0; i < Save.state.cards.Count; i++) {
                if (Save.state.cards[i].cardType == CardType.Skill) {
                    yield return new RewardOptionPart() {
                        value = "Bottled Lightning: " + i,
                    };
                }
            }
        }
        public static IEnumerable<RewardOptionPart> BottledTornado() {
            for (int i = 0; i < Save.state.cards.Count; i++) {
                if (Save.state.cards[i].cardType == CardType.Power) {
                    yield return new RewardOptionPart() {
                        value = "Bottled Tornado: " + i,
                    };
                }
            }
        }
        public static IEnumerable<RewardOptionPart> BottledFlame() {
            for (int i = 0; i < Save.state.cards.Count; i++) {
                if (Save.state.cards[i].cardType == CardType.Attack) {
                    yield return new RewardOptionPart() {
                        value = "Bottled Flame: " + i,
                    };
                }
            }
        }
    }
}
