using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    // There are 2 ways to enforce the limit on the deck size:
    //  1) We want to be able to get an infinite against the heart
    //  2) We want to be able to infinite now if possible
    internal class EnforceDeckSizeLimit : IGlobalRule {
        public static readonly float END_OF_GAME_POINTS = 4f;
        public static readonly float NEMESIS_POINTS = 2f;
        public static readonly float NOW_POINTS = 3.5f;
        bool IGlobalRule.ShouldApply {
            get {
                return Save.state.buildingInfinite;
            }
        }

        float IGlobalRule.Apply(Path path) {
            var room = Save.state.infiniteRoom;
            var expectedCardRemoves = path == null ? Path.ExpectedFutureActCardRemoves() : path.ExpectedPossibleCardRemoves();
            var expectedPreNemesisRemoves = path == null ? Path.ExpectedFutureActCardRemovesBeforeNemesis() : path.ExpectedPossibleCardRemovesBeforeNemesis();
            var finalRoom = room + expectedCardRemoves;
            var nemesisRoom = room + expectedPreNemesisRemoves;
            var infiniteNow = room >= 0;
            var infiniteEnd = finalRoom >= 2f;
            var infiniteNemesis = nemesisRoom >= 5f;
            if (Save.state.relics.Contains("Medical Kit")) {
                infiniteEnd = finalRoom >= 0;
                infiniteNemesis = nemesisRoom >= 0;
            }
            return (infiniteNow ? 1 : -1) * NOW_POINTS +
                (infiniteEnd ? 1 : -1) * END_OF_GAME_POINTS +
                (infiniteNemesis ? 1 : -1) * NEMESIS_POINTS;
        }
    }
}
