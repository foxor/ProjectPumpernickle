using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class FightSimulator {
        public static readonly float DECAY_RATE = 2f;
        protected static float InductiveStrength() {
            // TODO: decay rate should depend on "important" things happening, like finishing a combo or getting a significant relic
            float strengthDelta = 0f;
            var currentFloor = Save.state.floor_num;
            foreach (var damageTaken in Save.state.metric_damage_taken) {
                var pctRunAgo = (currentFloor - damageTaken.floor) * 1f / currentFloor;
                var significance = MathF.Pow(1f - pctRunAgo, DECAY_RATE);
                var expectedDamage = Database.instance.encounterDict[damageTaken.enemies].medianExpectedHealthLoss;
                var damageDelta = damageTaken.damage - expectedDamage;
                strengthDelta += damageDelta * significance;
            }
            return strengthDelta;
        }
        public static void SimulateFight(string encounterId, out float expectedHealthLoss, out float worstCaseHealthLoss) {
            var medianExpectedDamage = Database.instance.encounterDict[encounterId].medianExpectedHealthLoss;
            var medianWorstCaseDamage = Database.instance.encounterDict[encounterId].medianWorstCaseHealthLoss;
            var expectedDamageDelta = InductiveStrength();
            var worstCaseMultiplier = medianWorstCaseDamage / medianExpectedDamage;
            expectedHealthLoss = MathF.Max(medianExpectedDamage + expectedDamageDelta, 0f);
            var worstCaseDamageDelta = Math.Max(expectedDamageDelta, expectedDamageDelta * worstCaseMultiplier);
            worstCaseHealthLoss = MathF.Max(medianWorstCaseDamage + worstCaseDamageDelta, 0f);
        }
    }
}
