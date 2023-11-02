using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class DupeAvoidance : IGlobalRule {
        public static readonly float PUNISHMENT_PER_CARD = -2f;
        public static readonly float PARTIAL_PUNISHMENT_PER_CARD = -1f;
        bool IGlobalRule.ShouldApply => true;

        protected bool ShouldAvoidPunishment(string cardId) => false;
        protected bool PartialPunishment(string cardId) => false;

        float IGlobalRule.Apply(Path path) {
            var totalPunishment = 0f;
            var counts = new Dictionary<string, int>();
            foreach (var cardId in Save.state.cards.Select(x => x.id)) {
                counts[cardId] = counts.GetValueOrDefault(cardId, 0) + 1;
            }
            foreach (var dupe in counts) {
                if (ShouldAvoidPunishment(dupe.Key)) {
                    continue;
                }
                else if (PartialPunishment(dupe.Key)) {
                    totalPunishment += PARTIAL_PUNISHMENT_PER_CARD * (dupe.Value - 1);
                }
                else {
                    totalPunishment += PUNISHMENT_PER_CARD * (dupe.Value - 1);
                }
            }
            return totalPunishment;
        }
    }
}
