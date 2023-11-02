﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class Lerp {
        public static float From(float min, float max, float t) {
            if (t <= 0) {
                return min;
            }
            if (t >= max) {
                return max;
            }
            return (max - min) * t + min;
        }
        public static float FromUncapped(float min, float max, float t) {
            return (max - min) * t + min;
        }
        public static float Inverse(float min, float max, float value) {
            if (value < min) {
                return 0f;
            }
            if (value > max) {
                return 1f;
            }
            return (value - min) / (max - min);
        }
        public static float InverseUncapped(float min, float max, float value) {
            return (value - min) / (max - min);
        }
    }
}
