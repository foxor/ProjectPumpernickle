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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace ProjectPumpernickle {
    public class Advice {
        protected static IEnumerable<Evaluation> MultiplexRewards(List<RewardOption> rewardOptions, bool eligibleForBlueKey, bool isShop) {
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
                using (var context = new RewardContext(rewardOptions, rewardIndicies, eligibleForBlueKey, isShop)) {
                    if (!context.IsValid()) {
                        continue;
                    }
                    var evaluations = GenerateEvaluationsWithinContext(context);
                    foreach (var evaluation in evaluations) {
                        yield return evaluation;
                    }
                }
            }
        }

        protected static void SetEvaluationOffRamps(MapNode root, Evaluation[] evaluations) {
            if (root != null) {
                foreach (var child in root.children) {
                    var pathsThatGoThisWay = evaluations.Where(x => x.Path.nodes[0] == child);
                    var safestPathThisWay = pathsThatGoThisWay.OrderByDescending(x => x.Path.chanceToSurvive).First();
                    foreach (var path in pathsThatGoThisWay) {
                        path.OffRamp = safestPathThisWay;
                    }
                }
            }
        }

        public static IEnumerable<Evaluation> GenerateEvaluationsWithinContext(RewardContext context) {
            var currentNode = PumpernickelSaveState.instance.GetCurrentNode();
            var allPaths = Path.BuildAllPaths(currentNode, context.bonusCardRewards);
            var allEvaluations = allPaths.Select(x => new Evaluation(context, x)).ToArray();
            SetEvaluationOffRamps(currentNode, allEvaluations);
            if (!allPaths.Any()) {
                allEvaluations = new Evaluation[] { new Evaluation(context, null) };
            }
            foreach (var eval in allEvaluations) {
                Evaluate(eval);
            }
            foreach (var eval in allEvaluations) {
                eval.MergeScoreWithOffRamp();
            }
            return allEvaluations;
        }

        public static void Evaluate(Evaluation evaluation) {
            for (int i = 0; i < PumpernickelSaveState.instance.cards.Count; i++) {
                var card = PumpernickelSaveState.instance.cards[i];
                evaluation.AddScore(ScoreReason.DeckQuality, EvaluationFunctionReflection.GetCardEvalFunctionCached(card.id)(card, i));
            }
            for (int i = 0; i < PumpernickelSaveState.instance.relics.Count; i++) {
                var relicId = PumpernickelSaveState.instance.relics[i];
                // TODO: fix setup relics?
                var relic = Database.instance.relicsDict[relicId];
                evaluation.AddScore(ScoreReason.RelicQuality, EvaluationFunctionReflection.GetRelicEvalFunctionCached(relic.id)(relic));
            }
            Scoring.EvaluateGlobalRules(evaluation);
            Scoring.ScorePath(evaluation);
        }
        public static Evaluation[] AdviseOnRewards(List<RewardOption> rewardOptions) {
            var isShop = rewardOptions.Any(x => x.cost != 0);
            var currentNode = PumpernickelSaveState.instance.GetCurrentNode();
            var eligibleForBlueKey = currentNode?.nodeType == NodeType.Chest && !Save.state.has_sapphire_key;
            var evaluations = MultiplexRewards(rewardOptions, eligibleForBlueKey, isShop).ToArray();
            var preRewardEvaluation = new Evaluation(null, null);
            Evaluate(preRewardEvaluation);
            Scoring.ApplyVariance(evaluations, preRewardEvaluation);
            var sorted = evaluations.OrderByDescending(x => x.Score).ToArray();
            return sorted;
        }
        public static Evaluation[] AddPathingAdvice(IEnumerable<string> previousAdvice) {
            var rewards = new List<RewardOption>();
            var evals = AdviseOnRewards(rewards);
            foreach(var eval in evals) {
                eval.Advice = previousAdvice.Concat(eval.Advice).ToList();
            }
            return evals;
        }
    }
}