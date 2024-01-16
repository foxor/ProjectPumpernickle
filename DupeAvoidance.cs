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

        void IGlobalRule.Apply(Evaluation evaluation) {
            var totalPunishment = 0f;
            var counts = new Dictionary<string, int>();
            foreach (var card in Save.state.cards.Where(x => x.cardRarity != Rarity.Basic)) {
                if (!counts.ContainsKey(card.id) && card.tags.TryGetValue(Tags.PickLimit.ToString(), out var limit)) {
                    counts[card.id] = (int)-limit;
                }
                counts[card.id] = counts.GetValueOrDefault(card.id, 0) + 1;
            }
            foreach (var dupe in counts) {
                if (dupe.Value > 1) {
                    totalPunishment += PUNISHMENT_PER_CARD * (dupe.Value - 1);
                }
            }
            evaluation.AddScore(ScoreReason.AvoidDuplicateCards, totalPunishment);
        }
    }
}
