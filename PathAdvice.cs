using Accessibility;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ProjectPumpernickle {
    public class  Evaluation {
        public static Evaluation Active;
        // How to make score auditable
        public float Score;
        public List<string> Advice = new List<string>();
        public Path Path;

        public Evaluation() {
            Active = this;
            Save.state.earliestInfinite = 0;
            Save.state.buildingInfinite = false;
            Save.state.expectingToRedBlue = Save.state.character == PlayerCharacter.Watcher;
            Save.state.huntingCards.Clear();
        }

        public override string ToString() {
            return string.Join("\r\n", Advice);
        }
    }
    public class RewardOption {
        public RewardType rewardType;
        public string[] values;
        public int cost;
    }
    public class RewardContext : IDisposable {
        public List<string> relics = new List<string>();
        public List<int> cardIndicies = new List<int>();
        public int goldAdded;
        public List<int> potionIndicies = new List<int>();
        public List<string> description = new List<string>();
        public bool tookGreenKey;
        public List<Card> cardsRemoved = new List<Card>();
        public List<int> removedCardIndicies = new List<int>();
        public RewardContext(List<RewardOption> rewardOptions, List<int> rewardIndicies) {
            for (int i = 0; i < rewardIndicies.Count; i++) {
                var rewardGroup = rewardOptions[i];
                var index = rewardIndicies[i];
                if (index >= rewardGroup.values.Length) {
                    // We're skipping this reward
                    description.Add("Skip the " + rewardGroup.rewardType);
                    continue;
                }
                var chosen = rewardGroup.values[index];
                var chosenId = chosen.Replace("+", "");
                Save.state.gold -= rewardGroup.cost;
                goldAdded -= rewardGroup.cost;
                switch (rewardGroup.rewardType) {
                    case RewardType.Cards: {
                        cardIndicies.Add(PumpernickelSaveState.instance.AddCardById(chosenId));
                        description.Add("Take the " + Database.instance.cardsDict[chosenId].name);
                        break;
                    }
                    case RewardType.Gold: {
                        var value = int.Parse(chosen);
                        goldAdded += value;
                        Save.state.gold += value;
                        description.Add("Take the gold");
                        break;
                    }
                    case RewardType.Relic: {
                        // TODO: some relics require more advice when they are chosen, like bottles, empty cage and astolabe
                        relics.Add(chosen);
                        Save.state.relics.Add(chosen);
                        description.Add("Take the " + chosen);
                        break;
                    }
                    case RewardType.Potion: {
                        description.Add("Take the " + chosen);
                        potionIndicies.Add(Save.state.TakePotion(chosen));
                        break;
                    }
                    case RewardType.Key: {
                        description.Add("Take the " + chosen + " key");
                        switch (chosen) {
                            case "Green": {
                                Save.state.has_emerald_key = true;
                                tookGreenKey = true;
                                break;
                            }
                            default: {
                                throw new System.NotImplementedException();
                            }
                        }
                        break;
                    }
                    case RewardType.CardRemove: {
                        var removeIndex = Evaluators.WorstCardIndex();
                        cardsRemoved.Add(Save.state.cards[removeIndex]);
                        removedCardIndicies.Add(removeIndex);
                        description.Add("Remove the " + Save.state.cards[removeIndex].name);
                        Save.state.cards.RemoveAt(removeIndex);
                        break;
                    }
                }
            }
        }

        public void Dispose() {
            foreach (var relic in relics) {
                Save.state.relics.Remove(relic);
            }
            foreach (var cardIndex in cardIndicies.OrderByDescending(x => x)) {
                PumpernickelSaveState.instance.RemoveCardByIndex(cardIndex);
            }
            Save.state.gold -= goldAdded;
            foreach (var potionIndex in potionIndicies) {
                if (potionIndex != -1) {
                    Save.state.RemovePotion(potionIndex);
                }
            }
            if (tookGreenKey) {
                Save.state.has_emerald_key = false;
            }
            for (int i = cardsRemoved.Count - 1; i >= 0; i--) {
                var card = cardsRemoved[i];
                var index = removedCardIndicies[i];
                Save.state.cards.Insert(index, card);
            }
        }

        public bool IsValid() {
            var valid = true;
            valid &= Save.state.gold > 0;
            valid &= !potionIndicies.Any(x => x == -1);
            return valid;
        }
    }
    public class PathAdvice {
        protected static IEnumerable<Evaluation> MultiplexRewards(List<RewardOption> rewardOptions) {
            var totalOptions = rewardOptions.Select(x => x.values.Length + 1).Aggregate(1, (a, x) => a * x);
            List<int> rewardIndicies = new List<int>();
            for (int i = 0; i < totalOptions; i++) {
                rewardIndicies.Clear();
                int residual = i;
                for (int j = 0; j < rewardOptions.Count; j++) {
                    var optionCount = rewardOptions[j].values.Length + 1;
                    rewardIndicies.Add(residual % optionCount);
                    residual /= optionCount;
                }
                using (var context = new RewardContext(rewardOptions, rewardIndicies)) {
                    if (!context.IsValid()) {
                        continue;
                    }
                    yield return Evaluate(context.description);
                }
            }
        }
        public static Evaluation AdviseOnRewards(List<RewardOption> rewardOptions) {
            var evaluations = MultiplexRewards(rewardOptions).ToArray();
            return evaluations.OrderByDescending(x => x.Score).First();
        }

        public static float ExpectedCardRemovesAvailable(Path path) {
            // TODO
            return 5f;
        }

        public static float CardRemovePoints(Path path) {
            if (Save.state.character == PlayerCharacter.Watcher) {
                var anticipatedEndGameDeckSize = Evaluators.PermanentDeckSize() - ExpectedCardRemovesAvailable(path) + (Evaluators.HasCalmEnter() ? 0 : 1);
                if (anticipatedEndGameDeckSize > 8) {
                    return .2f;
                }
                // Allocate 20 pumpernickel points to getting 5 removes on watcher going for infinite
                return path.ExpectedPossibleCardRemoves() / 5f * 20f;
            }
            // TODO
            return .2f;
        }

        public static void DamageStatsPerCardReward(int byTurn, float cardsInDeck, out float damage, out float cost) {
            // Assumes unupgraded
            switch (Save.state.character) {
                case PlayerCharacter.Watcher: {
                    break;
                }
                default: {
                    throw new NotImplementedException();
                }
            }
            damage = 0; cost = 0;
        }

        public static string DescribePathing(Span<MapNode> pathNodes) {
            if (pathNodes.IsEmpty) {
                return "Go to the boss fight";
            }
            var moveToPos = pathNodes[0].position;
            var currentNode = PumpernickelSaveState.instance.GetCurrentNode();
            if (currentNode == null) {
                return "Go to the next act";
            }
            var moveFromPos = currentNode.position;
            var direction = moveFromPos.x > moveToPos.x ? "left" :
                (moveFromPos.x < moveToPos.x ? "right" : "up");
            var destination = pathNodes[0].nodeType.ToString();
            return string.Format("Go {0} to the {1}", direction, destination);
        }

        public static Path EvaluatePathing(Evaluation evaluation, out float score) {
            var currentNode = PumpernickelSaveState.instance.GetCurrentNode();
            var allPaths = Path.BuildAllPaths(currentNode);
            if (!allPaths.Any()) {
                score = Scoring.ScorePath(null);
                return null;
            }
            foreach (var path in allPaths) {
                path.score = Scoring.ScorePath(path);
            }
            foreach (var path in allPaths) {
                path.MergeScoreWithOffRamp();
            }
            var bestPath = allPaths.OrderByDescending(x => x.score).First();
            score = bestPath.score;
            bool needsMoreInfo = false;
            int i = 0;
            while (!needsMoreInfo && bestPath.nodes.Count() > i) {
                evaluation.Advice.Add(DescribePathing(bestPath.nodes[i..]));
                switch (bestPath.nodes[i].nodeType) {
                    case NodeType.Shop:
                    case NodeType.Question:
                    case NodeType.Fight:
                    case NodeType.Elite:
                    case NodeType.MegaElite: {
                        needsMoreInfo = true;
                        break;
                    }
                    case NodeType.Chest: {
                        needsMoreInfo = true;
                        evaluation.Advice.Add("Open the chest");
                        break;
                    }
                    case NodeType.Fire: {
                        switch (bestPath.fireChoices[i]) {
                            case FireChoice.Rest: {
                                evaluation.Advice.Add("Rest");
                                break;
                            }
                            case FireChoice.Upgrade: {
                                var bestUpgrade = Evaluators.ChooseBestUpgrade(bestPath, i);
                                evaluation.Advice.Add("Upgrade " + bestUpgrade);
                                break;
                            }
                            default:
                                throw new System.NotImplementedException();
                        }
                        break;
                    }
                }
                i++;
            }
            if (!needsMoreInfo) {
                evaluation.Advice.Add("Fight " + Save.state.boss);
            }
            return bestPath;
        }

        public static Evaluation Evaluate(List<string> existingAdvice) {
            Evaluation evaluation = new Evaluation();
            if (existingAdvice != null) {
                evaluation.Advice = existingAdvice;
            }
            evaluation.Path = EvaluatePathing(evaluation, out var pathScore);
            evaluation.Score = pathScore;
            for (int i = 0; i < PumpernickelSaveState.instance.cards.Count; i++) {
                var card = PumpernickelSaveState.instance.cards[i];
                evaluation.Score += EvaluationFunctionReflection.GetCardEvalFunctionCached(card.id)(card, i);
            }
            for (int i = 0; i < PumpernickelSaveState.instance.relics.Count; i++) {
                var relicId = PumpernickelSaveState.instance.relics[i];
                // TODO: fix setup relics?
                var relic = Database.instance.relicsDict[relicId];
                evaluation.Score += EvaluationFunctionReflection.GetRelicEvalFunctionCached(relic.id)(relic);
            }
            evaluation.Score += Scoring.EvaluateGlobalRules(evaluation.Path);
            return evaluation;
        }
    }
}