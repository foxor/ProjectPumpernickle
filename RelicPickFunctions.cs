using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class RelicPickFunctions {
        public static void EmptyCage(RewardContext context) {
            var bestRemove = Evaluators.CardRemoveTarget();
            var removed = Save.state.cards[bestRemove];
            Save.state.cards.RemoveAt(bestRemove);
            var secondRemove = Evaluators.CardRemoveTarget();
            Save.state.cards.Insert(bestRemove, removed);
            if (bestRemove <= secondRemove) {
                secondRemove++;
            }
            var bestRemoveName = Save.state.cards[bestRemove].name;
            var secondBestRemoveName = Save.state.cards[secondRemove].name;
            if (bestRemoveName.Equals(secondBestRemoveName)) {
                context.description.Add("Remove 2 " + bestRemoveName + "s");
            }
            else {
                context.description.Add("Remove the " + bestRemove + " and the " + secondBestRemoveName);
            }
        }
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
