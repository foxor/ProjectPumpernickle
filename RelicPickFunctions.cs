using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class RelicPickFunctions {
        public static void BottledLightning(RewardContext context) {
            Card bestBottle = null;
            float bestValue = float.MinValue;
            foreach (var card in Save.state.cards) {
                if (card.tags.TryGetValue(Tags.BottleEquity.ToString(), out var tagValue)) {
                    if (tagValue > bestValue) {
                        bestValue = tagValue;
                        bestBottle = card;
                    }
                }
            }
            if (bestBottle == null) {
                bestBottle = Save.state.cards[0];
                Save.state.badBottle = true;
            }
            else {
                bestBottle.bottled = true;
                context.bottled = bestBottle;
            }
            context.description.Add("Bottle the " + bestBottle.name);
        }
    }
}
