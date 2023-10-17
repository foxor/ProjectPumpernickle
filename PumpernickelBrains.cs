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

        public override string ToString() {
            return string.Join("\r\n", Advice);
        }
    }
    public class PumpernickelBrains {
        public static string AdviseOnRewards(IEnumerable<string> cardRewards) {
            // Techincally, this needs to accept all the card rewards, and we need to multiplex them
            var skip = Evaluate();
            skip.Advice.Append("Skip the cards");
            int bestPick = -1;
            var bestValue = skip;
            var reward = cardRewards.ToArray();
            for (int i = 0; i < reward.Length; i++) {
                var cardId = reward[i];
                var cardIndex = PumpernickelSaveState.instance.AddCardById(cardId);
                var evaluation = Evaluate();
                evaluation.Advice.Append("Take the " + Database.instance.cardsDict[reward[i]].name);
                PumpernickelSaveState.instance.RemoveCardByIndex(cardIndex);
                if (evaluation.Score > bestValue.Score) {
                    bestPick = i;
                    bestValue = evaluation;
                }
            }
            return bestValue.ToString();
        }
        protected static int RareRelicsAvailable() {
            var classRelics = 0;
            switch (PumpernickelSaveState.instance.character) {
                case PlayerCharacter.Ironclad: {
                    classRelics = 3;
                    break;
                }
                case PlayerCharacter.Silent: {
                    classRelics = 3;
                    break;
                }
                case PlayerCharacter.Defect: {
                    classRelics = 1;
                    break;
                }
                case PlayerCharacter.Watcher: {
                    classRelics = 2;
                    break;
                }
            }
            return 25 + classRelics;
        }
        public static float ExpectedGoldFromRandomRelic() {
            return 1f / 6f * (1f / RareRelicsAvailable()) * 300f;
        }

        public static int PermanentDeckSize() {
            return Save.state.cards.Select(x => {
                if (x.cardType == CardType.Power) {
                    return 0;
                }
                if (x.id == "Purity") {
                    return x.upgrades > 0 ? -5 : -3;
                }
                if (x.tags?.ContainsKey(Tags.NonPermanent.ToString()) == true) {
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
            if (Save.state.character == PlayerCharacter.Watcher) {
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

        public static void CardRewardDamageStats(Path path, int nodeIndex, out float totalDamage, out float totalCost) {
            //path.expectedCardRewards[nodeIndex]
            totalCost = 0;
            totalDamage = 0;
        }

        protected static float ScorePath(Path path) {
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

            // ROUGH Rules:
            //  - you get ~10 points for surviving acts 1, 2 and 3
            //  - you get 2 points for the first 4 upgrades, then 1 for the next 6
            //  - 1 point per relic up to 15
            //  - .5 points per card reward
            //  - card removes?
            var points = 0f;

            points += 10f * (1f - path.Risk);

            var upgrades = path.expectedUpgrades[^1];
            if (upgrades <= 4) {
                points += upgrades * 2f;
            }
            else {
                points += 8 + MathF.Min(upgrades - 4, 6);
            }

            points += MathF.Min(path.expectedRewardRelics[^1], 15f);

            points += .5f * path.expectedCardRewards[^1];

            return points;
        }

        public static string DescribePathing(Span<MapNode> pathNodes) {
            var moveToPos = pathNodes[0].position;
            var moveFromPos = PumpernickelSaveState.instance.GetCurrentNode().position;
            var direction = moveFromPos.x > moveToPos.x ? "left" :
                (moveFromPos.x < moveToPos.x ? "right" : "up");
            var destination = pathNodes[0].nodeType.ToString();
            return string.Format("Go {0} to the {1}", direction, destination);
        }

        public static string ChooseBestUpgrade(Path path, int index) {
            return "Eruption";
        }

        public static Path EvaluatePathing(Evaluation evaluation, out float score) {
            var currentNode = PumpernickelSaveState.instance.GetCurrentNode();
            var allPaths = Path.BuildAllPaths(currentNode);
            var evaluatedPaths = allPaths.Select(x => (Path: x, Score: ScorePath(x))).OrderByDescending(x => x.Score).ToArray();
            var bestPath = evaluatedPaths.First();
            score = bestPath.Score;
            bool needsMoreInfo = false;
            int i = 0;
            while (!needsMoreInfo) {
                evaluation.Advice.Add(DescribePathing(bestPath.Path.nodes[i..]));
                switch (bestPath.Path.nodes[i].nodeType) {
                    case NodeType.Shop:
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
                                var bestUpgrade = ChooseBestUpgrade(bestPath.Path, i);
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

        public static Evaluation Evaluate() {
            Evaluation evaluation = new Evaluation();
            var chosenPath = EvaluatePathing(evaluation, out var pathScore);
            evaluation.Score = pathScore;
            for (int i = 0; i < PumpernickelSaveState.instance.cards.Count; i++) {
                var card = PumpernickelSaveState.instance.cards[i];
                evaluation.Score += CardFunctionReflection.GetEvalFunctionCached(card.id)(card, i);
            }
            return evaluation;
        }
    }
}