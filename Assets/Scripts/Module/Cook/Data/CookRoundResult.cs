/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪回合结算结果，保存本回合得分与目标命中状态
* │  类    名: CookRoundResult.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Cook
{
    // 烹饪回合结算结果，保存本回合得分与目标命中状态
    public class CookRoundResult
    {
        public int TurnIndex { get; private set; }
        public int RoundScore { get; private set; }
        public bool IsTargetMatched { get; private set; }
        public bool IsOverHeat { get; private set; }

        public CookRoundResult(int turnIndex, int roundScore, bool isTargetMatched, bool isOverHeat)
        {
            TurnIndex = turnIndex;
            RoundScore = roundScore;
            IsTargetMatched = isTargetMatched;
            IsOverHeat = isOverHeat;
        }
    }
}
