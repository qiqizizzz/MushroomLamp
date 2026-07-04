/*
* ┌──────────────────────────────────┐
* │  描    述: 总结算展示数据，汇总本次大局的得分、金币与亮点
* │  类    名: SummaryModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Cook;
using Module.Level;
using MVC.Model;

namespace Module.Summary
{
    // 总结算展示数据，负责从关卡流程生成最终展示内容
    public class SummaryModel : BaseModel
    {
        public string DeckName { get; private set; }
        public int RoundsDone { get; private set; }
        public int RoundsTotal { get; private set; }
        public string TotalFlavorText { get; private set; }
        public string MaxSingleRoundText { get; private set; }
        public int ResonanceCount { get; private set; }
        public int AngelBlessCount { get; private set; }
        public int DevilDealCount { get; private set; }
        public int GoldEarned { get; private set; }
        public int FinalScore { get; private set; }
        public bool ShowAlmanacBadge { get; private set; }
        public IReadOnlyList<string> Highlights => _highlights;

        private readonly List<string> _highlights = new();

        // 从当前大局流程读取最终结算数据
        public void LoadFromCurrentRun()
        {
            LevelFlow levelFlow = LevelFlow.Instance;

            DeckName = string.IsNullOrWhiteSpace(levelFlow.BoxName) ? "未知材料箱" : levelFlow.BoxName;
            RoundsDone = levelFlow.CompletedStageCount > 0 ? levelFlow.CompletedStageCount : levelFlow.StageIndex + 1;
            RoundsTotal = levelFlow.StageCount > 0 ? levelFlow.StageCount : RoundsDone;
            TotalFlavorText = CookRoundResultData.FormatScore(levelFlow.TotalScore);
            MaxSingleRoundText = CookRoundResultData.FormatScore(levelFlow.MaxRoundScore);
            ResonanceCount = levelFlow.ResonanceCount;
            AngelBlessCount = levelFlow.AngelBlessCount;
            DevilDealCount = levelFlow.DevilDealCount;
            GoldEarned = levelFlow.TotalCoinEarned;
            FinalScore = calculateFinalScore(levelFlow);
            ShowAlmanacBadge = RoundsDone > 0;

            refreshHighlights(levelFlow);
        }

        // 根据大局表现计算最终评分
        private static int calculateFinalScore(LevelFlow levelFlow)
        {
            float progressScore = levelFlow.StageCount > 0
                ? levelFlow.CompletedStageCount * 10f / levelFlow.StageCount
                : levelFlow.CompletedStageCount * 10f;
            float total = levelFlow.TotalScore + levelFlow.TotalCoinEarned + progressScore;
            return UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Max(0, total));
        }

        // 生成右侧亮点列表
        private void refreshHighlights(LevelFlow levelFlow)
        {
            _highlights.Clear();

            _highlights.Add(levelFlow.CompletedStageCount >= levelFlow.StageCount && levelFlow.StageCount > 0
                ? $"完成全部小局: {levelFlow.CompletedStageCount}/{levelFlow.StageCount}"
                : $"推进小局: {levelFlow.CompletedStageCount}/{levelFlow.StageCount}");
            _highlights.Add($"累计火候: {CookRoundResultData.FormatScore(levelFlow.TotalScore)}");
            _highlights.Add($"最高单次投锅: {CookRoundResultData.FormatScore(levelFlow.MaxRoundScore)}");

            if (levelFlow.AngelBlessCount > 0)
                _highlights.Add($"天使祝福生效 {levelFlow.AngelBlessCount} 次");
            if (levelFlow.DevilDealCount > 0)
                _highlights.Add($"恶魔交易触发 {levelFlow.DevilDealCount} 次");
            if (levelFlow.ResonanceCount > 0)
                _highlights.Add($"共鸣连携触发 {levelFlow.ResonanceCount} 次");
            if (levelFlow.TotalCoinEarned > 0)
                _highlights.Add($"获得金币 {levelFlow.TotalCoinEarned} 枚");
        }
    }
}
