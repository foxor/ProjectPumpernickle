using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class PunishSkillsIntoNob : IGlobalRule {
        public GlobalRuleEvaluationTiming Timing => GlobalRuleEvaluationTiming.PreCardEvaluation;

        public static readonly float FULL_PUNISHMENT = -5f;
        void IGlobalRule.Apply(Evaluation evaluation) {
            if (evaluation.Path.Threats.TryGetValue("Gremlin Nob", out float nobThreat)) {
                foreach (var newCard in Save.state.CardsJustChosen().Where(x => x.cardType == CardType.Skill)) {
                    evaluation.SetScore(ScoreReason.SkillIntoNob, FULL_PUNISHMENT * nobThreat);
                }
            }
        }
    }
}
