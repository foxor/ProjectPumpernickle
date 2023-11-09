using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class HuntForCards : IGlobalRule {
        public static readonly float POINT_FOR_FINDING_HUNTED_CARD = 8f;
        bool IGlobalRule.ShouldApply => Save.state.huntingCards.Any();

        float IGlobalRule.Apply(Path path) {
            var expectedHuntedCardsFound = path == null ? Path.ExpectedHuntedCardsFoundInFutureActs() : path.ExpectedHuntedCardsFound();
            return expectedHuntedCardsFound * POINT_FOR_FINDING_HUNTED_CARD;
        }
    }
}
