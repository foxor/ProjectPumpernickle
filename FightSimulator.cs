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
        internal static float SignificanceFactor(int fromFloor) {
            // TODO: fighting an act 1 boss in act 3 should have very low significance... or a different median or something
            // TODO: decay rate should depend on "important" things happening, like finishing a combo or getting a significant relic
            var currentFloor = Save.state.floor_num;
            if (currentFloor <= 0) {
                return 1f;
            }
            float stretchFloors = 0f;
            var actsApart = Save.state.act_num - Evaluators.FloorToAct((int)MathF.Round(fromFloor));
            stretchFloors += actsApart * ACT_STRETCH_FLOORS;

            var effectiveFloor = currentFloor + stretchFloors;
            var pctRunAgo = (effectiveFloor - fromFloor) * 1f / effectiveFloor;
            var significance = MathF.Pow(1f - pctRunAgo, DECAY_RATE);
            return significance;
        }
        public static float EstimateIncomingDamage(Encounter encounter, float turns, float damagePerTurn) {
            var totalDamage = 0f;
            var enemyHealths = encounter.characters.Select(x => Database.instance.creatureDict[x].averageHealth).ToArray();
            var enemyDamageArrays = encounter.characters.Select(x => Database.instance.creatureDict[x].damage).ToArray();
            var alive = enemyHealths.Select(x => true).ToArray();
            var residualPlayerDamage = 0f;
            for (int i = 0; i < MathF.Ceiling(turns); i++) {
                var probability = MathF.Min(1f, turns - i);
                residualPlayerDamage += damagePerTurn * probability;
                for (int j = 0; j < enemyHealths.Length; j++) {
                    if (residualPlayerDamage > enemyHealths[j]) {
                        residualPlayerDamage -= enemyHealths[j];
                        alive[j] = false;
                        // FIXME: it is possible for all the enemies to die before the fight ends?
                    }
                    if (!alive[j]) {
                        continue;
                    }
                    var enemyDamageArray = enemyDamageArrays[j];
                    totalDamage += enemyDamageArray[i % enemyDamageArray.Length] * probability;
                }
            }
            // TODO: If we used a potion here, model that as though we took more "metagame health" damage
            return totalDamage;
        }
        public static float AverageEnemyHealth(Encounter encounter) {
            var enemyHealth = encounter.characters.Select(x => Database.instance.creatureDict[x].averageHealth).Sum();
            return enemyHealth;
        }
        public static float EstimateDefensivePower() {
            var totalSignificance = 0f;
            var totalPower = 0f;
            foreach (var damageTaken in Save.state.metric_damage_taken) {
                var encounter = Database.instance.encounterDict[damageTaken.enemies];
                var totalHealth = AverageEnemyHealth(encounter);
                var incomingDamage = EstimateIncomingDamage(encounter, damageTaken.turns, totalHealth / damageTaken.turns);
                var realDamage = damageTaken.damage;
                var estimatedRealBlock = incomingDamage - realDamage;
                var estimatedMedianBlock = incomingDamage - encounter.medianExpectedHealthLoss;
                float powerRatio = 1f;
                if (MathF.Abs(estimatedMedianBlock) > 0.2f) {
                    powerRatio = estimatedRealBlock / estimatedMedianBlock;
                }
                else {
                    powerRatio = Lerp.Inverse(-10f, 10f, realDamage - encounter.medianExpectedHealthLoss) + .5f;
                }
                var significance = SignificanceFactor((int)damageTaken.floor);
                totalSignificance += significance;
                totalPower += powerRatio * significance;
            }
            if (MathF.Abs(totalSignificance) - 0.2f < 0f) {
                return 1f;
            }
            totalPower /= totalSignificance;
            return totalPower;
        }
        public static readonly float BEGINNING_OF_GAME_DAMAGE = 12f;
        public static readonly float BEGINNING_OF_GAME_DAMAGE_LOG = MathF.Log(BEGINNING_OF_GAME_DAMAGE);
        public static readonly float END_OF_GAME_DAMAGE_LOG = MathF.Log(100);
        public static readonly float FLOORS_IN_GAME = 55;
        public static readonly float DAMAGE_LOG_PER_FLOOR = (END_OF_GAME_DAMAGE_LOG - BEGINNING_OF_GAME_DAMAGE_LOG) / FLOORS_IN_GAME;
        public static float EstimateDamagePerTurn() {
            float totalSignificance = 0f;
            float totalDamagePerTurn = 0f;
            foreach (var damageTaken in Save.state.metric_damage_taken) {
                if (MathF.Abs(damageTaken.turns) < .2f) {
                    continue;
                }
                var totalEnemyHealth = AverageEnemyHealth(Database.instance.encounterDict[damageTaken.enemies]);
                var damagePerTurn = totalEnemyHealth / damageTaken.turns;
                var significance = SignificanceFactor((int)damageTaken.floor);
                totalSignificance += significance;
                totalDamagePerTurn += damagePerTurn * significance;
            }
            totalDamagePerTurn += Save.state.addedDamagePerTurn;
            if (totalDamagePerTurn < BEGINNING_OF_GAME_DAMAGE) {
                // If it's act one, this is a somewhat reasonable estimate
                // If you stalled for a magnetism hand of greed or something, this is a nice BS preventor
                return BEGINNING_OF_GAME_DAMAGE;
            }
            totalDamagePerTurn /= totalSignificance;
            return totalDamagePerTurn;
        }
        public static float ProjectDamageForFutureFloor(float damagePerTurn, int floorsFromNow) {
            var damageMultiplier = MathF.Exp(DAMAGE_LOG_PER_FLOOR * floorsFromNow);
            return damagePerTurn * damageMultiplier;
        }
        public static float NormalDamageForFloor(int floor) {
            return ProjectDamageForFutureFloor(BEGINNING_OF_GAME_DAMAGE, floor);
        }
        public static float ExpectedScalingFactor(int floor, NodeType nodeType) {
            if (floor <= 10) {
                return 1f;
            }
            return nodeType switch {
                NodeType.Elite => 2.5f,
                NodeType.Boss => 4f,
                _ => 1f
            };
        }
        public static float SimulateFight(Encounter encounter, int floor, float estimatedDamagePerTurn, float defensivePower) {
            if (defensivePower - 0.02f <= 0f) {
                return encounter.medianWorstCaseHealthLoss;
            }
            estimatedDamagePerTurn *= ExpectedScalingFactor(floor, encounter.NodeType);
            var estimatedFightLength = AverageEnemyHealth(encounter) / estimatedDamagePerTurn;
            if (encounter.id.Equals("Transient")) {
                estimatedFightLength = Math.Min(estimatedFightLength, 6f);
            }
            var incomingDamage = EstimateIncomingDamage(encounter, estimatedFightLength, estimatedDamagePerTurn);
            incomingDamage -= Save.state.addedBlockPerTurn * estimatedFightLength;
            if (MathF.Abs(incomingDamage) < 0.2f) {
                return 0f;
            }
            var estimatedMedianBlock = incomingDamage - encounter.medianExpectedHealthLoss;
            if (estimatedMedianBlock < 0.2f) {
                return incomingDamage / defensivePower;
            }
            var medianBlockRatio = estimatedMedianBlock / incomingDamage;
            var powerAdjustedBlockRatio = medianBlockRatio * defensivePower;
            var estimatedHealthLoss = incomingDamage * (1 - powerAdjustedBlockRatio);
            return estimatedHealthLoss;
        }
    }
}
