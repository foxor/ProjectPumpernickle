using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ProjectPumpernickle {
    public class Path {
        public int elites;
        public bool hasMegaElite;
        public MapNode[] nodes = null;
        public float[] expectedGold = null;
        public int[] minPlanningGold = null;
        public float expectedHealthLoss;
        public PathShopPlan shortTermShopPlan;
        public Dictionary<string, float> Threats = new Dictionary<string, float>();
        public float[] expectedCardRewards = null;
        public float[] expectedRewardRelics = null;
        public bool[] plannedCardRemove = null;
        public float[] expectedHealth = null;
        public float[] worstCaseHealth = null;
        public Encounter[][] possibleThreats = null;
        public float[] expectedUpgrades = null;
        public FireChoice[] fireChoices = null;
        public float[] expectedShops = null;
        public float[] fightChance = null;
        public float[] chanceOfDeath = null;
        public int remainingFloors;
        public bool EndOfActPath;
        public float chanceToSurvive;
        public NodeType[] nodeTypes = null;
        public float[] expectedMaxHealth = null;
        public static Path[] BuildAllPaths(MapNode root, int startingCardRewards) {
            var skipFirstNode = true;
            var nodeSequences = IterateNodeSequences(root, skipFirstNode);
            var paths = nodeSequences.Select(x => BuildPath(x.ToArray(), startingCardRewards)).ToArray();
            return paths;
        }
        public static Path BuildPath(MapNode[] nodeSequence, int startingCardRewards = 0) {
            Path path = new Path();
            path.nodes = nodeSequence;

            path.InitArrays();
            path.InitNodeTypes();
            path.InitializePossibleThreats();
            path.FindBasicProperties();
            path.ExpectGoldProgression();
            path.ExpectBasicProgression(startingCardRewards);
            path.SimulateHealthEvolution();
            path.ChooseShortTermShopPlan();
            path.ExpectShopProgression();
            path.PlanFires();
            path.FinalThreatAnalysis();
            return path;
        }

        public static Path Copy(Path path) {
            Path r = new Path();
            r.elites = path.elites;
            r.hasMegaElite = path.hasMegaElite;
            r.expectedHealthLoss = path.expectedHealthLoss;
            r.shortTermShopPlan = path.shortTermShopPlan;
            r.remainingFloors = path.remainingFloors;
            r.EndOfActPath = path.EndOfActPath;
            r.chanceToSurvive = path.chanceToSurvive;

            r.nodes = path.nodes.ToArray();
            r.expectedGold = path.expectedGold.ToArray();
            r.minPlanningGold = path.minPlanningGold.ToArray();
            r.expectedCardRewards = path.expectedCardRewards.ToArray();
            r.expectedRewardRelics = path.expectedRewardRelics.ToArray();
            r.plannedCardRemove = path.plannedCardRemove.ToArray();
            r.expectedHealth = path.expectedHealth.ToArray();
            r.worstCaseHealth = path.worstCaseHealth.ToArray();
            r.possibleThreats = path.possibleThreats.ToArray();
            r.expectedUpgrades = path.expectedUpgrades.ToArray();
            r.fireChoices = path.fireChoices.ToArray();
            r.expectedShops = path.expectedShops.ToArray();
            r.fightChance = path.fightChance.ToArray();
            r.chanceOfDeath = path.chanceOfDeath.ToArray();
            r.nodeTypes = path.nodeTypes.ToArray();
            r.expectedMaxHealth = path.expectedMaxHealth.ToArray();

            return r;
        }

        public static int PathIndexToFloorNum(int index) {
            return Save.state.floor_num + 1 + index;
        }

        public void InitArrays() {
            remainingFloors = 55 - Save.state.floor_num;
            expectedGold = new float[remainingFloors];
            plannedCardRemove = new bool[remainingFloors];
            minPlanningGold = new int[remainingFloors];
            expectedCardRewards = new float[remainingFloors];
            expectedRewardRelics = new float[remainingFloors];
            expectedHealth = new float[remainingFloors];
            worstCaseHealth = new float[remainingFloors];
            possibleThreats = new Encounter[remainingFloors][];
            expectedUpgrades = new float[remainingFloors];
            fireChoices = new FireChoice[remainingFloors];
            expectedShops = new float[remainingFloors];
            fightChance = new float[remainingFloors];
            chanceOfDeath = new float[remainingFloors];
            nodeTypes = new NodeType[remainingFloors];
            expectedMaxHealth = new float[remainingFloors];
        }

        public void InitNodeTypes() {
            for (int i = 0; i < nodeTypes.Length; i++) {
                nodeTypes[i] = GetNodeType(i);
            }
        }

        public NodeType GetNodeType(int i) {
            if (i < nodes.Length) {
                return nodes[i].nodeType;
            }
            var nodeType = FutureActPath.ExpectedNode(PathIndexToFloorNum(i));
            if (nodeType == NodeType.Unknown) {
                var endOfActGold = nodes.Length > 0 ? expectedGold[nodes.Length - 1] : Save.state.gold;
                nodeType = FutureActPath.EstimateNodeType(i, endOfActGold);
            }
            return nodeType;
        }

        public static float ExpectedFutureActCardRemoves() {
            float totalRemoves = 0f;
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
        public static float ExpectedFutureActCardRemovesBeforeNemesis() {
            return ExpectedFutureActCardRemoves() - 2f;
        }

        protected float ExpectedCardRemovesInternal(bool beforeElite = false) {
            float removeCost = Save.state.purgeCost;
            float totalRemoves = 0f;
            for (int i = 0; i < nodes.Length; i++) {
                if (nodes[i].nodeType == NodeType.Shop && minPlanningGold[i] > removeCost) {
                    var maxPlanningGold = expectedGold[i] - (expectedGold[i] -  minPlanningGold[i]);
                    var removeAvailableChance = Lerp.Inverse(minPlanningGold[i], maxPlanningGold, removeCost);
                    removeCost += 25f * removeAvailableChance;
                    totalRemoves += removeAvailableChance;
                }
                if ((nodes[i].nodeType == NodeType.Elite || nodes[i].nodeType == NodeType.MegaElite) && beforeElite) {
                    break;
                }
            }
            return totalRemoves;
        }

        public float ExpectedPossibleCardRemoves() {
            return ExpectedCardRemovesInternal() + ExpectedFutureActCardRemoves();
        }

        public float ExpectedPossibleCardRemovesBeforeNemesis() {
            if (Save.state.act_num != 3) {
                return ExpectedCardRemovesInternal() + ExpectedFutureActCardRemovesBeforeNemesis();
            }
            var beforeElite = true;
            return ExpectedCardRemovesInternal(beforeElite);
        }
        protected void InitializePossibleThreats() {
            var hasSeenElite = false;
            var easyPoolLeft = Save.state.act_num == 1 ? 3 : 2;
            var lastAct = Save.state.act_num;
            easyPoolLeft = Math.Max(0, easyPoolLeft - Save.state.monsters_killed);
            for (int i = 0; i < possibleThreats.Length; i++) {
                var floor = PathIndexToFloorNum(i);
                var act = Evaluators.FloorToAct(floor);
                if (act != lastAct) {
                    easyPoolLeft = 2;
                    hasSeenElite = false;
                    lastAct = act;
                }
                switch (nodeTypes[i]) {
                    case NodeType.Shop:
                    case NodeType.Fire:
                    case NodeType.BossChest:
                    case NodeType.Chest: {
                        possibleThreats[i] = new Encounter[0];
                        continue;
                    }
                    case NodeType.Elite:
                    case NodeType.MegaElite: {
                        if (hasSeenElite) {
                            possibleThreats[i] = NextEliteOptions().ToArray();
                            hasSeenElite = false;
                        }
                        else {
                            possibleThreats[i] = EliteOptions(act);
                        }
                        break;
                    }
                    case NodeType.Fight: {
                        if (easyPoolLeft > 0) {
                            possibleThreats[i] = EasyPool(act);
                            easyPoolLeft--;
                        }
                        else {
                            possibleThreats[i] = HardPool(act);
                        }
                        break;
                    }
                    case NodeType.Question: {
                        // If we hit a question mark fight, that doesn't change the total number of easy pool fights we do
                        possibleThreats[i] = HardPool(act);
                        // TODO: add event fights
                        break;
                    }
                    case NodeType.Boss: {
                        if (floor == 51 && Save.state.act_num == 3) {
                            possibleThreats[i] = NextBossOptions().ToArray();
                        }
                        else if (act == Save.state.act_num) {
                            possibleThreats[i] = new Encounter[] { Database.instance.encounterDict[Save.state.boss] };
                        }
                        else {
                            possibleThreats[i] = BossOptions(act);
                        }
                        break;
                    }
                }
            }
        }

        public static Encounter[] EasyPool(int act) {
            return Database.instance.EasyPools[act - 1];
        }

        public static Encounter[] HardPool(int act) {
            return Database.instance.HardPools[act - 1];
        }

        public static Encounter[] EliteOptions(int act) {
            return Database.instance.Elites[act - 1];
        }

        public static Encounter[] BossOptions(int act) {
            return Database.instance.Bosses[act - 1];
        }

        public static IEnumerable<Encounter> NextEliteOptions() {
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
            return EliteOptions(Save.state.act_num).Where(x => x.act == Save.state.act_num && x.pool.Equals("elite") && x.id != cantBe);
        }

        public static IEnumerable<Encounter> NextBossOptions() {
            switch (Save.state.boss) {
                case "Time Eater": {
                    return new string[] {
                        "Awakened One",
                        "Donu Deca",
                    }.Select(x => Database.instance.encounterDict[x]);
                }
                case "Awakened One": {
                    return new string[] {
                        "Time Eater",
                        "Donu Deca",
                    }.Select(x => Database.instance.encounterDict[x]);
                }
                case "Donu Deca": {
                    return new string[] {
                        "Awakened One",
                        "Time Eater",
                    }.Select(x => Database.instance.encounterDict[x]);
                }
                default: {
                    throw new NotImplementedException();
                }
            }
        }

        public void FindBasicProperties() {
            var elites = 0;
            int minGold = Save.state.gold;
            for (var i = 0; i < nodeTypes.Length; i++) {
                if (nodeTypes[i] == NodeType.Elite || nodeTypes[i] == NodeType.MegaElite) {
                    elites++;
                }
                hasMegaElite |= nodeTypes[i] == NodeType.MegaElite;
                minGold += MinPlanningGoldFrom(i);
                minPlanningGold[i] = minGold;
            }
        }
        public static readonly float CARD_PICK_RATE_OVER_BOWL = .3f;
        public static readonly float FEED_HIT_RATE = .8f;
        public void ExpectBasicProgression(int startingCardRewards) {
            // Early in the run, every card reward is a huge impact on the deck quality.
            // We need this so that we know a floor 10 elite won't kill us
            float fightsSoFar = 0f;
            float relicsSoFar = 0f;
            float cardRewardsSoFar = startingCardRewards;
            float floorFightChance = Save.state.event_chances[1];
            float shopChance = Save.state.event_chances[2];
            float shopsSoFar = 0f;
            var hasSingingBowl = Save.state.relics.Contains("Singing Bowl");
            var feed = Save.state.cards.Where(x => x.id.Equals("Feed"));
            var hasFeed = feed.Any();
            var feedHealth = feed.Select(x => x.upgrades > 0 ? 5f : 3f).Max();
            if (Save.state.cards.Any(x => x.id.Equals("Armaments")) && feedHealth == 3f) {
                feedHealth = 4f; // 50% chance to upgrade
            }
            if (Save.state.cards.Any(x => x.id.Equals("Apotheosis")) && feedHealth == 3f) {
                feedHealth = 4.8f;
            }
            var maxHp = (float)Save.state.max_health;
            if (hasSingingBowl) {
                maxHp += startingCardRewards * (1f - CARD_PICK_RATE_OVER_BOWL);
            }

            for (int i = 0; i < expectedCardRewards.Length; i++) {
                fightChance[i] = 0f;
                switch (nodeTypes[i]) {
                    case NodeType.MegaElite:
                    case NodeType.Elite: {
                        fightsSoFar++;
                        relicsSoFar++;
                        cardRewardsSoFar++;
                        fightChance[i] = 1f;
                        if (hasFeed) {
                            maxHp += FEED_HIT_RATE * feedHealth;
                        }
                        break;
                    }
                    case NodeType.Boss:
                    case NodeType.Fight: {
                        fightsSoFar++;
                        cardRewardsSoFar++;
                        fightChance[i] = 1f;
                        if (hasFeed) {
                            maxHp += FEED_HIT_RATE * feedHealth;
                        }
                        break;
                    }
                    case NodeType.Shop: {
                        shopsSoFar++;
                        break;
                    }
                    case NodeType.Question: {
                        fightsSoFar += floorFightChance;
                        if (hasFeed) {
                            maxHp += FEED_HIT_RATE * feedHealth * floorFightChance;
                        }
                        shopsSoFar += shopChance;
                        cardRewardsSoFar += floorFightChance;
                        fightChance[i] = floorFightChance;
                        floorFightChance = .1f * floorFightChance + (floorFightChance + .1f) * (1f - floorFightChance);
                        shopChance = .03f * shopChance + (shopChance + .03f) * (1f - shopChance);
                        break;
                    }
                    case NodeType.Chest:
                    case NodeType.BossChest: {
                        relicsSoFar++;
                        break;
                    }
                }
                expectedCardRewards[i] = cardRewardsSoFar;
                expectedRewardRelics[i] = relicsSoFar;
                expectedShops[i] = shopsSoFar;
                expectedMaxHealth[i] = maxHp;
            }
        }

        public void ExpectGoldProgression() {
            var gold = (float)Save.state.gold;
            for (int i = 0; i < expectedGold.Length; i++) {
                gold += ExpectedGoldGain(i);
                expectedGold[i] = gold;
            }
        }

        public void ExpectShopProgression() {
            var totalSpent = 0f;
            for (int i = 0; i < expectedGold.Length; i++) {
                var nodeType = nodeTypes[i];
                if (nodeType == NodeType.Shop) {
                    totalSpent += ExpectedGoldSpend(i);
                    // Simulate getting stronger by buying things
                }
                expectedGold[i] -= totalSpent;
            }
        }

        public bool NeedsRedKey(int index) {
            if (Save.state.act_num != 3 || Save.state.has_ruby_key) {
                return false;
            }
            var fireNodes = Enumerable.Range(0, nodes.Length).Where(x => nodes[x].nodeType == NodeType.Fire);
            var fireCount = fireNodes.Count();
            if (fireCount <= 1) {
                return true;
            }
            return fireNodes.Skip(fireCount - 2).First() == index;
        }
        public static float UNACCEPTABLE_RISK = 0.05f;
        public bool HealthTooLow(int index) {
            if (index < 0) {
                return false;
            }
            return chanceOfDeath[index] >= UNACCEPTABLE_RISK;
        }

        public bool ShouldRest(int index) {
            for (int i = index + 1; i < expectedHealth.Length; i++) {
                var nodeType = nodeTypes[i];
                if (nodeType == NodeType.Fire) {
                    return false;
                }
                if (HealthTooLow(i)) {
                    return true;
                }
            }
            return HealthTooLow(nodes.Length - 1);
        }

        private void TestPlan(ref float bestValue, ref FireChoice bestFireChoice, float value, FireChoice choice) {
            if (value > bestValue) {
                bestValue = value;
                bestFireChoice = choice;
            }
        }

        public void PlanFires() {
            var upgrades = 0f;
            for (int i = 0; i < fireChoices.Length; i++) {
                var nodeType = nodeTypes[i];
                if (nodeType != NodeType.Fire) {
                    fireChoices[i] = FireChoice.None;
                    if (Save.state.cards.Any(x => x.id.Equals("LessonLearned"))) {
                        if (nodeType == NodeType.Fight) {
                            upgrades += .9f;
                        }
                        if (nodeType == NodeType.Elite || nodeType == NodeType.MegaElite) {
                            upgrades += .6f;
                        }
                    }
                }
                else {
                    var restValue = Evaluators.RestValue(this, i);
                    var upgradeValue = Evaluators.UpgradeHealthSaved(this, i);
                    var liftValue = Evaluators.LiftValue(this, i);

                    var bestChoice = FireChoice.Upgrade;
                    var bestValue = upgradeValue;
                    TestPlan(ref bestValue, ref bestChoice, restValue, FireChoice.Rest);
                    TestPlan(ref bestValue, ref bestChoice, liftValue, FireChoice.Lift);

                    if (NeedsRedKey(i)) {
                        fireChoices[i] = FireChoice.Key;
                    }
                    else if (ShouldRest(i)) {
                        fireChoices[i] = FireChoice.Rest;
                    }
                    else {
                        fireChoices[i] = bestChoice;
                        if (bestChoice == FireChoice.Upgrade) {
                            upgrades++;
                        }
                    }
                }
                expectedUpgrades[i] = upgrades;
            }
        }

        public void SimulateHealthEvolution(float powerMultiplier = 1f, int floorsFromNow = 0) {
            var currentDefensivePower = FightSimulator.EstimateDefensivePower() * powerMultiplier;
            var currentDamagePerTurn = FightSimulator.EstimateDamagePerTurn();
            Threats.Clear();
            for (int i = floorsFromNow; i < possibleThreats.Length; i++) {
                var estimatedDamageThisFloor = FightSimulator.ProjectDamageForFutureFloor(currentDamagePerTurn, i);
                var lastExpectedHealth = i == 0 ? Evaluators.GetEffectiveHealth() : expectedHealth[i - 1];
                // Worst case doesn't continue to stack every floor
                // You start at the expected health, and then ONE bad thing happens, not the worst case every floor
                var lastWorstCaseHealth = i == 0 ? Evaluators.GetEffectiveHealth() : expectedHealth[i - 1];
                if (fireChoices[i] == FireChoice.Rest) {
                    lastExpectedHealth += Save.state.max_health * .3f;
                    lastWorstCaseHealth += Save.state.max_health * .3f;
                    lastExpectedHealth = MathF.Min(Save.state.max_health, lastExpectedHealth);
                    lastWorstCaseHealth = MathF.Min(Save.state.max_health, lastWorstCaseHealth);
                }
                var floor = PathIndexToFloorNum(i);
                if (Evaluators.FloorsIntoAct(floor) == 0) {
                    var missing = Save.state.max_health - lastExpectedHealth;
                    var healing = (int)((missing * .75) + 0.9999f);
                    lastExpectedHealth += healing;
                    lastWorstCaseHealth += healing;
                }
                float averageExpectedHealthLoss = 0f;
                float worstWorstCaseHealthLoss = 0f;
                var totalWeight = possibleThreats[i].Select(x => x.weight).Sum();
                chanceOfDeath[i] = 0f;
                foreach (var possibleEncounter in possibleThreats[i]) {
                    var expectedHealthLoss = FightSimulator.SimulateFight(possibleEncounter, PathIndexToFloorNum(i), estimatedDamageThisFloor, currentDefensivePower);
                    worstWorstCaseHealthLoss = MathF.Max(possibleEncounter.medianWorstCaseHealthLoss, worstWorstCaseHealthLoss);
                    var chanceOfThis = (possibleEncounter.weight * 1f / totalWeight) * fightChance[i];
                    averageExpectedHealthLoss += expectedHealthLoss * chanceOfThis;
                    AssessThreat(possibleEncounter, i, expectedHealthLoss, possibleEncounter.medianWorstCaseHealthLoss, lastExpectedHealth, chanceOfThis);
                }
                expectedHealth[i] = lastExpectedHealth - averageExpectedHealthLoss;
                worstCaseHealth[i] = lastWorstCaseHealth - worstWorstCaseHealthLoss;
            }
            chanceToSurvive = chanceOfDeath.Aggregate(1f, (s, d) => s * (1f - d));
        }

        public void FinalThreatAnalysis() {
            // Could we fight gremlin nob by the time we get there?
            SimulateHealthEvolution();
            NormalizeThreat();
        }
        public void NormalizeThreat() {
            var totalThreat = Threats.Select(x => x.Value).Sum();
            foreach (var threatKey in Threats.Keys) {
                Threats[threatKey] /= totalThreat;
            }
        }
        public static readonly float CHANCE_OF_WORST_CASE = 0.01f;
        // From wolfram alpha
        public static readonly float MULTIPLIER_FOR_TWENTY_PERCENT = -4.59512f;
        public static readonly float MULTIPLIER_FOR_EIGHTY_PERCENT = -MULTIPLIER_FOR_TWENTY_PERCENT;

        public static readonly float UPCOMING_THREAT_BONUS = 5f;
        public static readonly float UPCOMING_THREAT_FALLOFF = 14f;
        public static readonly float UPCOMING_DEATH_THREAT_MULTIPLIER = 8f;
        protected void AssessThreat(Encounter threat, int floorIndex, float expectedDamage, float worstCaseDamage, float expectedHealth, float chanceOfThis) {
            Threats.TryAdd(threat.id, 0f);
            var soonMultiplier = Lerp.Inverse(0, UPCOMING_THREAT_FALLOFF, UPCOMING_THREAT_FALLOFF - floorIndex) + 1;
            var upcomingBonus = UPCOMING_THREAT_BONUS * soonMultiplier;
            var healthFraction = expectedDamage / Save.state.max_health;
            Threats[threat.id] += healthFraction * chanceOfThis * upcomingBonus;
            if (worstCaseDamage <= expectedDamage) {
                Threats[threat.id] += chanceOfThis * upcomingBonus;
                chanceOfDeath[floorIndex] += chanceOfThis;
            }
            else {
                var worstCaseResidualHealth = expectedHealth - worstCaseDamage;
                var damageRange = worstCaseDamage - expectedDamage;
                var bestCaseResidualHealth = expectedHealth - (expectedDamage - damageRange);
                var deathHealth = 0f;
                var deathParam = Lerp.InverseUncapped(worstCaseResidualHealth, bestCaseResidualHealth, deathHealth);
                var sigmoidX = Lerp.FromUncapped(MULTIPLIER_FOR_TWENTY_PERCENT, MULTIPLIER_FOR_EIGHTY_PERCENT, deathParam);
                var deathLikelihood = PumpernickelMath.Sigmoid(sigmoidX);
                var upcomingDeathMultiplier = ((soonMultiplier - 1) * UPCOMING_DEATH_THREAT_MULTIPLIER) + 1f;
                Threats[threat.id] += deathLikelihood * chanceOfThis * upcomingBonus * upcomingDeathMultiplier;
                chanceOfDeath[floorIndex] += deathLikelihood * chanceOfThis;
            }
        }

        public static IEnumerable<List<MapNode>> IterateNodeSequences(MapNode root, bool skipFirstNode = false) {
            if (root == null) {
                yield return new List<MapNode>();
                yield break;
            }
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
            var expectedGoldForFloor = index > 0 ? expectedGold[index - 1] : Save.state.gold;
            var minGoldForFloor = index > 0 ? minPlanningGold[index - 1] : Save.state.gold;
            switch (shortTermShopPlan) {
                case PathShopPlan.MaxRemove: {
                    var removeCost = Save.state.purgeCost;
                    var previousRemoves = index == 0 ? 0 : plannedCardRemove.Take(index).Select(b => b ? 1 : 0).Aggregate((acc, x) => acc + x);
                    removeCost += 25 * previousRemoves;
                    if (minGoldForFloor >= removeCost) {
                        plannedCardRemove[index] = true;
                        return removeCost;
                    }
                    return 0;
                }
                case PathShopPlan.HuntForShopRelic: {
                    return 0;
                }
                case PathShopPlan.NormalShop: {
                    var remainingShops = nodeTypes.Skip(index).Where(x => x == NodeType.Shop).Count();
                    if (remainingShops <= 1) {
                        return expectedGoldForFloor;
                    }
                    else if (remainingShops == 2) {
                        var lastShopIndex = Enumerable.Range(0, nodeTypes.Length).Where(x => nodeTypes[x] == NodeType.Shop).Last();
                        var totalGold = expectedGold[lastShopIndex];
                        return totalGold / 2f;
                    }
                    else {
                        return Math.Min(expectedGoldForFloor - 15f, 400f);
                    }
                }
                default: {
                    throw new NotImplementedException();
                }
            }
        }
        protected int MinPlanningGoldFrom(int index) {
            // The point of this is to plan probabilities for having enough gold to do X.
            // We don't want to plan our shops around getting old coin or winning a joust.
            switch (nodeTypes[index]) {
                case NodeType.Fight: {
                    return 10;
                }
                case NodeType.Elite: {
                    return 25;
                }
                case NodeType.MegaElite: {
                    return 25;
                }
                case NodeType.Boss: {
                    return 71;
                }
                case NodeType.Shop: {
                    // We should expect to spend gold, even for max gold planning purposes,
                    // if our shop plan is card remove
                    return 0;
                }
            }
            return 0;
        }
        public float ExpectedGoldGain(int index) {
            var nodeType = nodeTypes[index];
            switch (nodeType) {
                case NodeType.Fight: {
                    return 15f;
                }
                case NodeType.Elite: {
                    return 30f + Evaluators.ExpectedGoldFromRandomRelic();
                }
                case NodeType.MegaElite: {
                    return 30 + Evaluators.ExpectedGoldFromRandomRelic();
                }
                case NodeType.Boss: {
                    return 75;
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
        public static readonly float NORMAL_SHOP_BASELINE = 100f;
        public float NormalShopPoint() {
            return NORMAL_SHOP_BASELINE;
        }
        public float HuntForShopRelicPoint() {
            return 0f;
        }
        public float SaveGoldPoint() {
            return 0f;
        }
        public void ChooseShortTermShopPlan() {
            var removeValue = Evaluators.CardRemovePoints(this);
            var fixValue = FixShopPoint();
            var normalValue = NormalShopPoint();
            var huntValue = HuntForShopRelicPoint();
            var saveValue = SaveGoldPoint();
            var highest = new float[] {removeValue, fixValue, normalValue, huntValue, saveValue}.Max();
            if (removeValue == highest) {
                shortTermShopPlan = PathShopPlan.MaxRemove;
            }
            else if (fixValue == highest) {
                shortTermShopPlan = PathShopPlan.FixFight;
            }
            else if (normalValue == highest) {
                shortTermShopPlan = PathShopPlan.NormalShop;
            }
            else if (huntValue == highest) {
                shortTermShopPlan = PathShopPlan.HuntForShopRelic;
            }
            else if (saveValue == highest) {
                shortTermShopPlan = PathShopPlan.SaveGold;
            }
        }
        public IEnumerable<float> ExpectedGoldBroughtToShops() {
            for (int i = 0; i < nodeTypes.Length; i++) {
                if (nodeTypes[i] == NodeType.Shop) {
                    yield return expectedGold[i];
                }
            }
        }
        public static float ExpectedFutureActCardRewards() {
            return 8.5f;
        }
        public static float ExpectedHuntedCardsFoundInFutureActs() {
            var normalActCardRewards = ExpectedFutureActCardRewards();
            var normalActShops = .85f;
            var normalActsLeft = Evaluators.NormalFutureActsLeft();
            if (!Save.state.huntingCards.Any()) {
                return 0f;
            }
            // +1 from spire elites
            var totalCardRewards = (normalActCardRewards * normalActsLeft) + 1;
            var totalShopsLeft = (normalActShops * normalActsLeft) + 1;

            var totalChance = 0f;
            foreach (var huntedCard in Save.state.huntingCards) {
                var card = Database.instance.cardsDict[huntedCard];
                var rarity = card.cardRarity;
                var color = card.cardColor;
                var cardType = card.cardType;
                var chanceToFind = 0f;
                if (color == Color.Colorless) {
                    chanceToFind += Evaluators.ChanceOfAppearingInShop(color, rarity, cardType) * totalShopsLeft;
                }
                else {
                    switch (rarity) {
                        case Rarity.Common:
                        case Rarity.Uncommon:
                        case Rarity.Rare: {
                            chanceToFind += Evaluators.ChanceOfSpecificCardInReward(color, rarity, totalCardRewards);
                            chanceToFind += Evaluators.ChanceOfAppearingInShop(color, rarity, cardType) * totalShopsLeft;
                            break;
                        }
                        case Rarity.Special: {
                            // god help you
                            break;
                        }
                    }
                }
                totalChance += chanceToFind;
            }
            return totalChance;
        }
        public float ExpectedHuntedCardsFound() {
            if (!Save.state.huntingCards.Any()) {
                return 0f;
            }
            var totalChance = 0f;
            var totalExpectedCardRewards = expectedCardRewards[^1];
            foreach (var huntedCard in Save.state.huntingCards) {
                var card = Database.instance.cardsDict[huntedCard];
                var rarity = card.cardRarity;
                var color = card.cardColor;
                var cardType = card.cardType;
                var shopsWithGold = 0f;
                var previousFloorShopChance = 0f;
                for (int i = 0; i < nodes.Length; i++) {
                    var marginalShopChance = expectedShops[i] - previousFloorShopChance;
                    previousFloorShopChance = expectedShops[i];
                    shopsWithGold += Evaluators.IsEnoughToBuyCard(expectedGold[i], rarity) * marginalShopChance;
                }
                var chanceToFind = 0f;
                if (color == Color.Colorless) {
                    chanceToFind += Evaluators.ChanceOfAppearingInShop(color, rarity, cardType) * shopsWithGold;
                }
                else {
                    switch (rarity) {
                        case Rarity.Common:
                        case Rarity.Uncommon:
                        case Rarity.Rare: {
                            chanceToFind += Evaluators.ChanceOfSpecificCardInReward(color, rarity, totalExpectedCardRewards);
                            chanceToFind += Evaluators.ChanceOfAppearingInShop(color, rarity, cardType) * shopsWithGold;
                            break;
                        }
                        case Rarity.Special: {
                            // god help you
                            break;
                        }
                    }
                }
                totalChance += chanceToFind;
            }
            return totalChance + ExpectedHuntedCardsFoundInFutureActs();
        }

        public static IEnumerable<float> ExpectedFutureUpgradesDuringFights(float endOfActUpgrades) {
            var averageActSequence = new float[] {
                0f,
                0f,
                .4f,
                .8f,
                1.2f,
                1.6f,
                2.0f,
                2.4f,
            };
            var hasLessonLearned = Save.state.cards.Any(x => x.id.Equals("LessonLearned"));
            if (hasLessonLearned) {
                averageActSequence = new float[] {
                    .9f,
                    1.8f,
                    2.7f,
                    3.6f,
                    4.5f,
                    5.4f,
                    6.3f,
                    7.2f,
                };
            }
            var normalActsLeft = MathF.Max(2 - Save.state.act_num, 0);
            var fightsLeft = normalActsLeft * averageActSequence.Length + 2;
            var upgradeSequence = new List<float>();
            var upgrades = endOfActUpgrades;
            for (int i = 0; i < normalActsLeft; i++) {
                for (int j = 0; j < averageActSequence.Length; j++) {
                    upgrades += averageActSequence[j];
                    yield return upgrades;
                }
            }
            // Act 4
            upgrades += .5f;
            yield return upgrades;
            if (hasLessonLearned) {
                upgrades += .9f;
            }
            yield return upgrades;
        }
        public IEnumerable<float> ExpectedPowerMetascaling(int floorIndex) {
            // This is only written to support lesson learned...
            var fightFloors = Enumerable.Range(floorIndex, expectedUpgrades.Length - floorIndex).Where(x => x >= nodes.Length || nodes[x].nodeType.IsFight());
            return fightFloors.Select(x => expectedUpgrades[x]).Concat(ExpectedFutureUpgradesDuringFights(expectedUpgrades[^1]));
        }
        public bool ContainsGuaranteedChest() {
            return nodes.Any(x => x.nodeType == NodeType.Chest);
        }
        public void AddEventScore(Evaluation evaluation) {
            for (int i = 0; i < remainingFloors; i++) {
                var nodeType = nodeTypes[i];
                if (nodeType == NodeType.Question) {
                    var eligibleEvents = Evaluators.GetEligibleEventNames(this, i).Select(x => Database.instance.eventDict[x]).Where(x => x.eligible).ToArray();
                    var eventPoolWeight = eligibleEvents.Where(x => !x.shrine).Count();
                    var shrinePoolWeight = eligibleEvents.Where(x => x.shrine).Count();
                    var eventChance = .75f / eventPoolWeight;
                    var shrineChance = .25f / shrinePoolWeight;
                    foreach (var eligibleEvent in eligibleEvents) {
                        var value = EvaluationFunctionReflection.GetEventValueFunctionCached(eligibleEvent.name)(i);
                        var chanceOfThis = eligibleEvent.shrine ? shrineChance : eventChance;
                        evaluation.AddScore(eligibleEvent.reason, value * chanceOfThis);
                    }
                }
            }
        }
        public float ChanceToSurviveAct(int act) {
            if (Save.state.act_num > act) {
                return 1f;
            }
            float totalChance = 1f;
            for (int i = 0; i < remainingFloors; i++) {
                if (Evaluators.FloorToAct(PathIndexToFloorNum(i)) == act) {
                    return totalChance;
                }
                totalChance *= (1f - chanceOfDeath[i]);
            }
            return totalChance;
        }
    }
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
        public override bool Equals([NotNullWhen(true)] object? obj) {
            if (obj is Vector2Int v) {
                return v.x == x && v.y == y;
            }
            return base.Equals(obj);
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
        None,
        Rest,
        Upgrade,
        Key,
        Lift,
    }
    public static class FirstIndexOfExtension {
        public static int FirstIndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate) {
            var index = 0;
            foreach (var item in source) {
                if (predicate(item)) {
                    return index;
                }
                index++;
            }
            return -1;
        }
    }
}
