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
                context.description.Add("Remove " + Save.state.cards[removal].name);
                Save.state.cards.RemoveAt(removal);
            }
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
            var reasonableRemoveTarget = Evaluators.ReasonableRemoveTargets().ToArray();
            for (int i = 0; i < reasonableRemoveTarget.Length; i++) {
                for (int j = i + 1; j < reasonableRemoveTarget.Length; j++) {
                    yield return new RewardOptionPart() {
                        value = "Empty Cage: " + j + ", " + i,
                    };
                }
            }
        }
    }
}
