using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class EventAdvice {
        public static Evaluation[] GoldenIdolEvent() {
            var existingAdvice = new List<string>() {
                "Take the golden idol, give up max hp"
            };
            return Advice.AddPathingAdvice(existingAdvice);
        }

        public static Evaluation[] Vampires() {
            var existingAdvice = new List<string>() {
                "Refuse the bites"
            };
            return Advice.AddPathingAdvice(existingAdvice);
        }

        public static Evaluation[] TheMausoleum() {
            var existingAdvice = new List<string>() {
                "Leave the Mausoleum"
            };
            return Advice.AddPathingAdvice(existingAdvice);
        }

        public static Evaluation[] MindBloom() {
            var existingAdvice = new List<string>() {
                "Fight the act 1 boss"
            };
            var evals = Advice.AddPathingAdvice(existingAdvice);
            foreach (var eval in evals) {
                eval.NeedsMoreInfo = true;
            }
            return evals;
        }

        public static Evaluation[] Duplicator() {
            var existingAdvice = new List<string>() {
                "Duplicate " + Database.instance.cardsDict[Evaluators.BestCopyTarget()].name
            };
            return Advice.AddPathingAdvice(existingAdvice);
        }
    }
}
