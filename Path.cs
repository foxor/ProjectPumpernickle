﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ProjectPumpernickle {
    public class NodeSequence {
        public List<MapNode> nodes = new List<MapNode>();
        public List<FireChoice> fireChoices = new List<FireChoice>();
        public int jumps;
        public bool invalidJump;
        public bool invalidFireWalk;
        public bool IsValid() {
            var availableJumps = Evaluators.WingedBootsChargesLeft();
            if (jumps > availableJumps) {
                return false;
            }
            if (invalidJump) {
                return false;
            }
            if (invalidFireWalk) {
                return false;
            }
            return true;
        }
    }
    public class Path {
        public static List<FireChoice> availableFireOptions;
        public static bool hasWingBoots;

        public int elites;
        public bool hasMegaElite;
        public MapNode[] nodes = null;
        public float[] expectedGold = null;
        public int[] minPlanningGold = null;
        public PathShopPlan shortTermShopPlan;
        public Dictionary<string, float> Threats = null;
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
        public NodeType[] nodeTypes = null;
        public float[] expectedMaxHealth = null;
        public float[] projectedDefensivePower = null;
        public float[] expectedHealthLoss = null;
        public float[] expectedPotionsAdded = null;
        public float[] expectedPotionsSpent = null;
        public long pathId;
        public int jumps;
        public float chanceToSurviveAct;
        public float[] expectedShopRelics = null;
        protected static void CheckPathRelic(string relicId, ref bool hasWingBoots, ref List<FireChoice> fireChoices) {
            if (relicId.Equals("WingedGreaves")) {
                hasWingBoots = true;
            }
        }
        public static long CountNodeSequences(List<RewardOption> rewardOptions) {
            availableFireOptions = new List<FireChoice>() {
                    FireChoice.Rest,
                    FireChoice.Upgrade,
                    FireChoice.Key,
                };
            hasWingBoots = Save.state.relics.Contains("WingedGreaves");
            foreach (var relic in rewardOptions.Where(x => x.rewardType == RewardType.Relic).Select(x => x.values).Merge()) {
                CheckPathRelic(relic, ref hasWingBoots, ref availableFireOptions);
            }
            var root = Save.state.GetCurrentNode();
            return CountNodeSequences(root);
        }
        public static long CountNodeSequences(MapNode root) {
            if (root == null) {
                return 1;
            }
            if (root.totalChildOptions.HasValue) {
                return root.totalChildOptions.Value;
            }
            IEnumerable<MapNode> nextOptions = root.children;
            if (hasWingBoots && root.position.y < PumpernickelSaveState.MAX_MAP_Y - 1) {
                nextOptions = Enumerable.Range(0, PumpernickelSaveState.MAX_MAP_X)
                    .Select(x => Save.state.map[Save.state.act_num, x, root.position.y + 1])
                    .Where(x => x != null);
            }
            var subSequenceCount = nextOptions.Select(CountNodeSequences).Sum();
            if (!nextOptions.Any()) {
                subSequenceCount = 1;
            }
            if (root.nodeType == NodeType.Fire) {
                subSequenceCount *= availableFireOptions.Count;
            }
            root.totalChildOptions = subSequenceCount;
            return subSequenceCount;
        }
        public static NodeSequence BuildNodeSequence(long pathIndex, MapNode root, bool skipFirstNode = false) {
            var nextOptions = root.children;
            if (hasWingBoots && root.position.y < PumpernickelSaveState.MAX_MAP_Y - 1) {
                nextOptions = Enumerable.Range(0, PumpernickelSaveState.MAX_MAP_X)
                    .Select(x => Save.state.map[Save.state.act_num, x, root.position.y + 1])
                    .Where(x => x != null)
                    .ToList();
            }
            var fireOptionIndex = -1;
            if (root.nodeType == NodeType.Fire) {
                fireOptionIndex = (int)(pathIndex % availableFireOptions.Count);
                pathIndex /= availableFireOptions.Count;
            }
            var optionCount = nextOptions.Count;
            if (optionCount == 0) {
                var r = new NodeSequence();
                r.nodes.Add(root);
                if (root.nodeType == NodeType.Fire) {
                    r.fireChoices.Add(availableFireOptions[fireOptionIndex]);
                }
                if (pathIndex != 0) {
                    Console.WriteLine();
                }
                return r;
            }
            int childIndex = 0;
            var residual = pathIndex;
            for (; childIndex < nextOptions.Count; childIndex++) {
                if (nextOptions[childIndex].totalChildOptions > residual) {
                    break;
                }
                residual -= nextOptions[childIndex].totalChildOptions.Value;
            }
            var recur = BuildNodeSequence(residual, nextOptions[childIndex]);
            if (!skipFirstNode) {
                recur.nodes.Add(root);
                if (root.nodeType == NodeType.Fire) {
                    recur.fireChoices.Add(availableFireOptions[fireOptionIndex]);
                }
            }
            if (hasWingBoots && !root.children.Contains(nextOptions[childIndex])) {
                recur.jumps++;
                recur.invalidJump |= root.children.Any(x => x.nodeType == nextOptions[childIndex].nodeType);
            }
            if (!nextOptions[childIndex].children.Any() && root.children.Any(x => x.position.x < nextOptions[childIndex].position.x)) {
                recur.invalidFireWalk = true;
            }
            return recur;
        }
        public static Path BuildPath(NodeSequence nodeSequence, long pathId) {
            Path path = new Path();
            path.pathId = pathId;
            path.jumps = nodeSequence.jumps;
            nodeSequence.nodes.Reverse();
            path.nodes = nodeSequence.nodes.ToArray();
            nodeSequence.fireChoices.Reverse();
            path.InitFireChoices(nodeSequence.fireChoices.ToArray());
            return path;
        }

        public void ExplorePath() {
            // Basic setup stuff
            InitArrays();
            InitNodeTypes();
            AssumeFutureActUpgrades();
            InitializePossibleThreats();
            FindBasicProperties();
            ExpectGoldProgression();

            // Make plans to get stronger
            ChooseShortTermShopPlan();
            ExpectShopProgression();
            ExpectProgression();
            ProjectDefensivePower();

            // Final health estimation for risk etc.
            SimulateHealthEvolution(healthFloor: 1);
            NormalizeThreats();
        }

        public static Path Copy(Path path) {
            Path r = new Path();
            r.elites = path.elites;
            r.hasMegaElite = path.hasMegaElite;
            r.expectedHealthLoss = path.expectedHealthLoss;
            r.shortTermShopPlan = path.shortTermShopPlan;
            r.remainingFloors = path.remainingFloors;
            r.EndOfActPath = path.EndOfActPath;
            r.chanceToSurviveAct = path.chanceToSurviveAct;

            r.nodes = path.nodes.ToArray();
            r.expectedGold = path.expectedGold?.ToArray();
            r.minPlanningGold = path.minPlanningGold?.ToArray();
            r.expectedCardRewards = path.expectedCardRewards?.ToArray();
            r.expectedRewardRelics = path.expectedRewardRelics?.ToArray();
            r.plannedCardRemove = path.plannedCardRemove?.ToArray();
            r.expectedHealth = path.expectedHealth?.ToArray();
            r.worstCaseHealth = path.worstCaseHealth?.ToArray();
            r.possibleThreats = path.possibleThreats?.ToArray();
            r.expectedUpgrades = path.expectedUpgrades?.ToArray();
            r.fireChoices = path.fireChoices.ToArray();
            r.expectedShops = path.expectedShops?.ToArray();
            r.fightChance = path.fightChance?.ToArray();
            r.chanceOfDeath = path.chanceOfDeath?.ToArray();
            r.nodeTypes = path.nodeTypes?.ToArray();
            r.expectedMaxHealth = path.expectedMaxHealth?.ToArray();
            r.projectedDefensivePower = path.projectedDefensivePower?.ToArray();
            r.expectedHealthLoss = path.expectedHealthLoss?.ToArray();
            r.expectedPotionsAdded = path.expectedPotionsAdded?.ToArray();
            r.expectedPotionsSpent = path.expectedPotionsSpent?.ToArray();
            r.expectedShopRelics = path.expectedShopRelics?.ToArray();

            return r;
        }

        public static int PathIndexToFloorNum(int index) {
            return Save.state.floor_num + 1 + index;
        }
        public static int FloorNumToPathIndex(int floor) {
            return floor - Save.state.floor_num - 1;
        }

        public void InitArrays() {
            remainingFloors = 56 - Save.state.floor_num;
            Threats = new Dictionary<string, float>();
            expectedGold = new float[remainingFloors];
            plannedCardRemove = new bool[remainingFloors];
            minPlanningGold = new int[remainingFloors];
            expectedCardRewards = new float[remainingFloors];
            expectedRewardRelics = new float[remainingFloors];
            expectedHealth = new float[remainingFloors];
            worstCaseHealth = new float[remainingFloors];
            possibleThreats = new Encounter[remainingFloors][];
            expectedUpgrades = new float[remainingFloors];
            expectedShops = new float[remainingFloors];
            fightChance = new float[remainingFloors];
            chanceOfDeath = new float[remainingFloors];
            nodeTypes = new NodeType[remainingFloors];
            expectedMaxHealth = new float[remainingFloors];
            projectedDefensivePower = new float[remainingFloors];
            expectedHealthLoss = new float[remainingFloors];
            expectedPotionsAdded = new float[remainingFloors];
            expectedPotionsSpent = new float[remainingFloors];
            expectedShopRelics = new float[remainingFloors];
        }

        public void InitFireChoices(FireChoice[] fireChoices) {
            remainingFloors = 56 - Save.state.floor_num;
            this.fireChoices = new FireChoice[remainingFloors];
            var choiceIndex = 0;
            for (int i = 0; i < nodes.Length; i++) {
                if (nodes[i].nodeType == NodeType.Fire) {
                    this.fireChoices[i] = fireChoices[choiceIndex++];
                }
                else {
                    this.fireChoices[i] = FireChoice.None;
                }
            }
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

        public void AssumeFutureActUpgrades() {
            for (int i = 0; i < fireChoices.Length; i++) {
                if (nodeTypes[i] == NodeType.Fire && fireChoices[i] == FireChoice.None) {
                    fireChoices[i] = FireChoice.Upgrade;
                }
            }
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
                projectedDefensivePower[i] = 1f;
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
                    case NodeType.Animation:
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
                        else if (act == Save.state.act_num && Save.state.boss != null) {
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
                        "Donu and Deca",
                    }.Select(x => Database.instance.encounterDict[x]);
                }
                case "Awakened One": {
                    return new string[] {
                        "Time Eater",
                        "Donu and Deca",
                    }.Select(x => Database.instance.encounterDict[x]);
                }
                case "Donu and Deca": {
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
        public static readonly float AVG_CARD_PRICE = 67.5f; // avg common + uncommon
        public static readonly float AVG_POTION_PRICE = 54.5f; // avg common only
        public static readonly float AVG_RELIC_PRICE = 190f; // more likely to buy common and shop
        public static readonly Vector3 EARLY_GAME_SHOP_PREFERENCE = new Vector3(.7f, .3f, 0f);
        public static readonly Vector3 LATE_GAME_SHOP_PREFERENCE = new Vector3(.1f, .6f, .3f);
        public static readonly float CARD_SPEND_CAP = 180f;
        public static readonly float POTION_SPEND_CAP = AVG_POTION_PRICE * 2f;
        public void ExpectedShopProgression(int i, out float cards, out float potions, out float relics, bool limitPotionsToOpenSlots) {
            var potionMax = limitPotionsToOpenSlots ? Save.state.EmptyPotionSlots() : int.MaxValue;
            var initialGold = i > 0 ? expectedGold[i - 1] : Save.state.gold;
            var spend = initialGold - expectedGold[i];
            var preferences = Lerp.From(EARLY_GAME_SHOP_PREFERENCE, LATE_GAME_SHOP_PREFERENCE, Evaluators.PercentGameOver(Path.PathIndexToFloorNum(i)));
            var preferredSpend = preferences * spend;
            if (preferredSpend.X > CARD_SPEND_CAP) {
                var potionRelicRatio = preferences.Y / preferences.Z;
                var residual = preferredSpend.X - CARD_SPEND_CAP;
                preferredSpend.X = CARD_SPEND_CAP;
                preferredSpend.Y += residual * potionRelicRatio;
                preferredSpend.Z += residual * (1 - potionRelicRatio);
            }
            if (preferredSpend.Y > POTION_SPEND_CAP) {
                var residual = preferredSpend.Y - POTION_SPEND_CAP;
                preferredSpend.Y = POTION_SPEND_CAP;
                preferredSpend.Z += residual;
                // Slight bugabo here: this doesn't redistribute towards cards.
                // Usually not a problem, since shop cards are generally only important early.
            }
            cards = preferredSpend.X / AVG_CARD_PRICE;
            potions = preferredSpend.Y / AVG_POTION_PRICE;
            relics = preferredSpend.Z / AVG_RELIC_PRICE;
            if (potions > potionMax) {
                var excess = potions - potionMax;
                potions = potionMax;
                var residual = excess * AVG_POTION_PRICE;
                relics += residual / AVG_RELIC_PRICE;
            }
        }
        public static readonly float SHOP_CARD_VALUE_FACTOR = 1.3f;
        public static readonly float CARD_PICK_RATE_OVER_BOWL = .3f;
        public static readonly float FEED_HIT_RATE = .8f;
        public static readonly float PER_QUESTION_FIGHT_CHANCE = 0.1f;
        public static readonly float PER_QUESTION_SHOP_CHANCE = 0.03f;
        public void ExpectProgression() {
            // Early in the run, every card reward is a huge impact on the deck quality.
            // We need this so that we know a floor 10 elite won't kill us
            float fightsSoFar = 0f;
            float rewardRelicsSoFar = 0f;
            float shopRelicsSoFar = 0f;
            float potionAddsSoFar = 0f;
            var potionSlots = Save.state.relics.Contains("Potion Belt") ? 4 : 2;
            float residualPotions = potionSlots - Save.state.EmptyPotionSlots();
            float potionChance = (40 + Save.state.potion_chance) * 0.01f;
            float cardRewardsSoFar = 0f;
            float floorFightChance = Save.state.event_chances == null ? PER_QUESTION_FIGHT_CHANCE : Save.state.event_chances[1];
            float shopChance = Save.state.event_chances == null ? PER_QUESTION_SHOP_CHANCE :Save.state.event_chances[2];
            float shopsSoFar = 0f;
            float upgrades = 0f;
            var hasSingingBowl = Save.state.relics.Contains("Singing Bowl");
            var hasPrayerWheel = Save.state.relics.Contains("Prayer Wheel");
            var feed = Save.state.cards.Where(x => x.id.Equals("Feed"));
            var hasFeed = feed.Any();
            var feedHealth = hasFeed ? feed.Select(x => x.upgrades > 0 ? 5f : 3f).Max() : 0f;
            var limitPotionsToOpenSlots = true;
            if (Save.state.cards.Any(x => x.id.Equals("Armaments")) && feedHealth == 3f) {
                feedHealth = 4f; // 50% chance to upgrade
            }
            if (Save.state.cards.Any(x => x.id.Equals("Apotheosis")) && feedHealth == 3f) {
                feedHealth = 4.8f;
            }
            var maxHp = (float)Save.state.max_health;

            for (int i = 0; i < expectedCardRewards.Length; i++) {
                float potionsSpentThisFloor = 0f;
                float chancePotionBarFull = MathF.Max(residualPotions - potionSlots + 1, 0);
                fightChance[i] = 0f;
                if (Evaluators.IsFirstFloorOfAnAct(Path.PathIndexToFloorNum(i))) {
                    potionChance = 0.4f;
                    floorFightChance = PER_QUESTION_FIGHT_CHANCE;
                    shopChance = PER_QUESTION_SHOP_CHANCE;
                }
                switch (nodeTypes[i]) {
                    case NodeType.MegaElite:
                    case NodeType.Elite:
                    case NodeType.Boss:
                    case NodeType.Fight: {
                        if (nodeTypes[i] == NodeType.Elite || nodeTypes[i] == NodeType.MegaElite) {
                            rewardRelicsSoFar++;
                            if (residualPotions >= 1f) {
                                potionsSpentThisFloor++;
                                residualPotions--;
                            }
                        }
                        else if (nodeTypes[i] == NodeType.Boss) {
                            potionsSpentThisFloor = residualPotions;
                            residualPotions = 0f;
                        }
                        else {
                            potionsSpentThisFloor += chancePotionBarFull;
                            residualPotions -= chancePotionBarFull;
                        }
                        fightsSoFar++;
                        cardRewardsSoFar++;
                        if (hasPrayerWheel && nodeTypes[i] == NodeType.Fight) {
                            cardRewardsSoFar++;
                        }
                        fightChance[i] = 1f;
                        if (hasFeed) {
                            maxHp += FEED_HIT_RATE * feedHealth;
                        }
                        potionAddsSoFar += potionChance;
                        residualPotions += potionChance;
                        if (potionChance > .51f) {
                            potionChance -= .1f;
                        }
                        if (potionChance < .49f) {
                            potionChance += .1f;
                        }
                        limitPotionsToOpenSlots = false;
                        break;
                    }
                    case NodeType.Shop: {
                        ExpectedShopProgression(i, out var cardRewards, out var potions, out var relics, limitPotionsToOpenSlots);
                        shopsSoFar++;
                        cardRewardsSoFar += cardRewards * SHOP_CARD_VALUE_FACTOR;
                        shopRelicsSoFar += relics;
                        potionAddsSoFar += potions;
                        residualPotions += potions;
                        break;
                    }
                    case NodeType.Question: {
                        fightsSoFar += floorFightChance;
                        if (hasFeed) {
                            maxHp += FEED_HIT_RATE * feedHealth * floorFightChance;
                        }
                        shopsSoFar += shopChance;
                        cardRewardsSoFar += floorFightChance;
                        if (hasPrayerWheel) {
                            cardRewardsSoFar += floorFightChance;
                        }
                        fightChance[i] = floorFightChance;
                        floorFightChance = PER_QUESTION_FIGHT_CHANCE * floorFightChance + (floorFightChance + PER_QUESTION_FIGHT_CHANCE) * (1f - floorFightChance);
                        shopChance = PER_QUESTION_SHOP_CHANCE * shopChance + (shopChance + PER_QUESTION_SHOP_CHANCE) * (1f - shopChance);
                        potionAddsSoFar += floorFightChance * potionChance;
                        residualPotions += floorFightChance * potionChance;
                        // potionChance could now be the same, higher, or lower.  We'll leave it the same.
                        break;
                    }
                    case NodeType.Chest:
                    case NodeType.BossChest: {
                        rewardRelicsSoFar++;
                        break;
                    }
                    case NodeType.Fire: {
                        if (fireChoices[i] == FireChoice.Upgrade) {
                            upgrades++;
                        }
                        break;
                    }
                }
                expectedCardRewards[i] = cardRewardsSoFar;
                expectedRewardRelics[i] = rewardRelicsSoFar;
                expectedShops[i] = shopsSoFar;
                expectedMaxHealth[i] = maxHp;
                expectedPotionsAdded[i] = potionAddsSoFar;
                expectedPotionsSpent[i] = potionsSpentThisFloor;
                expectedUpgrades[i] = upgrades;
                expectedShopRelics[i] = shopRelicsSoFar;
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
            for (int i = 0; i < expectedGold.Length; i++) {
                var nodeType = nodeTypes[i];
                if (nodeType == NodeType.Shop) {
                    var spent = ExpectedGoldSpend(i);
                    for (int j = i; j < expectedGold.Length; j++) {
                        expectedGold[j] -= spent;
                    }
                }
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
        public static float UNACCEPTABLE_RISK = 0.20f;
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
        public static readonly float MAX_NORMAL_SHOP_POWER_SPIKE = 1.3f;
        public static readonly float MAX_FIX_SHOP_POWER_SPIKE = 1.5f;
        public float ShopPowerSpike(int index) {
            var startingGold = index == 0 ? Save.state.gold : expectedGold[index - 1];
            // You don't get almost any value until you bring at least 50 gold
            var goldSpent = startingGold - expectedGold[index] - 50f;
            return shortTermShopPlan switch {
                PathShopPlan.NormalShop => Lerp.From(1f, MAX_NORMAL_SHOP_POWER_SPIKE, goldSpent / 250f),
                PathShopPlan.FixFight => Lerp.From(1f, MAX_FIX_SHOP_POWER_SPIKE, goldSpent / 70f),
                _ => 1.05f,
            };
        }
        public static readonly float RELIC_POWER_SPIKE = 1.05f;
        public static readonly float REGRESSION_RATE = .4f;
        public static readonly float REGRESSION_TARGET = 0.95f;
        public void AdjustDefensivePowerForNodeType(float previousPower, NodeType nodeType, int index, int easyPoolLeft, out float floorPower, out float residualPower) {
            residualPower = (previousPower * (1 - REGRESSION_RATE)) + (REGRESSION_TARGET * REGRESSION_RATE);
            switch (nodeType) {
                case NodeType.Shop: {
                    // Power spike after shop, particularly early shops and those where we buy potions
                    residualPower *= ShopPowerSpike(index);
                    break;
                }
                case NodeType.Fire: {
                    if (fireChoices[index] == FireChoice.Upgrade) {
                        residualPower *= Evaluators.UpgradePowerMultiplier(this, index);
                    }
                    break;
                }
                case NodeType.Elite:
                case NodeType.MegaElite:
                case NodeType.Chest: {
                    // Power spike from relic
                    residualPower *= RELIC_POWER_SPIKE;
                    break;
                }
                default: {
                    break;
                }
            }
            switch (nodeType) {
                case NodeType.Fight:
                case NodeType.Elite:
                case NodeType.MegaElite: {
                    // Power slightly raised if the pool is "old"
                    floorPower = residualPower * Evaluators.PoolAge(nodeType, easyPoolLeft > 0, PathIndexToFloorNum(index));
                    break;
                }
                default: {
                    floorPower = residualPower;
                    break;
                }
            }
        }
        public static readonly float AVERAGE_POTION_DEFENSIVE_VALUE = 0.2f;
        public void AdjustDefensivePowerForPotion(int index, ref float floorPower) {
            // Sometimes you save ghost in a jar for the heart, this is wrong long term
            var assumeCurrentPotion = index <= 3;
            var consumed = expectedPotionsSpent[index];
            if (assumeCurrentPotion) {
                var potions = Save.state.Potions().ToArray();
                var weight = potions.Length / consumed;
                foreach (var potion in potions) {
                    floorPower += weight * potion switch {
                        "PowerPotion" => .5f,
                        _ => AVERAGE_POTION_DEFENSIVE_VALUE,
                    };
                }
            }
            else {
                floorPower += consumed * AVERAGE_POTION_DEFENSIVE_VALUE;
            }
        }
        public void ProjectDefensivePower() {
            var easyPoolLeft = Save.state.act_num == 1 ? 3 : 2;
            var lastAct = Save.state.act_num;
            easyPoolLeft = Math.Max(0, easyPoolLeft - Save.state.monsters_killed);
            float residualPower = 0f;
            for (int i = 0; i < projectedDefensivePower.Length; i++) {
                var floor = PathIndexToFloorNum(i);
                var act = Evaluators.FloorToAct(floor);
                if (act != lastAct) {
                    easyPoolLeft = 2;
                    lastAct = act;
                }
                if (i == 0) {
                    projectedDefensivePower[i] = FightSimulator.EstimateDefensivePower();
                    residualPower = projectedDefensivePower[i];
                }
                else {
                    AdjustDefensivePowerForNodeType(residualPower, nodeTypes[i], i, easyPoolLeft, out projectedDefensivePower[i], out residualPower);
                    AdjustDefensivePowerForPotion(i, ref projectedDefensivePower[i]);
                }
                if (nodeTypes[i] == NodeType.Fight) {
                    easyPoolLeft--;
                }
            }
        }

        public void SimulateHealthEvolution(float powerMultiplier = 1f, int floorsFromNow = 0, int healthFloor = 15) {
            var currentDamagePerTurn = FightSimulator.EstimateDamagePerTurn();
            Threats.Clear();
            var maxHeal = Evaluators.MaxHealing();
            for (int i = floorsFromNow; i < possibleThreats.Length; i++) {
                var estimatedDamageThisFloor = FightSimulator.ProjectDamageForFutureFloor(currentDamagePerTurn, i);
                var lastExpectedHealth = i == 0 ? Evaluators.GetCurrentEffectiveHealth() : expectedHealth[i - 1];
                if (fireChoices[i] == FireChoice.Rest) {
                    lastExpectedHealth += Save.state.max_health * .3f;
                    lastExpectedHealth = MathF.Min(Save.state.max_health, lastExpectedHealth);
                }
                var floor = PathIndexToFloorNum(i);
                if (Evaluators.FloorsIntoAct(floor) == 0) {
                    var missing = Save.state.max_health - lastExpectedHealth;
                    var healing = (int)((missing * .75) + 0.9999f);
                    lastExpectedHealth += healing;
                }
                float averageExpectedHealthLoss = 0f;
                float worstWorstCaseHealthLoss = 0f;
                var totalWeight = possibleThreats[i].Select(x => x.weight).Sum();
                chanceOfDeath[i] = 0f;
                foreach (var possibleEncounter in possibleThreats[i]) {
                    var expectedHealthLoss = FightSimulator.SimulateFight(possibleEncounter, PathIndexToFloorNum(i), estimatedDamageThisFloor, projectedDefensivePower[i] * powerMultiplier);
                    expectedHealthLoss = MathF.Max(expectedHealthLoss, -maxHeal);
                    var chanceOfThis = (possibleEncounter.weight * 1f / totalWeight) * fightChance[i];
                    averageExpectedHealthLoss += expectedHealthLoss * chanceOfThis;
                    AssessThreat(possibleEncounter, i, expectedHealthLoss, possibleEncounter.medianWorstCaseHealthLoss, lastExpectedHealth, chanceOfThis);
                    worstWorstCaseHealthLoss = Math.Max(worstWorstCaseHealthLoss, possibleEncounter.medianWorstCaseHealthLoss);
                }
                var marginalPotions = expectedPotionsAdded[i] - (i == 0 ? 0 : expectedPotionsAdded[i - 1]);
                var expectedPotionHealth = Evaluators.RandomPotionHealthValue() * marginalPotions;
                expectedHealthLoss[i] = averageExpectedHealthLoss;
                expectedHealth[i] = Math.Max(lastExpectedHealth - averageExpectedHealthLoss + expectedPotionHealth, healthFloor);
                // Worst case doesn't continue to stack every floor
                // You start at the expected health, and then ONE bad thing happens, not the worst case every floor
                worstCaseHealth[i] = lastExpectedHealth - worstWorstCaseHealthLoss;
            }
            SetChanceToSurviveAct();
        }
        public static float THREAT_PRIORITIZATION_POWER = 4f;
        public void NormalizeThreats() {
            foreach (var threatKey in Threats.Keys) {
                if (threatKey.Equals("3 Darklings")) {
                    // They have double threat because they're in 2 pools
                    Threats[threatKey] /= 2f;
                }
                Threats[threatKey] = MathF.Pow(Threats[threatKey], THREAT_PRIORITIZATION_POWER);
            }
            var totalThreat = Threats.Select(x => x.Value).Sum();
            foreach (var threatKey in Threats.Keys) {
                Threats[threatKey] /= totalThreat;
            }
        }
        public static readonly float CHANCE_OF_WORST_CASE = 0.01f;
        // From wolfram alpha
        public static readonly float MULTIPLIER_FOR_FIVE_PERCENT = -2.94444f;
        public static readonly float MULTIPLIER_FOR_NINTEY_FIVE_PERCENT = -MULTIPLIER_FOR_FIVE_PERCENT;

        public static readonly float NEXT_FLOOR_DAMAGE_THREAT_CAP = 5f;
        public static readonly float DISTANCE_INTO_FUTURE_DIVISOR = 5f;
        public static readonly float UPCOMING_DEATH_THREAT_MULTIPLIER = 8f;
        protected void AssessThreat(Encounter threat, int floorIndex, float expectedDamage, float worstCaseDamage, float expectedHealth, float chanceOfThis) {
            Threats.TryAdd(threat.id, 0f);
            var distanceFactor = 1f / (1 + (floorIndex / DISTANCE_INTO_FUTURE_DIVISOR));
            var damageThreatCap = NEXT_FLOOR_DAMAGE_THREAT_CAP * distanceFactor;
            var healthFraction = expectedDamage / Save.state.max_health;
            Threats[threat.id] += healthFraction * chanceOfThis * damageThreatCap;
            if (worstCaseDamage <= 0f) {
                throw new Exception("Nonsense expected damage! Data bug with: " + threat.id + " ?");
            }
            if (worstCaseDamage < expectedDamage) {
                var temp = worstCaseDamage;
                worstCaseDamage = expectedDamage;
                expectedDamage = temp;
            }
            var worstCaseResidualHealth = expectedHealth - worstCaseDamage;
            var damageRange = worstCaseDamage - threat.medianExpectedHealthLoss;
            var bestCaseResidualHealth = expectedHealth - (expectedDamage - damageRange);
            var deathHealth = 0f;
            var deathParam = Lerp.InverseUncapped(worstCaseResidualHealth, bestCaseResidualHealth, deathHealth);
            var sigmoidX = Lerp.FromUncapped(MULTIPLIER_FOR_FIVE_PERCENT, MULTIPLIER_FOR_NINTEY_FIVE_PERCENT, deathParam);
            var deathLikelihood = PumpernickelMath.Sigmoid(sigmoidX);
            var upcomingDeathMultiplier = UPCOMING_DEATH_THREAT_MULTIPLIER * distanceFactor;
            Threats[threat.id] += deathLikelihood * chanceOfThis * upcomingDeathMultiplier;
            chanceOfDeath[floorIndex] += deathLikelihood * chanceOfThis;
            if (chanceOfDeath[floorIndex] > .99f) {
                //throw new Exception("Bot thinks it's 100% chance to die.  This causes problems usually");
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
                    var floorsTillNextShop = nodeTypes.Skip(index + 1).TakeWhile(x => x != NodeType.Shop).Count();
                    var nextShopIndex = index + floorsTillNextShop + 1;
                    if (remainingShops <= 1) {
                        return expectedGoldForFloor;
                    }
                    else if (remainingShops == 2 || floorsTillNextShop < 10) {
                        var totalGold = expectedGold[nextShopIndex - 1];
                        var desiredSpend = totalGold / 2f;
                        return MathF.Min(desiredSpend, expectedGoldForFloor);
                    }
                    else {
                        return Math.Min(expectedGoldForFloor - 15f, 400f);
                    }
                }
                case PathShopPlan.FixFight: {
                    return Math.Min(expectedGoldForFloor, 500f);
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
            var act = Evaluators.FloorToAct(PathIndexToFloorNum(index));
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
                    if (act > 2) {
                        return 0;
                    }
                    return 75;
                }
                case NodeType.Question: {
                    // TODO
                    return 0f;
                }
            }
            return 0f;
        }
        public static readonly float NORMAL_SHOP_BASELINE = 100f;
        public float NormalShopPoint() {
            return NORMAL_SHOP_BASELINE;
        }
        public float FixShopPoint() {
            // How valuable is it to buy an attack card or potion to fix a scary fight?
            return Save.state.floor_num <= 10 ? NORMAL_SHOP_BASELINE * 2f : 0f;
        }
        public float HuntForShopRelicPoint() {
            return 0f;
        }
        public float CardRemovePoints() {
            return 0f;
        }
        public void ChooseShortTermShopPlan() {
            var removeValue = CardRemovePoints();
            var fixValue = FixShopPoint();
            var normalValue = NormalShopPoint();
            var huntValue = HuntForShopRelicPoint();
            var highest = new float[] {removeValue, fixValue, normalValue, huntValue}.Max();
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
        }
        public IEnumerable<float> ExpectedGoldBroughtToShops() {
            for (int i = 0; i < nodeTypes.Length - 1; i++) {
                if (nodeTypes[i + 1] == NodeType.Shop) {
                    yield return expectedGold[i];
                }
            }
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
        public void SetChanceToSurviveAct() {
            float totalChance = 1f;
            for (int i = 0; i < remainingFloors; i++) {
                if (Evaluators.FloorToAct(PathIndexToFloorNum(i)) == Save.state.act_num + 1) {
                    chanceToSurviveAct = totalChance;
                    return;
                }
                totalChance *= (1f - chanceOfDeath[i]);
            }
            chanceToSurviveAct = totalChance;
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
        COUNT,
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
