using System;
using UnityEngine;
using Random = System.Random;

namespace r8teful {
    public static class RandomnessHelpers {
        public static int PickIndex(int[] weights, Random rng = null) {
            if (weights == null || weights.Length == 0) throw new ArgumentException("weights required");
            rng ??= new Random();

            long total = 0;
            foreach (var w in weights) {
                if (w < 0) throw new ArgumentException("weights must be non-negative");
                total += w;
            }
            if (total == 0) throw new ArgumentException("sum of weights must be > 0");

            // roll in [0, total)
            long r = (long)(rng.NextDouble() * total);

            long cum = 0;
            for (int i = 0; i < weights.Length; i++) {
                cum += weights[i];
                if (r < cum) return i;
            }
            return weights.Length - 1; 
        }
        public static int PickIndexWithLuck(int[] baseWeights, float luck) {
            int n = baseWeights.Length;
            if (n == 0) return -1;
            float rarityBase = 1.2f;
            float luckBonus = LuckBonus(luck);
            float[] adjusted = new float[n];
            float total = 0f;
            for (int i = 0; i < n; i++) {
                int distance = n - i - 1; // 0 for top rarity
                float mult = Mathf.Pow(rarityBase, -distance * luckBonus);
                mult = Mathf.Max(mult, 0.05f);
                adjusted[i] = baseWeights[i] * mult;
                total += adjusted[i];
            }

            // pick from adjusted weights
            if (total <= 0f) return 0;
            float r = UnityEngine.Random.value * total;
            float acc = 0f;
            for (int i = 0; i < n; i++) {
                acc += adjusted[i];
                if (r <= acc) return i;
            }
            return n - 1;
        }

        public static int GetDropScewed(int max, float luck) {
            float luckBonus = LuckBonus(luck);
            return Mathf.FloorToInt(Mathf.Lerp(1, max + 1, Skewed01(luckBonus)));
        }

        // Helpers 
        private static float Skewed01(float luckBonus) {
            float exponent = 1f / (1f + luckBonus);
            return Mathf.Pow(UnityEngine.Random.value, exponent);
        }
        private static float LuckBonus(float luck) {
            return Mathf.Log(luck + 1f) * 1.2f;
        }

    }
}