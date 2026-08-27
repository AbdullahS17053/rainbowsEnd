namespace RainbowsEnd.Gameplay.Enemies
{
    public static class MilitaryBurstUtility
    {
        public static bool ShouldStartTelegraph(int shotsInBurst, bool hasTelegraphedCurrentBurst, float telegraphSeconds)
        {
            return shotsInBurst == 0
                && !hasTelegraphedCurrentBurst
                && telegraphSeconds > 0f;
        }
    }
}