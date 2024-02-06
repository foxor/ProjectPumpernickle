namespace ProjectPumpernickle {
    internal class HuntForCards : IGlobalRule {
        public static readonly float PENALTY_MULTIPLIER = -8f;
        bool IGlobalRule.ShouldApply => Save.state.huntingCards.Any();

        void IGlobalRule.Apply(Evaluation evaluation) {
            //var expectedHuntedCardsFound = evaluation.Path.EndOfActPath ? Path.ExpectedHuntedCardsFoundInFutureActs() : evaluation.Path.ExpectedHuntedCardsFound();
            //var huntedCardsMissing = Save.state.missingCardCount;
            //var penaltyFactor = (huntedCardsMissing * 2) / (huntedCardsMissing + expectedHuntedCardsFound);
            //evaluation.AddScore(ScoreReason.MissingComboPieces, penaltyFactor * PENALTY_MULTIPLIER);
        }
    }
}
