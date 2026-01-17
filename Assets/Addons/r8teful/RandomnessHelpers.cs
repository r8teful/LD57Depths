using System;
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
    }
}