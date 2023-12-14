using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    public enum WeaknessAxis : byte {
        Lethality,
        Survivabiltiy,
    }
    internal struct Weakness {
        public static readonly float RADICAL_TWO_OVER_TWO = (float)Math.Sqrt(2) / 2f;
        public float[] Weaknesses;
        // This is for weaknesses YOU have
        public Weakness() {
            Weaknesses = new float[2];
            Weaknesses[0] = AnalyzeWeakness(WeaknessAxis.Lethality);
            Weaknesses[1] = MathF.Sqrt(1f - Weaknesses[0] * Weaknesses[0]);
            var weaknessMagnitude = 1f / FightSimulator.EstimateDefensivePower();
            for (int i = 0; i < Weaknesses.Length; i++) {
                Weaknesses[i] *= weaknessMagnitude;
            }
        }
        // This is for the weaknesses the card addresses
        public Weakness(Card c) {
            if (c.tags.ContainsKey(Tags.Block.ToString())) {
                Weaknesses = new float[] { 0f, 1f };
            }
            else if (c.tags.ContainsKey(Tags.Damage.ToString())) {
                Weaknesses = new float[] { 1f, 0f };
            }
            else {
                Weaknesses = new float[] { RADICAL_TWO_OVER_TWO, RADICAL_TWO_OVER_TWO };
            }
        }

        public static float AnalyzeWeakness(WeaknessAxis axis) {
            switch (axis) {
                case WeaknessAxis.Lethality: {
                    var observed = FightSimulator.EstimateDamagePerTurn();
                    var projected = FightSimulator.NormalDamageForFloor(Save.state.floor_num);
                    var minExpected = projected * .8f;
                    var maxExpected = projected / .8f;
                    return 1f - Lerp.Inverse(minExpected, maxExpected, observed);
                }
                default: {
                    throw new System.NotImplementedException();
                }
            }
        }

        public static float operator *(Weakness a, Weakness b) {
            return Enumerable.Range(0, a.Weaknesses.Length).Select(x => a.Weaknesses[x] * b.Weaknesses[x]).Sum();
        }
    }
}
