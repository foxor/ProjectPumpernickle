using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class PathOptions : IGlobalRule {
        public static readonly float POINTS_PER_OPTION = 0.001f;
        public void Apply(Evaluation evaluation) {
            var optionCount = evaluation.Path.nodes.Select(x => x.children.Count).Sum();
            evaluation.SetScore(ScoreReason.PathOptions, optionCount * POINTS_PER_OPTION);
        }
    }
}
