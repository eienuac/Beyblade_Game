using NUnit.Framework;

namespace BeybladeCore.Tests
{
    public class BeyStatCalculatorTests
    {
        // Başlangıç seti: Çelik Kanat + Standart Disk + Denge Ucu (DefaultPartCatalog ile aynı değerler)
        static readonly PartStats StarterLayer = new PartStats(2.5f, 0.15f, 1f, 0.6f);
        static readonly PartStats StarterDisk = new PartStats(0f, 0.10f, 1f, 1.0f);
        static readonly PartStats StarterTip = new PartStats(0.5f, 0.10f, 1.5f, 0f);

        [Test]
        public void StarterSet_IsBalancedType()
        {
            BeyStatBlock s = BeyStatCalculator.Compute(StarterLayer, StarterDisk, StarterTip);
            Assert.AreEqual(5f, s.Attack, 1e-4f);
            Assert.AreEqual(0.45f, s.Defense, 1e-4f);
            Assert.AreEqual(0.9f, s.StaminaDecay, 1e-4f);
            Assert.AreEqual(2.4f, s.Weight, 1e-4f);
            Assert.AreEqual(BeyArchetype.Balance, s.Archetype);
        }

        [Test]
        public void MoreStamina_MeansSlowerDecay()
        {
            BeyStatBlock low = BeyStatCalculator.Compute(new PartStats(0, 0, 1f, 0));
            BeyStatBlock high = BeyStatCalculator.Compute(new PartStats(0, 0, 4f, 0));
            Assert.Less(high.StaminaDecay, low.StaminaDecay, "Stamina parçası beyi daha uzun döndürmeli");
        }

        [Test]
        public void AttackParts_GiveAttackType()
        {
            BeyStatBlock s = BeyStatCalculator.Compute(
                new PartStats(5f, 0.05f, 0f, 0.5f),  // Ejderha
                new PartStats(0.5f, 0f, 2f, 0.4f),   // Hafif Disk
                new PartStats(2f, 0f, 0f, 0f));      // Düz Uç
            Assert.AreEqual(BeyArchetype.Attack, s.Archetype);
        }

        [Test]
        public void DefenseParts_GiveDefenseType()
        {
            BeyStatBlock s = BeyStatCalculator.Compute(
                new PartStats(1f, 0.40f, 0.5f, 1.2f),  // Kale
                new PartStats(0.5f, 0.20f, 0.5f, 2.5f), // Ağır Disk
                new PartStats(0f, 0.25f, 1f, 0.1f));    // Top Uç
            Assert.AreEqual(BeyArchetype.Defense, s.Archetype);
        }

        [Test]
        public void StaminaParts_GiveStaminaType()
        {
            BeyStatBlock s = BeyStatCalculator.Compute(
                new PartStats(1f, 0.10f, 3f, 0.3f),  // Rüzgar
                new PartStats(0.5f, 0f, 2f, 0.4f),   // Hafif Disk
                new PartStats(0f, 0.05f, 4f, 0f));   // İğne Uç
            Assert.AreEqual(BeyArchetype.Stamina, s.Archetype);
        }

        [Test]
        public void Stats_AreClampedToGameRanges()
        {
            BeyStatBlock s = BeyStatCalculator.Compute(new PartStats(100f, 100f, 100f, 100f));
            Assert.AreEqual(BeyStatCalculator.AttackMax, s.Attack);
            Assert.AreEqual(BeyStatCalculator.DefenseMax, s.Defense);
            Assert.AreEqual(BeyStatCalculator.DecayMin, s.StaminaDecay);
            Assert.AreEqual(BeyStatCalculator.WeightMax, s.Weight);

            BeyStatBlock none = BeyStatCalculator.Compute();
            Assert.GreaterOrEqual(none.Weight, BeyStatCalculator.WeightMin);
            Assert.LessOrEqual(none.StaminaDecay, BeyStatCalculator.DecayMax);
        }

        [Test]
        public void Norms_StayBetweenZeroAndOne()
        {
            BeyStatBlock s = BeyStatCalculator.Compute(StarterLayer, StarterDisk, StarterTip);
            foreach (float n in new[] { s.AttackNorm, s.DefenseNorm, s.StaminaNorm, s.WeightNorm })
            {
                Assert.GreaterOrEqual(n, 0f);
                Assert.LessOrEqual(n, 1f);
            }
        }
    }

    public class BotDifficultyTests
    {
        [Test]
        public void HigherLevel_IsHarder()
        {
            BotDifficulty easy = BotDifficulty.ForLevel(1);
            BotDifficulty hard = BotDifficulty.ForLevel(BotDifficulty.MaxLevel);
            Assert.Greater(hard.LaunchPowerMin, easy.LaunchPowerMin);
            Assert.Greater(hard.DashChance, easy.DashChance);
            Assert.Greater(hard.SkillQualityMax, easy.SkillQualityMax);
            Assert.Less(hard.ThinkIntervalMin, easy.ThinkIntervalMin);
            Assert.Less(hard.SkillReactionMin, easy.SkillReactionMin);
            Assert.Greater(hard.AttackScale, easy.AttackScale);
            Assert.Less(hard.StaminaDecayScale, easy.StaminaDecayScale);
            Assert.IsFalse(easy.UsesEdgeRecovery);
            Assert.IsTrue(hard.UsesEdgeRecovery);
        }

        [Test]
        public void Ranges_AreValid_ForAllLevels()
        {
            for (int lv = -2; lv <= 20; lv++)
            {
                BotDifficulty d = BotDifficulty.ForLevel(lv);
                Assert.LessOrEqual(d.LaunchPowerMin, d.LaunchPowerMax, "lv " + lv);
                Assert.LessOrEqual(d.ThinkIntervalMin, d.ThinkIntervalMax, "lv " + lv);
                Assert.LessOrEqual(d.SkillQualityMin, d.SkillQualityMax, "lv " + lv);
                Assert.LessOrEqual(d.SkillReactionMin, d.SkillReactionMax, "lv " + lv);
                Assert.LessOrEqual(d.LaunchPowerMax, 1f);
                Assert.Greater(d.ThinkIntervalMin, 0f);
            }
        }

        [Test]
        public void Archetype_CyclesEveryFourLevels()
        {
            Assert.AreEqual(BeyArchetype.Attack, BotDifficulty.ArchetypeForLevel(1));
            Assert.AreEqual(BeyArchetype.Defense, BotDifficulty.ArchetypeForLevel(2));
            Assert.AreEqual(BeyArchetype.Stamina, BotDifficulty.ArchetypeForLevel(3));
            Assert.AreEqual(BeyArchetype.Balance, BotDifficulty.ArchetypeForLevel(4));
            Assert.AreEqual(BeyArchetype.Attack, BotDifficulty.ArchetypeForLevel(5));
            Assert.AreEqual(BeyArchetype.Attack, BotDifficulty.ArchetypeForLevel(0));
        }
    }

    public class MatchRewardsTests
    {
        [Test]
        public void Win_PaysMoreThanDraw_PaysMoreThanLoss()
        {
            int win = MatchRewards.Calculate(MatchOutcome.Win, false, 1, false);
            int draw = MatchRewards.Calculate(MatchOutcome.Draw, false, 1, false);
            int loss = MatchRewards.Calculate(MatchOutcome.Loss, false, 1, false);
            Assert.Greater(win, draw);
            Assert.Greater(draw, loss);
            Assert.Greater(loss, 0);
        }

        [Test]
        public void RingOut_GivesBonus()
        {
            Assert.AreEqual(
                MatchRewards.Calculate(MatchOutcome.Win, false, 1, false) + MatchRewards.RingOutBonus,
                MatchRewards.Calculate(MatchOutcome.Win, false, 1, true));
        }

        [Test]
        public void CareerReward_GrowsWithLevel()
        {
            int lv1 = MatchRewards.Calculate(MatchOutcome.Win, true, 1, false);
            int lv5 = MatchRewards.Calculate(MatchOutcome.Win, true, 5, false);
            Assert.AreEqual(MatchRewards.CareerWinBase, lv1);
            Assert.Greater(lv5, lv1);
            Assert.Greater(lv1, MatchRewards.Calculate(MatchOutcome.Win, false, 1, false));
        }
    }
}
