/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪回合结算结果，保存本回合得分与目标命中状态
* │  类    名: CookRoundResultData.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Cook
{
    // 烹饪回合结算结果，保存本回合得分拆分与目标命中状态
    public class CookRoundResultData
    {
        public int TurnIndex { get; private set; }
        public float BaseScore { get; private set; }
        public float ProcessBonus { get; private set; }
        public float SlotBonus { get; private set; }
        public float ComboBonus { get; private set; }
        public int ComboCount { get; private set; }
        public int OrderComboCount { get; private set; }
        public float MagicBoxBonus { get; private set; }
        public float DevilRisk { get; private set; }
        public float PenaltyScore { get; private set; }
        public float RoundScore { get; private set; }
        public float FinalScore { get; private set; }
        public int CoinReward { get; private set; }
        public bool IsAngelRescued { get; private set; }
        public bool IsTargetMatched { get; private set; }
        public bool IsOverHeat { get; private set; }
        public string ComboText { get; private set; }

        public CookRoundResultData(
            int turnIndex,
            float baseScore,
            float processBonus,
            float slotBonus,
            float comboBonus,
            int comboCount,
            int orderComboCount,
            float magicBoxBonus,
            float devilRisk,
            float penaltyScore,
            int coinReward,
            bool isAngelRescued,
            bool isTargetMatched,
            bool isOverHeat,
            string comboText)
        {
            TurnIndex = turnIndex;
            BaseScore = baseScore;
            ProcessBonus = processBonus;
            SlotBonus = slotBonus;
            ComboBonus = comboBonus;
            ComboCount = comboCount;
            OrderComboCount = orderComboCount;
            MagicBoxBonus = magicBoxBonus;
            DevilRisk = devilRisk;
            PenaltyScore = penaltyScore;
            RoundScore = baseScore + processBonus + slotBonus + comboBonus + magicBoxBonus;
            FinalScore = UnityEngine.Mathf.Max(0, RoundScore - penaltyScore);
            CoinReward = coinReward;
            IsAngelRescued = isAngelRescued;
            IsTargetMatched = isTargetMatched;
            IsOverHeat = isOverHeat;
            ComboText = comboText;
        }

        // 获取简短得分拆分文本
        public string GetBreakdownText()
        {
            string magicText = MagicBoxBonus > 0 ? $" + 魔盒{FormatScore(MagicBoxBonus)}" : string.Empty;
            string comboText = ComboBonus > 0 ? $" + 连携{FormatScore(ComboBonus)}" : string.Empty;
            string penaltyText = PenaltyScore > 0 ? $" - 惩罚{FormatScore(PenaltyScore)}" : string.Empty;
            return $"基础{FormatScore(BaseScore)} + 加工{FormatScore(ProcessBonus)} + 熟度{FormatScore(SlotBonus)}{comboText}{magicText}{penaltyText} = {FormatScore(FinalScore)}";
        }

        // 格式化可能带半分的数值
        public static string FormatScore(float value)
        {
            return value.ToString("0.#");
        }
    }
}
