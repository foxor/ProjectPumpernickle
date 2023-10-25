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
        public float Score;
        public List<string> Advice = new List<string>();

        public Evaluation() {
            Save.state.earliestInfinite = 0;
            Save.state.buildingInfinite = false;
            Save.state.missingCards = 0;
            Save.state.expectingToRedBlue = Save.state.character == PlayerCharacter.Watcher;
        }

        public override string ToString() {
            return string.Join("\r\n", Advice);
        }
    }
    public class RewardOption {
        public RewardType rewardType;
        public string[] values;
    }
    public class RewardContext : IDisposable {
        public List<string> relics = new List<string>();
        public List<int> cardIndicies = new List<int>();
        public int gold;
        public List<int> potionIndicies = new List<int>();
        public List<string> description = new List<string>();
        public bool tookGreenKey;
        public RewardContext(List<RewardOption> rewardOptions, List<int> rewardIndicies) {
            var i = 0;
            foreach (var reward in rewardOptions) {
                var index = rewardIndicies[i];
                if (index >= reward.values.Length) {
                    // We're skipping this reward
                    description.Add("Skip the " + reward.rewardType);
                    continue;
                }
                var chosen = reward.values[index];
                switch (reward.rewardType) {
                    case RewardType.Cards: {
                        cardIndicies.Add(PumpernickelSaveState.instance.AddCardById(chosen));
                        description.Add("Take the " + Database.instance.cardsDict[chosen].name);
                        break;
                    }
                    case RewardType.Gold: {
                        var value = int.Parse(chosen);
                        gold += value;
                        Save.state.gold += value;
                        description.Add("Take the gold");
                        break;
                    }
                    case RewardType.Relic: {
                        relics.Add(chosen);
                        Save.state.relics.Add(chosen);
                        description.Add("Take the " + chosen);
                        break;
                    }
                    case RewardType.Potions: {
                        description.Add("Take the " + chosen);
                        Save.state.TakePotion(chosen);
                        relics.Add(chosen);
                        Save.state.relics.Add(chosen);
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
                }
                i++;
            }
        }

        public void Dispose() {
            foreach (var relic in relics) {
                Save.state.relics.Remove(relic);
            }
            foreach (var cardIndex in cardIndicies) {
                PumpernickelSaveState.instance.RemoveCardByIndex(cardIndex);
            }
            Save.state.gold -= gold;
            foreach (var potionIndex in potionIndicies) {
                Save.state.RemovePotion(potionIndex);
            }
            if (tookGreenKey) {
                Save.state.has_emerald_key = false;
            }
        }
    }
    public class PathAdvice {
        protected static IEnumerable<Evaluation> MultiplexRewards(List<RewardOption> rewardOptions) {
            var totalOptions = rewardOptions.Select(x => x.values.Length + 1).Aggregate(1, (a, x) => a * x);
            List<int> rewardIndicies = new List<int>();
            for (int i = 0; i < totalOptions; i++) {
                int residual = i;
                for (int j = 0; j < rewardOptions.Count; j++) {
                    var optionCount = rewardOptions[j].values.Length + 1;
                    rewardIndicies.Add(residual % optionCount);
                    residual /= optionCount;
                }
                using (var context = new RewardContext(rewardOptions, rewardIndicies)) {
                    yield return Evaluate(context.description);
                }
                rewardIndicies.Clear();
            }
        }
        public static string AdviseOnRewards(List<RewardOption> rewardOptions) {
            var evaluations = MultiplexRewards(rewardOptions).ToArray();
            return evaluations.OrderByDescending(x => x.Score).First().ToString();
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
            var moveFromPos = PumpernickelSaveState.instance.GetCurrentNode().position;
            var direction = moveFromPos.x > moveToPos.x ? "left" :
                (moveFromPos.x < moveToPos.x ? "right" : "up");
            var destination = pathNodes[0].nodeType.ToString();
            return string.Format("Go {0} to the {1}", direction, destination);
        }

        public static Path EvaluatePathing(Evaluation evaluation, out float score) {
            var currentNode = PumpernickelSaveState.instance.GetCurrentNode();
            var allPaths = Path.BuildAllPaths(currentNode);
            var evaluatedPaths = allPaths.Select(x => (Path: x, Score: Scoring.ScorePath(x))).OrderByDescending(x => x.Score).ToArray();
            var bestPath = evaluatedPaths.First();
            score = bestPath.Score;
            bool needsMoreInfo = false;
            int i = 0;
            while (!needsMoreInfo && bestPath.Path.nodes.Count() > i) {
                evaluation.Advice.Add(DescribePathing(bestPath.Path.nodes[i..]));
                switch (bestPath.Path.nodes[i].nodeType) {
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
                        switch (bestPath.Path.fireChoices[i]) {
                            case FireChoice.Rest: {
                                evaluation.Advice.Add("Rest");
                                break;
                            }
                            case FireChoice.Upgrade: {
                                var bestUpgrade = Evaluators.ChooseBestUpgrade(bestPath.Path, i);
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
            return bestPath.Path;
        }

        public static Evaluation Evaluate(List<string> existingAdvice) {
            Evaluation evaluation = new Evaluation();
            if (existingAdvice != null) {
                evaluation.Advice = existingAdvice;
            }
            var chosenPath = EvaluatePathing(evaluation, out var pathScore);
            evaluation.Score = pathScore;
            for (int i = 0; i < PumpernickelSaveState.instance.cards.Count; i++) {
                var card = PumpernickelSaveState.instance.cards[i];
                evaluation.Score += CardFunctionReflection.GetEvalFunctionCached(card.id)(card, i);
            }
            evaluation.Score += Scoring.EvaluateGlobalRules(chosenPath);
            return evaluation;
        }
    }
}