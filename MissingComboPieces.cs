using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class MissingComboPieces : IGlobalRule {
        public static readonly float END_OF_GAME_POINTS = 4f;
        public static readonly float NOW_POINTS = 1.5f;
        bool IGlobalRule.ShouldApply {
            get {
                return Save.state.buildingInfinite;
            }
        }

        float IGlobalRule.Apply(Path path) {
            // TODO: how rare are the cards?
            return 0f;
        }
    }
}
