using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    // There are 2 ways to enforce the limit on the deck size:
    //  1) We want to be able to get an infinite against the heart
    //  2) We want to be able to infinite now if possible
    internal class RewardInfinite : IGlobalRule {
        public static readonly float END_OF_GAME_POINTS = 12f;
        public static readonly float NEMESIS_POINTS = 6f;
        public static readonly float NOW_POINTS = 12f;
        bool IGlobalRule.ShouldApply {
            get {
                return Save.state.buildingInfinite;
            }
        }

        void IGlobalRule.Apply(Evaluation evaluation) {
            var room = Save.state.infiniteMaxSize - Evaluators.PermanentDeckSize() - Save.state.missingCardCount;
            var expectedCardRemoves = evaluation.Path.EndOfActPath ? Path.ExpectedFutureActCardRemoves() : evaluation.Path.ExpectedPossibleCardRemoves();
            var expectedPreNemesisRemoves = evaluation.Path.EndOfActPath ? Path.ExpectedFutureActCardRemovesBeforeNemesis() : evaluation.Path.ExpectedPossibleCardRemovesBeforeNemesis();
            var finalRoom = room + expectedCardRemoves;
            var nemesisRoom = room + expectedPreNemesisRemoves;
            var infiniteNow = room >= 0;
            var infiniteEnd = finalRoom >= 2f;
            var infiniteNemesis = nemesisRoom >= 5f;
            var infiniteQuality = Evaluators.CurrentInfiniteQuality();
            if (Save.state.relics.Contains("Medical Kit")) {
                infiniteEnd = finalRoom >= 0;
                infiniteNemesis = nemesisRoom >= 0;
            }
            evaluation.AddScore(ScoreReason.InfiniteNow, (infiniteNow ? 1 : -1) * NOW_POINTS * infiniteQuality);
            evaluation.AddScore(ScoreReason.InfiniteByHeart, (infiniteEnd ? 1 : -1) * END_OF_GAME_POINTS * infiniteQuality);
            evaluation.AddScore(ScoreReason.InfiniteNemesis, (infiniteNemesis ? 1 : -1) * NEMESIS_POINTS * infiniteQuality);
        }
    }
}
