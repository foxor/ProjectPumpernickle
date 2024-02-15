using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class PunishSkillsIntoNob : IGlobalRule {
        public static readonly float FULL_PUNISHMENT = -20f;
        public static readonly float PCT_SKILLS_NO_PUNISH = 0.3f;
        void IGlobalRule.Apply(Evaluation evaluation) {
            if (evaluation.Path.Threats.TryGetValue("Gremlin Nob", out float nobThreat)) {
                var pctSkills = Save.state.cards.Where(x => x.cardType == CardType.Skill).Count() * 1f / Save.state.cards.Count();
                var punishFraction = MathF.Max(0f, (pctSkills - PCT_SKILLS_NO_PUNISH) / (1f - PCT_SKILLS_NO_PUNISH));
                evaluation.SetScore(ScoreReason.SkillIntoNob, FULL_PUNISHMENT * nobThreat * punishFraction);
            }
        }
    }
}
