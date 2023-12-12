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
        public static readonly float GOLD_AT_SHOP_PER_POINT = 150f;

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

        public static void ApplyVariance(Evaluation[] evaluations, Evaluation preRewardScore) {
            // Evaluations are projected optimistically
            // Normally, we want to punish overly optimistic evaluations,
            // however, if all the evaluations are risky, we want to pick 
            // evaluations that actually have a shot
            var defaultChanceToSurvive = preRewardScore.Path.chanceToSurvive;
            var defaultScore = preRewardScore.Score;
            var availableSafetyFactor = Lerp.Inverse(0.5f, 1f, defaultChanceToSurvive);
            foreach (var evaluation in evaluations) {
                var worstCaseScore = defaultScore;
                var worstCaseEstimatedScore = Lerp.From(worstCaseScore, evaluation.Score, evaluation.WorstCaseRewardFactor);
                var maxScoreLoss = worstCaseEstimatedScore - evaluation.Score;
                if (evaluation.RewardVariance != 0f) {
                    var failureChance = 1f - evaluation.RewardVariance;
                    var variancePenalty = maxScoreLoss * failureChance * availableSafetyFactor;
                    evaluation.AddScore(ScoreReason.Variance, variancePenalty);
                    evaluation.NeedsMoreInfo = true;
                }
            }
        }

        public static void ScorePath(Evaluation evaluation) {
            var path = evaluation.Path;

            evaluation.AddScore(ScoreReason.ActSurvival, 10f * path.ChanceToSurviveAct(1));
            evaluation.AddScore(ScoreReason.ActSurvival, 10f * path.ChanceToSurviveAct(2));
            evaluation.AddScore(ScoreReason.ActSurvival, 10f * path.ChanceToSurviveAct(3));

            evaluation.AddScore(ScoreReason.Upgrades, 5f * Evaluators.PercentAllGreen(evaluation));

            var expectedRelics = path.expectedRewardRelics[^1];
            evaluation.AddScore(ScoreReason.RelicCount, expectedRelics);

            var expectedCardRewards = path.expectedCardRewards[^1];
            evaluation.AddScore(ScoreReason.CardReward, .5f * expectedCardRewards);

            evaluation.AddScore(ScoreReason.Key, Save.state.has_emerald_key ? .1f : 0);
            evaluation.AddScore(ScoreReason.Key, Save.state.has_ruby_key ? .1f : 0);
            evaluation.AddScore(ScoreReason.Key, Save.state.has_sapphire_key ? .1f : 0);

            var effectiveHealth = Evaluators.GetEffectiveHealth();
            evaluation.AddScore(ScoreReason.CurrentEffectiveHealth, effectiveHealth / 10f);

            // this is too high
            var goldBrought = path.ExpectedGoldBroughtToShops();
            evaluation.AddScore(ScoreReason.BringGoldToShop, goldBrought / GOLD_AT_SHOP_PER_POINT);

            path.AddEventScore(evaluation);

            if (Save.state.act_num < 3 && (Save.state.has_emerald_key || path.hasMegaElite)) {
                evaluation.AddScore(ScoreReason.EarlyMegaElite, 2f);
            }

            if (Save.state.act_num == 3 && !Save.state.has_emerald_key && path.hasMegaElite != true) {
                evaluation.AddScore(ScoreReason.MissingKey, float.MinValue);
            }
            if (Save.state.act_num == 3 && !Save.state.has_sapphire_key && path.ContainsGuaranteedChest()) {
                evaluation.AddScore(ScoreReason.MissingKey, float.MinValue);
            }

            if (Save.state.badBottle) {
                evaluation.AddScore(ScoreReason.BadBottle, -5f);
            }
        }
        public static void Score(Evaluation evaluation) {
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
            EvaluateGlobalRules(evaluation);
            ScorePath(evaluation);
        }
    }
}
