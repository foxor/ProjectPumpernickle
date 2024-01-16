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
            Weaknesses = new float[] { 0f, 0f };
            if (c.tags.ContainsKey(Tags.Block.ToString())) {
                Weaknesses[1] += 1f;
            }
            else if (c.tags.ContainsKey(Tags.Damage.ToString())) {
                Weaknesses[0] += .8f;
                Weaknesses[1] += .2f;
            }
        }

        public static float AnalyzeWeakness(WeaknessAxis axis) {
            switch (axis) {
                case WeaknessAxis.Lethality: {
                    // You start out very far behind on damage
                    var earlyBonus = (5f / (Save.state.floor_num / 3f + 1)) + 1;
                    var observed = FightSimulator.EstimateDamagePerTurn();
                    var projected = FightSimulator.NormalDamageForFloor(Save.state.floor_num) * earlyBonus;
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
