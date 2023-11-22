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
            var evaluation = new Evaluation() {
                Advice = existingAdvice,
            };
            PathAdvice.Evaluate(evaluation);
            return evaluation;
        }

        public static Evaluation Vampires() {
            var existingAdvice = new List<string>() {
                "Refuse the bites"
            };
            var evaluation = new Evaluation() {
                Advice = existingAdvice,
            };
            PathAdvice.Evaluate(evaluation);
            return evaluation;
        }

        public static Evaluation TheMausoleum() {
            var existingAdvice = new List<string>() {
                "Leave the Mausoleum"
            };
            var evaluation = new Evaluation() {
                Advice = existingAdvice,
            };
            PathAdvice.Evaluate(evaluation);
            return evaluation;
        }

        public static Evaluation MindBloom() {
            var existingAdvice = new List<string>() {
                "Fight the act 1 boss"
            };
            var evaluation = new Evaluation() {
                Advice = existingAdvice,
                NeedsMoreInfo = true,
            };
            PathAdvice.Evaluate(evaluation);
            return evaluation;
        }

        public static Evaluation Duplicator() {
            var existingAdvice = new List<string>() {
                "Duplicate " + Database.instance.cardsDict[Evaluators.BestCopyTarget()].name
            };
            var evaluation = new Evaluation() {
                Advice = existingAdvice,
            };
            PathAdvice.Evaluate(evaluation);
            return evaluation;
        }
    }
}
