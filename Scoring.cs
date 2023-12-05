using System;
using System.Collections.Generic;
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
                var failureChance = 1f - evaluation.RewardVariance;
                var variancePenalty = maxScoreLoss * failureChance * availableSafetyFactor;
                evaluation.AddScore(ScoreReason.Variance, variancePenalty);
            }
        }

        public static void ScorePath(Evaluation evaluation) {
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
            //  - 1 point per key
            //  - 1 point per 10 health
            //  - points for bringing gold to shops
            //  - -5 point if you made a bad bottle
            var path = evaluation.Path;

            evaluation.AddScore(ScoreReason.ActSurvival, 10f * path.chanceToSurvive);

            var futureUpgrades = path.expectedUpgrades[^1];
            var presentUpgrades = Save.state.cards.Select(x => x.upgrades).Sum();
            var upgrades = futureUpgrades + presentUpgrades;
            if (upgrades <= 4) {
                evaluation.AddScore(ScoreReason.UpgradeCount, upgrades * 2f);
            }
            else {
                evaluation.AddScore(ScoreReason.UpgradeCount, 8 + MathF.Min(upgrades - 4, 6));
            }

            var expectedRelics = (float)Save.state.relics.Count;
            expectedRelics += path.expectedRewardRelics[^1];
            evaluation.AddScore(ScoreReason.RelicCount, MathF.Min(expectedRelics, 15f));

            var expectedCardRewards = path.expectedCardRewards[^1];
            evaluation.AddScore(ScoreReason.CardReward, .5f * expectedCardRewards);

            evaluation.AddScore(ScoreReason.Key, Save.state.has_emerald_key ? .1f : 0);
            evaluation.AddScore(ScoreReason.Key, Save.state.has_ruby_key ? .1f : 0);
            evaluation.AddScore(ScoreReason.Key, Save.state.has_sapphire_key ? .1f : 0);

            var effectiveHealth = Evaluators.GetEffectiveHealth();
            evaluation.AddScore(ScoreReason.CurrentEffectiveHealth, effectiveHealth / 10f);

            var goldBrought = path.ExpectedGoldBroughtToShops();
            evaluation.AddScore(ScoreReason.BringGoldToShop, goldBrought / GOLD_AT_SHOP_PER_POINT);

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
    }
}
