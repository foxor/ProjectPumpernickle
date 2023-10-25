using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    public struct Vector2Int {
        public int x;
        public int y;
        public Vector2Int() {
            x = 0;
            y = 0;
        }
        public Vector2Int(int x, int y) {
            this.x = x;
            this.y = y;
        }
    }
    public enum PathShopPlan {
        MaxRemove,
        FixFight,
        NormalShop,
        HuntForShopRelic,
        SaveGold,
    }
    public enum FireChoice {
        Rest,
        Upgrade,
        Key,
    }
    public class Path {
        public int elites;
        public bool hasMegaElite;
        public MapNode[] nodes = null;
        public float[] expectedGold = null;
        public int[] minPlanningGold = null;
        public float expectedHealthLoss;
        public int shopCount;
        public PathShopPlan shopPlan;
        public Dictionary<string, float> Threats = new Dictionary<string, float>();
        public float Risk;
        public float[] expectedCardRewards = null;
        public float[] expectedRewardRelics = null;
        public bool[] plannedCardRemove = null;
        public float[] expectedHealth = null;
        public float[] worstCaseHealth = null;
        public string[][] possibleThreats = null;
        public float[] expectedUpgrades = null;
        public FireChoice[] fireChoices = null;

        public void InitArrays() {
            expectedGold = new float[nodes.Length];
            minPlanningGold = new int[nodes.Length];
            expectedCardRewards = new float[nodes.Length];
            expectedRewardRelics = new float[nodes.Length];
            plannedCardRemove = new bool[nodes.Length];
            expectedHealth = new float[nodes.Length];
            worstCaseHealth = new float[nodes.Length];
            possibleThreats = new string[nodes.Length][];
            expectedUpgrades = new float[nodes.Length];
            fireChoices = new FireChoice[nodes.Length];
            InitializePossibleThreats();
        }

        public float ExpectedPossibleCardRemoves() {
            float removeCost = Save.state.purgeCost;
            float totalRemoves = 0f;
            for (int i = 0; i < nodes.Length; i++) {
                if (nodes[i].nodeType == NodeType.Shop && minPlanningGold[i] > removeCost) {
                    var maxPlanningGold = expectedGold[i] - (expectedGold[i] -  minPlanningGold[i]);
                    var removeAvailableChance = Lerp.Inverse(minPlanningGold[i], maxPlanningGold, removeCost);
                    removeCost += 25f * removeAvailableChance;
                    totalRemoves += removeAvailableChance;
                }
            }
            var removesForMidActs = 2.3f;
            var removesForActFour = 1f;
            if (Save.state.act_num == 1) {
                totalRemoves += removesForMidActs * 2 + removesForActFour;
            }
            else if (Save.state.act_num == 2) {
                totalRemoves += removesForMidActs + removesForActFour;
            }
            else if (Save.state.act_num == 3) {
                // TODO: make sure we will have enough gold for this
                totalRemoves += removesForActFour;
            }
            return totalRemoves;
        }

        public void ChooseShopPlan() {
            var removeValue = PathAdvice.CardRemovePoints(this);
            var fixValue = FixShopPoint();
            var normalValue = NormalShopPoint();
            var huntValue = HuntForShopRelicPoint();
            var saveValue = SaveGoldPoint();
            var highest = new float[] {removeValue, fixValue, normalValue, huntValue, saveValue}.Max();
            if (removeValue == highest) {
                shopPlan = PathShopPlan.MaxRemove;
            }
            else if (fixValue == highest) {
                shopPlan = PathShopPlan.FixFight;
            }
            else if (normalValue == highest) {
                shopPlan = PathShopPlan.NormalShop;
            }
            else if (huntValue == highest) {
                shopPlan = PathShopPlan.HuntForShopRelic;
            }
            else if (saveValue == highest) {
                shopPlan = PathShopPlan.SaveGold;
            }
        }

        protected void InitializePossibleThreats() {
            var hasSeenElite = false;
            var easyPoolLeft = Save.state.act_num == 1 ? 3 : 2;
            easyPoolLeft = Math.Max(0, easyPoolLeft - Save.state.monsters_killed);
            for (int i = 0; i < nodes.Length; i++) {
                switch (nodes[i].nodeType) {
                    case NodeType.Shop:
                    case NodeType.Fire:
                    case NodeType.Chest: {
                        possibleThreats[i] = new string[0];
                        continue;
                    }
                    case NodeType.Elite:
                    case NodeType.MegaElite: {
                        if (!hasSeenElite) {
                            possibleThreats[i] = NextEliteOptions().ToArray();
                            hasSeenElite = true;
                        }
                        else {
                            possibleThreats[i] = EliteOptions();
                        }
                        break;
                    }
                    case NodeType.Fight: {
                        if (easyPoolLeft > 0) {
                            possibleThreats[i] = EasyPool();
                            easyPoolLeft--;
                        }
                        else {
                            possibleThreats[i] = HardPool();
                        }
                        break;
                    }
                    case NodeType.Question: {
                        // TODO
                        possibleThreats[i] = new string[0];
                        break;
                    }
                }
            }
        }

        public static string[] EasyPool() {
            return Database.instance.encounters.Where(x => x.act == Save.state.act_num && x.pool.Equals("easy")).Select(x => x.id).ToArray();
        }

        public static string[] HardPool() {
            return Database.instance.encounters.Where(x => x.act == Save.state.act_num && x.pool.Equals("hard")).Select(x => x.id).ToArray();
        }

        public static string[] EliteOptions() {
            return Database.instance.encounters.Where(x => x.act == Save.state.act_num && x.pool.Equals("elite")).Select(x => x.id).ToArray();
        }

        public static IEnumerable<string> NextEliteOptions() {
            var killed = 0;
            if (Save.state.act_num == 1) {
                killed = Save.state.elites1_killed;
            }
            else if (Save.state.act_num == 2) {
                killed = Save.state.elites2_killed;
            }
            else if (Save.state.act_num == 3) {
                killed = Save.state.elites3_killed;
            }
            var cantBe = "";
            if (killed != 0) {
                cantBe = Save.state.elite_monster_list[Save.state.elites1_killed - 1];
            }
            return Database.instance.encounters.Where(x => x.act == Save.state.act_num && x.pool.Equals("elite") && x.id != cantBe).Select(x => x.id);
        }

        public void FindBasicProperties() {
            var elites = 0;
            int minGold = Save.state.gold;
            var index = 0;
            foreach (var node in nodes) {
                if (node.nodeType == NodeType.Elite || node.nodeType == NodeType.MegaElite) {
                    elites++;
                }
                hasMegaElite |= node.nodeType == NodeType.MegaElite;
                shopCount += (node.nodeType == NodeType.Shop) ? 1 : 0;
                minGold += MinPlanningGoldFrom(index);
                minPlanningGold[index] = minGold;
                index++;
            }
        }

        public void ExpectBasicProgression() {
            // Early in the run, every card reward is a huge impact on the deck quality.
            // We need this so that we know a floor 10 elite won't kill us
            float fightsSoFar = 0f;
            float relicsSoFar = 0f;
            float fightChance = Save.state.event_chances[1];

            expectedCardRewards = new float[nodes.Length];
            expectedRewardRelics = new float[nodes.Length];
            for (int i = 0; i < nodes.Length; i++) {
                switch (nodes[i].nodeType) {
                    case NodeType.Elite: {
                        fightsSoFar++;
                        relicsSoFar++;
                        break;
                    }
                    case NodeType.MegaElite: {
                        fightsSoFar++;
                        relicsSoFar++;
                        break;
                    }
                    case NodeType.Fight: {
                        fightsSoFar++;
                        break;
                    }
                    case NodeType.Question: {
                        fightsSoFar += fightChance;
                        fightChance = .1f * fightChance + (fightChance + .1f) * (1f - fightChance);
                        break;
                    }
                }
                expectedCardRewards[i] = fightsSoFar;
                expectedRewardRelics[i] = relicsSoFar;
            }
        }

        public void ExpectShopProgression() {
            float expectedRemoves = 0;
            for (int i = 0; i < nodes.Length; i++) {
                expectedGold[i] += ExpectedGoldFrom(i);
                // TODO: also add cards and relics and such
            }
        }

        public bool NeedsRedKey(int index) {
            return false;
        }

        public bool HealthTooLow(int index) {
            // TODO: this better
            return expectedHealth[index] <= 0;
        }

        public bool ShouldRest(int index) {
            for (int i = index + 1; i < nodes.Length; i++) {
                if (nodes[i].nodeType == NodeType.Fire) {
                    return false;
                }
                if (HealthTooLow(i)) {
                    return true;
                }
            }
            return HealthTooLow(nodes.Length - 1);
        }

        public void PlanFires() {
            var upgrades = 0;
            for (int i = 0; i < nodes.Length; i++) {
                if (NeedsRedKey(i)) {
                    fireChoices[i] = FireChoice.Key;
                }
                else if (ShouldRest(i)) {
                    fireChoices[i] = FireChoice.Rest;
                    expectedHealth[i] += Save.state.max_health * .3f;
                }
                else {
                    fireChoices[i] = FireChoice.Upgrade;
                    upgrades++;
                }
                expectedUpgrades[i] = upgrades;
            }
        }

        public void ChoosePlanningThreats() {
            // What are the scary fights with our deck right now
            for (int i = 0; i < nodes.Length; i++) {
                var lastExpectedHealth = i == 0 ? Save.state.current_health : expectedHealth[i - 1];
                var lastWorstCaseHealth = i == 0 ? Save.state.current_health : worstCaseHealth[i - 1];
                float totalExpectedHealthLoss = 0f;
                float worstWorstCaseHealthLoss = 0f;
                foreach (var possibleEncounter in possibleThreats[i]) {
                    FightSimulator.SimulateFight(possibleEncounter, out var expectedHealthLoss, out var worstCaseHealthLoss);
                    totalExpectedHealthLoss += expectedHealthLoss;
                    worstWorstCaseHealthLoss = MathF.Max(worstCaseHealthLoss, worstWorstCaseHealthLoss);
                    AssessThreat(possibleEncounter, expectedHealthLoss, worstCaseHealthLoss, lastExpectedHealth);
                }
                var averageExpectedHealthLoss = totalExpectedHealthLoss / possibleThreats.Length;
                expectedHealth[i] = lastExpectedHealth - averageExpectedHealthLoss;
                worstCaseHealth[i] = lastWorstCaseHealth - worstWorstCaseHealthLoss;
            }
            NormalizeThreats();
            // TODO: threats from future acts
        }

        public void FinalThreatAnalysis() {
            // Could we fight gremlin nob by the time we get there?
            // TODO: fight simulator needs to know that we got stronger in the mean time
            ChoosePlanningThreats();
            // TODO: threats from future acts
        }

        protected void AssessThreat(string threat, float expectedDamage, float worstCaseDamage, float expectedHealth) {
            Threats.TryAdd(threat, 0f);
            var healthFraction = expectedDamage / Save.state.max_health;
            Threats[threat] += healthFraction;
            var worstCaseHealth = expectedHealth - worstCaseDamage;
            if (worstCaseDamage > expectedDamage && worstCaseHealth < 0f) {
                var deathLikelihood = -worstCaseHealth / (worstCaseDamage - expectedDamage);
                var fractionMultiplier = Lerp.From(0, 2, deathLikelihood);
                Threats[threat] += fractionMultiplier * healthFraction;
                // These risks aggregate across all encounters, which is a bit strange
                Risk += deathLikelihood;
            }
        }

        protected void NormalizeThreats() {
            var totalThreat = Threats.Values.Sum();
            foreach (var threat in Threats.Keys.ToArray()) {
                Threats[threat] /= totalThreat;
            }
        }

        public static IEnumerable<List<MapNode>> IterateNodeSequences(MapNode root, bool skipFirstNode = false) {
            if (root.children.Any()) {
                foreach (var child in root.children) {
                    foreach (var path in IterateNodeSequences(child)) {
                        if (!skipFirstNode) {
                            path.Insert(0, root);
                        }
                        yield return path;
                    }
                }
            }
            else {
                yield return new List<MapNode>() { root };
            }
        }

        protected float ExpectedGoldSpend(int index) {
            switch (shopPlan) {
                case PathShopPlan.MaxRemove: {
                    var removeCost = Save.state.purgeCost;
                    var previousRemoves = plannedCardRemove.Take(index).Select(b => b ? 1 : 0).Aggregate((acc, x) => acc + x);
                    removeCost += 25 * previousRemoves;
                    if (minPlanningGold[index] >= removeCost) {
                        plannedCardRemove[index] = true;
                        return removeCost;
                    }
                    return 0;
                }
                case PathShopPlan.HuntForShopRelic: {
                    return 0;
                }
                default: {
                    //TODO: hack
                    var currentGold = expectedGold[index];
                    var residualGold = 15f;
                    var spend = MathF.Min(currentGold - residualGold, 400f);
                    return spend;
                }
            }
        }
        protected int MinPlanningGoldFrom(int index) {
            // The point of this is to plan probabilities for having enough gold to do X.
            // We don't want to plan our shops around getting old coin or winning a joust.
            var type = nodes[index].nodeType;
            switch (type) {
                case NodeType.Fight: {
                    return 10;
                }
                case NodeType.Elite: {
                    return 25;
                }
                case NodeType.MegaElite: {
                    return 25;
                }
                case NodeType.Shop: {
                    // We should expect to spend gold, even for max gold planning purposes,
                    // if our shop plan is card remove
                    return 0;
                }
            }
            return 0;
        }
        public float ExpectedGoldFrom(int index) {
            var type = nodes[index].nodeType;
            switch (type) {
                case NodeType.Fight: {
                    return 15f;
                }
                case NodeType.Elite: {
                    return 30f + Evaluators.ExpectedGoldFromRandomRelic();
                }
                case NodeType.MegaElite: {
                    return 30 + Evaluators.ExpectedGoldFromRandomRelic();
                }
                case NodeType.Shop: {
                    return -1 * ExpectedGoldSpend(index);
                }
                case NodeType.Question: {
                    // TODO
                    return 0f;
                }
            }
            return 0f;
        }
        public float FixShopPoint() {
            // How valuable is it to buy an attack card or potion to fix a scary fight?
            return 0f;
            //if (Save.state.act_num == 1) {
            //var fightsBeforeElite = FightsBeforeElite(path);
            //if (fightsBeforeElite == 
            //}
        }
        public float NormalShopPoint() {
            return 0f;
        }
        public float HuntForShopRelicPoint() {
            return 0f;
        }
        public float SaveGoldPoint() {
            return 0f;
        }
        public float HowGoodIsTheShop() {
            if (shopCount == 0) {
                return 0f;
            }
            throw new System.NotImplementedException();
            //for (int i = 0; i < path.nodes.Length; i++) {
            //    if (path.nodes[i].nodeType == NodeType.Shop) {
            //    }
            //}
        }
        public static Path[] BuildAllPaths(MapNode root) {
            // FIXME: this is only accurate in act 1?
            var skipFirstNode = Save.state.floor_num != 0;
            var nodeSequences = IterateNodeSequences(root, skipFirstNode);
            var paths = nodeSequences.Select(x => new Path() { nodes = x.ToArray() }).ToArray();
            foreach (var path in paths) {
                path.InitArrays();
                path.FindBasicProperties();
                path.ExpectBasicProgression();
                path.ChoosePlanningThreats();
                path.ChooseShopPlan();
                path.ExpectShopProgression();
                path.PlanFires();
                path.FinalThreatAnalysis();
            }
            foreach (var child in root.children) {
                var pathsThatGoThisWay = paths.Where(x => x.nodes[0] == child);
                var safestPathThisWay = pathsThatGoThisWay.OrderBy(x => x.Risk).First();
                foreach (var path in pathsThatGoThisWay) {
                    path.Risk = safestPathThisWay.Risk;
                }
            }
            return paths;
        }
    }
}

public static class FirstIndexOfExtension {
    public static int FirstIndexOf<T>(this IEnumerable<T> source,  Func<T, bool> predicate) {
        var index = 0;
        foreach(var item in source) {
            if (predicate(item)) {
                return index;
            }
            index++;
        }
        return -1;
    }
}