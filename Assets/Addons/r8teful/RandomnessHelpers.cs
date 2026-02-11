using System;
using System.Collections.Generic;
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
        public static bool GetBoolRoll(float luck, float baseChance = 0.5f) {
            float luckBonus = LuckBonus(luck);
            return Skewed01(luckBonus) >=  (1 - baseChance); // if base chanse is 80% then we have to roll higher than 0.2
        }
        // Helpers 
        private static float Skewed01(float luckBonus) {
            float exponent = 1f / (1f + luckBonus);
            return Mathf.Pow(UnityEngine.Random.value, exponent);
        }
        private static float LuckBonus(float luck) {
            return Mathf.Log(luck + 1f) * 1.2f;
        }


        // Returns random points within an area
        public static List<Vector2> PoissonDisc(float width, float height, float minDist, int seed, int k = 50) {
            float cellSize = minDist / Mathf.Sqrt(2f);
            int gridW = Mathf.CeilToInt(width / cellSize);
            int gridH = Mathf.CeilToInt(height / cellSize);

            Vector2[,] grid = new Vector2[gridW, gridH];
            bool[,] has = new bool[gridW, gridH];

            var rand = new System.Random(seed);

            List<Vector2> points = new List<Vector2>();
            List<Vector2> active = new List<Vector2>();

            // initial sample
            Vector2 first = new Vector2((float)rand.NextDouble() * width, (float)rand.NextDouble() * height);
            points.Add(first);
            active.Add(first);
            int gi = (int)(first.x / cellSize);
            int gj = (int)(first.y / cellSize);
            grid[gi, gj] = first;
            has[gi, gj] = true;

            while (active.Count > 0) {
                int idx = rand.Next(active.Count);
                Vector2 cur = active[idx];
                bool found = false;

                for (int i = 0; i < k; i++) {
                    // sample in annulus [minDist, 2*minDist)
                    float radius = minDist * (1f + (float)rand.NextDouble());
                    float angle = (float)rand.NextDouble() * Mathf.PI * 2f;
                    Vector2 cand = cur + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                    if (cand.x < 0 || cand.x >= width || cand.y < 0 || cand.y >= height) continue;

                    int ci = (int)(cand.x / cellSize);
                    int cj = (int)(cand.y / cellSize);

                    bool ok = true;
                    // check neighbors within 2 cells in each direction (safe bound)
                    int i0 = Mathf.Max(0, ci - 2), i1 = Mathf.Min(gridW - 1, ci + 2);
                    int j0 = Mathf.Max(0, cj - 2), j1 = Mathf.Min(gridH - 1, cj + 2);
                    for (int gx = i0; gx <= i1 && ok; gx++) {
                        for (int gy = j0; gy <= j1; gy++) {
                            if (!has[gx, gy]) continue;
                            if (Vector2.SqrMagnitude(grid[gx, gy] - cand) < minDist * minDist) {
                                ok = false; break;
                            }
                        }
                    }

                    if (ok) {
                        points.Add(cand);
                        active.Add(cand);
                        grid[ci, cj] = cand;
                        has[ci, cj] = true;
                        found = true;
                        break;
                    }
                }

                if (!found) {
                    // no candidate found, remove active
                    active.RemoveAt(idx);
                }
            }

            return points;
        }

        internal static bool TryGetCritDamage(AbilityInstance abilityInstance, out float critMult) {
            critMult = 1;
            var critChance = abilityInstance.GetEffectiveStat(StatType.MiningCritChance);
            Debug.Log("crit chance! " + critChance);
            var critDmg = abilityInstance.GetEffectiveStat(StatType.MiningCritDamage);
            if (UnityEngine.Random.value < critChance) {
                // crit!
                critMult = critDmg;
                return true;
            }
            return false;
        }
    }
}