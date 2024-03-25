using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class DecreasingLethality : IGlobalRule {
        public static readonly float PUNISH_PER_LOST_LETHAL = .05f;
        public static readonly float AVERAGE_EARLY_ENEMY_HEALTH = 20f;
        public static readonly float AVERAGE_LATE_ENEMY_HEALTH = 300f;
        public static readonly float LOG_EARLY_HEALTH = MathF.Log(AVERAGE_EARLY_ENEMY_HEALTH);
        public static readonly float LOG_LATE_HEALTH = MathF.Log(AVERAGE_LATE_ENEMY_HEALTH);
        public GlobalRuleEvaluationTiming Timing => GlobalRuleEvaluationTiming.PreCardEvaluation;

        public void Apply(Evaluation evaluation) {
            var totalPunishment = 0f;
            var gameCompletionFraction = Evaluators.PercentGameOver(Save.state.floor_num);
            var averageCurrentHealthPower = Lerp.From(LOG_EARLY_HEALTH, LOG_LATE_HEALTH, gameCompletionFraction);
            var averageCurrentHealth = MathF.Exp(averageCurrentHealthPower);
            foreach (var card in Save.state.cards) {
                if (card.tags.TryGetValue(Tags.Damage.ToString(), out var damage)) {
                    var initialLethalityRate = AVERAGE_EARLY_ENEMY_HEALTH / damage;
                    var currentLethalityRate = averageCurrentHealth / damage;
                    var lostLethality = initialLethalityRate - currentLethalityRate;
                    totalPunishment += lostLethality * PUNISH_PER_LOST_LETHAL;
                }
            }
            evaluation.SetScore(ScoreReason.DecreasingLethality, totalPunishment);
        }
    }
}
