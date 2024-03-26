using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class PunishSpeculation : IGlobalRule {
        public GlobalRuleEvaluationTiming Timing => GlobalRuleEvaluationTiming.Late;

        public void Apply(Evaluation evaluation) {
            var speculationPunishmentRate = -(1f - Evaluators.SpeculationAppropriateness());
            var totalPunishment = 0f;
            foreach (var card in Save.state.cards) {
                if (card.isNew) {
                    if (card.tags.TryGetValue(Tags.Speculative.ToString(), out var speculative)) {
                        totalPunishment += speculative * speculationPunishmentRate;
                    }
                }
            }
            evaluation.SetScore(ScoreReason.Speculation, totalPunishment);
        }
    }
}
