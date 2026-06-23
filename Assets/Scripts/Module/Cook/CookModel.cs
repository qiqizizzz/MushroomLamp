/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪玩法数据模型，保存局内回合与分数状态
* │  类    名: CookModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using MVC.Model;

namespace Module.Cook
{
    // 烹饪玩法数据模型，保存局内基础状态
    public class CookModel : BaseModel
    {
        private const int MAX_TURN = 5;

        public int DayIndex { get; private set; }
        public int TurnIndex { get; private set; }
        public int TargetScore { get; private set; }
        public int CurrentScore { get; private set; }
        public bool IsRunActive { get; private set; }

        // 开始新一局烹饪
        public void StartRun()
        {
            DayIndex = 1;
            TurnIndex = 1;
            TargetScore = 8;
            CurrentScore = 0;
            IsRunActive = true;
        }

        // 推进到下一回合并返回是否仍可继续
        public bool AdvanceTurn()
        {
            if (!IsRunActive) return false;

            CurrentScore += 2;
            if (TurnIndex >= MAX_TURN)
            {
                IsRunActive = false;
                return false;
            }

            TurnIndex++;
            return true;
        }

        // 获取当前回合进度文本
        public string GetTurnText()
        {
            return $"Day {DayIndex}  回合 {TurnIndex}/{MAX_TURN}";
        }

        // 获取当前分数目标文本
        public string GetScoreText()
        {
            return $"{CurrentScore}/{TargetScore}";
        }
    }
}
