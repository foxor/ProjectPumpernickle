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
        Lift,
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
        public Encounter[][] possibleThreats = null;
        public float[] expectedUpgrades = null;
        public FireChoice[] fireChoices = null;
        public float[] expectedShops = null;
        public float[] scores = new float[(byte)ScoreReason.COUNT];
        public float[] fightChance = null;
        public int planningNodes;
        public bool EndOfActPath;

        public void AddScore(ScoreReason reason, float delta) {
            scores[(byte)reason] += delta;
        }

        public float score {
            get {
                return scores.Sum();
            }
        }

        public void InitArrays() {
            var bosses = Save.state.act_num == 3 ? 5 : 1;
            planningNodes = nodes.Length + bosses;
            expectedGold = new float[planningNodes];
            minPlanningGold = new int[planningNodes];
            expectedCardRewards = new float[planningNodes];
            expectedRewardRelics = new float[planningNodes];
            plannedCardRemove = new bool[planningNodes];
            expectedHealth = new float[planningNodes];
            worstCaseHealth = new float[planningNodes];
            possibleThreats = new Encounter[planningNodes][];
            expectedUpgrades = new float[planningNodes];
            fireChoices = new FireChoice[planningNodes];
            expectedShops = new float[planningNodes];
            fightChance = new float[planningNodes];
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
            for (int i = 0; i < possibleThreats.Length; i++) {
                switch (GetNodeType(i)) {
                    case NodeType.Shop:
                    case NodeType.Fire:
                    case NodeType.Chest: {
                        possibleThreats[i] = new Encounter[0];
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
                        // If we hit a question mark fight, that doesn't change the total number of easy pool fights we do
                        possibleThreats[i] = HardPool();
                        // TODO: add event fights
                        break;
                    }
                    // case NodeType.Boss is handled below
                }
            }
            possibleThreats[nodes.Length] = new Encounter[] { Database.instance.encounterDict[Save.state.boss] };
            if (Save.state.act_num == 3) {
                possibleThreats[nodes.Length + 0] = NextBossOptions().ToArray();
                possibleThreats[nodes.Length + 1] = new Encounter[0];
                possibleThreats[nodes.Length + 2] = new Encounter[0];
                possibleThreats[nodes.Length + 3] = new Encounter[0];
                possibleThreats[nodes.Length + 4] = new Encounter[] { Database.instance.encounterDict["Corrupt Heart"] };
            }
        }

        public static Encounter[] EasyPool() {
            return Database.instance.EasyPools[Save.state.act_num - 1];
        }

        public static Encounter[] HardPool() {
            return Database.instance.HardPools[Save.state.act_num - 1];
        }

        public static Encounter[] EliteOptions() {
            return Database.instance.Elites[Save.state.act_num - 1];
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
            return EliteOptions().Where(x => x.act == Save.state.act_num && x.pool.Equals("elite") && x.id != cantBe);
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

        public void ExpectBasicProgression(int startingCardRewards) {
            // Early in the run, every card reward is a huge impact on the deck quality.
            // We need this so that we know a floor 10 elite won't kill us
            float fightsSoFar = 0f;
            float relicsSoFar = 0f;
            float cardRewardsSoFar = startingCardRewards;
            float floorFightChance = Save.state.event_chances[1];
            float shopChance = Save.state.event_chances[2];
            float shopsSoFar = 0f;

            for (int i = 0; i < expectedCardRewards.Length; i++) {
                fightChance[i] = 0f;
                switch (GetNodeType(i)) {
                    case NodeType.Elite: {
                        fightsSoFar++;
                        relicsSoFar++;
                        cardRewardsSoFar++;
                        fightChance[i] = 1f;
                        break;
                    }
                    case NodeType.MegaElite: {
                        fightsSoFar++;
                        cardRewardsSoFar++;
                        relicsSoFar++;
                        fightChance[i] = 1f;
                        break;
                    }
                    case NodeType.Fight: {
                        fightsSoFar++;
                        cardRewardsSoFar++;
                        fightChance[i] = 1f;
                        break;
                    }
                    case NodeType.Shop: {
                        shopsSoFar++;
                        break;
                    }
                    case NodeType.Question: {
                        fightsSoFar += floorFightChance;
                        shopsSoFar += shopChance;
                        cardRewardsSoFar += floorFightChance;
                        fightChance[i] = floorFightChance;
                        floorFightChance = .1f * floorFightChance + (floorFightChance + .1f) * (1f - floorFightChance);
                        shopChance = .03f * shopChance + (shopChance + .03f) * (1f - shopChance);
                        // TODO: shop chance
                        break;
                    }
                }
                expectedCardRewards[i] = cardRewardsSoFar;
                expectedRewardRelics[i] = relicsSoFar;
                expectedShops[i] = shopsSoFar;
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
            if (Save.state.act_num != 3 || Save.state.has_ruby_key) {
                return false;
            }
            var fireNodes = Enumerable.Range(0, nodes.Length).Where(x => nodes[x].nodeType == NodeType.Fire);
            var fireCount = fireNodes.Count();
            if (fireCount == 1) {
                return true;
            }
            return fireNodes.Skip(fireCount - 2).First() == index;
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

        public NodeType GetNodeType(int i) {
            if (i < nodes.Length) {
                return nodes[i].nodeType;
            }
            switch (i - nodes.Length) {
                case 0: {
                    return NodeType.Boss;
                }
                case 1: {
                    return NodeType.Fire;
                }
                case 2: {
                    return NodeType.Shop;
                }
                case 3: {
                    return NodeType.Elite;
                }
                case 4: {
                    return NodeType.Boss;
                }
                default: {
                    throw new NotImplementedException();
                }
            }
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
                var restValue = Evaluators.RestValue(this, i);
                var upgradeValue = Evaluators.UpgradeValue(this, i);
                var liftValue = Evaluators.LiftValue(this, i);

                var bestChoice = FireChoice.Upgrade;
                var bestValue = upgradeValue;
                TestPlan(ref bestValue, ref bestChoice, restValue, FireChoice.Rest);
                TestPlan(ref bestValue, ref bestChoice, liftValue, FireChoice.Lift);

                var nodeType = GetNodeType(i);
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
                else if (NeedsRedKey(i)) {
                    fireChoices[i] = FireChoice.Key;
                }
                else if (ShouldRest(i)) {
                    fireChoices[i] = FireChoice.Rest;
                    expectedHealth[i] += Save.state.max_health * .3f;
                }
                else {
                    fireChoices[i] = bestChoice;
                    if (bestChoice == FireChoice.Rest) {
                        expectedHealth[i] += Save.state.max_health * .3f;
                    }
                    else if (bestChoice == FireChoice.Upgrade) {
                        upgrades++;
                    }
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
                var totalWeight = possibleThreats[i].Select(x => x.weight).Sum();
                foreach (var possibleEncounter in possibleThreats[i]) {
                    FightSimulator.SimulateFight(possibleEncounter, out var expectedHealthLoss, out var worstCaseHealthLoss);
                    totalExpectedHealthLoss += expectedHealthLoss;
                    worstWorstCaseHealthLoss = MathF.Max(worstCaseHealthLoss, worstWorstCaseHealthLoss);
                    var chanceOfThis = (possibleEncounter.weight * 1f / totalWeight) * fightChance[i];
                    AssessThreat(possibleEncounter, expectedHealthLoss, worstCaseHealthLoss, lastExpectedHealth, chanceOfThis);
                }
                var averageExpectedHealthLoss = totalExpectedHealthLoss / possibleThreats.Length;
                expectedHealth[i] = lastExpectedHealth - averageExpectedHealthLoss;
                worstCaseHealth[i] = lastWorstCaseHealth - worstWorstCaseHealthLoss;
            }
            // TODO: threats from future acts
        }

        public void FinalThreatAnalysis() {
            // Could we fight gremlin nob by the time we get there?
            // TODO: fight simulator needs to know that we got stronger in the mean time
            ChoosePlanningThreats();
            // TODO: threats from future acts
        }

        protected void AssessThreat(Encounter threat, float expectedDamage, float worstCaseDamage, float expectedHealth, float chanceOfThis) {
            Threats.TryAdd(threat.id, 0f);
            var healthFraction = expectedDamage / Save.state.max_health;
            Threats[threat.id] += healthFraction;
            var worstCaseHealth = expectedHealth - worstCaseDamage;
            if (worstCaseDamage > expectedDamage && worstCaseHealth < 0f) {
                var deathLikelihood = -worstCaseHealth / (worstCaseDamage - expectedDamage);
                var fractionMultiplier = Lerp.FromUncapped(0, 2, deathLikelihood);
                Threats[threat.id] += fractionMultiplier * healthFraction * chanceOfThis;
                // These risks aggregate across all encounters, which is a bit strange
                Risk += deathLikelihood;
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
        public static Path[] BuildAllPaths(MapNode root, int startingCardRewards) {
            var skipFirstNode = true;
            var nodeSequences = IterateNodeSequences(root, skipFirstNode);
            var paths = nodeSequences.Select(x => new Path() { nodes = x.ToArray() }).ToArray();
            foreach (var path in paths) {
                path.InitArrays();
                path.FindBasicProperties();
                path.ExpectBasicProgression(startingCardRewards);
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
            for (int i = 0; i < (byte)ScoreReason.COUNT; i++) {
                scores[i] = Lerp.From(scores[i], OffRamp.scores[i], riskRelevance);
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
        public IEnumerable<float> ExpectedUpgradesDuringFights() {
            var fightFloors = Enumerable.Range(0, expectedUpgrades.Length).Where(x => x >= nodes.Length || nodes[x].nodeType.IsFight());
            return fightFloors.Select(x => expectedUpgrades[x]).Concat(ExpectedFutureUpgradesDuringFights(expectedUpgrades[^1]));
        }
        public bool ContainsGuaranteedChest() {
            return nodes.Any(x => x.nodeType == NodeType.Chest);
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