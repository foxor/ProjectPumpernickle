using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class SynergyBonuses : IGlobalRule {
        public bool ShouldApply => true;

        public void Apply(Evaluation evaluation) {
            var totalSetup = new Dictionary<string, float>();
            var totalPayoff = new Dictionary<string, float>();
            foreach (var card in Save.state.cards) {
                foreach (var setup in card.setup) {
                    if (totalSetup.TryGetValue(setup.Key, out var val)) {
                        totalSetup[setup.Key] = val + setup.Value;
                    }
                    else {
                        totalSetup[setup.Key] = setup.Value;
                    }
                }
                foreach (var payoff in card.payoff) {
                    if (totalPayoff.TryGetValue(payoff.Key, out var val)) {
                        totalPayoff[payoff.Key] = val + payoff.Value;
                    }
                    else {
                        totalPayoff[payoff.Key] = payoff.Value;
                    }
                }
            }
            foreach (var payoff in totalPayoff) {
                totalSetup.TryGetValue(payoff.Key, out var setup);
                var value = payoff.Value * setup;
                var rewardReason = Enum.Parse<ScoreReason>(payoff.Key);
                evaluation.AddScore(rewardReason, value);
            }
        }
    }
}
