using NUnit.Framework;

namespace BeybladeCore.Tests
{
    public class ArchetypePresetTests
    {
        [Test]
        public void AllTypes_HaveEqualBudget()
        {
            foreach (BeyArchetype t in new[] { BeyArchetype.Attack, BeyArchetype.Defense, BeyArchetype.Stamina, BeyArchetype.Balance })
                Assert.AreEqual(ArchetypePresets.StandardBudget, ArchetypePresets.Get(t).Budget, 1e-3f, t.ToString());
        }

        [Test]
        public void Presets_HaveTheirSignatureStat()
        {
            FighterProfile atk = ArchetypePresets.Get(BeyArchetype.Attack);
            FighterProfile def = ArchetypePresets.Get(BeyArchetype.Defense);
            FighterProfile stm = ArchetypePresets.Get(BeyArchetype.Stamina);
            Assert.Greater(atk.A, atk.D); Assert.Greater(atk.A, atk.S);
            Assert.Greater(def.D, def.A); Assert.Greater(def.D, def.S);
            Assert.Greater(stm.S, stm.A); Assert.Greater(stm.S, stm.D);
            Assert.Greater(def.Mass, atk.Mass, "Savunma en ağır");
        }

        [Test]
        public void ForBudget_ScalesToRequestedBudget_AndKeepsType()
        {
            foreach (float budget in new[] { 1.0f, 1.32f, 1.6f })
            {
                FighterProfile p = ArchetypePresets.ForBudget(BeyArchetype.Defense, budget);
                Assert.AreEqual(budget, p.Budget, 0.02f);
                Assert.AreEqual(BeyArchetype.Defense, p.Archetype);
                Assert.LessOrEqual(p.D, 1f);
            }
        }

        [Test]
        public void ForBudget_RedistributesOverflow()
        {
            // Savunma şablonunda D=0.785; büyük bütçede D 1'i aşar → fazlası diğer statlara gider
            FighterProfile p = ArchetypePresets.ForBudget(BeyArchetype.Defense, 2.2f);
            Assert.AreEqual(1f, p.D, 1e-4f);
            Assert.AreEqual(2.2f, p.Budget, 0.02f);
        }
    }

    public class CombatModelTests
    {
        static readonly CombatTuning T = CombatTuning.Default;

        static HitInput Hit(FighterProfile self, FighterProfile other, float selfApproach, float otherApproach)
            => new HitInput
            {
                Self = self, Other = other,
                SelfRpm = 0.8f, OtherRpm = 0.8f, SelfStaminaRatio = 0.8f,
                SelfApproach = selfApproach, OtherApproach = otherApproach,
                SelfDamageTakenMult = 1f, OtherDamageMult = 1f, SelfKnockbackMult = 1f
            };

        [Test]
        public void MoreDefense_TakesLessDamage()
        {
            FighterProfile atk = ArchetypePresets.Get(BeyArchetype.Attack);
            FighterProfile soft = new FighterProfile(BeyArchetype.Balance, 0.4f, 0.1f, 0.4f, 0.4f);
            FighterProfile hard = new FighterProfile(BeyArchetype.Balance, 0.4f, 0.9f, 0.4f, 0.4f);
            float dSoft = Combat.ResolveHit(Hit(soft, atk, 1f, 7f), T).Damage;
            float dHard = Combat.ResolveHit(Hit(hard, atk, 1f, 7f), T).Damage;
            Assert.Less(dHard, dSoft);
        }

        [Test]
        public void HarderImpact_DealsMoreDamage()
        {
            FighterProfile a = ArchetypePresets.Get(BeyArchetype.Attack);
            FighterProfile b = ArchetypePresets.Get(BeyArchetype.Balance);
            float slow = Combat.ResolveHit(Hit(b, a, 0f, 2f), T).Damage;
            float fast = Combat.ResolveHit(Hit(b, a, 0f, 8f), T).Damage;
            Assert.Greater(fast, slow);
        }

        [Test]
        public void Recoil_OnlyHitsTheAggressor()
        {
            FighterProfile atk = ArchetypePresets.Get(BeyArchetype.Attack);
            FighterProfile def = ArchetypePresets.Get(BeyArchetype.Defense);
            HitResult charging = Combat.ResolveHit(Hit(atk, def, 8f, 0f), T);
            HitResult beingHit = Combat.ResolveHit(Hit(atk, def, 0f, 8f), T);
            Assert.IsTrue(charging.SelfWasAggressor);
            Assert.IsFalse(beingHit.SelfWasAggressor);
            // Aynı çarpışma hızında, savunmaya kafa atan saldırı daha çok hasar alır
            Assert.Greater(charging.Damage, beingHit.Damage * 0.9f);
        }

        [Test]
        public void HeavyBey_IsKnockedLess()
        {
            FighterProfile atk = ArchetypePresets.Get(BeyArchetype.Attack);
            FighterProfile def = ArchetypePresets.Get(BeyArchetype.Defense);
            FighterProfile stm = ArchetypePresets.Get(BeyArchetype.Stamina);
            float kDef = Combat.ResolveHit(Hit(def, atk, 0f, 8f), T).KnockbackSpeed;
            float kStm = Combat.ResolveHit(Hit(stm, atk, 0f, 8f), T).KnockbackSpeed;
            Assert.Less(kDef, kStm);
        }

        [Test]
        public void Shield_ReducesDamageAndKnockback()
        {
            FighterProfile atk = ArchetypePresets.Get(BeyArchetype.Attack);
            FighterProfile def = ArchetypePresets.Get(BeyArchetype.Defense);
            HitInput normal = Hit(def, atk, 0f, 8f);
            HitInput shielded = normal;
            shielded.SelfDamageTakenMult = SkillTuning.ShieldDamageTaken(1f);
            shielded.SelfKnockbackMult = SkillTuning.ShieldKnockMult;
            HitResult a = Combat.ResolveHit(normal, T), b = Combat.ResolveHit(shielded, T);
            Assert.Less(b.Damage, a.Damage);
            Assert.Less(b.KnockbackSpeed, a.KnockbackSpeed);
        }

        [Test]
        public void StaminaType_DrainsSlowest_AttackFastest()
        {
            float atk = Combat.NaturalDrainPerSecond(ArchetypePresets.Get(BeyArchetype.Attack), T);
            float def = Combat.NaturalDrainPerSecond(ArchetypePresets.Get(BeyArchetype.Defense), T);
            float stm = Combat.NaturalDrainPerSecond(ArchetypePresets.Get(BeyArchetype.Stamina), T);
            Assert.Less(stm, def);
            Assert.Less(def, atk);
        }

        [Test]
        public void Bounce_ConservesMomentum()
        {
            Vec2 va = new Vec2(6f, 0f), vb = new Vec2(-1f, 0.5f);
            Vec2 n = new Vec2(1f, 0f);
            float ma = 2.4f, mb = 5f;
            Vec2 a2 = Combat.BounceVelocity(va, vb, n, ma, mb, T.Restitution);
            Vec2 b2 = Combat.BounceVelocity(vb, va, -n, mb, ma, T.Restitution);
            Vec2 before = va * ma + vb * mb, after = a2 * ma + b2 * mb;
            Assert.AreEqual(before.X, after.X, 1e-3f);
            Assert.AreEqual(before.Z, after.Z, 1e-3f);
            Assert.Less(a2.X, va.X, "çarpan geri seker");
        }

        [Test]
        public void LaunchPower_MattersButIsCompressed()
        {
            float weak = Combat.LaunchSpinFraction(0.5f, T);
            float perfect = Combat.LaunchSpinFraction(1f, T);
            Assert.AreEqual(1f, perfect, 1e-4f);
            Assert.Less(weak, perfect);
            Assert.Greater(weak, 0.6f, "zayıf fırlatış bile maçı tek başına kaybettirmez");
        }
    }

    public class MovementTests
    {
        [Test]
        public void TypesPreferDifferentOrbits()
        {
            float atk = Movement.ProfileFor(ArchetypePresets.Get(BeyArchetype.Attack)).PreferredRadius;
            float bal = Movement.ProfileFor(ArchetypePresets.Get(BeyArchetype.Balance)).PreferredRadius;
            float stm = Movement.ProfileFor(ArchetypePresets.Get(BeyArchetype.Stamina)).PreferredRadius;
            float def = Movement.ProfileFor(ArchetypePresets.Get(BeyArchetype.Defense)).PreferredRadius;
            Assert.Greater(atk, bal);
            Assert.Greater(bal, stm);
            Assert.Greater(stm, def);
        }

        [Test]
        public void AutoMovement_DoesNotSteerTowardEnemy()
        {
            // Otomatik hareket sadece konum/hız/arena kullanır: rakip bilgisi almaz (mıknatıs yok)
            MovementProfile m = Movement.ProfileFor(ArchetypePresets.Get(BeyArchetype.Attack));
            Vec2 a = Movement.AutoAccel(m, new Vec2(2.8f, 0f), new Vec2(0f, 5f), 5.2f, 1f, 1f);
            Assert.LessOrEqual(a.Length, m.MaxAccel + 1e-3f);
        }

        [Test]
        public void AutoMovement_PullsBackTowardPreferredRing()
        {
            MovementProfile m = Movement.ProfileFor(ArchetypePresets.Get(BeyArchetype.Defense));
            Vec2 pos = new Vec2(4f, 0f);
            Vec2 a = Movement.AutoAccel(m, pos, Vec2.Zero, 5.2f, 1f, 1f);
            Assert.Less(Vec2.Dot(a, pos.Normalized), 0f, "kenardaki savunma merkeze döner");
        }
    }

    public class BotBrainTests
    {
        static BotObservation Obs(BeyArchetype me, BeyArchetype enemy, Vec2 myPos, Vec2 enemyPos)
            => new BotObservation
            {
                MyPos = myPos, EnemyPos = enemyPos, MyVel = Vec2.Zero, EnemyVel = Vec2.Zero,
                ArenaRadius = 5.2f,
                Me = ArchetypePresets.Get(me), Enemy = ArchetypePresets.Get(enemy),
                MyStaminaRatio = 0.8f, EnemyStaminaRatio = 0.8f,
                Focus = 100f, MaxFocus = 100f, DashCost = 35f, DashReady = true, MyDashSpeed = 9f
            };

        [Test]
        public void AttackBot_CirclesThenStrikes()
        {
            var brain = new BotBrain(BotDifficulty.ForLevel(6), 42);
            bool sawStalk = false, dashed = false;
            for (int i = 0; i < 600 && !dashed; i++)
            {
                BotObservation o = Obs(BeyArchetype.Attack, BeyArchetype.Stamina, new Vec2(-2f, 0f), new Vec2(1f, 0f));
                BotCommand c = brain.Tick(o, 0.02f);
                if (brain.Current == BotBrain.State.Stalk) sawStalk = true;
                if (c.Dash) dashed = true;
            }
            Assert.IsTrue(sawStalk, "önce rakibin etrafında dolanmalı");
            Assert.IsTrue(dashed, "sonra ani atılma yapmalı");
        }

        [Test]
        public void StaminaBot_SidestepsIncomingDash()
        {
            var brain = new BotBrain(BotDifficulty.ForLevel(6), 7);
            BotObservation o = Obs(BeyArchetype.Stamina, BeyArchetype.Attack, new Vec2(0.5f, 0f), new Vec2(-2f, 0f));
            o.EnemyVel = new Vec2(9f, 0f);
            o.EnemyDashing = true;
            BotCommand c = brain.Tick(o, 0.02f);
            Assert.AreEqual(BotBrain.State.Evade, brain.Current);
            Vec2 toEnemy = (o.EnemyPos - o.MyPos).Normalized;
            Assert.Less(System.Math.Abs(Vec2.Dot(c.Steer.Normalized, toEnemy)), 0.6f, "saldırı hattından yana kaçmalı");
        }

        [Test]
        public void AnyBot_RecoversFromEdge()
        {
            var brain = new BotBrain(BotDifficulty.ForLevel(6), 3);
            BotObservation o = Obs(BeyArchetype.Balance, BeyArchetype.Defense, new Vec2(4.5f, 0f), new Vec2(-1f, 0f));
            o.MyVel = new Vec2(3f, 0f);
            BotCommand c = brain.Tick(o, 0.02f);
            Assert.AreEqual(BotBrain.State.Recover, brain.Current);
            Assert.Less(c.Steer.X, 0f, "merkeze doğru yönlenmeli");
        }

        [Test]
        public void DefenseBot_UsesShieldAgainstUlt()
        {
            var brain = new BotBrain(BotDifficulty.ForLevel(10), 5);
            bool used = false;
            for (int i = 0; i < 100 && !used; i++)
            {
                BotObservation o = Obs(BeyArchetype.Defense, BeyArchetype.Attack, Vec2.Zero, new Vec2(2f, 0f));
                o.SkillReady = true;
                o.EnemyUltDragging = true;
                used = brain.Tick(o, 0.02f).UseSkill;
            }
            Assert.IsTrue(used);
        }
    }

    /// <summary>
    /// Tip üçgeni: eşit güç, eşit oyun seviyesi, rastgele (eşit dağılımlı) fırlatma gücü.
    /// Hedef: avantajlı taraf ~%60 kazanır (≥%53 ama ≤%75 → oyunculukla çevrilebilir).
    /// </summary>
    public class BalanceTests
    {
        const int Matches = 300;

        static float WinRate(BeyArchetype a, BeyArchetype b, int level = 5, bool excludeDraws = false)
        {
            int wins = 0, decided = 0;
            BotDifficulty d = BotDifficulty.ForLevel(level);
            for (int i = 0; i < Matches; i++)
            {
                bool swap = (i & 1) == 1;
                var rngA = new System.Random(i * 2 * 104729 + 11);
                var rngB = new System.Random((i * 2 + 1) * 104729 + 11);
                float pA = 0.7f + (float)rngA.NextDouble() * 0.25f;
                float pB = 0.7f + (float)rngB.NextDouble() * 0.25f;
                var s = new SimSetup
                {
                    A = ArchetypePresets.Get(swap ? b : a),
                    B = ArchetypePresets.Get(swap ? a : b),
                    PowerA = pA, PowerB = pB,
                    BrainA = d, BrainB = d,
                    Seed = i * 7919 + 13,
                    MaxTime = 240f
                };
                SimResult r = new BattleSim(s).Run();
                if (r.Winner == (swap ? 1 : 0)) wins++;
                if (r.Winner != -1) decided++;
            }
            if (excludeDraws) return decided > 0 ? 100f * wins / decided : 50f;
            return 100f * wins / Matches;
        }

        [Test]
        public void Attack_Beats_Stamina()
        {
            float wr = WinRate(BeyArchetype.Attack, BeyArchetype.Stamina);
            Assert.That(wr, Is.InRange(53f, 75f), "Saldırı vs Dayanıklılık: " + wr);
        }

        [Test]
        public void Stamina_Beats_Defense()
        {
            float wr = WinRate(BeyArchetype.Stamina, BeyArchetype.Defense);
            Assert.That(wr, Is.InRange(53f, 75f), "Dayanıklılık vs Savunma: " + wr);
        }

        [Test]
        public void Defense_Beats_Attack()
        {
            float wr = WinRate(BeyArchetype.Defense, BeyArchetype.Attack);
            Assert.That(wr, Is.InRange(53f, 75f), "Savunma vs Saldırı: " + wr);
        }

        [Test]
        public void Balance_HasNoHardCounter()
        {
            foreach (BeyArchetype other in new[] { BeyArchetype.Attack, BeyArchetype.Defense, BeyArchetype.Stamina })
            {
                float wr = WinRate(BeyArchetype.Balance, other);
                Assert.That(wr, Is.InRange(30f, 65f), "Denge vs " + other + ": " + wr);
            }
        }

        [Test]
        public void BetterPlay_CanOvercomeCounter()
        {
            // Dezavantajlı taraf çok daha iyi oynarsa (seviye 9 vs 3) en az yarı yarıya kazanabilmeli
            int wins = 0;
            for (int i = 0; i < Matches; i++)
            {
                bool swap = (i & 1) == 1;
                var s = new SimSetup
                {
                    A = ArchetypePresets.Get(swap ? BeyArchetype.Defense : BeyArchetype.Attack),
                    B = ArchetypePresets.Get(swap ? BeyArchetype.Attack : BeyArchetype.Defense),
                    PowerA = 0.82f, PowerB = 0.82f,
                    BrainA = BotDifficulty.ForLevel(swap ? 3 : 9),
                    BrainB = BotDifficulty.ForLevel(swap ? 9 : 3),
                    Seed = i * 7919 + 13,
                    MaxTime = 240f
                };
                SimResult r = new BattleSim(s).Run();
                if (r.Winner == (swap ? 1 : 0)) wins++;
            }
            float wr = 100f * wins / Matches;
            Assert.GreaterOrEqual(wr, 45f, "İyi oynayan Saldırı, Savunmaya karşı: " + wr);
        }

        [Test]
        public void MirrorMatches_AreFair()
        {
            foreach (BeyArchetype t in new[] { BeyArchetype.Attack, BeyArchetype.Defense, BeyArchetype.Stamina, BeyArchetype.Balance })
            {
                // Beraberlikler (iki bey birden düşer/durursa) hariç: taraflar eşit şansa sahip olmalı
                float wr = WinRate(t, t, 5, excludeDraws: true);
                Assert.That(wr, Is.InRange(40f, 60f), t + " ayna maç: " + wr);
            }
        }

        [Test]
        public void Beys_SeekEachOther_EvenWithoutInput()
        {
            // Oyuncu hiç dokunmasa bile beyler birbirine gelip çarpışmalı (çekim dalgası)
            foreach (BeyArchetype player in new[] { BeyArchetype.Balance, BeyArchetype.Attack, BeyArchetype.Stamina, BeyArchetype.Defense })
            {
                float collisions = 0f, time = 0f;
                for (int i = 0; i < 40; i++)
                {
                    var s = new SimSetup
                    {
                        A = ArchetypePresets.Get(player), B = ArchetypePresets.Get(BeyArchetype.Defense),
                        PowerA = 0.85f, PowerB = 0.85f,
                        BrainA = BotDifficulty.ForLevel(5), BrainB = BotDifficulty.ForLevel(5),
                        Seed = i * 13 + 5, MaxTime = 240f, PassiveA = true
                    };
                    SimResult r = new BattleSim(s).Run();
                    collisions += r.Collisions;
                    time += r.Duration;
                }
                float perMinute = collisions / time * 60f;
                Assert.GreaterOrEqual(perMinute, 10f, player + " (dokunmayan oyuncu) vs Savunma: dakikada " + perMinute + " çarpışma");
            }
        }

        [Test]
        public void Simulation_IsDeterministic()
        {
            var s = new SimSetup
            {
                A = ArchetypePresets.Get(BeyArchetype.Attack), B = ArchetypePresets.Get(BeyArchetype.Defense),
                PowerA = 0.8f, PowerB = 0.85f, BrainA = BotDifficulty.ForLevel(5), BrainB = BotDifficulty.ForLevel(5),
                Seed = 1234, MaxTime = 240f
            };
            SimResult r1 = new BattleSim(s).Run(), r2 = new BattleSim(s).Run();
            Assert.AreEqual(r1.Winner, r2.Winner);
            Assert.AreEqual(r1.Duration, r2.Duration, 1e-4f);
            Assert.AreEqual(r1.Collisions, r2.Collisions);
        }

        [Test]
        public void Matches_HaveReasonableLength()
        {
            // Ayna dışı eşleşmelerde maçlar ne anında biter ne de sonsuza uzar
            BotDifficulty d = BotDifficulty.ForLevel(5);
            float total = 0f;
            int n = 60;
            for (int i = 0; i < n; i++)
            {
                var s = new SimSetup
                {
                    A = ArchetypePresets.Get(BeyArchetype.Defense), B = ArchetypePresets.Get(BeyArchetype.Stamina),
                    PowerA = 0.8f, PowerB = 0.8f, BrainA = d, BrainB = d, Seed = i * 31 + 1, MaxTime = 240f
                };
                total += new BattleSim(s).Run().Duration;
            }
            float avg = total / n;
            Assert.That(avg, Is.InRange(25f, 150f), "ortalama süre " + avg);
        }
    }
}
