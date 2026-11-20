using System;

namespace BeybladeCore
{
    /// <summary>
    /// Savaş dengesi ayarları. Unity (BeybladeController, SkillSystem) ve BattleSim aynı değerleri kullanır,
    /// böylece simülasyonda yapılan denge testleri oyuna birebir yansır.
    /// Değerler BattleSim ile yapılan Monte Carlo denemeleriyle ayarlandı (bkz. Tests/Editor/BalanceTests.cs).
    /// </summary>
    public class CombatTuning
    {
        public static readonly CombatTuning Default = new CombatTuning();

        // ---- Dönüş / stamina ----
        public float MaxStamina = 160f;
        /// <summary>Saniyede doğal stamina kaybı (tipe göre çarpanlarla).</summary>
        public float BaseDrain = 1.55f;
        public float DrainStaminaBest = 0.677f;   // S=1 iken çarpan
        public float DrainStaminaWorst = 1.318f;  // S=0 iken çarpan
        public float DrainAttackLow = 0.92f;     // A=0 iken çarpan
        public float DrainAttackHigh = 1.036f;    // A=1 iken çarpan (agresif dönüş daha çok yorar)

        // ---- Çarpışma ----
        public float Restitution = 0.55f;
        public float DamageScale = 1.05f;
        public float ImpactRefSpeed = 5.5f;
        public float ImpactMinSpeed = 0.6f;
        public float ImpactMin = 0.12f;
        public float ImpactMax = 1.6f;
        public float MitigationDefense = 0.64f;
        public float MitigationWeight = 0.165f;
        /// <summary>Saldıran taraf ağır/savunmacı rakibe çarpınca kendisi de hasar alır.</summary>
        public float RecoilScale = 0.6f;
        public float RecoilDefense = 2.0f;
        public float RecoilWeight = 1.6f;
        public float AggressorMargin = 0.4f;
        public float DashStrikeMult = 1.2f;
        /// <summary>Ağır bey hafife daha sert vurur: vuruş *= (kütle oranı)^MassExponent.</summary>
        public float MassExponent = 0.02f;
        public float KnockBase = 1.0f;
        public float KnockScale = 0.332f;
        public float KnockResistPerDefense = 0.45f;
        public float CollisionCooldown = 0.12f;

        // ---- Hız ----
        public float MaxSpeedLow = 4.5f;
        public float MaxSpeedHigh = 12f;
        public float MaxSpeedWeightPenalty = 0.15f;
        public float LinearDamping = 0.35f;
        public float DefenseFriction = 1.3f;

        // ---- Atılma ----
        public float DashSpeedLow = 7f;
        public float DashSpeedHigh = 10f;
        public float DashCost = 35f;
        public float DashCooldown = 2.2f;
        public float MaxFocus = 100f;
        public float FocusRegen = 16f;
        public float FocusRegenDelay = 0.6f;
        public float SteerDrain = 30f;

        // ---- Fırlatma ----
        /// <summary>Fırlatma itkisi (kütleye bölünür): hız = LaunchImpulse * güç / kütle.</summary>
        public float LaunchImpulse = 14f;
        /// <summary>Fırlatma gücünün stamina'ya etkisi: oran = Floor + (1-Floor)*güç. Fırlatma önemli ama tek başına belirleyici değil.</summary>
        public float LaunchStaminaFloor = 0.65f;

        // ---- Arena (Unity'de gerçek geometri kullanılır, bunlar simülasyon içindir) ----
        public float ArenaRadius = 5.2f;
        public float BowlPull = 3.2f;
        public float WallRestitution = 0.35f;
        /// <summary>Duvara bu hızdan hızlı dışa doğru çarpan bey arenadan çıkar.</summary>
        public float RingOutSpeed = 10.12f;

        // ---- Skill enerjisi ----
        public float SkillPassiveEnergy = 2.94f;
        public float SkillCollisionEnergy = 5f;
        public float SkillCooldown = 3f;
    }

    /// <summary>Normalize (0..1) statlar + bunlardan türeyen fiziksel değerler.</summary>
    public struct FighterProfile
    {
        public BeyArchetype Archetype;
        public float A, D, S, W;

        public FighterProfile(BeyArchetype archetype, float a, float d, float s, float w)
        {
            Archetype = archetype;
            A = Clamp01(a); D = Clamp01(d); S = Clamp01(s); W = Clamp01(w);
        }

        public static FighterProfile FromStatBlock(BeyStatBlock b)
            => new FighterProfile(b.Archetype, b.AttackNorm, b.DefenseNorm, b.StaminaNorm, b.WeightNorm);

        /// <summary>BeybladeController'daki ham değerlerden (attackPower, defensePower, staminaDecayRate, weight).</summary>
        public static FighterProfile FromRaw(BeyArchetype archetype, float attackPower, float defensePower, float staminaDecay, float weight)
        {
            return new FighterProfile(archetype,
                BeyStatCalculator.InverseLerp(BeyStatCalculator.AttackMin, BeyStatCalculator.AttackMax, attackPower),
                BeyStatCalculator.InverseLerp(BeyStatCalculator.DefenseMin, BeyStatCalculator.DefenseMax, defensePower),
                BeyStatCalculator.InverseLerp(BeyStatCalculator.DecayMax, BeyStatCalculator.DecayMin, staminaDecay),
                BeyStatCalculator.InverseLerp(BeyStatCalculator.WeightMin, BeyStatCalculator.WeightMax, weight));
        }

        public float AttackPower => BeyStatCalculator.Lerp(BeyStatCalculator.AttackMin, BeyStatCalculator.AttackMax, A);
        public float DefensePower => BeyStatCalculator.Lerp(BeyStatCalculator.DefenseMin, BeyStatCalculator.DefenseMax, D);
        public float StaminaDecay => BeyStatCalculator.Lerp(BeyStatCalculator.DecayMax, BeyStatCalculator.DecayMin, S);
        public float Mass => BeyStatCalculator.Lerp(BeyStatCalculator.WeightMin, BeyStatCalculator.WeightMax, W);
        /// <summary>A + D + S: tipler arası "eşit güç" karşılaştırması için bütçe.</summary>
        public float Budget => A + D + S;

        static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        public override string ToString() => $"{Archetype} A{A:0.00} D{D:0.00} S{S:0.00} W{W:0.00}";
    }

    /// <summary>Parçasız (bot / hızlı maç) beylerin tip şablonları. Hepsinin A+D+S bütçesi eşittir.</summary>
    public static class ArchetypePresets
    {
        public const float StandardBudget = 1.32f;

        public static FighterProfile Get(BeyArchetype type)
        {
            switch (type)
            {
                case BeyArchetype.Attack: return new FighterProfile(type, 0.78f, 0.22f, 0.32f, 0.22f);
                case BeyArchetype.Defense: return new FighterProfile(type, 0.185f, 0.785f, 0.35f, 0.78f);
                case BeyArchetype.Stamina: return new FighterProfile(type, 0.16f, 0.24f, 0.92f, 0.31f);
                default: return new FighterProfile(type, 0.42f, 0.42f, 0.48f, 0.45f);
            }
        }

        /// <summary>Şablonu verilen bütçeye ölçekler (oyuncunun beyi ile eşit güçte rakip için).</summary>
        public static FighterProfile ForBudget(BeyArchetype type, float budget)
        {
            FighterProfile p = Get(type);
            float current = p.Budget;
            if (current < 1e-4f) return p;
            float k = budget / current;
            // Taşan (>1) statın fazlasını diğerlerine dağıt ki bütçe korunsun
            float a = p.A * k, d = p.D * k, s = p.S * k;
            for (int i = 0; i < 3; i++)
            {
                float over = Math.Max(0f, a - 1f) + Math.Max(0f, d - 1f) + Math.Max(0f, s - 1f);
                if (over <= 1e-4f) break;
                a = Math.Min(a, 1f); d = Math.Min(d, 1f); s = Math.Min(s, 1f);
                float room = (1f - a) + (1f - d) + (1f - s);
                if (room <= 1e-4f) break;
                a += over * (1f - a) / room;
                d += over * (1f - d) / room;
                s += over * (1f - s) / room;
            }
            return new FighterProfile(type, a, d, s, p.W);
        }
    }

    /// <summary>Bir çarpışmanın girdileri (her iki taraf kendi açısından hesaplar).</summary>
    public struct HitInput
    {
        public FighterProfile Self;
        public FighterProfile Other;
        public float SelfRpm;            // 0..1
        public float OtherRpm;           // 0..1
        public float SelfStaminaRatio;   // 0..1
        /// <summary>Kendi hızımın rakibe doğru bileşeni (çarpışma öncesi).</summary>
        public float SelfApproach;
        /// <summary>Rakibin hızının bana doğru bileşeni (çarpışma öncesi).</summary>
        public float OtherApproach;
        public bool OtherDashing;
        /// <summary>Kalkan vb. (1 = normal).</summary>
        public float SelfDamageTakenMult;
        /// <summary>Rakibin hücum çarpanı (rush vb., 1 = normal).</summary>
        public float OtherDamageMult;
        public float SelfKnockbackMult;
        /// <summary>Rakibin rush itme bonusu (m/s, direnç ile azalır).</summary>
        public float OtherPushBonus;
    }

    public struct HitResult
    {
        public float Damage;
        /// <summary>Rakipten uzağa eklenen ekstra hız (m/s).</summary>
        public float KnockbackSpeed;
        public bool SelfWasAggressor;
        public float Impact;
    }

    public static class Combat
    {
        public static float MaxSpeed(in FighterProfile f, CombatTuning t)
            => BeyStatCalculator.Lerp(t.MaxSpeedLow, t.MaxSpeedHigh, f.A) * (1f - t.MaxSpeedWeightPenalty * f.W);

        public static float KnockbackResist(in FighterProfile f, CombatTuning t) => t.KnockResistPerDefense * f.D;

        /// <summary>Stamina bittikçe 1 → 1.8: yorgun bey daha çok savrulur.</summary>
        public static float FatigueKnockMult(float staminaRatio) => BeyStatCalculator.Lerp(1.8f, 1f, staminaRatio);

        public static float NaturalDrainPerSecond(in FighterProfile f, CombatTuning t)
            => t.BaseDrain
               * BeyStatCalculator.Lerp(t.DrainStaminaWorst, t.DrainStaminaBest, f.S)
               * BeyStatCalculator.Lerp(t.DrainAttackLow, t.DrainAttackHigh, f.A);

        /// <summary>Fırlatma gücünden (0..1) başlangıç stamina/RPM oranı.</summary>
        public static float LaunchSpinFraction(float power, CombatTuning t)
        {
            power = BeyStatCalculator.Clamp(power, 0f, 1f);
            return t.LaunchStaminaFloor + (1f - t.LaunchStaminaFloor) * power;
        }

        public static float DashSpeed(in FighterProfile f, CombatTuning t) => BeyStatCalculator.Lerp(t.DashSpeedLow, t.DashSpeedHigh, f.A);

        public static float StrikePower(in FighterProfile f, float rpm)
            => f.AttackPower * (0.55f + 0.45f * BeyStatCalculator.Clamp(rpm, 0f, 1f));

        public static float Mitigation(in FighterProfile f, CombatTuning t)
            => 1f / (1f + t.MitigationDefense * f.D + t.MitigationWeight * f.W);

        public static float Impact(float closingSpeed, CombatTuning t)
            => BeyStatCalculator.Clamp((closingSpeed - t.ImpactMinSpeed) / t.ImpactRefSpeed, t.ImpactMin, t.ImpactMax);

        public static HitResult ResolveHit(in HitInput h, CombatTuning t)
        {
            float closing = Math.Max(0f, h.SelfApproach) + Math.Max(0f, h.OtherApproach);
            float impact = Impact(closing, t);

            float massRatio = BeyStatCalculator.Clamp(h.Other.Mass / Math.Max(0.5f, h.Self.Mass), 0.4f, 2.5f);
            float strike = StrikePower(h.Other, h.OtherRpm)
                           * (float)Math.Pow(massRatio, t.MassExponent)
                           * (h.OtherDamageMult <= 0f ? 1f : h.OtherDamageMult)
                           * (h.OtherDashing ? t.DashStrikeMult : 1f);

            float damage = t.DamageScale * strike * impact * Mitigation(h.Self, t);

            bool aggressor = h.SelfApproach > h.OtherApproach + t.AggressorMargin;
            if (aggressor)
            {
                float recoil = t.RecoilScale * impact
                               * (h.Other.D * t.RecoilDefense + h.Other.W * t.RecoilWeight)
                               * (1f - 0.5f * h.Self.D);
                damage += recoil;
            }

            damage *= h.SelfDamageTakenMult <= 0f ? 1f : h.SelfDamageTakenMult;

            float knock = (t.KnockBase + t.KnockScale * strike * impact)
                          * (1f - KnockbackResist(h.Self, t))
                          * FatigueKnockMult(h.SelfStaminaRatio)
                          / Math.Max(0.5f, h.Self.Mass * 0.5f);
            knock += h.OtherPushBonus * (1f - KnockbackResist(h.Self, t));
            knock *= h.SelfKnockbackMult <= 0f ? 1f : h.SelfKnockbackMult;

            return new HitResult { Damage = damage, KnockbackSpeed = knock, SelfWasAggressor = aggressor, Impact = impact };
        }

        /// <summary>
        /// Esnek çarpışma: kendi hızımın yeni hali (normal = benden rakibe birim vektör).
        /// İki taraf da aynı formülü kendi açısından uygular → momentum korunur.
        /// </summary>
        public static Vec2 BounceVelocity(Vec2 selfVel, Vec2 otherVel, Vec2 normalToOther, float selfMass, float otherMass, float restitution)
        {
            float vrel = Vec2.Dot(selfVel - otherVel, normalToOther);
            if (vrel <= 0f) return selfVel;
            float j = (1f + restitution) * otherMass / (selfMass + otherMass) * vrel;
            return selfVel - normalToOther * j;
        }
    }
}
