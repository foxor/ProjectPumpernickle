using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class EventAdvice {
        public static Evaluation GoldenIdolEvent() {
            var existingAdvice = new List<string>() {
                "Take the golden idol, give up max hp"
            };
            var evaluation = PathAdvice.Evaluate(existingAdvice);
            return evaluation;
        }
    }
}
