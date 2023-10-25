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
        public static readonly float NOW_POINTS = 1.5f;
        bool IGlobalRule.ShouldApply {
            get {
                return Save.state.buildingInfinite;
            }
        }

        float IGlobalRule.Apply(Path path) {
            var room = Save.state.infiniteRoom;
            var expectedCardRemoves = path.ExpectedPossibleCardRemoves();
            var finalRoom = room + expectedCardRemoves;
            var infiniteNow = room > 0;
            var infiniteEnd = finalRoom > 2f;
            return (infiniteNow ? 1 : -1) * NOW_POINTS + (infiniteEnd ? 1 : -1) * END_OF_GAME_POINTS;
        }
    }
}
