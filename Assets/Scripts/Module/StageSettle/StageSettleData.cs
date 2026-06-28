/*
* ┌──────────────────────────────────┐
* │  描    述: 小局结算展示数据，保存小局得分与目标达成信息
* │  类    名: StageSettleData.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.StageSettle
{
    // 小局结算展示数据，保存小局得分与目标达成信息
    public class StageSettleData
    {
        public string BoxName;
        public string StageId;
        public int StageIndex;
        public int StageCount;
        public int TurnCount;
        public int TargetMin;
        public int TargetMax;
        public float CurrentScore;
        public int Coin;
        public bool IsTargetReached;
        public bool IsFinalStage;

        // 右下角按钮去向：未达标 或 最后小局 → 最终结算；否则 → 商店
        public bool GoToFinalSummary => !IsTargetReached || IsFinalStage;
    }
}
