using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class PunishMissingCards : IGlobalRule {
        public static readonly float PENALTY_PER_MISSING_CARD = -1.5f;
        bool IGlobalRule.ShouldApply => Save.state.missingCardCount != 0;

        float IGlobalRule.Apply(Path path) {
            return Save.state.missingCardCount * PENALTY_PER_MISSING_CARD;
        }
    }
}
