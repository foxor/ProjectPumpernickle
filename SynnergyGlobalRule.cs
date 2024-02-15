using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class SynnergyGlobalRule : IGlobalRule {

        void IGlobalRule.Apply(Evaluation evaluation) {
            var synnergies = new Dictionary<string, float>();
            var payoffs = new Dictionary<string, float>();
            foreach (var card in Save.state.cards) {
                foreach (var synnergy in card.setup) {
                    if (synnergies.TryGetValue(synnergy.Key, out var value)) {
                        synnergies[synnergy.Key] = value + synnergy.Value;
                    }
                    else {
                        synnergies[synnergy.Key] = synnergy.Value;
                    }
                }
                foreach (var synnergy in card.payoff) {
                    if (payoffs.TryGetValue(synnergy.Key, out var value)) {
                        payoffs[synnergy.Key] = value + synnergy.Value;
                    }
                    else {
                        payoffs[synnergy.Key] = synnergy.Value;
                    }
                }
            }
            foreach(var synnergy in payoffs) {
                var reason = Enum.Parse<ScoreReason>(synnergy.Key);
                if (synnergies.TryGetValue(synnergy.Key, out var value)) {
                    evaluation.SetScore(reason, synnergy.Value * value);
                }
            }
        }
    }
}
