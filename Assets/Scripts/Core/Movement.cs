using System;

namespace BeybladeCore
{
    /// <summary>
    /// Tipin kendi kendine (yönlendirme olmadan) arenadaki hareketi.
    /// Gerçek beyler gibi: Saldırı kenara yakın hızlı tur atar, Denge orta halkada döner,
    /// Dayanıklılık merkeze yakın yavaş süzülür, Savunma merkezi tutar.
    /// Rakibe dalgalı (zayıflayıp güçlenen) bir çekim de var: beyler birbirine gelip çarpışır,
    /// sekip açılır, birkaç saniye sonra yine gelir — ama eskisi gibi dümdüz kilitlenmez.
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
        /// <summary>Rakibe doğru en yüksek çekim ivmesi (m/s²).</summary>
        public float EngagePull;
        /// <summary>Dalganın en düşük noktası (0..1): çekim hiç tamamen kaybolmaz.</summary>
        public float EngageFloor;
        /// <summary>Dalga hızı (rad/sn). ~1 → yaklaşık 6 sn'de bir yoğun hücum.</summary>
        public float EngageWave;
    }

    public static class Movement
    {
        // Tipe göre rakibe çekim (m/s², EngageScale ile çarpılır). Saldırı en istekli, Dayanıklılık en kaçak.
        public static float AttackEngage = 9.53f;
        public static float BalanceEngage = 3.83f;
        public static float DefenseEngage = 3f;
        public static float StaminaEngage = 2.41f;

        public static MovementProfile ProfileFor(in FighterProfile f)
        {
            switch (f.Archetype)
            {
                case BeyArchetype.Attack:
                    return new MovementProfile
                    {
                        PreferredRadius = 0.55f,
                        OrbitSpeed = BeyStatCalculator.Lerp(4.2f, 6f, f.A),
                        RadialGain = 2.4f,
                        TangentGain = 2.2f,
                        MaxAccel = 15f,
                        Wander = 1.2f,
                        EngagePull = AttackEngage,
                        EngageFloor = 0.4f,
                        EngageWave = 1.1f
                    };
                case BeyArchetype.Defense:
                    return new MovementProfile
                    {
                        PreferredRadius = 0.05f,
                        OrbitSpeed = 1.3f,
                        RadialGain = 3.0f,
                        TangentGain = 1.2f,
                        MaxAccel = 8f,
                        Wander = 0.5f,
                        EngagePull = DefenseEngage,
                        EngageFloor = 0.3f,
                        EngageWave = 0.8f
                    };
                case BeyArchetype.Stamina:
                    return new MovementProfile
                    {
                        PreferredRadius = 0.24f,
                        OrbitSpeed = 2.6f,
                        RadialGain = 2.6f,
                        TangentGain = 1.6f,
                        MaxAccel = 9f,
                        Wander = 0.6f,
                        EngagePull = StaminaEngage,
                        EngageFloor = 0.25f,
                        EngageWave = 0.9f
                    };
                default:
                    return new MovementProfile
                    {
                        PreferredRadius = 0.42f,
                        OrbitSpeed = 4.2f,
                        RadialGain = 2.4f,
                        TangentGain = 2f,
                        MaxAccel = 12f,
                        Wander = 0.9f,
                        EngagePull = BalanceEngage,
                        EngageFloor = 0.35f,
                        EngageWave = 1f
                    };
            }
        }

        /// <summary>
        /// Otomatik hareket ivmesi. pos arena merkezine göredir.
        /// orbitSign: +1 saat yönünün tersi, -1 saat yönü.
        /// engage: EngageWeight (0..1) — hücum dalgası güçlendikçe ev halkasına bağlılık gevşer.
        /// </summary>
        public static Vec2 AutoAccel(in MovementProfile m, Vec2 pos, Vec2 vel, float arenaRadius, float rpm, float orbitSign, float engage = 0f)
        {
            float spin = 0.45f + 0.55f * BeyStatCalculator.Clamp(rpm, 0f, 1f);
            float r = pos.Length;
            Vec2 radial = r > 1e-3f ? pos / r : new Vec2(1f, 0f);
            float targetR = m.PreferredRadius * arenaRadius;

            float homeHold = 1f - 0.85f * BeyStatCalculator.Clamp(engage, 0f, 1f);
            Vec2 a = -radial * ((r - targetR) * m.RadialGain * homeHold);

            if (r > 0.35f)
            {
                Vec2 tangent = radial.Perp * orbitSign;
                float vt = Vec2.Dot(vel, tangent);
                a += tangent * ((m.OrbitSpeed * spin * homeHold - vt) * m.TangentGain * (0.3f + 0.7f * homeHold));
            }

            return a.ClampLength(m.MaxAccel * spin);
        }

        /// <summary>Çekim çarpanı (tüm tipler için, denge ayarı).</summary>
        public static float EngageScale = 1.4f;

        /// <summary>Hücum dalgasının o anki gücü (EngageFloor..1). Yavaşça güçlenip zayıflar.</summary>
        public static float EngageWeight(in MovementProfile m, float time, float phase)
        {
            if (m.EngagePull <= 0f) return 0f;
            float wave = 0.5f + 0.5f * (float)Math.Sin(time * m.EngageWave + phase);
            return m.EngageFloor + (1f - m.EngageFloor) * wave;
        }

        /// <summary>
        /// Rakibe doğru dalgalı çekim: beyler gelir, çarpışır, sekip açılır, tekrar gelir.
        /// Otomatik yörünge ile birleşince yaklaşma kavisli olur (dümdüz kilitlenme yok).
        /// </summary>
        public static Vec2 EngageAccel(in MovementProfile m, Vec2 pos, Vec2 vel, Vec2 enemyPos, float rpm, float engage)
        {
            Vec2 to = enemyPos - pos;
            float d = to.Length;
            if (d < 1e-3f || m.EngagePull <= 0f) return Vec2.Zero;
            Vec2 dir = to / d;
            float spin = 0.45f + 0.55f * BeyStatCalculator.Clamp(rpm, 0f, 1f);
            float near = BeyStatCalculator.Clamp(d / 1.5f, 0.6f, 1f);
            Vec2 a = dir * (m.EngagePull * EngageScale * spin * engage * near);
            // Rakibin etrafında uydu gibi dönmesin: yanal kaymayı dalgayla orantılı sönümle
            Vec2 lateral = vel - dir * Vec2.Dot(vel, dir);
            a -= lateral * (EngageHoming * engage);
            return a;
        }

        /// <summary>Hücum sırasında yanal kaymanın sönümü (1/sn). Yüksek = daha düz yaklaşma.</summary>
        public static float EngageHoming = 0.8f;

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
        public static float RegenHealLow = 4.4f, RegenHealHigh = 8.8f;
        public static float RegenHeal(float q) => BeyStatCalculator.Lerp(RegenHealLow, RegenHealHigh, q);
        public static float RegenDamageTaken(float q) => BeyStatCalculator.Lerp(0.85f, 0.68f, q);

        // Denge burst (yörünge + hücum)
        public const float BurstOrbitTime = 0.85f;
        public static float BurstRushTime(float q) => BeyStatCalculator.Lerp(2.6f, 3.6f, q);
        public static float BurstDamageMultLow = 1.12f, BurstDamageMultHigh = 1.3f;
        public static float BurstDamageMult(float q) => BeyStatCalculator.Lerp(BurstDamageMultLow, BurstDamageMultHigh, q);
        public static float BurstHealLow = 4.31f, BurstHealHigh = 8.62f;
        public static float BurstHeal(float q) => BeyStatCalculator.Lerp(BurstHealLow, BurstHealHigh, q);
        public static float BurstPush(float q) => BeyStatCalculator.Lerp(1.8f, 4.2f, q);
        public static float BurstOrbitForce(float q) => BeyStatCalculator.Lerp(14f, 20f, q);
        public static float BurstRushPull(float q) => BeyStatCalculator.Lerp(12f, 18f, q);
        public static float BurstOrbitDegPerSec(float q) => BeyStatCalculator.Lerp(220f, 320f, q);
    }
}
