using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;

namespace ProjectPumpernickle {
    public class Advice {
        public static Dictionary<long, Evaluation> perThreadEvaluations;
        public static List<RewardOption> rewardOptions;
        public static IEnumerable<string> previousAdvice;
        public static bool eligibleForBlueKey;
        public static bool isShop;
        public static bool needsMoreInfo;
        protected static long threadsOutstanding;
        protected static void MultiplexRewards(List<RewardOption> rewardOptions, bool eligibleForBlueKey, bool isShop, IEnumerable<string> previousAdvice, bool needsMoreInfo) {
            Advice.rewardOptions = rewardOptions;
            Advice.eligibleForBlueKey = eligibleForBlueKey;
            Advice.isShop = isShop;
            Advice.previousAdvice = previousAdvice;
            Advice.needsMoreInfo = needsMoreInfo;
            var totalRewardOptions = rewardOptions.Select(x => x.values.Length + 1).Aggregate(1, (a, x) => a * x);
            var pathingOptions = Path.CountNodeSequences(rewardOptions);
            var totalEvals = totalRewardOptions * pathingOptions;
            perThreadEvaluations = new Dictionary<long, Evaluation>();
            PumpernickelAdviceWindow.instance.AdviceBox.Text = String.Format("Creating {0} threads", totalEvals);
            Application.DoEvents();

            var workChunks = NeededWorkChunks(totalEvals);
            Profiling.MainThreadStart(totalEvals);
            for (long i = 0; i < workChunks; i++) {
                QueueWorkChunk(i, workChunks, totalEvals, pathingOptions);
                AwaitWorkChunk(i == 0);
                MergeChunks(i, workChunks, totalRewardOptions);
                Application.DoEvents();
                if (TcpListener.WaitingCount != 0) {
                    break;
                }
            }
            Profiling.StopWork();
        }


        protected static void QueueWorkChunk(long workChunk, long totalChunks, long totalEvals, long pathingOptions) {
            var stride = totalChunks;
            var offset = workChunk;
            for (long i = offset; i < totalEvals; i += stride) {
                var optionIndex = i / pathingOptions;
                var pathIndex = i % pathingOptions;
                var threadId = i;
                ThreadPool.QueueUserWorkItem((_) => GenerateEvaluation(optionIndex, pathIndex, threadId));
                Interlocked.Increment(ref threadsOutstanding);
            }
        }
        public static void GenerateEvaluation(long optionIndex, long pathIndex, long threadId) {
            try {
                // The thread statics leak through from one task to another
                Evaluation.Active = null;
                PumpernickelSaveState.instance = null;
                Profiling.WorkerStart();

                var skipFirstNode = true;
                var nodeSequence = Path.BuildNodeSequence(pathIndex, PumpernickelSaveState.parsed.GetCurrentNode(), skipFirstNode);
                if (!nodeSequence.IsValid()) {
                    return;
                }
                PumpernickelSaveState.instance = new PumpernickelSaveState(PumpernickelSaveState.parsed);
                List<int> rewardIndicies = new List<int>();
                long residual = optionIndex;
                for (int j = 0; j < rewardOptions.Count; j++) {
                    var optionCount = rewardOptions[j].values.Length + 1;
                    rewardIndicies.Add((int)(residual % optionCount));
                    residual /= optionCount;
                }
                using (var context = new RewardContext(rewardOptions, rewardIndicies, eligibleForBlueKey, isShop, nodeSequence.upgradeIndex)) {
                    if (!context.IsValid()) {
                        return;
                    }
                    Save.state.upgraded = context.upgradeIndicies;
                    var eval = new Evaluation(context, threadId, optionIndex, previousAdvice);
                    eval.NeedsMoreInfo = Advice.needsMoreInfo | context.needsMoreInfo;
                    var path = Path.BuildPath(nodeSequence, pathIndex);
                    eval.SetPath(path);
                    Scoring.ScoreBasedOnEvaluation(eval);
                    context.UpdateRewardPopulationStatistics(eval);
                    Scoring.ScoreBasedOnStatistics(eval);
                    lock (perThreadEvaluations) {
                        perThreadEvaluations[threadId] = eval;
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
            finally {
                Profiling.StopWork();
                Interlocked.Decrement(ref threadsOutstanding);
            }
        }
        protected static void AwaitWorkChunk(bool firstChunk) {
            while (threadsOutstanding != 0) {
                Application.DoEvents();
                Thread.Sleep(10);
                Application.DoEvents();
            }
        }
        protected static void MergeChunks(long chunksComplete, long totalChunks, int totalRewardOptions) {
            Profiling.StartZone("MergeChunks");
            IEnumerable<Evaluation> validEvaluations = perThreadEvaluations.Values.OrderBy(x => x.Id);
            if (!validEvaluations.Any()) {
                return;
            }
            validEvaluations = PruneSubOptimalPaths(validEvaluations);
            SetEvaluationOffRamps(validEvaluations);
            foreach (var eval in validEvaluations) {
                Scoring.ScoreBasedOnOffRamp(eval);
            }
            foreach (var eval in validEvaluations) {
                eval.MergeScoreWithOffRamp();
            }
            // For debugging determinism
            //var rewardsChosenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join(',', validEvaluations.Select(x => x.Score)))));
            var sorted = validEvaluations.OrderByDescending(x => x.Score).ToArray();
            PumpernickelAdviceWindow.instance.SetEvaluations(sorted, chunksComplete + 1, totalChunks);
            Profiling.StopZone("MergeChunks");
        }
        public static void AdviseOnReward(RewardOption option, bool needsMoreInfo = false) {
            AdviseOnRewards(new List<RewardOption>() { option }, needsMoreInfo: needsMoreInfo);
        }
        public static void AdviseOnRewards(List<RewardOption> rewardOptions, IEnumerable<string> previousAdvice = null, bool needsMoreInfo = false) {
            if (rewardOptions == null) {
                rewardOptions = new List<RewardOption>();
            }
            var isShop = rewardOptions.Any(x => x.cost != 0);
            var currentNode = Save.state.GetCurrentNode();
            var eligibleForBlueKey = currentNode?.nodeType == NodeType.Chest && !Save.state.has_sapphire_key;
            Evaluators.ReorderOptions(rewardOptions);
            Evaluators.SkipUnpalatableOptions(rewardOptions);
            MultiplexRewards(rewardOptions, eligibleForBlueKey, isShop, previousAdvice, needsMoreInfo);
        }
        protected struct OfframpGroup {
            public MapNode finalPosition;
            public long rewardOption;
            public int fireChoice;
            public OfframpGroup(Evaluation evaluation) {
                fireChoice = 0;
                rewardOption = evaluation.RewardIndex;
                finalPosition = null;
                for (int i = 0; i < evaluation.Path.nodes.Length; i++) {
                    var node = evaluation.Path.nodes[i];
                    if (node.nodeType == NodeType.Fire) {
                        fireChoice *= ((int)FireChoice.COUNT) - 1;
                        fireChoice += (int)evaluation.Path.fireChoices[i];
                    }
                    else {
                        finalPosition = node;
                        break;
                    }
                }
            }
            public override bool Equals([NotNullWhen(true)] object? obj) {
                if (obj is OfframpGroup other) {
                    return other.finalPosition == this.finalPosition &&
                        other.rewardOption == this.rewardOption &&
                        other.fireChoice == this.fireChoice;
                }
                return base.Equals(obj);
            }
        }
        protected static float PruningScore(Evaluation evaluation) {
            // This gets set each time a chunk of evaluations finishes, so new evaluations won't have one yet
            // Therefore, remove it so that it's fair
            return evaluation.InternalScore - evaluation.InternalScores[(int)ScoreReason.ActSurvival];
        }
        protected static IEnumerable<Evaluation> PruneSubOptimalPaths(IEnumerable<Evaluation> evaluations) {
            foreach (var offRampGroup in evaluations.GroupBy(x => new OfframpGroup(x))) {
                var evalsByScore = offRampGroup.OrderByDescending(PruningScore).ToArray();
                var bestChanceSoFar = -1f;
                for (int i = 0;  i < evalsByScore.Length; i++) {
                    if (evalsByScore[i].Path.chanceToSurviveAct > bestChanceSoFar) {
                        bestChanceSoFar = evalsByScore[i].Path.chanceToSurviveAct;
                        yield return evalsByScore[i];
                    }
                }
            }
        }
        protected static void SetEvaluationOffRamps(IEnumerable<Evaluation> evaluations) {
            foreach (var offRampGroup in evaluations.GroupBy(x => new OfframpGroup(x))) {
                var sortedPathThisWay = offRampGroup.OrderByDescending(PruningScore);
                foreach (var evaluation in offRampGroup) {
                    var saferPathThisWay = sortedPathThisWay
                        .Where(x => x.Path.chanceToSurviveAct > evaluation.Path.chanceToSurviveAct)
                        .FirstOrDefault();
                    evaluation.OffRamp = saferPathThisWay;
                }
            }
        }
        protected static readonly int WORK_CHUNK_SIZE = 10000;
        protected static long NeededWorkChunks(long totalEvaluations) {
            if (totalEvaluations % WORK_CHUNK_SIZE == 0) {
                return totalEvaluations / WORK_CHUNK_SIZE;
            }
            return (totalEvaluations / WORK_CHUNK_SIZE) + 1;
        }
    }
}