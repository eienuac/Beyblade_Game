namespace BeybladeCore
{
    /// <summary>
    /// Bot zorluğu. Kariyer seviyesi arttıkça bot daha güçlü fırlatır,
    /// daha sık düşünür, daha isabetli skill kullanır.
    /// </summary>
    public struct BotDifficulty
    {
        /// <summary>Bu seviyede zorluk tavan yapar.</summary>
        public const int MaxLevel = 10;

        public int Level;
        /// <summary>0 (en kolay) .. 1 (en zor)</summary>
        public float T;

        public float LaunchPowerMin;
        public float LaunchPowerMax;
        public float ThinkIntervalMin;
        public float ThinkIntervalMax;
        public float DashChance;
        public float SkillQualityMin;
        public float SkillQualityMax;
        public float SkillReactionMin;
        public float SkillReactionMax;
        /// <summary>Kenardan kurtulmak için atılma kullanır mı?</summary>
        public bool UsesEdgeRecovery;
        /// <summary>Saldırı gücü çarpanı.</summary>
        public float AttackScale;
        /// <summary>Dönüş kaybı çarpanı (küçük = daha uzun döner).</summary>
        public float StaminaDecayScale;

        public static BotDifficulty ForLevel(int level)
        {
            if (level < 1) level = 1;
            float t = BeyStatCalculator.InverseLerp(1f, MaxLevel, level);
            return new BotDifficulty
            {
                Level = level,
                T = t,
                LaunchPowerMin = BeyStatCalculator.Lerp(0.45f, 0.8f, t),
                LaunchPowerMax = BeyStatCalculator.Lerp(0.7f, 1f, t),
                ThinkIntervalMin = BeyStatCalculator.Lerp(0.2f, 0.08f, t),
                ThinkIntervalMax = BeyStatCalculator.Lerp(0.32f, 0.15f, t),
                DashChance = BeyStatCalculator.Lerp(0.15f, 0.5f, t),
                SkillQualityMin = BeyStatCalculator.Lerp(0.25f, 0.65f, t),
                SkillQualityMax = BeyStatCalculator.Lerp(0.6f, 0.98f, t),
                SkillReactionMin = BeyStatCalculator.Lerp(0.6f, 0.15f, t),
                SkillReactionMax = BeyStatCalculator.Lerp(1.4f, 0.45f, t),
                UsesEdgeRecovery = level >= 3,
                AttackScale = BeyStatCalculator.Lerp(0.85f, 1.2f, t),
                StaminaDecayScale = BeyStatCalculator.Lerp(1.15f, 0.88f, t)
            };
        }

        /// <summary>Kariyerde rakip tipi seviyeye göre döner: Saldırı, Savunma, Dayanıklılık, Denge...</summary>
        public static BeyArchetype ArchetypeForLevel(int level)
        {
            if (level < 1) level = 1;
            return (BeyArchetype)((level - 1) % 4);
        }
    }
}
