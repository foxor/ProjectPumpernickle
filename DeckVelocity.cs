using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class DeckVelocity : IGlobalRule {
        public GlobalRuleEvaluationTiming Timing => GlobalRuleEvaluationTiming.PreCardEvaluation;

        public static readonly float SHUFFLE_PER_TURN_VELOCITY_SCORE = 6f;
        public static readonly float MAX_SETUP_SCORE = 4f;
        public void Apply(Evaluation evaluation) {
            var setupCards = Evaluators.OneTimeDrawEffects();
            var sustainCards = Evaluators.SustainableCardDrawPerTurn();
            var totalCards = Save.state.cards.Count();

            var setupCost = Evaluators.CostOfNonPermanent();
            var setupEnergy = Evaluators.ExtraPerFightEnergy();
            var sustainEnergy = Evaluators.PerTurnEnergy();

            var permenantCards = Evaluators.PermanentDeckSize();
            var sustainableDeckFraction = sustainCards / permenantCards;
            evaluation.SetScore(ScoreReason.SustainableSpeed, sustainableDeckFraction * SHUFFLE_PER_TURN_VELOCITY_SCORE);

            // t * sustainEnergy + setupEnergy = setupCost
            // t * sustainCards + setupCards = fullDeck
            var energySetupTurn = MathF.Max(1f, (setupCost - setupEnergy) / sustainEnergy);
            var firstShuffleTurn = MathF.Max(1f, (totalCards - setupCards) / sustainCards);
            var setupByTurn = (energySetupTurn + firstShuffleTurn) / 2f;
            evaluation.SetScore(ScoreReason.SetupSpeed, MAX_SETUP_SCORE / setupByTurn);
        }
    }
}
