using System;

namespace BeybladeCore
{
    public enum FinishType { None, Spin, RingOut, TimeUp }

    public struct SimResult
    {
        /// <summary>0 = A kazandı, 1 = B kazandı, -1 = berabere.</summary>
        public int Winner;
        public FinishType Finish;
        public float Duration;
        public int Collisions;
        public float StaminaA, StaminaB;
        public int SkillsA, SkillsB;
        public int DashesA, DashesB;
        /// <summary>Çarpışmalardan alınan toplam hasar (teşhis için).</summary>
        public float HitDamageTakenA, HitDamageTakenB;
        /// <summary>Skill kaynaklı alınan hasar.</summary>
        public float SkillDamageTakenA, SkillDamageTakenB;
    }

    public struct SimSetup
    {
        public FighterProfile A, B;
        public float PowerA, PowerB;
        public BotDifficulty BrainA, BrainB;
        public int Seed;
        public float MaxTime;
        /// <summary>A tarafı hiç yönlendirmez/atılmaz/skill kullanmaz (dokunmayan oyuncu): sadece otomatik hareket.</summary>
        public bool PassiveA;
    }

    /// <summary>
    /// Unity sahnesinin 2B modeli: aynı hareket profilleri, çarpışma formülleri, skill'ler ve bot beyni.
    /// Denge testleri için binlerce maç saniyeler içinde oynatılır. Gerçek 3B fizik (stadyum eğimi,
    /// mesh çarpışmaları) birebir değildir; tipler arası göreli dengeyi ölçmek içindir.
    /// </summary>
    public class BattleSim
    {
        public const float Dt = 0.02f;
        public const float BeyRadius = 0.45f;

        enum SkillPhase { Charging, Ready, Active, Cooldown }

        class Fighter
        {
            public FighterProfile P;
            public MovementProfile Move;
            public BotBrain Brain;
            public Vec2 Pos, Vel;
            public float Stamina, MaxStamina;
            public float Focus;
            public bool Exhausted;
            public float LastFocusUse = -10f, LastDash = -10f;
            public float DashUntil, OverspeedUntil, OverspeedMult = 1f, StunUntil;
            public float OrbitSign;
            public float WavePhase;
            public bool Out, Toppled;

            public SkillPhase Phase;
            public float Energy, SkillTimer, SkillDuration, SkillQ;
            public bool Shield, UltDrag, Rushing, BurstSkill;
            public float DamageTakenMult = 1f, RushMult = 1f, RushPush;
            public Vec2 UltPushDir;
            public float LastUltContact = -10f;
            public float OrbitAngle;

            public int Skills, Dashes;
            public float HitTaken, SkillTaken;

            public float Rpm => MaxStamina > 0f ? Math.Max(0f, Stamina) / MaxStamina : 0f;
            public bool Alive => !Out && !Toppled;
            public bool Overridden => Phase == SkillPhase.Active && (UltDrag || BurstSkill);
            public bool Dashing(float t) => t < DashUntil;
        }

        readonly CombatTuning tune;
        readonly Random rng;
        readonly Fighter[] f = new Fighter[2];
        float time;
        float lastCollision = -10f;
        int collisions;

        public BattleSim(SimSetup s, CombatTuning tuning = null)
        {
            tune = tuning ?? CombatTuning.Default;
            rng = new Random(s.Seed);
            setup = s;
            float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
            Vec2 axis = new Vec2((float)Math.Cos(angle), (float)Math.Sin(angle));
            f[0] = MakeFighter(s.A, s.PowerA, axis * -3f, s.BrainA, s.Seed * 31 + 7);
            f[1] = MakeFighter(s.B, s.PowerB, axis * 3f, s.BrainB, s.Seed * 17 + 3);
        }

        readonly SimSetup setup;

        Fighter MakeFighter(FighterProfile p, float power, Vec2 pos, BotDifficulty diff, int seed)
        {
            power = BeyStatCalculator.Clamp(power, 0.08f, 1f);
            var x = new Fighter
            {
                P = p,
                Move = Movement.ProfileFor(p),
                Brain = new BotBrain(diff, seed),
                Pos = pos,
                MaxStamina = tune.MaxStamina,
                Stamina = tune.MaxStamina * Combat.LaunchSpinFraction(power, tune),
                Focus = tune.MaxFocus,
                OrbitSign = rng.NextDouble() < 0.5 ? 1f : -1f,
                WavePhase = (float)(rng.NextDouble() * Math.PI * 2.0)
            };
            Vec2 toCenter = (-pos).Normalized;
            // Fırlatmada hafif açı sapması
            float dev = (float)(rng.NextDouble() - 0.5) * 0.5f;
            float c = (float)Math.Cos(dev), sn = (float)Math.Sin(dev);
            toCenter = new Vec2(toCenter.X * c - toCenter.Z * sn, toCenter.X * sn + toCenter.Z * c);
            x.Vel = toCenter * (tune.LaunchImpulse * power / p.Mass);
            return x;
        }

        public SimResult Run()
        {
            float maxTime = setup.MaxTime > 0f ? setup.MaxTime : 240f;
            while (time < maxTime)
            {
                Step();
                bool a = f[0].Alive, b = f[1].Alive;
                if (!a || !b)
                {
                    int winner = a ? 0 : (b ? 1 : -1);
                    Fighter loser = a ? f[1] : f[0];
                    return Result(winner, loser.Out ? FinishType.RingOut : FinishType.Spin);
                }
            }
            float diff = f[0].Stamina - f[1].Stamina;
            return Result(Math.Abs(diff) < 2f ? -1 : (diff > 0f ? 0 : 1), FinishType.TimeUp);
        }

        SimResult Result(int winner, FinishType finish) => new SimResult
        {
            Winner = winner,
            Finish = finish,
            Duration = time,
            Collisions = collisions,
            StaminaA = f[0].Stamina,
            StaminaB = f[1].Stamina,
            SkillsA = f[0].Skills,
            SkillsB = f[1].Skills,
            DashesA = f[0].Dashes,
            DashesB = f[1].Dashes,
            HitDamageTakenA = f[0].HitTaken,
            HitDamageTakenB = f[1].HitTaken,
            SkillDamageTakenA = f[0].SkillTaken,
            SkillDamageTakenB = f[1].SkillTaken
        };

        void Step()
        {
            time += Dt;
            for (int i = 0; i < 2; i++) Control(f[i], f[1 - i]);
            for (int i = 0; i < 2; i++) TickSkill(f[i], f[1 - i]);
            for (int i = 0; i < 2; i++) Physics(f[i], f[1 - i]);
            Collide(f[0], f[1]);
            for (int i = 0; i < 2; i++) Walls(f[i]);
            for (int i = 0; i < 2; i++) Drain(f[i]);
        }

        // ---------------- Kontrol ----------------

        void Control(Fighter me, Fighter en)
        {
            if (!me.Alive) return;
            if (setup.PassiveA && me == f[0])
            {
                steerCache[0] = Vec2.Zero;
                if (time - me.LastFocusUse >= tune.FocusRegenDelay)
                    me.Focus = Math.Min(tune.MaxFocus, me.Focus + tune.FocusRegen * Dt);
                return;
            }

            bool dashReady = !me.Overridden && me.Focus >= tune.DashCost && time - me.LastDash >= tune.DashCooldown;
            var obs = new BotObservation
            {
                MyPos = me.Pos, MyVel = me.Vel, EnemyPos = en.Pos, EnemyVel = en.Vel,
                ArenaRadius = tune.ArenaRadius,
                Me = me.P, Enemy = en.P,
                MyStaminaRatio = me.Rpm, EnemyStaminaRatio = en.Rpm,
                Focus = me.Focus, MaxFocus = tune.MaxFocus, DashCost = tune.DashCost,
                DashReady = dashReady,
                MyDashSpeed = Combat.DashSpeed(me.P, tune),
                EnemyDashing = en.Dashing(time),
                SkillReady = me.Phase == SkillPhase.Ready,
                EnemySkillActive = en.Phase == SkillPhase.Active,
                EnemyShielded = en.Phase == SkillPhase.Active && en.Shield,
                EnemyUltDragging = en.Phase == SkillPhase.Active && en.UltDrag,
                MovementLocked = me.Overridden
            };
            BotCommand cmd = me.Brain.Tick(obs, Dt);

            // Odak (BeyControl ile aynı)
            if (me.Exhausted && me.Focus >= 25f) me.Exhausted = false;
            bool wantsSteer = cmd.Steer.SqrLength > 0.0025f && !me.Overridden;
            Vec2 steer = Vec2.Zero;
            if (wantsSteer && !me.Exhausted && me.Focus > 0f)
            {
                me.Focus = Math.Max(0f, me.Focus - tune.SteerDrain * Dt);
                me.LastFocusUse = time;
                steer = cmd.Steer.ClampLength(1f);
                if (me.Focus <= 0f) me.Exhausted = true;
            }
            else if (time - me.LastFocusUse >= tune.FocusRegenDelay)
            {
                me.Focus = Math.Min(tune.MaxFocus, me.Focus + tune.FocusRegen * Dt);
            }
            steerCache[me == f[0] ? 0 : 1] = steer;

            if (cmd.Dash && dashReady && cmd.DashDir.SqrLength > 1e-4f)
            {
                me.Focus -= tune.DashCost;
                me.LastFocusUse = time;
                me.LastDash = time;
                Dash(me, cmd.DashDir, Combat.DashSpeed(me.P, tune));
                me.Dashes++;
            }

            if (cmd.UseSkill && me.Phase == SkillPhase.Ready)
                BeginSkill(me, cmd.SkillQuality);
        }

        readonly Vec2[] steerCache = new Vec2[2];

        void Dash(Fighter me, Vec2 dir, float strength)
        {
            dir = dir.Normalized;
            float along = Vec2.Dot(me.Vel, dir);
            me.Vel += dir * (Math.Max(0f, -along) + strength);
            float maxSpeed = Combat.MaxSpeed(me.P, tune);
            me.OverspeedUntil = time + 0.4f;
            me.OverspeedMult = Math.Max(1.7f, (strength + 3f) / Math.Max(1f, maxSpeed));
            me.DashUntil = time + 0.45f;
            me.StunUntil = Math.Max(me.StunUntil, time + 0.4f);
        }

        // ---------------- Skill ----------------

        void BeginSkill(Fighter me, float q)
        {
            q = BeyStatCalculator.Clamp(q, 0f, 1f);
            me.SkillQ = q;
            me.Energy = 0f;
            me.Phase = SkillPhase.Active;
            me.SkillTimer = 0f;
            me.Shield = me.UltDrag = me.Rushing = me.BurstSkill = false;
            me.DamageTakenMult = 1f;
            me.RushMult = 1f;
            me.RushPush = 0f;
            me.Skills++;

            switch (me.P.Archetype)
            {
                case BeyArchetype.Attack:
                    me.UltDrag = true;
                    me.SkillDuration = SkillTuning.UltDuration;
                    me.LastUltContact = -10f;
                    break;
                case BeyArchetype.Defense:
                    me.Shield = true;
                    me.SkillDuration = SkillTuning.ShieldDuration(q);
                    me.DamageTakenMult = SkillTuning.ShieldDamageTaken(q);
                    break;
                case BeyArchetype.Stamina:
                    me.Shield = true;
                    me.SkillDuration = SkillTuning.RegenDuration(q);
                    me.Stamina = Math.Min(me.MaxStamina, me.Stamina + SkillTuning.RegenHeal(q));
                    me.DamageTakenMult = SkillTuning.RegenDamageTaken(q);
                    break;
                default:
                    me.BurstSkill = true;
                    me.SkillDuration = SkillTuning.BurstOrbitTime + SkillTuning.BurstRushTime(q);
                    me.RushMult = SkillTuning.BurstDamageMult(q);
                    me.RushPush = SkillTuning.BurstPush(q);
                    me.Stamina = Math.Min(me.MaxStamina, me.Stamina + SkillTuning.BurstHeal(q));
                    me.OrbitAngle = (float)(rng.NextDouble() * 360.0);
                    break;
            }
        }

        void TickSkill(Fighter me, Fighter en)
        {
            if (!me.Alive)
            {
                if (me.Phase == SkillPhase.Active) EndSkill(me);
                return;
            }

            switch (me.Phase)
            {
                case SkillPhase.Charging:
                    me.Energy = Math.Min(100f, me.Energy + tune.SkillPassiveEnergy * Dt);
                    if (me.Energy >= 100f) me.Phase = SkillPhase.Ready;
                    break;
                case SkillPhase.Active:
                    me.SkillTimer += Dt;
                    if (me.BurstSkill) me.Rushing = me.SkillTimer >= SkillTuning.BurstOrbitTime;
                    if (me.UltDrag) TickUlt(me, en);
                    else if (me.BurstSkill) TickBurst(me, en);
                    if (me.SkillTimer >= me.SkillDuration)
                    {
                        if (me.UltDrag) UltFinale(me, en);
                        EndSkill(me);
                    }
                    break;
                case SkillPhase.Cooldown:
                    me.SkillTimer += Dt;
                    if (me.SkillTimer >= tune.SkillCooldown)
                    {
                        me.Phase = SkillPhase.Charging;
                        me.SkillTimer = 0f;
                    }
                    break;
            }
        }

        void EndSkill(Fighter me)
        {
            me.Phase = SkillPhase.Cooldown;
            me.SkillTimer = 0f;
            me.Shield = me.UltDrag = me.Rushing = me.BurstSkill = false;
            me.DamageTakenMult = 1f;
            me.RushMult = 1f;
            me.RushPush = 0f;
        }

        void TickUlt(Fighter me, Fighter en)
        {
            if (!en.Alive) return;
            Vec2 to = en.Pos - me.Pos;
            float dist = to.Length;
            if (dist < 1e-3f) return;
            Vec2 dir = to / dist;
            float touch = BeyRadius * 2f;

            if (dist > touch + 0.15f)
            {
                Vec2 desired = dir * SkillTuning.UltLockSpeed(me.SkillQ);
                me.Vel = MoveTowards(me.Vel, desired, 40f * Dt);
                return;
            }

            if (time - me.LastUltContact > 0.4f) me.UltPushDir = dir;
            me.UltPushDir = Vec2.Lerp(me.UltPushDir, dir, 0.08f).Normalized;
            me.LastUltContact = time;

            float speed = SkillTuning.UltDragSpeed(me.SkillQ);
            float along = Vec2.Dot(en.Vel, me.UltPushDir);
            Vec2 lateral = en.Vel - me.UltPushDir * along;
            en.Vel = me.UltPushDir * Math.Max(along, speed) + lateral * 0.5f;

            float gap = dist - touch;
            me.Vel = me.UltPushDir * speed + dir * BeyStatCalculator.Clamp(gap * 6f, -2f, 2f);

            float dragDmg = SkillTuning.UltDragDps * Dt * (en.Phase == SkillPhase.Active ? en.DamageTakenMult : 1f);
            en.Stamina -= dragDmg;
            en.SkillTaken += dragDmg;
        }

        void UltFinale(Fighter me, Fighter en)
        {
            bool contact = en.Alive && time - me.LastUltContact <= 0.45f;
            if (!contact) return;
            Vec2 dir = (en.Pos - me.Pos).Normalized;
            if (dir.SqrLength < 0.5f) dir = me.UltPushDir;
            float enScale = (1f - Combat.KnockbackResist(en.P, tune)) * Combat.FatigueKnockMult(en.Rpm);
            float meScale = (1f - Combat.KnockbackResist(me.P, tune)) * Combat.FatigueKnockMult(me.Rpm);
            if (en.Phase == SkillPhase.Active && en.Shield) enScale *= SkillTuning.ShieldKnockMult;
            Burst(en, dir, SkillTuning.UltFinaleBaseKnock * SkillTuning.UltFinaleMult * enScale);
            Burst(me, -dir, SkillTuning.UltFinaleBaseKnock * meScale);
            float finDmg = SkillTuning.UltFinaleDamage(me.SkillQ) * (en.Phase == SkillPhase.Active ? en.DamageTakenMult : 1f);
            en.Stamina -= finDmg;
            en.SkillTaken += finDmg;
        }

        void Burst(Fighter x, Vec2 dir, float speed)
        {
            float along = Vec2.Dot(x.Vel, dir);
            x.Vel += dir * (Math.Max(0f, -along) + speed);
            float maxSpeed = Combat.MaxSpeed(x.P, tune);
            x.OverspeedUntil = time + 0.7f;
            x.OverspeedMult = Math.Max(1.7f, (x.Vel.Length + 1f) / Math.Max(1f, maxSpeed));
            x.StunUntil = Math.Max(x.StunUntil, time + 0.7f);
        }

        void TickBurst(Fighter me, Fighter en)
        {
            if (!en.Alive || time < me.StunUntil) return;
            Vec2 to = en.Pos - me.Pos;
            float dist = to.Length;
            if (dist < 1e-3f) return;
            float safeOrbit = BeyRadius * 2f + BeyStatCalculator.Lerp(0.55f, 0.35f, me.SkillQ);
            if (!me.Rushing)
            {
                me.OrbitAngle += SkillTuning.BurstOrbitDegPerSec(me.SkillQ) * Dt;
                float rad = me.OrbitAngle * (float)Math.PI / 180f;
                Vec2 desired = en.Pos + new Vec2((float)Math.Cos(rad), (float)Math.Sin(rad)) * safeOrbit;
                me.Vel += (desired - me.Pos).Normalized * (SkillTuning.BurstOrbitForce(me.SkillQ) * Dt);
            }
            else
            {
                me.Vel += (to / dist) * (SkillTuning.BurstRushPull(me.SkillQ) * Dt);
                if (dist < safeOrbit * 0.92f) me.Vel += -(to / dist) * (10f * Dt);
            }
            if (me.Vel.Length > 15f) me.Vel = me.Vel.Normalized * 15f;
        }

        // ---------------- Fizik ----------------

        void Physics(Fighter me, Fighter en)
        {
            if (!me.Alive) return;
            int idx = me == f[0] ? 0 : 1;
            Vec2 steer = steerCache[idx];
            bool steering = steer.SqrLength > 0.0025f;
            bool stunned = time < me.StunUntil;
            float R = tune.ArenaRadius;

            Vec2 a = Vec2.Zero;
            // Stadyum eğimi her zaman var
            float r = me.Pos.Length;
            if (r > 1e-3f) a += -(me.Pos / r) * (tune.BowlPull * Math.Min(1.2f, r / R));

            if (!me.Overridden)
            {
                if (!stunned)
                {
                    float engage = en.Alive ? Movement.EngageWeight(me.Move, time, me.WavePhase) : 0f;
                    Vec2 auto = Movement.AutoAccel(me.Move, me.Pos, me.Vel, R, me.Rpm, me.OrbitSign, engage);
                    if (en.Alive) auto += Movement.EngageAccel(me.Move, me.Pos, me.Vel, en.Pos, me.Rpm, engage);
                    a += auto * (steering ? 0.35f : 1f);
                    // Hafif yalpa
                    a += new Vec2((float)(rng.NextDouble() - 0.5), (float)(rng.NextDouble() - 0.5)) * (2f * me.Move.Wander);
                }
                if (steering)
                    a += steer * (Movement.SteerAccel(me.P) * (stunned ? 0.4f : 1f));
            }

            me.Vel += a * Dt;
            me.Vel *= 1f / (1f + tune.LinearDamping * Dt);

            if (!me.Overridden)
            {
                float cap = Combat.MaxSpeed(me.P, tune);
                if (time < me.OverspeedUntil) cap *= me.OverspeedMult;
                float speed = me.Vel.Length;
                if (speed > cap)
                {
                    float decel = stunned ? 14f : 45f;
                    float ns = Math.Max(cap, speed - decel * Dt);
                    me.Vel *= ns / speed;
                }
                if (me.P.Archetype == BeyArchetype.Defense && !steering && !me.Dashing(time) && !stunned)
                    me.Vel *= (float)Math.Exp(-tune.DefenseFriction * Dt);
            }

            me.Pos += me.Vel * Dt;
        }

        void Collide(Fighter a, Fighter b)
        {
            if (!a.Alive || !b.Alive) return;
            Vec2 d = b.Pos - a.Pos;
            float dist = d.Length;
            float touch = BeyRadius * 2f;
            if (dist >= touch) return;
            Vec2 n = dist > 1e-4f ? d / dist : new Vec2(1f, 0f);

            // Ayır
            float overlap = touch - dist;
            float ma = a.P.Mass, mb = b.P.Mass;
            a.Pos -= n * (overlap * mb / (ma + mb));
            b.Pos += n * (overlap * ma / (ma + mb));

            float approachA = Vec2.Dot(a.Vel, n);
            float approachB = Vec2.Dot(b.Vel, -n);
            if (approachA + approachB <= 0.05f) return;
            if (time - lastCollision < tune.CollisionCooldown)
            {
                // Bekleme süresinde sadece fiziksel sekme
                Vec2 va0 = a.Vel, vb0 = b.Vel;
                a.Vel = Combat.BounceVelocity(va0, vb0, n, ma, mb, tune.Restitution);
                b.Vel = Combat.BounceVelocity(vb0, va0, -n, mb, ma, tune.Restitution);
                return;
            }
            lastCollision = time;
            collisions++;

            HitResult ha = Combat.ResolveHit(MakeHit(a, b, approachA, approachB), tune);
            HitResult hb = Combat.ResolveHit(MakeHit(b, a, approachB, approachA), tune);

            bool ult = (a.Phase == SkillPhase.Active && a.UltDrag) || (b.Phase == SkillPhase.Active && b.UltDrag);
            Vec2 va = a.Vel, vb = b.Vel;
            if (!ult)
            {
                a.Vel = Combat.BounceVelocity(va, vb, n, ma, mb, tune.Restitution) - n * ha.KnockbackSpeed;
                b.Vel = Combat.BounceVelocity(vb, va, -n, mb, ma, tune.Restitution) + n * hb.KnockbackSpeed;
            }

            a.Stamina -= ha.Damage;
            b.Stamina -= hb.Damage;
            a.HitTaken += ha.Damage;
            b.HitTaken += hb.Damage;
            a.StunUntil = time + (a.P.Archetype == BeyArchetype.Defense ? 0.3f : 0.28f);
            b.StunUntil = time + (b.P.Archetype == BeyArchetype.Defense ? 0.3f : 0.28f);

            Parry(a, b, n);
            Parry(b, a, -n);

            AddEnergy(a);
            AddEnergy(b);
        }

        HitInput MakeHit(Fighter self, Fighter other, float selfApproach, float otherApproach)
        {
            bool selfShield = self.Phase == SkillPhase.Active && self.Shield;
            return new HitInput
            {
                Self = self.P,
                Other = other.P,
                SelfRpm = self.Rpm,
                OtherRpm = other.Rpm,
                SelfStaminaRatio = self.Rpm,
                SelfApproach = selfApproach,
                OtherApproach = otherApproach,
                OtherDashing = other.Dashing(time),
                SelfDamageTakenMult = self.Phase == SkillPhase.Active ? self.DamageTakenMult : 1f,
                OtherDamageMult = other.Phase == SkillPhase.Active && other.Rushing ? other.RushMult
                                  : (other.Phase == SkillPhase.Active && other.UltDrag ? 1.1f : 1f),
                SelfKnockbackMult = selfShield ? SkillTuning.ShieldKnockMult : 1f,
                OtherPushBonus = other.Phase == SkillPhase.Active && other.Rushing ? other.RushPush * 0.35f : 0f
            };
        }

        /// <summary>Kalkan aktifken çarpan rakip geri itilir ve küçük hasar alır. n: kalkanlıdan saldırgana.</summary>
        void Parry(Fighter shielded, Fighter attacker, Vec2 n)
        {
            if (shielded.Phase != SkillPhase.Active || !shielded.Shield) return;
            if (shielded.P.Archetype != BeyArchetype.Defense) return;
            attacker.Vel += n * (SkillTuning.ParryImpulse(shielded.SkillQ) / attacker.P.Mass);
            attacker.Stamina -= SkillTuning.ParryDamage(shielded.SkillQ);
            attacker.SkillTaken += SkillTuning.ParryDamage(shielded.SkillQ);
        }

        void AddEnergy(Fighter x)
        {
            if (x.Phase != SkillPhase.Charging) return;
            x.Energy = Math.Min(100f, x.Energy + tune.SkillCollisionEnergy);
            if (x.Energy >= 100f) x.Phase = SkillPhase.Ready;
        }

        void Walls(Fighter x)
        {
            if (!x.Alive) return;
            float wallR = tune.ArenaRadius * 0.95f;
            float r = x.Pos.Length;
            if (r <= wallR) return;
            Vec2 radial = x.Pos / r;
            float outSpeed = Vec2.Dot(x.Vel, radial);
            if (outSpeed > tune.RingOutSpeed)
            {
                x.Out = true;
                return;
            }
            x.Pos = radial * wallR;
            if (outSpeed > 0f) x.Vel -= radial * (outSpeed * (1f + tune.WallRestitution));
        }

        void Drain(Fighter x)
        {
            if (!x.Alive) return;
            x.Stamina -= Combat.NaturalDrainPerSecond(x.P, tune) * Dt;
            if (x.Stamina <= 0f)
            {
                x.Stamina = 0f;
                x.Toppled = true;
            }
        }

        static Vec2 MoveTowards(Vec2 cur, Vec2 target, float maxDelta)
        {
            Vec2 d = target - cur;
            float l = d.Length;
            return l <= maxDelta || l < 1e-5f ? target : cur + d / l * maxDelta;
        }
    }
}
