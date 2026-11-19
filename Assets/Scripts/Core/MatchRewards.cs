namespace BeybladeCore
{
    public enum MatchOutcome { Win, Loss, Draw }

    /// <summary>Maç sonu beycoin ödülü.</summary>
    public static class MatchRewards
    {
        public const int WinBase = 40;
        public const int RingOutBonus = 10;
        public const int DrawReward = 15;
        public const int LossReward = 10;
        public const int CareerWinBase = 60;
        public const int CareerPerLevel = 15;

        public static int Calculate(MatchOutcome outcome, bool isCareer, int careerLevel, bool byRingOut)
        {
            switch (outcome)
            {
                case MatchOutcome.Win:
                    int baseWin = isCareer ? CareerWinBase + CareerPerLevel * (careerLevel < 1 ? 0 : careerLevel - 1) : WinBase;
                    return baseWin + (byRingOut ? RingOutBonus : 0);
                case MatchOutcome.Draw:
                    return DrawReward;
                default:
                    return LossReward;
            }
        }
    }
}
