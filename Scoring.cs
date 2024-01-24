using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    public interface IGlobalRule {
        public bool ShouldApply { get; }
        public void Apply(Evaluation evaluation);
    }
    internal class Scoring {
        protected static IGlobalRule[] GlobalRules;
        static Scoring() {
            var globalRuleTypes = typeof(Scoring).Assembly.GetTypes().Where(x => typeof(IGlobalRule).IsAssignableFrom(x) && typeof(IGlobalRule) != x);
            GlobalRules = globalRuleTypes.Select(x => (IGlobalRule)Activator.CreateInstance(x)).ToArray();
        }
        public static void EvaluateGlobalRules(Evaluation evaluation) {
            foreach (var globalRule in GlobalRules) {
                if (globalRule.ShouldApply) {
                    globalRule.Apply(evaluation);
                }
            }
        }

        public static void ApplyVariance(Evaluation[] evaluations) {
            // Evaluations are projected optimistically
            // Normally, we want to punish overly optimistic evaluations,
            // however, if all the evaluations are risky, we want to pick 
            // evaluations that actually have a shot
            var highestLikelihood = evaluations.Select(x => x.Likelihood).Max();
            var defaultEval = evaluations.Where(x => x.Likelihood == highestLikelihood).OrderByDescending(x => x.Score).First();
            var defaultChanceToSurvive = defaultEval.Path.chanceToWin;
            var defaultScore = defaultEval.Score;
            var availableSafetyFactor = MathF.Pow(defaultChanceToSurvive, 2f);
            foreach (var evaluation in evaluations) {
                if (evaluation.Likelihood != 1f) {
                    // Our current score is proportional to the best case reward
                    // The goal here is to interpolate towards a more average result
                    // We took a sample of the probability distribution when we allocated the rewards
                    // We will assume that the score probability distribution is similarly shaped
                    var worstCaseEstimatedScore = Lerp.FromUncapped(defaultScore, evaluation.Score, evaluation.WorstCaseRewardFactor);
                    var maxScoreLoss = worstCaseEstimatedScore - evaluation.Score;

                    var replacementProposition = evaluation.AverageCaseRewardFactor * (1f - availableSafetyFactor);
                    // best case value proportion is bestValue / bestValue => 1
                    var failureProportion = 1f - Lerp.Inverse(evaluation.WorstCaseRewardFactor, 1f, replacementProposition);
                    var variancePenalty = maxScoreLoss * failureProportion * (1f - evaluation.Likelihood);
                    
                    evaluation.AddScore(ScoreReason.Variance, variancePenalty);
                    evaluation.NeedsMoreInfo = true;
                }
            }
        }
        public static readonly float MAX_SHOP_VALUE = 5f;
        public static float PointsForShop(float goldBrought) {
            if (goldBrought < 180f) {
                return 0f;
            }
            // https://www.wolframalpha.com/input?i=1+-+%281+%2F+%281+%2B+%28%28x+-+180%29+%2F+360%29%29%29+from+0+to+1000
            var shopValue = 1f - (1f / (1 + ((goldBrought - 180f) / 360f)));
            return (float)shopValue * MAX_SHOP_VALUE;
        }
        public static void ScorePath(Evaluation evaluation) {
            // This doesn't ever award points for future acts to avoid perverse incentives
            var path = evaluation.Path;
            var floorsTillEndOfAct = Evaluators.LastFloorThisAct(Save.state.act_num) - Save.state.floor_num;


            evaluation.AddScore(ScoreReason.Upgrades, 20f * Evaluators.PercentAllGreen(evaluation));

            var expectedRelics = path.expectedRewardRelics[floorsTillEndOfAct];
            // Should we take an average?
            evaluation.AddScore(ScoreReason.RelicCount, expectedRelics);

            var expectedCardRewards = path.expectedCardRewards[floorsTillEndOfAct];
            var cardRewardValue = 1f - (Lerp.Inverse(0f, 40f, Save.state.floor_num) * .8f);
            evaluation.AddScore(ScoreReason.CardReward, .1f * expectedCardRewards * cardRewardValue);

            evaluation.AddScore(ScoreReason.Key, Save.state.has_emerald_key ? .1f : 0);
            evaluation.AddScore(ScoreReason.Key, Save.state.has_ruby_key ? .1f : 0);
            evaluation.AddScore(ScoreReason.Key, Save.state.has_sapphire_key ? .1f : 0);

            var effectiveHealth = Evaluators.GetCurrentEffectiveHealth();
            evaluation.AddScore(ScoreReason.CurrentEffectiveHealth, effectiveHealth / 30f);

            // This does incorporate future act points, because otherwise we might misbehave
            var goldBrought = path.ExpectedGoldBroughtToShops();
            evaluation.AddScore(ScoreReason.BringGoldToShop, goldBrought.Select(PointsForShop).Sum());

            var wingedBootChargesLeft = Math.Max(Evaluators.WingedBootsChargesLeft(), 0) - path.jumps;
            var actsLeft = Math.Max(3 - Save.state.act_num, 0);
            evaluation.AddScore(ScoreReason.WingedBootsCharges, wingedBootChargesLeft * actsLeft * 0.03f);
            evaluation.AddScore(ScoreReason.WingedBootsFlexibility, (wingedBootChargesLeft * actsLeft >= 1) ? 1.5f : 0f);

            path.AddEventScore(evaluation);

            if (Save.state.act_num < 3 && (Save.state.has_emerald_key || path.hasMegaElite)) {
                evaluation.AddScore(ScoreReason.EarlyMegaElite, 2f);
            }

            if (Save.state.act_num == 3 && !Save.state.has_emerald_key && path.hasMegaElite != true) {
                evaluation.AddScore(ScoreReason.MissingKey, float.MinValue);
            }
            if (Save.state.act_num == 3 && !Save.state.has_sapphire_key && !path.ContainsGuaranteedChest()) {
                evaluation.AddScore(ScoreReason.MissingKey, float.MinValue);
            }

            if (Save.state.badBottle) {
                evaluation.AddScore(ScoreReason.BadBottle, -5f);
            }
        }
        public static void Score(Evaluation evaluation) {
            for (int i = 0; i < Save.state.cards.Count; i++) {
                var card = Save.state.cards[i];
                var cardValue = EvaluationFunctionReflection.GetCardEvalFunctionCached(card.id)(card, i);
                evaluation.AddScore(ScoreReason.DeckQuality, cardValue);
            }
            for (int i = 0; i < Save.state.relics.Count; i++) {
                var relicId = Save.state.relics[i];
                // TODO: fix setup relics?
                var relic = Database.instance.relicsDict[relicId];
                evaluation.AddScore(ScoreReason.RelicQuality, EvaluationFunctionReflection.GetRelicEvalFunctionCached(relic.id)(relic));
            }
            EvaluateGlobalRules(evaluation);
            ScorePath(evaluation);
        }
        public static void ScoreAfterOffRampDetermined(Evaluation evaluation) {
            var offRamp = evaluation.OffRamp?.Path ?? evaluation.Path;
            // this has the potential to provide "phantom" points, where you plan a really ambitious path, and then chicken out when the off-ramp disappears
            // but that's kinda the right way to play the game
            evaluation.AddScore(ScoreReason.ActSurvival, 10f * offRamp.ChanceToSurviveAct(Save.state.act_num));
        }
    }
}
