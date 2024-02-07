using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class PunishSkillIntoNob : IGlobalRule {
        public static readonly float FULL_PUNISHMENT = -30f;
        void IGlobalRule.Apply(Evaluation evaluation) {
            if (Save.state.addedSkill && evaluation.Path.Threats.TryGetValue("Gremlin Nob", out float nobThreat)) {
                evaluation.AddScore(ScoreReason.SkillIntoNob, FULL_PUNISHMENT * nobThreat);
            }
        }
    }
}
