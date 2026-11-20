using System;

namespace BeybladeCore
{
    /// <summary>Botun bir karede gördükleri (konumlar arena merkezine göre).</summary>
    public struct BotObservation
    {
        public Vec2 MyPos, MyVel, EnemyPos, EnemyVel;
        public float ArenaRadius;
        public FighterProfile Me, Enemy;
        public float MyStaminaRatio, EnemyStaminaRatio;
        public float Focus, MaxFocus, DashCost;
        /// <summary>Bekleme süresi doldu, odak yetiyor, hareket kilitli değil.</summary>
        public bool DashReady;
        public float MyDashSpeed;
        public bool EnemyDashing;
        public bool SkillReady;
        public bool EnemySkillActive;
        public bool EnemyShielded;
        public bool EnemyUltDragging;
        /// <summary>Kendi skill'im hareketi devraldı (ulti / burst).</summary>
        public bool MovementLocked;
    }

    public struct BotCommand
    {
        /// <summary>Yönlendirme (dünya XZ, uzunluk 0..1).</summary>
        public Vec2 Steer;
        public bool Dash;
        public Vec2 DashDir;
        public bool UseSkill;
        public float SkillQuality;
    }

    /// <summary>
    /// Tipe göre taktik yapan bot:
    ///  Saldırı/Denge: turlar, rakibin etrafında çember çizip pozisyon alır, sonra ani atılmayla vurur;
    ///                 kenardaki rakibi dışarı itmeye çalışır, savunmaya gereksiz yere çarpmaz.
    ///  Dayanıklılık: merkeze yakın süzülür, gelen saldırıdan yana kaçar.
    ///  Savunma: merkezi tutar, savrulan rakibe karşı atak (kontra) yapar.
    /// Zorluk: düşünme hızı, nişan hatası, atılma isteği, kenardan kurtulma ve skill zamanlaması.
    /// </summary>
    public class BotBrain
    {
        public enum State { Cruise, Stalk, Strike, Evade, Hold, Recover, Counter }

        public State Current { get; private set; } = State.Cruise;
        public BotDifficulty Difficulty { get; private set; }

        readonly Random rng;
        float thinkTimer;
        float stateTimer;
        float skillDelay = -1f;
        float skillReadyTime;
        Vec2 heldSteer;
        float stalkSign = 1f;
        float stalkRadius = 2.2f;

        public BotBrain(BotDifficulty difficulty, int seed)
        {
            Difficulty = difficulty;
            rng = new Random(seed);
            stateTimer = Range(0.6f, 1.6f);
        }

        public void SetDifficulty(BotDifficulty d) => Difficulty = d;

        float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        bool Chance(float p) => rng.NextDouble() < p;

        float AimError => BeyStatCalculator.Lerp(0.38f, 0.05f, Difficulty.T);

        public BotCommand Tick(in BotObservation o, float dt)
        {
            BotCommand cmd = default;
            cmd.Steer = heldSteer;

            UpdateSkill(o, dt, ref cmd);

            thinkTimer -= dt;
            stateTimer -= dt;
            if (thinkTimer > 0f) return cmd;
            thinkTimer = Range(Difficulty.ThinkIntervalMin, Difficulty.ThinkIntervalMax);

            cmd.Steer = Vec2.Zero;
            if (!o.MovementLocked) Think(o, ref cmd);
            heldSteer = cmd.Steer;
            return cmd;
        }

        // ---------------- Skill ----------------

        void UpdateSkill(in BotObservation o, float dt, ref BotCommand cmd)
        {
            if (!o.SkillReady || o.MovementLocked)
            {
                skillDelay = -1f;
                skillReadyTime = 0f;
                return;
            }

            skillReadyTime += dt;
            bool want = WantsSkill(o) || skillReadyTime > 14f;
            if (want && skillDelay < 0f)
                skillDelay = Range(Difficulty.SkillReactionMin, Difficulty.SkillReactionMax);

            if (skillDelay >= 0f)
            {
                skillDelay -= dt;
                if (skillDelay <= 0f)
                {
                    skillDelay = -1f;
                    skillReadyTime = 0f;
                    cmd.UseSkill = true;
                    cmd.SkillQuality = Range(Difficulty.SkillQualityMin, Difficulty.SkillQualityMax);
                }
            }
        }

        bool WantsSkill(in BotObservation o)
        {
            Vec2 toEnemy = o.EnemyPos - o.MyPos;
            float d = toEnemy.Length;
            float enemyApproach = d > 1e-3f ? Vec2.Dot(o.EnemyVel, -toEnemy / d) : 0f;

            switch (o.Me.Archetype)
            {
                case BeyArchetype.Attack:
                    return d < 4.2f && !o.EnemyShielded;
                case BeyArchetype.Balance:
                    return d < 3.6f && !o.EnemyShielded;
                case BeyArchetype.Defense:
                    return o.EnemyUltDragging
                           || (o.EnemyDashing && enemyApproach > 1.5f && d < 3.5f)
                           || (o.EnemySkillActive && d < 3.2f)
                           || o.MyStaminaRatio < 0.3f;
                default: // Dayanıklılık
                    return o.EnemyUltDragging
                           || o.MyStaminaRatio < 0.55f
                           || (enemyApproach > 2.2f && d < 2.4f);
            }
        }

        // ---------------- Hareket ----------------

        void Think(in BotObservation o, ref BotCommand cmd)
        {
            float R = Math.Max(1f, o.ArenaRadius);
            float r = o.MyPos.Length;
            Vec2 radial = r > 1e-3f ? o.MyPos / r : new Vec2(1f, 0f);
            float outward = Vec2.Dot(o.MyVel, radial);

            Vec2 toEnemy = o.EnemyPos - o.MyPos;
            float d = toEnemy.Length;
            Vec2 dirE = d > 1e-3f ? toEnemy / d : radial * -1f;
            float enemyApproach = Vec2.Dot(o.EnemyVel, -dirE);
            float enemyR = o.EnemyPos.Length;

            // 1) Kenar: önce hayatta kal
            if (r > 0.74f * R && outward > 0.5f)
            {
                Current = State.Recover;
                cmd.Steer = -radial;
                if (Difficulty.UsesEdgeRecovery && r > 0.86f * R && outward > 2.5f && o.DashReady)
                    RequestDash(ref cmd, -radial + radial.Perp * 0.25f, 0f);
                return;
            }

            // Denge tipi rakibe göre oyun planı seçer: savunmaya karşı dayanıklılık gibi (dönerek bekle),
            // diğerlerine karşı saldırgan.
            BeyArchetype plan = o.Me.Archetype;
            if (plan == BeyArchetype.Balance)
            {
                if (o.Enemy.Archetype == BeyArchetype.Defense) plan = BeyArchetype.Stamina;
            }

            switch (plan)
            {
                case BeyArchetype.Defense: ThinkDefense(o, ref cmd, R, r, radial, d, dirE, enemyApproach, enemyR); break;
                case BeyArchetype.Stamina: ThinkStamina(o, ref cmd, R, r, radial, d, dirE, enemyApproach); break;
                default: ThinkStriker(o, ref cmd, R, r, radial, d, dirE, enemyApproach, enemyR); break;
            }
        }

        void ThinkStriker(in BotObservation o, ref BotCommand cmd, float R, float r, Vec2 radial, float d, Vec2 dirE, float enemyApproach, float enemyR)
        {
            bool enemyIsWall = o.Enemy.Archetype == BeyArchetype.Defense;

            // Ulti sürükleyen rakipten kaç
            if (o.EnemyUltDragging && d < 4f)
            {
                Current = State.Evade;
                cmd.Steer = SideStep(o, dirE);
                if (o.DashReady && d < 2.5f && Chance(Difficulty.DashChance)) RequestDash(ref cmd, SideStep(o, dirE), 0f);
                return;
            }

            // Fırsat: rakip kenarda, ben daha içerideyim → dışarı it
            bool pushChance = enemyR > 0.62f * R && r < enemyR && d < 3.6f;
            if (pushChance && o.DashReady && Chance(Difficulty.DashChance + 0.25f))
            {
                Current = State.Strike;
                RequestDash(ref cmd, LeadDir(o, d), AimError);
                stateTimer = Range(1.0f, 2.0f);
                return;
            }

            switch (Current)
            {
                case State.Cruise:
                case State.Recover:
                case State.Evade:
                case State.Hold:
                case State.Counter:
                    // Tur at; süre dolunca ve odak yeterliyse avlanmaya başla
                    Current = State.Cruise;
                    if (stateTimer <= 0f && o.Focus >= o.DashCost + 25f && d < 0.95f * R * 2f)
                    {
                        // Savunmaya karşı sabırlı ol: merkezdeki savunmaya kafa atma
                        bool waitForOpening = enemyIsWall && enemyR < 0.3f * R && Chance(0.45f);
                        if (waitForOpening)
                        {
                            stateTimer = Range(0.6f, 1.4f);
                        }
                        else
                        {
                            Current = State.Stalk;
                            stalkSign = Chance(0.5f) ? 1f : -1f;
                            stalkRadius = Range(1.9f, 2.6f);
                            stateTimer = Range(0.35f, 0.9f) * BeyStatCalculator.Lerp(1.3f, 0.8f, Difficulty.T);
                        }
                    }
                    break;

                case State.Stalk:
                {
                    // Rakibin etrafında çember çiz (ani atılma için açı ara)
                    Vec2 tangent = dirE.Perp * stalkSign;
                    float radialErr = d - stalkRadius;
                    Vec2 steer = tangent + dirE * BeyStatCalculator.Clamp(radialErr * 0.8f, -1f, 1f);
                    if (r > 0.65f * R) steer += -radial * 0.8f;
                    cmd.Steer = steer.ClampLength(1f);

                    float enemySpeed = o.EnemyVel.Length;
                    float crossing = enemySpeed > 0.5f ? Math.Abs(Vec2.Dot(o.EnemyVel / enemySpeed, dirE)) : 0f;
                    bool goodAngle = crossing < 0.5f && d < stalkRadius + 0.8f;
                    bool lowFocus = o.Focus < o.DashCost + 3f;
                    if (o.DashReady && (stateTimer <= 0f || lowFocus || (goodAngle && Chance(0.3f + 0.4f * Difficulty.T))))
                    {
                        Current = State.Strike;
                        cmd.Steer = Vec2.Zero;
                        RequestDash(ref cmd, LeadDir(o, d), AimError);
                        stateTimer = Range(0.9f, 1.8f);
                    }
                    else if (lowFocus || stateTimer < -1.2f)
                    {
                        // Odak yetmedi: vazgeç, turlamaya dön
                        Current = State.Cruise;
                        stateTimer = Range(0.6f, 1.2f);
                    }
                    break;
                }

                case State.Strike:
                    // Atıldıktan sonra kısa süre rakibe doğru bastır, sonra çekil
                    if (stateTimer > 0.9f && d < 3f && !enemyIsWall)
                        cmd.Steer = dirE * 0.6f;
                    if (stateTimer <= 0f)
                    {
                        Current = State.Cruise;
                        stateTimer = Range(0.5f, 1.4f) * BeyStatCalculator.Lerp(1.4f, 0.75f, Difficulty.T);
                    }
                    break;
            }
        }

        void ThinkStamina(in BotObservation o, ref BotCommand cmd, float R, float r, Vec2 radial, float d, Vec2 dirE, float enemyApproach)
        {
            bool threatened = (enemyApproach > 1.2f && d < 3.2f) || (o.EnemyUltDragging && d < 4.5f) || (o.EnemyDashing && d < 3.8f);
            if (threatened)
            {
                Current = State.Evade;
                Vec2 side = SideStep(o, dirE);
                cmd.Steer = side;
                bool danger = (o.EnemyDashing && d < 3f) || (o.EnemyUltDragging && d < 3f);
                if (danger && o.DashReady && Chance(Difficulty.DashChance + 0.15f))
                    RequestDash(ref cmd, side, 0f);
                return;
            }

            Current = State.Cruise;
            // Merkezden çok uzaklaştıysa nazikçe dön (odak harcamadan küçük düzeltme)
            if (r > 0.45f * R) cmd.Steer = -radial * 0.5f;
        }

        void ThinkDefense(in BotObservation o, ref BotCommand cmd, float R, float r, Vec2 radial, float d, Vec2 dirE, float enemyApproach, float enemyR)
        {
            // Kontra: rakip kenarda ve ben aradayım → dışarı it
            bool enemyOutward = Vec2.Dot(o.EnemyVel, o.EnemyPos.Normalized) > -0.5f;
            if (enemyR > 0.6f * R && d < 3.4f && r < enemyR && enemyOutward && o.DashReady
                && Chance(Difficulty.DashChance + 0.2f))
            {
                Current = State.Counter;
                RequestDash(ref cmd, LeadDir(o, d), AimError);
                return;
            }

            // Dayanıklılık tipine karşı bekleyen kaybeder: ağırlığını kullanıp baskı yap
            bool enemyOutlasts = o.Enemy.Archetype == BeyArchetype.Stamina || o.EnemyStaminaRatio > o.MyStaminaRatio + 0.12f;
            if (enemyOutlasts)
            {
                if (d < 3.2f && o.DashReady && o.Focus >= o.DashCost + 15f && Chance(Difficulty.DashChance + 0.1f))
                {
                    Current = State.Counter;
                    RequestDash(ref cmd, LeadDir(o, d), AimError);
                    return;
                }
                // Yavaşça yaklaş (odak dolarken), ama merkezden fazla uzaklaşma
                if (d > 2.4f && o.Focus > 60f && r < 0.45f * R)
                {
                    Current = State.Stalk;
                    cmd.Steer = dirE * 0.7f;
                    return;
                }
            }

            // Saldırı geliyorsa merkeze yaslan (sabit dur)
            bool incoming = (o.EnemyDashing || enemyApproach > 2f) && d < 3.5f;
            if (incoming || r > 0.3f * R)
            {
                Current = State.Hold;
                cmd.Steer = r > 0.15f * R ? -radial * (incoming ? 0.8f : 0.5f) : Vec2.Zero;
                return;
            }

            Current = State.Cruise;
        }

        // ---------------- Yardımcılar ----------------

        /// <summary>Rakibin geliş hattından yana kaç; kenara değil merkeze yakın tarafı seç.</summary>
        static Vec2 SideStep(in BotObservation o, Vec2 dirE)
        {
            Vec2 a = dirE.Perp;
            Vec2 b = -a;
            Vec2 pa = o.MyPos + a;
            Vec2 pb = o.MyPos + b;
            Vec2 side = pa.SqrLength < pb.SqrLength ? a : b;
            // Biraz da merkeze
            return (side + (-o.MyPos.Normalized) * 0.3f).Normalized;
        }

        /// <summary>Rakibin hareketini hesaba katarak (öne nişan) atılma yönü.</summary>
        static Vec2 LeadDir(in BotObservation o, float d)
        {
            float t = d / Math.Max(4f, o.MyDashSpeed) * 0.8f;
            Vec2 target = o.EnemyPos + o.EnemyVel * t;
            Vec2 dir = target - o.MyPos;
            return dir.SqrLength > 1e-4f ? dir.Normalized : (o.EnemyPos - o.MyPos).Normalized;
        }

        void RequestDash(ref BotCommand cmd, Vec2 dir, float aimError)
        {
            if (aimError > 0f)
            {
                float ang = Range(-aimError, aimError);
                float c = (float)Math.Cos(ang), s = (float)Math.Sin(ang);
                dir = new Vec2(dir.X * c - dir.Z * s, dir.X * s + dir.Z * c);
            }
            cmd.Dash = true;
            cmd.DashDir = dir.Normalized;
        }
    }
}
