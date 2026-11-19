using System;

namespace BeybladeCore
{
    /// <summary>Bir parçanın stat katkısı (Unity'den bağımsız).</summary>
    public struct PartStats
    {
        public float Attack;
        public float Defense;
        public float Stamina;
        public float Weight;

        public PartStats(float attack, float defense, float stamina, float weight)
        {
            Attack = attack;
            Defense = defense;
            Stamina = stamina;
            Weight = weight;
        }
    }

    /// <summary>Sıra BeybladeType ile aynı: Saldiri, Savunma, Dayaniklilik, Denge.</summary>
    public enum BeyArchetype
    {
        Attack = 0,
        Defense = 1,
        Stamina = 2,
        Balance = 3
    }

    /// <summary>Parçalardan hesaplanan son statlar.</summary>
    public struct BeyStatBlock
    {
        public float Attack;        // 2..10
        public float Defense;       // 0.05..0.9
        public float StaminaDecay;  // 0.45..1.3 (düşük = uzun dayanır)
        public float Weight;        // 1.4..6
        public BeyArchetype Archetype;

        public float AttackNorm => BeyStatCalculator.InverseLerp(BeyStatCalculator.AttackMin, BeyStatCalculator.AttackMax, Attack);
        public float DefenseNorm => BeyStatCalculator.InverseLerp(BeyStatCalculator.DefenseMin, BeyStatCalculator.DefenseMax, Defense);
        public float StaminaNorm => BeyStatCalculator.InverseLerp(BeyStatCalculator.DecayMax, BeyStatCalculator.DecayMin, StaminaDecay);
        public float WeightNorm => BeyStatCalculator.InverseLerp(BeyStatCalculator.WeightMin, BeyStatCalculator.WeightMax, Weight);
    }

    /// <summary>
    /// Parça statlarını oyun statlarına çevirir.
    /// Stamina katkısı yüksekse dönüş kaybı (decay) azalır.
    /// </summary>
    public static class BeyStatCalculator
    {
        public const float AttackMin = 2f, AttackMax = 10f;
        public const float DefenseMin = 0.05f, DefenseMax = 0.9f;
        public const float DecayMin = 0.45f, DecayMax = 1.3f;
        public const float WeightMin = 1.4f, WeightMax = 6f;

        public const float BaseAttack = 2f;
        public const float BaseDefense = 0.1f;
        public const float BaseDecay = 1.25f;
        public const float DecayPerStamina = 0.1f;
        public const float BaseWeight = 0.8f;

        /// <summary>En yüksek iki stat bu farktan yakınsa tip "Denge" olur.</summary>
        public const float BalanceMargin = 0.1f;

        public static BeyStatBlock Compute(params PartStats[] parts)
        {
            float atk = BaseAttack, def = BaseDefense, stm = 0f, wgt = BaseWeight;
            if (parts != null)
            {
                foreach (PartStats p in parts)
                {
                    atk += p.Attack;
                    def += p.Defense;
                    stm += p.Stamina;
                    wgt += p.Weight;
                }
            }

            var block = new BeyStatBlock
            {
                Attack = Clamp(atk, AttackMin, AttackMax),
                Defense = Clamp(def, DefenseMin, DefenseMax),
                StaminaDecay = Clamp(BaseDecay - DecayPerStamina * stm, DecayMin, DecayMax),
                Weight = Clamp(wgt, WeightMin, WeightMax)
            };
            block.Archetype = Classify(block.AttackNorm, block.DefenseNorm, block.StaminaNorm);
            return block;
        }

        public static BeyArchetype Classify(float attackNorm, float defenseNorm, float staminaNorm)
        {
            float[] v = { attackNorm, defenseNorm, staminaNorm };
            int best = 0;
            for (int i = 1; i < 3; i++) if (v[i] > v[best]) best = i;
            float second = float.MinValue;
            for (int i = 0; i < 3; i++) if (i != best && v[i] > second) second = v[i];
            if (v[best] - second < BalanceMargin) return BeyArchetype.Balance;
            return (BeyArchetype)best;
        }

        public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);

        public static float InverseLerp(float a, float b, float v)
        {
            if (Math.Abs(b - a) < 1e-6f) return 0f;
            return Clamp((v - a) / (b - a), 0f, 1f);
        }

        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp(t, 0f, 1f);
    }
}
