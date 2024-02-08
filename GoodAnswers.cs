using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class GoodAnswers : IGlobalRule {
        public void Apply(Evaluation evaluation) {
            var goodAgainstTotal = new Dictionary<string, float>();
            foreach (var card in Save.state.cards) {
                foreach (var goodEntry in card.goodAgainst) {
                    goodAgainstTotal[goodEntry.Key] = goodEntry.Value + goodAgainstTotal.GetValueOrDefault(goodEntry.Key, 0f);
                }
            }
            var total = 0f;
            foreach (var threat in evaluation.Path.Threats) {
                goodAgainstTotal.TryGetValue(threat.Key, out var goodAgainst);
                total += goodAgainst * threat.Value;
            }
            evaluation.SetScore(ScoreReason.GoodAnswers, total);
        }
    }
}
