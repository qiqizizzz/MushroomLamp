/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪回合结算结果，保存本回合得分与目标命中状态
* │  类    名: CookRoundResult.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Cook
{
    // 烹饪回合结算结果，保存本回合得分拆分与目标命中状态
    public class CookRoundResult
    {
        public int TurnIndex { get; private set; }
        public int BaseScore { get; private set; }
        public int ProcessBonus { get; private set; }
        public int ComboBonus { get; private set; }
        public int ComboCount { get; private set; }
        public int MagicBoxBonus { get; private set; }
        public int DevilRisk { get; private set; }
        public int PenaltyScore { get; private set; }
        public int RoundScore { get; private set; }
        public int FinalScore { get; private set; }
        public int CoinReward { get; private set; }
        public bool IsAngelRescued { get; private set; }
        public bool IsTargetMatched { get; private set; }
        public bool IsOverHeat { get; private set; }
        public string ComboText { get; private set; }

        public CookRoundResult(
            int turnIndex,
            int baseScore,
            int processBonus,
            int comboBonus,
            int comboCount,
            int magicBoxBonus,
            int devilRisk,
            int penaltyScore,
            int coinReward,
            bool isAngelRescued,
            bool isTargetMatched,
            bool isOverHeat,
            string comboText)
        {
            TurnIndex = turnIndex;
            BaseScore = baseScore;
            ProcessBonus = processBonus;
            ComboBonus = comboBonus;
            ComboCount = comboCount;
            MagicBoxBonus = magicBoxBonus;
            DevilRisk = devilRisk;
            PenaltyScore = penaltyScore;
            RoundScore = baseScore + processBonus + comboBonus + magicBoxBonus;
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
            string magicText = MagicBoxBonus > 0 ? $" + 魔盒{MagicBoxBonus}" : string.Empty;
            string penaltyText = PenaltyScore > 0 ? $" - 惩罚{PenaltyScore}" : string.Empty;
            return $"基础{BaseScore} + 加工{ProcessBonus} + 连携{ComboBonus}{magicText}{penaltyText} = {FinalScore}";
        }
    }
}
