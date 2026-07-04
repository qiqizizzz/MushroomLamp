using System;

namespace Module.Blackjack
{
    [Serializable]
    public class BlackjackDialogRulesJsonData
    {
        public int blackjackLowMaxPoint = 10;
        public int blackjackMidMaxPoint = 16;
        public int blackjackHighMinPoint = 17;
        public float nearTargetRatio = 0.85f;
    }

    [Serializable]
    public class BlackjackDialogLineJsonData
    {
        public string id;
        public string role;
        public string phase;
        public string text;
        public int weight = 100;
        public int sortOrder;
        public bool enabled = true;
        public string priority;
        public int maxPerStage = 2;
        public int maxPerSession = 1;
        public float cooldownSeconds = 20f;
        public float displaySeconds = 3f;
        public string emotion;
    }

    [Serializable]
    public class BlackjackDialogJsonConfig
    {
        public BlackjackDialogRulesJsonData rules;
        public BlackjackDialogLineJsonData[] lines;
    }
}
