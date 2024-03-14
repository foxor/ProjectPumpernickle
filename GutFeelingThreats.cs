using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class GutFeelingThreats : IGlobalRule {
        public GlobalRuleEvaluationTiming Timing => GlobalRuleEvaluationTiming.PrePathExploration;

        public void Apply(Evaluation evaluation) {
            evaluation.Path.Threats = new Dictionary<string, float>() {
                { "Nemesis", .3f },
                { "Automaton", .5f },
                { "The Heart", .2f }
            };
        }
    }
}
