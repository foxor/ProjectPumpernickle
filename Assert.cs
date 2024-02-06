using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class Assert {
        public static void Break(int id = 4831) {
            if (Evaluation.Active.Id == id) {
                Console.WriteLine();
            }
        }
    }
}
