using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    }
    public class Path {
        public static readonly float MAX_ACCEPTABLE_RISK = .8f;
        public static readonly float MIN_ACCEPTABLE_RISK = .05f;

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
        public Path OffRamp;
        public float[] expectedCardRewards = null;
        public float[] expectedRewardRelics = null;
        public bool[] plannedCardRemove = null;
        public float[] expectedHealth = null;
        public float[] worstCaseHealth = null;
        public string[][] possibleThreats = null;
        public float[] expectedUpgrades = null;
        public FireChoice[] fireChoices = null;
        public float[] expectedShops = null;
        public float score;

        public void InitArrays() {
            var bosses = Save.state.act_num == 3 ? 2 : 1;
            var planningNodes = nodes.Length + bosses;
            expectedGold = new float[planningNodes];
            minPlanningGold = new int[planningNodes];
            expectedCardRewards = new float[planningNodes];
            expectedRewardRelics = new float[planningNodes];
            plannedCardRemove = new bool[planningNodes];
            expectedHealth = new float[planningNodes];
            worstCaseHealth = new float[planningNodes];
            possibleThreats = new string[planningNodes][];
            expectedUpgrades = new float[planningNodes];
            fireChoices = new FireChoice[planningNodes];
            expectedShops = new float[planningNodes];
            InitializePossibleThreats();
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
            possibleThreats[nodes.Length] = new string[] { Save.state.boss };
            if (Save.state.act_num == 3) {
                possibleThreats[nodes.Length + 1] = NextBossOptions().ToArray();
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

        public static IEnumerable<string> NextBossOptions() {
            switch (Save.state.boss) {
                case "Time Eater": {
                    return new string[] {
                        "Awakened One",
                        "Donu Deca",
                    };
                }
                case "Awakened One": {
                    return new string[] {
                        "Time Eater",
                        "Donu Deca",
                    };
                }
                case "Donu Deca": {
                    return new string[] {
                        "Awakened One",
                        "Time Eater",
                    };
                }
                default: {
                    throw new NotImplementedException();
                }
            }
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
            float shopChance = Save.state.event_chances[2];
            float shopsSoFar = 0f;

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
                    case NodeType.Shop: {
                        shopsSoFar++;
                        break;
                    }
                    case NodeType.Question: {
                        fightsSoFar += fightChance;
                        shopsSoFar += shopChance;
                        fightChance = .1f * fightChance + (fightChance + .1f) * (1f - fightChance);
                        shopChance = .03f * shopChance + (shopChance + .03f) * (1f - shopChance);
                        // TODO: shop chance
                        break;
                    }
                }
                expectedCardRewards[i] = fightsSoFar;
                expectedRewardRelics[i] = relicsSoFar;
                expectedShops[i] = shopsSoFar;
            }
            for (int i = nodes.Length; i < expectedCardRewards.Length; i++) {
                expectedCardRewards[i] = expectedCardRewards[i - 1];
                expectedRewardRelics[i] = expectedRewardRelics[i - 1];
                expectedShops[i] = expectedShops[i - 1];
            }
        }

        public void ExpectShopProgression() {
            var gold = (float)Save.state.gold;
            for (int i = 0; i < expectedGold.Length; i++) {
                gold += ExpectedGoldFrom(i);
                expectedGold[i] = gold;
                // TODO: also add cards and relics and such
            }
        }

        public bool NeedsRedKey(int index) {
            return false;
        }

        public bool HealthTooLow(int index) {
            // TODO: this better
            return worstCaseHealth[index] <= 0;
        }

        public bool ShouldRest(int index) {
            for (int i = index + 1; i < expectedHealth.Length; i++) {
                if (i < nodes.Length && nodes[i].nodeType == NodeType.Fire) {
                    return false;
                }
                if (HealthTooLow(i)) {
                    return true;
                }
            }
            return HealthTooLow(nodes.Length - 1);
        }

        public void PlanFires() {
            var upgrades = 0f;
            for (int i = 0; i < nodes.Length; i++) {
                var restValue = MathF.Min(Save.state.max_health * .3f, Save.state.max_health - expectedHealth[i]);
                var upgradeValue = Evaluators.UpgradeValue(this, i);
                if (nodes[i].nodeType != NodeType.Fire) {
                    fireChoices[i] = FireChoice.None;
                    if (Save.state.cards.Any(x => x.id.Equals("LessonLearned"))) {
                        if (nodes[i].nodeType == NodeType.Fight) {
                            upgrades += .9f;
                        }
                        if (nodes[i].nodeType == NodeType.Elite || nodes[i].nodeType == NodeType.MegaElite) {
                            upgrades += .6f;
                        }
                    }
                }
                else if (NeedsRedKey(i)) {
                    fireChoices[i] = FireChoice.Key;
                }
                else if (ShouldRest(i) || restValue > upgradeValue) {
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
            for (int i = 0; i < possibleThreats.Length; i++) {
                var lastExpectedHealth = i == 0 ? Evaluators.GetEffectiveHealth() : expectedHealth[i - 1];
                var lastWorstCaseHealth = i == 0 ? Evaluators.GetEffectiveHealth() : worstCaseHealth[i - 1];
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
                var fractionMultiplier = Lerp.FromUncapped(0, 2, deathLikelihood);
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
            if (root == null) {
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
            switch (shopPlan) {
                case PathShopPlan.MaxRemove: {
                    var removeCost = Save.state.purgeCost;
                    var previousRemoves = index == 0 ? 0 : plannedCardRemove.Take(index).Select(b => b ? 1 : 0).Aggregate((acc, x) => acc + x);
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
            if (index >= nodes.Length) {
                // Boss gold
                return 75f;
            }
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
        public static float ExpectedGoldBroughtToShopsNextAct(float goldNextAct) {
            var chanceOfTwoShops = Lerp.Inverse(300f, 600f, goldNextAct);
            var goldBeforeShop = 120f;
            var goldForSecondShop = 300f;
            return goldNextAct + goldBeforeShop + (goldForSecondShop * chanceOfTwoShops);
        }
        public float ExpectedGoldBroughtToShops() {
            var goldBrought = 0f;
            for (int i = 0; i < nodes.Length; i++) {
                if (nodes[i].nodeType == NodeType.Shop) {
                    goldBrought += expectedGold[i];
                }
            }
            var actThreeGoldToShop = 250;
            var actFourGoldToShop = 250;
            if (Save.state.act_num == 1) {
                goldBrought += ExpectedGoldBroughtToShopsNextAct(expectedGold[^1]);
                goldBrought += actThreeGoldToShop;
                goldBrought += actFourGoldToShop;
            }
            if (Save.state.act_num == 2) {
                goldBrought += ExpectedGoldBroughtToShopsNextAct(expectedGold[^1]);
                goldBrought += actFourGoldToShop;
            }
            if (Save.state.act_num == 3) {
                goldBrought += expectedGold[^1];
            }
            return goldBrought;
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
            var totalShopsLeft = (normalActsLeft * normalActsLeft) + 1;

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
            if (root != null) {
                foreach (var child in root.children) {
                    var pathsThatGoThisWay = paths.Where(x => x.nodes[0] == child);
                    var safestPathThisWay = pathsThatGoThisWay.OrderBy(x => x.Risk).First();
                    foreach (var path in pathsThatGoThisWay) {
                        path.OffRamp = safestPathThisWay;
                    }
                }
            }
            return paths;
        }
        public void MergeScoreWithOffRamp() {
            float riskT = Lerp.InverseUncapped(MIN_ACCEPTABLE_RISK, MAX_ACCEPTABLE_RISK, Risk);
            float offRampRiskT = Lerp.InverseUncapped(MIN_ACCEPTABLE_RISK, MAX_ACCEPTABLE_RISK, OffRamp.Risk);
            riskT = (riskT * MathF.E * 2) - MathF.E;
            offRampRiskT = (offRampRiskT * MathF.E * 2) - MathF.E;
            float riskRelevance = PumpernickelMath.Sigmoid(riskT - offRampRiskT);
            score = Lerp.From(score, OffRamp.score, riskRelevance);
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
        public IEnumerable<float> ExpectedUpgradesDuringFights() {
            var fightFloors = Enumerable.Range(0, expectedUpgrades.Length).Where(x => x >= nodes.Length || nodes[x].nodeType.IsFight());
            return fightFloors.Select(x => expectedUpgrades[x]).Concat(ExpectedFutureUpgradesDuringFights(expectedUpgrades[^1]));
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