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
        public static readonly float ACT_STRETCH_FLOORS = 8f;
        protected static float InductiveStrength() {
            // TODO: decay rate should depend on "important" things happening, like finishing a combo or getting a significant relic
            float strengthDelta = 0f;
            var currentFloor = Save.state.floor_num;
            foreach (var damageTaken in Save.state.metric_damage_taken) {
                float stretchFloors = 0f;
                var actsApart = Save.state.act_num - Evaluators.FloorToAct((int)MathF.Round(damageTaken.floor));
                stretchFloors += actsApart * ACT_STRETCH_FLOORS;

                var effectiveFloor = currentFloor + stretchFloors;
                var pctRunAgo = (effectiveFloor - damageTaken.floor) * 1f / effectiveFloor;
                var significance = MathF.Pow(1f - pctRunAgo, DECAY_RATE);
                var expectedDamage = Database.instance.encounterDict[damageTaken.enemies].medianExpectedHealthLoss;
                // TODO: If we used a potion here, model that as though we took more "metagame health" damage
                var damageDelta = damageTaken.damage - expectedDamage;
                strengthDelta += damageDelta * significance;
            }
            return strengthDelta;
        }
        // I don't like this
        public static void SimulateFight(Encounter encounter, out float expectedHealthLoss, out float worstCaseHealthLoss) {
            var medianExpectedDamage = encounter.medianExpectedHealthLoss;
            var medianWorstCaseDamage = encounter.medianWorstCaseHealthLoss;
            var expectedDamageDelta = InductiveStrength();
            var worstCaseMultiplier = medianWorstCaseDamage / MathF.Max(1f, medianExpectedDamage);
            expectedHealthLoss = MathF.Max(medianExpectedDamage + expectedDamageDelta, 0f);
            var worstCaseDamageDelta = Math.Max(expectedDamageDelta, expectedDamageDelta * worstCaseMultiplier);
            worstCaseHealthLoss = MathF.Max(medianWorstCaseDamage + worstCaseDamageDelta, 0f);
        }
    }
}
