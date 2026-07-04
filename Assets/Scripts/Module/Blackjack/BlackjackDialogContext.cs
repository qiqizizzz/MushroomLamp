namespace Module.Blackjack
{
    public readonly struct BlackjackDialogContext
    {
        public static readonly BlackjackDialogContext Empty = new(0f, 0);

        public float CookCurrentScore { get; }
        public int CookTargetMin { get; }

        public BlackjackDialogContext(float cookCurrentScore, int cookTargetMin)
        {
            CookCurrentScore = cookCurrentScore;
            CookTargetMin = cookTargetMin;
        }

        public bool HasCookTarget => CookTargetMin > 0;
    }
}
