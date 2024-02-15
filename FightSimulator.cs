using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class FightSimulator {
        public static readonly float DECAY_RATE = 1.5f;
        public static readonly int ACT_STRETCH_FLOORS = 8;
        public static readonly int BEGINNING_OF_GAME_STRETCH_FLOORS = 15;
        internal static float SignificanceFactor(int fromFloor) {
            // TODO: fighting an act 1 boss in act 3 should have very low significance... or a different median or something
            // TODO: decay rate should depend on "important" things happening, like finishing a combo or getting a significant relic
            var currentFloor = Save.state.floor_num;
            if (currentFloor <= 0) {
                return 1f;
            }
            var stretchFloors = 0;
            var actsApart = Save.state.act_num - Evaluators.FloorToAct((int)MathF.Round(fromFloor));
            stretchFloors += actsApart * ACT_STRETCH_FLOORS + BEGINNING_OF_GAME_STRETCH_FLOORS;
            fromFloor += BEGINNING_OF_GAME_STRETCH_FLOORS;

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
                        alive[j] = !alive.Where(x => x).Skip(1).Any();
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
        public static readonly float ACT_COMPLETION_WEIGHT = 10f;
        public static float EstimateDefensivePower() {
            if (Save.state.metric_damage_taken == null) {
                return 1f;
            }
            var totalSignificance = 0f;
            var totalPower = 0f;
            foreach (var damageTaken in Save.state.metric_damage_taken) {
                var encounter = Database.instance.encounterDict[damageTaken.enemies];
                var totalHealth = AverageEnemyHealth(encounter);
                var incomingDamage = EstimateIncomingDamage(encounter, damageTaken.turns, totalHealth / damageTaken.turns);
                var estimatedHealing = Evaluators.EstimatedHealingPerFight();
                var realDamage = damageTaken.damage;
                var estimatedRealBlock = incomingDamage - realDamage + estimatedHealing;
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
            for (var finishedAct = 0; finishedAct < Save.state.act_num; finishedAct++) {
                var endFloor = Evaluators.ActToFirstFloor(finishedAct + 1) - 1;
                var significance = SignificanceFactor(endFloor) * ACT_COMPLETION_WEIGHT;
                totalSignificance += significance;
                totalPower += significance;
                // TODO: If we're playing watcher, maybe we're more powerful than 1 * significance
            }
            if (MathF.Abs(totalSignificance) - 0.2f < 0f) {
                return 1f;
            }
            totalPower /= totalSignificance;
            return totalPower + Evaluation.Active.RewardPowerOffset;
        }
        public static readonly float GOOD_CARD_POINT_TO_POWER_RATIO = 1f / 8f;
        public static float AdjustDefensivePowerBasedOnGoodCards(string encounterId, float defensivePower) {
            var goodCardPoints = Save.state.cards.Select(x => x.goodAgainst.GetValueOrDefault(encounterId, 0f)).Sum();
            var multiplier = 1f + (goodCardPoints * GOOD_CARD_POINT_TO_POWER_RATIO);
            return defensivePower * multiplier;
        }
        public static float EstimatePastScalingPerTurn() {
            // We don't include cards we're choosing now, because the scaling gets projected backwards in time
            // which would cause us to think our damage went down if we pick a scaling card after a long fight
            var relevantCards = Save.state.CardsNotJustChosen();
            var totalScaling = relevantCards.Select(x => x.scaling).Sum();
            var totalEnergy = relevantCards.Select(Evaluators.AverageCost).Sum();
            var scalingPerEnergy = totalScaling / totalEnergy;
            var energyPerTurn = Evaluators.PerTurnEnergy();
            return scalingPerEnergy * energyPerTurn;
        }
        public static float EstimateDamagePerTurn(float estimatedScaling) {
            if (Save.state.metric_damage_taken == null) {
                return 1f;
            }
            float totalSignificance = 0f;
            float totalDamagePerTurn = 0f;
            foreach (var damageTaken in Save.state.metric_damage_taken) {
                if (MathF.Abs(damageTaken.turns) < .2f) {
                    continue;
                }
                var totalEnemyHealth = AverageEnemyHealth(Database.instance.encounterDict[damageTaken.enemies]);
                var averageDamagePerTurn = totalEnemyHealth / damageTaken.turns;
                var significance = SignificanceFactor((int)damageTaken.floor);
                // FIXME: scaling was probably lower for fights that happened a while ago
                var middleTurnScaling = 1f + (((damageTaken.turns - 1f) / 2f) * estimatedScaling);
                var initialDamageEstimate = averageDamagePerTurn / middleTurnScaling;
                totalSignificance += significance;
                totalDamagePerTurn += initialDamageEstimate * significance;
            }
            totalDamagePerTurn += Save.state.addedDamagePerTurn;
            if (totalDamagePerTurn < BEGINNING_OF_GAME_DAMAGE) {
                // If it's act one, this is a somewhat reasonable estimate
                // If you stalled for a magnetism hand of greed or something, this is a nice BS preventor
                return BEGINNING_OF_GAME_DAMAGE;
            }
            totalDamagePerTurn /= totalSignificance;

            var turnDensity = Evaluators.AverageCardsPerTurn() / Save.state.cards.Count;
            foreach (var card in Save.state.CardsJustChosen()) {
                card.tags.TryGetValue(Tags.Damage.ToString(), out var damage);
                // assume 100% drawn played rate for newly picked cards
                totalDamagePerTurn += damage * turnDensity;
            }
            return totalDamagePerTurn;
        }
        public static readonly float BEGINNING_OF_GAME_DAMAGE = 12f;
        public static readonly float BEGINNING_OF_GAME_BLOCK = 5f;
        public static readonly float BEGINNING_OF_GAME_DAMAGE_LOG = MathF.Log(BEGINNING_OF_GAME_DAMAGE);
        public static readonly float BEGINNING_OF_GAME_BLOCK_LOG = MathF.Log(BEGINNING_OF_GAME_BLOCK);
        public static readonly float END_OF_GAME_DAMAGE_LOG = MathF.Log(100);
        public static readonly float END_OF_GAME_BLOCK_LOG = MathF.Log(60);
        public static readonly float FLOORS_IN_GAME = 55;
        public static readonly float DAMAGE_LOG_PER_FLOOR = (END_OF_GAME_DAMAGE_LOG - BEGINNING_OF_GAME_DAMAGE_LOG) / FLOORS_IN_GAME;
        public static readonly float BLOCK_LOG_PER_FLOOR = (END_OF_GAME_BLOCK_LOG - BEGINNING_OF_GAME_BLOCK_LOG) / FLOORS_IN_GAME;
        public static float ProjectDamageForFutureFloor(float damagePerTurn, int floorsFromNow) {
            var damageMultiplier = MathF.Exp(DAMAGE_LOG_PER_FLOOR * floorsFromNow);
            return damagePerTurn * damageMultiplier;
        }
        public static float ProjectBlockForFutureFloor(float damagePerTurn, int floorsFromNow) {
            var damageMultiplier = MathF.Exp(BLOCK_LOG_PER_FLOOR * floorsFromNow);
            return damagePerTurn * damageMultiplier;
        }
        public static float NormalDamageForFloor(int floor) {
            return ProjectDamageForFutureFloor(BEGINNING_OF_GAME_DAMAGE, floor);
        }
        public static float NormalBlockForFloor(int floor) {
            return ProjectDamageForFutureFloor(BEGINNING_OF_GAME_BLOCK, floor);
        }
        public static float ExpectedFightLength(Encounter encounter, float initialDamage, float damageScaling) {
            var totalHealth = AverageEnemyHealth(encounter);
            var currentDamage = initialDamage - damageScaling;
            var turns = 0f;
            for (turns = 0; totalHealth > 0f; turns++) {
                currentDamage += damageScaling;
                totalHealth -= currentDamage;
            }
            turns -= -totalHealth / currentDamage;
            return turns;
        }
        public static float MedianFightLength(Encounter encounter) {
            if (encounter.medianFightLength != 0f) {
                return encounter.medianFightLength;
            }
            return encounter.pool switch {
                "hard" => 3f,
                "easy" => 2.5f,
                "elite" => 4f,
                "boss" => 7f,
                _ => 3f,
            };
        }
        public static float SimulateFight(Encounter encounter) {
            var scalingPerTurn = EstimatePastScalingPerTurn();
            var estimatedDamage = EstimateDamagePerTurn(scalingPerTurn);
            var defensivePower = EstimateDefensivePower();
            return SimulateFight(encounter, Save.state.floor_num, estimatedDamage, scalingPerTurn, defensivePower);
        }
        public static float SimulateFight(Encounter encounter, int floor, float initialDamage, float damageScaling, float defensivePower) {
            if (defensivePower - 0.02f <= 0f) {
                return encounter.medianWorstCaseHealthLoss;
            }
            defensivePower = AdjustDefensivePowerBasedOnGoodCards(encounter.id, defensivePower);
            var estimatedFightLength = ExpectedFightLength(encounter, initialDamage, damageScaling);
            if (encounter.id.Equals("Transient")) {
                estimatedFightLength = Math.Min(estimatedFightLength, 6f);
            }
            var medianFightLength = MedianFightLength(encounter);
            var incomingDamage = EstimateIncomingDamage(encounter, estimatedFightLength, initialDamage);
            var medianDamage = NormalDamageForFloor(floor);
            var medianIncomingDamage = EstimateIncomingDamage(encounter, medianFightLength, medianDamage);
            incomingDamage -= Save.state.addedBlockPerTurn * estimatedFightLength;
            if (MathF.Abs(incomingDamage) < 0.2f) {
                return 0f;
            }
            var medianHealthLoss = encounter.medianExpectedHealthLoss;
            var health = Evaluators.GetHealth(floor - 1);
            if (encounter.id.Equals("Hexaghost")) {
                // Median character is in the 12-23 range
                medianHealthLoss += 6 * ((int)(health / 12) - 1);
            }
            var estimatedMedianBlock = medianIncomingDamage - medianHealthLoss;
            var medianBlockRatio = estimatedMedianBlock / medianIncomingDamage;
            var powerAdjustedBlockRatio = medianBlockRatio * defensivePower;
            var estimatedHealthLoss = incomingDamage * (1 - powerAdjustedBlockRatio);
            return estimatedHealthLoss;
        }
        public static float ChanceToHitDamageBreakpoint(float requiredTotalDamage, int turn, int floorNum) {
            var scalingPerTurn = EstimatePastScalingPerTurn();
            var estimatedDamagePerTurn = EstimateDamagePerTurn(scalingPerTurn);
            estimatedDamagePerTurn = ProjectDamageForFutureFloor(estimatedDamagePerTurn, floorNum - Save.state.floor_num);
            var totalDamage = 0f;
            for (int t = 0; t < turn; t++) {
                totalDamage += estimatedDamagePerTurn;
                estimatedDamagePerTurn += scalingPerTurn;
            }
            var excessDamage = totalDamage - requiredTotalDamage;
            var turnsWorthOfExcessDamage = excessDamage / estimatedDamagePerTurn;
            // https://www.wolframalpha.com/input?i=sigmoid%28x+*+3%29+from+-1+to+1
            // if excessDamage == 0, you've hit the breakpoint EXACLY on time, so that's a 50-50
            return PumpernickelMath.Sigmoid(turnsWorthOfExcessDamage * 3f);
        }
        public static float ChanceToHitBlockBreakpoint(string encounterId, float neededBlockOnTurn, int turn, int floorNum) {
            var scalingPerTurn = EstimatePastScalingPerTurn();
            var defensivePower = EstimateDefensivePower();
            defensivePower = AdjustDefensivePowerBasedOnGoodCards(encounterId, defensivePower);
            var normalBlock = NormalBlockForFloor(floorNum);
            var estimatedBlockOnTurn = (normalBlock * defensivePower) + (scalingPerTurn * turn);
            var excessBlock = estimatedBlockOnTurn - neededBlockOnTurn;
            // https://www.wolframalpha.com/input?i=sigmoid%28x+%2F+5%29+from+-10+to+10
            return PumpernickelMath.Sigmoid(excessBlock / 5f);
        }
    }
}
