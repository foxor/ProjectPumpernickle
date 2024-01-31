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

            evaluation.AddScore(ScoreReason.Key, Save.state.has_sapphire_key ? .5f : 0);

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
        public static void ScoreBasedOnWinPerception(Evaluation evaluation) {
            var stats = evaluation.RewardStats ?? new RewardOutcomeStatistics();
            var winChance = stats.ChanceToWin(evaluation);
            evaluation.SetScore(ScoreReason.MeanCorrection , -(stats.chosenValue - stats.rewardOutcomeMean));
            evaluation.SetScore(ScoreReason.WinChance, winChance * 1f);
            evaluation.SetScore(ScoreReason.Variance, -stats.rewardOutcomeStd / 5f);
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
            ScoreBasedOnWinPerception(evaluation);
        }
        public static void ScoreAfterOffRampDetermined(Evaluation evaluation) {
            var offRamp = evaluation.OffRamp?.Path ?? evaluation.Path;
            // this has the potential to provide "phantom" points, where you plan a really ambitious path, and then chicken out when the off-ramp disappears
            // but that's kinda the right way to play the game
            evaluation.SetScore(ScoreReason.ActSurvival, 10f * offRamp.chanceToSurviveAct);
        }
    }
}
