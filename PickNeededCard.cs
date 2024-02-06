using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class PickNeededCard : IGlobalRule {
        public static readonly float NEED_BIAS = 0.3f;
        bool IGlobalRule.ShouldApply => Save.state.cards.Any(x => x.isNew);

        void IGlobalRule.Apply(Evaluation evaluation) {
            foreach (var card in Save.state.cards.Where(x => x.isNew)) {
                var needFactor = Evaluators.CardNeedFitFactor(card);
                evaluation.AddScore(ScoreReason.PickedNeededCard, needFactor * NEED_BIAS);
            }
        }
    }
}
