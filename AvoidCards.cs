using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    // Smaller decks are generally better.  Cards are very slightly bad
    internal class AvoidCards : IGlobalRule {
        bool IGlobalRule.ShouldApply => true;

        void IGlobalRule.Apply(Evaluation evaluation) {
            // TODO: modulate this based on perminant cards / card draw
            evaluation.AddScore(ScoreReason.AvoidCard, MathF.Pow(Save.state.cards.Count / -25f, 3f));
        }
    }
}
