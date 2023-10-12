using Accessibility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ProjectPumpernickle {
    public struct Vector2Int {
        public int x;
        public int y;
    }
    public enum Threat {
        GremlinNob,
        Lagavulin,
        Hexxaghost,
        Act1HardPool,
    }
    public enum PathShopPlan {
        MaxRemove,
        FixFight,
        NormalShop,
        HuntForShopRelic,
        SaveGold,
    }
    public class Path {
        public int elites;
        public bool hasMegaElite;
        public MapNode[] nodes = null;
        public float[] expectedGold;
        public int[] maxPlanningGold;
        public float expectedHealthLoss;
        public float riskOfDeath;
        public int shopCount;
        public PathShopPlan shopPlan;
        public Dictionary<Threat, float> Threats = new Dictionary<Threat, float>();
        public float Risk;

        public float ExpectedPossibleCardRemoves() {
            float removeCost = Save.state.purgeCost;
            float totalRemoves = 0f;
            for (int i = 0; i < nodes.Length; i++) {
                if (nodes[i].nodeType == NodeType.Shop && maxPlanningGold[i] > removeCost) {
                    var minPlanningGold = expectedGold[i] - (maxPlanningGold[i] - expectedGold[i]);
                    var removeAvailableChance = Lerp.Inverse(minPlanningGold, maxPlanningGold[i], removeCost);
                    removeCost += 25f * removeAvailableChance;
                    totalRemoves += removeAvailableChance;
                }
            }
            return totalRemoves;
        }

        public void ChooseShopPlan() {
            var removeValue = PumpernickelBrains.CardRemovePoints(this);
            var fixValue = PumpernickelBrains.FixShopPoint(this);
            var normalValue = PumpernickelBrains.NormalShopPoint(this);
            var huntValue = PumpernickelBrains.HuntForShopRelicPoint(this);
            var saveValue = PumpernickelBrains.SaveGoldPoint(this);
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

        public void ChoosePlanningThreats() {
            // Could we fight gremlin nob right now?
        }

        public void ExpectBasicProgression() {

        }

        public void ExpectShopProgression() {
            float expectedRemoves = 0;
            for (int i = 0; i < nodes.Length; i++) {
                expectedGold[i] += PumpernickelBrains.ExpectedGoldFrom(nodes[i].nodeType, shopPlan, expectedRemoves);
            }
        }

        public void ChooseFinalThreats() {
            // Could we fight gremlin nob by the time we get there?
        }
    }
    public class PumpernickelBrains {
        public static string AdviseOnRewards(IEnumerable<string> cardRewards) {
            // Techincally, this needs to accept all the card rewards, and we need to multiplex them
            float skip = Evaluate();
            int bestPick = -1;
            float bestValue = skip;
            var reward = cardRewards.ToArray();
            for (int i = 0; i < reward.Length; i++) {
                var cardName = reward[i];
                var cardIndex = PumpernickelSaveState.instance.AddCardByName(cardName);
                float eval = Evaluate();
                PumpernickelSaveState.instance.RemoveCardByIndex(cardIndex);
                if (eval > bestValue) {
                    bestPick = i;
                    bestValue = eval;
                }
            }
            if (bestPick == -1) {
                return "Skip the cards\r\n";
            }
            return "Choose " + reward[bestPick] + "\r\n";
        }

        protected static IEnumerable<IEnumerable<MapNode>> IterateNodeSequences(MapNode root) {
            foreach (var child in root.children) {
                foreach (var path in IterateNodeSequences(child)) {
                    yield return path.Append(child);
                }
            }
        }
        protected static int RareRelicsAvailable() {
            var classRelics = 0;
            switch (PumpernickelSaveState.instance.character) {
                case Character.Ironclad: {
                    classRelics = 3;
                    break;
                }
                case Character.Silent: {
                    classRelics = 3;
                    break;
                }
                case Character.Defect: {
                    classRelics = 1;
                    break;
                }
                case Character.Watcher: {
                    classRelics = 2;
                    break;
                }
            }
            return 25 + classRelics;
        }
        protected static float ExpectedGoldFromRandomRelic() {
            return 1f / 6f * (1f / RareRelicsAvailable()) * 300f;
        }
        protected static int MaxPlanningGoldFrom(NodeType type) {
            // The point of this is to plan probabilities for having enough gold to do X.
            // We don't want to plan our shops around getting old coin or winning a joust.
            switch (type) {
                case NodeType.Fight: {
                    return 20;
                }
                case NodeType.Elite: {
                    return 35;
                }
                case NodeType.MegaElite: {
                    return 35;
                }
            }
            return 0;
        }
        protected static float ExpectedGoldFrom(NodeType type) {
            switch(type) {
                case NodeType.Fight: {
                    return 15f;
                }
                case NodeType.Elite: {
                    return 30f + ExpectedGoldFromRandomRelic();
                }
                case NodeType.MegaElite: {
                    return 30 + ExpectedGoldFromRandomRelic();
                }
                case NodeType.Shop: {
                    return -
                }
                case NodeType.Question: {
                    // TODO
                    return 0f;
                }
            }
            return 0f;
        }
        protected static Path[] BuildAllPaths(MapNode root) {
            var nodeSequences = IterateNodeSequences(root);
            var paths = nodeSequences.Select(x => new Path() { nodes = x.ToArray() }).ToArray();
            foreach (var path in paths) {
                var elites = 0;
                path.expectedGold = new float[path.nodes.Length];
                path.expectedGold[0] = Save.state.gold;
                path.maxPlanningGold = new int[path.nodes.Length];
                path.maxPlanningGold[0] = Save.state.gold;
                int index = 0;
                foreach (var node in path.nodes) {
                    if (node.nodeType == NodeType.Elite || node.nodeType == NodeType.MegaElite) {
                        elites++;
                    }
                    path.hasMegaElite |= node.nodeType == NodeType.MegaElite;
                    path.shopCount += (node.nodeType == NodeType.Shop) ? 1 : 0;
                    path.maxPlanningGold[index] += MaxPlanningGoldFrom(node.nodeType);
                    index++;
                }
                path.ChoosePlanningThreats();
                path.ExpectBasicProgression();
                path.ChooseShopPlan();
                path.ExpectShopProgression();
                path.ChooseFinalThreats();
            }
            return paths;
        }

        public static int PermanentDeckSize() {
            return Save.state.cards.Select(x => {
                if (x.cardType == CardType.Power) {
                    return 0;
                }
                if (x.id == "Purity") {
                    return x.upgrades > 0 ? -5 : -3;
                }
                if (x.tags.ContainsKey(Tags.NonPermanent.ToString())) {
                    return 0f;
                }
                return 1;
            }).Count();
        }

        public static float ExpectedCardRemovesAvailable(Path path) {
            // TODO
            return 5f;
        }

        public static bool HasCalmEnter() {
            return Save.state.cards.Any(x => x.id == "InnerPeace" || x.id == "FearNoEvil");
        }

        public static float CardRemovePoints(Path path) {
            if (Save.state.character == Character.Watcher) {
                var anticipatedEndGameDeckSize = PermanentDeckSize() - ExpectedCardRemovesAvailable(path) + (HasCalmEnter() ? 0 : 1);
                if (anticipatedEndGameDeckSize > 8) {
                    return .2f;
                }
                // Allocate 20 pumpernickel points to getting 5 removes on watcher going for infinite
                return path.ExpectedPossibleCardRemoves() / 5f * 20f;
            }
            // TODO
            return .2f;
        }
        public static float FightsBeforeElite(Path path) {
            float fightsSoFar = 0f;
            float fightChance = Save.state.event_chances[1];
            for (int i = 0; i < path.nodes.Length; i++) {
                switch (path.nodes[i].nodeType) {
                    case NodeType.Elite: {
                        return fightsSoFar;
                    }
                    case NodeType.MegaElite: {
                        return fightsSoFar;
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
            }
            return float.NaN;
        }
        public static float FixShopPoint(Path path) {
            // How valuable is it to buy an attack card or potion to fix a scary fight?
            if (Save.state.act_num == 1) {
                var fightsBeforeElite = FightsBeforeElite(path);
                if (fightsBeforeElite == 
            }
        }
        public static float NormalShopPoint(Path path) {
        }
        public static float HuntForShopRelicPoint(Path path) {
        }
        public static float SaveGoldPoint(Path path) {
        }

        protected static float ShopValue(float expectedGold) {
        }
        public static float HowGoodIsTheShop(Path path) {
            if (!path.hasShop) {
                return 0f;
            }
            for (int i = 0; i < path.nodes.Length; i++) {
                if (path.nodes[i].nodeType == NodeType.Shop) {
                }
            }
        }

        public static void EvaluatePathing() {
            var currentNode = PumpernickelSaveState.instance.GetCurrentNode();
            var allPaths = BuildAllPaths(currentNode);
            var maxElites = allPaths.Select(x => x.elites).Max();
            var minElites = allPaths.Select(x => x.elites).Min();
            // Things to think about:
            // - How many elites can I do this act? 
            // ✔ What is the largest number of elites available?
            // ✔ Can I dodge all elites?
            // - Will this path kill me?
            // - Do we need to go to a shop?
            // - Do we have tiny chest / serpent head?
            // - Do we need green key?
            // - Does this path have an off-ramp?
            // - Are we looking for any events? (golden idol considerations etc)
            // - Do we have fight metascaling (ritual dagger, genetic algorithm, etc)
            // - What is our expected health loss per fight / elite

        }

        public static float Evaluate() {
            EvaluatePathing();
            float value = 0f;
            for (int i = 0; i < PumpernickelSaveState.instance.cards.Count; i++) {
                var card = PumpernickelSaveState.instance.cards[i];
                value += CardFunctionReflection.GetEvalFunctionCached(card.id)(card, i);
            }
            return value;
        }
    }
}