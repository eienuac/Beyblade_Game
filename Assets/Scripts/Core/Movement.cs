using System;

namespace BeybladeCore
{
    /// <summary>
    /// Tipin kendi kendine (yönlendirme olmadan) arenadaki hareketi.
    /// Gerçek beyler gibi: Saldırı kenara yakın hızlı tur atar, Denge orta halkada döner,
    /// Dayanıklılık merkeze yakın yavaş süzülür, Savunma merkezi tutar.
    /// Beyler artık birbirine mıknatıs gibi çekilmez; saldırı kararlarını AI/oyuncu verir.
    /// </summary>
    public struct MovementProfile
    {
        /// <summary>Arena yarıçapına oranla tercih edilen yörünge yarıçapı.</summary>
        public float PreferredRadius;
        /// <summary>Tam dönüşte hedef tur hızı (m/s).</summary>
        public float OrbitSpeed;
        public float RadialGain;
        public float TangentGain;
        public float MaxAccel;
        /// <summary>Rastgele yalpa ivmesi.</summary>
        public float Wander;
    }

    public static class Movement
    {
        public static MovementProfile ProfileFor(in FighterProfile f)
        {
            switch (f.Archetype)
            {
                case BeyArchetype.Attack:
                    return new MovementProfile
                    {
                        PreferredRadius = 0.55f,
                        OrbitSpeed = BeyStatCalculator.Lerp(4.2f, 6f, f.A),
                        RadialGain = 3.6f,
                        TangentGain = 2.4f,
                        MaxAccel = 15f,
                        Wander = 1.2f
                    };
                case BeyArchetype.Defense:
                    return new MovementProfile
                    {
                        PreferredRadius = 0.05f,
                        OrbitSpeed = 1.3f,
                        RadialGain = 3.6f,
                        TangentGain = 1.2f,
                        MaxAccel = 8f,
                        Wander = 0.5f
                    };
                case BeyArchetype.Stamina:
                    return new MovementProfile
                    {
                        PreferredRadius = 0.24f,
                        OrbitSpeed = 2.6f,
                        RadialGain = 3.2f,
                        TangentGain = 1.6f,
                        MaxAccel = 9f,
                        Wander = 0.6f
                    };
                default:
                    return new MovementProfile
                    {
                        PreferredRadius = 0.42f,
                        OrbitSpeed = 4.2f,
                        RadialGain = 3f,
                        TangentGain = 2f,
                        MaxAccel = 12f,
                        Wander = 0.9f
                    };
            }
        }

        /// <summary>
        /// Otomatik hareket ivmesi. pos arena merkezine göredir.
        /// orbitSign: +1 saat yönünün tersi, -1 saat yönü.
        /// </summary>
        public static Vec2 AutoAccel(in MovementProfile m, Vec2 pos, Vec2 vel, float arenaRadius, float rpm, float orbitSign)
        {
            float spin = 0.45f + 0.55f * BeyStatCalculator.Clamp(rpm, 0f, 1f);
            float r = pos.Length;
            Vec2 radial = r > 1e-3f ? pos / r : new Vec2(1f, 0f);
            float targetR = m.PreferredRadius * arenaRadius;

            Vec2 a = -radial * ((r - targetR) * m.RadialGain);

            if (r > 0.35f)
            {
                Vec2 tangent = radial.Perp * orbitSign;
                float vt = Vec2.Dot(vel, tangent);
                a += tangent * ((m.OrbitSpeed * spin - vt) * m.TangentGain);
            }

            return a.ClampLength(m.MaxAccel * spin);
        }

        public static float SteerAccel(in FighterProfile f) => BeyStatCalculator.Lerp(10f, 16f, f.A);
    }

    /// <summary>Özel güç değerleri (SkillSystem ve BattleSim ortak kullanır). q = zamanlama kalitesi 0..1.</summary>
    public static class SkillTuning
    {
        // Saldırı ultisi (kilitlen + sürükle + final savrulma)
        public const float UltDuration = 2.6f;
        public static float UltLockSpeed(float q) => BeyStatCalculator.Lerp(9f, 12f, q);
        public static float UltDragSpeed(float q) => 4.0f * BeyStatCalculator.Lerp(0.85f, 1.15f, q);
        public static float UltDragDps = 4.5f;
        public const float UltFinaleBaseKnock = 3.4f;
        public const float UltFinaleMult = 2.2f;
        public static float UltFinaleDamage(float q) => BeyStatCalculator.Lerp(5f, 10f, q);

        // Savunma kalkanı
        public static float ShieldDuration(float q) => BeyStatCalculator.Lerp(3.2f, 4.6f, q);
        public static float ShieldDamageTaken(float q) => BeyStatCalculator.Lerp(0.62f, 0.4f, q);
        public const float ShieldKnockMult = 0.5f;
        public static float ParryImpulse(float q) => BeyStatCalculator.Lerp(4.5f, 8f, q);
        public static float ParryDamageLow = 0.8f, ParryDamageHigh = 1.8f;
        public static float ParryDamage(float q) => BeyStatCalculator.Lerp(ParryDamageLow, ParryDamageHigh, q);

        // Dayanıklılık yenilenmesi
        public static float RegenDuration(float q) => BeyStatCalculator.Lerp(2.8f, 4f, q);
        public static float RegenHealLow = 4.3f, RegenHealHigh = 8.6f;
        public static float RegenHeal(float q) => BeyStatCalculator.Lerp(RegenHealLow, RegenHealHigh, q);
        public static float RegenDamageTaken(float q) => BeyStatCalculator.Lerp(0.85f, 0.68f, q);

        // Denge burst (yörünge + hücum)
        public const float BurstOrbitTime = 0.85f;
        public static float BurstRushTime(float q) => BeyStatCalculator.Lerp(2.6f, 3.6f, q);
        public static float BurstDamageMultLow = 1.12f, BurstDamageMultHigh = 1.3f;
        public static float BurstDamageMult(float q) => BeyStatCalculator.Lerp(BurstDamageMultLow, BurstDamageMultHigh, q);
        public static float BurstHealLow = 2.7f, BurstHealHigh = 5.4f;
        public static float BurstHeal(float q) => BeyStatCalculator.Lerp(BurstHealLow, BurstHealHigh, q);
        public static float BurstPush(float q) => BeyStatCalculator.Lerp(1.8f, 4.2f, q);
        public static float BurstOrbitForce(float q) => BeyStatCalculator.Lerp(14f, 20f, q);
        public static float BurstRushPull(float q) => BeyStatCalculator.Lerp(12f, 18f, q);
        public static float BurstOrbitDegPerSec(float q) => BeyStatCalculator.Lerp(220f, 320f, q);
    }
}
