using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class PumpernickelMath {
        public static float Sigmoid(double value) {
            return (float)(1.0 / (1.0 + Math.Pow(Math.E, -value)));
        }
    }
}
