using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;

namespace ProjectPumpernickle {
    public class Advice {
        public static Evaluation[] perThreadEvaluations;
        public static List<RewardOption> rewardOptions;
        public static bool eligibleForBlueKey;
        public static bool isShop;
        protected static IEnumerable<Evaluation> MultiplexRewards(List<RewardOption> rewardOptions, bool eligibleForBlueKey, bool isShop) {
            Advice.rewardOptions = rewardOptions;
            Advice.eligibleForBlueKey = eligibleForBlueKey;
            Advice.isShop = isShop;
            var totalRewardOptions = rewardOptions.Select(x => x.values.Length + 1).Aggregate(1, (a, x) => a * x);
            var pathingOptions = Path.CountNodeSequences(rewardOptions);
            perThreadEvaluations = new Evaluation[totalRewardOptions * pathingOptions];
            PumpernickelAdviceWindow.instance.AdviceBox.Text = String.Format("Creating {0} threads", perThreadEvaluations.Length);
            Application.DoEvents();
            for (int i = 0; i < totalRewardOptions; i++) {
                for (int p = 0; p < pathingOptions; p++) {
                    var optionIndexCapture = i;
                    var pathIndexCapture = p;
                    var threadId = (optionIndexCapture * pathingOptions) + pathIndexCapture;
                    ThreadPool.QueueUserWorkItem((_) => GenerateEvaluation(optionIndexCapture, pathIndexCapture, threadId));
                }
                Application.DoEvents();
            }
            var totalThreads = perThreadEvaluations.Length;
            while (ThreadPool.PendingWorkItemCount != 0) {
                Thread.Sleep(10);
                var done = ThreadPool.PendingWorkItemCount - totalThreads;
                PumpernickelAdviceWindow.instance.AdviceBox.Text = String.Format("{0:P2} complete", (done * 1f / totalThreads));
                Application.DoEvents();
            }
            PumpernickelAdviceWindow.instance.AdviceBox.Text = String.Format("Finalizing results");
            Application.DoEvents();
            var validEvaluations = perThreadEvaluations.Where(x => x != null);
            SetEvaluationOffRamps(Save.state.GetCurrentNode(), validEvaluations, totalRewardOptions);
            foreach (var eval in validEvaluations) {
                Scoring.ScoreAfterOffRampDetermined(eval);
            }
            foreach (var eval in validEvaluations) {
                eval.MergeScoreWithOffRamp();
            }
            // For debugging determinism
            //var rewardsChosenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join(',', validEvaluations.Select(x => x.Score)))));
            return validEvaluations;
        }
        public static void GenerateEvaluation(int optionIndex, int pathIndex, int threadId) {
            var skipFirstNode = true;
            var nodeSequence = Path.BuildNodeSequence(pathIndex, Save.state.GetCurrentNode(), skipFirstNode);
            if (!nodeSequence.IsValid()) {
                return;
            }
            PumpernickelSaveState.instance = new PumpernickelSaveState(PumpernickelSaveState.parsed);
            List<int> rewardIndicies = new List<int>();
            int residual = optionIndex;
            for (int j = 0; j < rewardOptions.Count; j++) {
                var optionCount = rewardOptions[j].values.Length + 1;
                rewardIndicies.Add(residual % optionCount);
                residual /= optionCount;
            }
            using (var context = new RewardContext(rewardOptions, rewardIndicies, eligibleForBlueKey, isShop)) {
                if (!context.IsValid(threadId)) {
                    return;
                }
                var eval = new Evaluation(context, threadId, optionIndex);
                var path = Path.BuildPath(nodeSequence, pathIndex);
                eval.SetPath(path, context.bonusCardRewards);
                Scoring.Score(eval);
                perThreadEvaluations[threadId] = eval;
            }
        }
        public static Evaluation[] AdviseOnRewards(List<RewardOption> rewardOptions) {
            var isShop = rewardOptions.Any(x => x.cost != 0);
            var currentNode = Save.state.GetCurrentNode();
            var eligibleForBlueKey = currentNode?.nodeType == NodeType.Chest && !Save.state.has_sapphire_key;
            Evaluators.ReorderOptions(rewardOptions);
            Evaluators.SkipUnpalatableOptions(rewardOptions);
            var evaluations = MultiplexRewards(rewardOptions, eligibleForBlueKey, isShop).ToArray();
            Scoring.ApplyVariance(evaluations);
            var sorted = evaluations.OrderByDescending(x => x.Score).ToArray();
            return sorted;
        }
        public static Evaluation[] AdviseOnRewards(List<RewardOption> rewardOptions, IEnumerable<string> previousAdvice) {
            var evaluations = AdviseOnRewards(rewardOptions);
            foreach (var evaluation in evaluations) {
                evaluation.Advice = previousAdvice.Concat(evaluation.Advice).ToList();
            }
            return evaluations;
        }
        public static Evaluation[] CreateEventEvaluations(IEnumerable<string> previousAdvice, bool NeedsMoreInfo = false) {
            var rewards = new List<RewardOption>();
            var evals = AdviseOnRewards(rewards);
            foreach(var eval in evals) {
                eval.Advice = previousAdvice.Concat(eval.Advice).ToList();
                eval.NeedsMoreInfo = NeedsMoreInfo;
            }
            return evals;
        }
        protected static void SetEvaluationOffRamps(MapNode root, IEnumerable<Evaluation> evaluations, int totalRewardOptions) {
            if (root != null) {
                foreach (var child in root.children) {
                    for (int i = 0; i < totalRewardOptions; i++) {
                        var pathsThatGoThisWay = evaluations.Where(x => x != null && x.Path.nodes[0] == child && x.RewardIndex == i);
                        if (pathsThatGoThisWay.Any()) {
                            var safestPathThisWay = pathsThatGoThisWay.OrderByDescending(x => x.Path.chanceToWin).First();
                            foreach (var path in pathsThatGoThisWay) {
                                path.OffRamp = safestPathThisWay;
                            }
                        }
                    }
                }
            }
        }
    }
}