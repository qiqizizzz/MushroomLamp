/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪锅内提交材料数据，记录提交顺序与最终计分状态
* │  类    名: CookPotEntryData.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using UnityEngine;

namespace Module.Cook
{
    // 烹饪锅内提交材料数据，记录提交顺序与最终计分状态
    public class CookPotEntryData
    {
        public int SubmitOrder { get; private set; }
        public int SourceSlotIndex { get; private set; }
        public string MaterialName { get; private set; }
        public int BaseValue { get; private set; }
        public int CurrentValue { get; private set; }
        public string TagText { get; private set; }
        public Sprite Icon { get; private set; }
        public float CookProgress { get; private set; }
        public float RequiredCookValue { get; private set; }
        public float ScoreMultiplier { get; private set; }
        public float FinalValue { get; private set; }
        public string CookStateText { get; private set; }
        public float CookScoreDelta => FinalValue - CurrentValue;
        public string DisplayText => $"{SubmitOrder}. {MaterialName} {CookStateText} {CookRoundResult.FormatScore(FinalValue)}";

        public CookPotEntryData(int submitOrder, int sourceSlotIndex, CookMaterialData material)
        {
            SubmitOrder = submitOrder;
            SourceSlotIndex = sourceSlotIndex;
            MaterialName = material.MaterialName;
            BaseValue = material.BaseValue;
            CurrentValue = material.CurrentValue;
            TagText = material.TagText;
            Icon = material.Icon;
            CookProgress = material.CookProgress;
            RequiredCookValue = material.RequiredCookValue;

            resolveCookScore();
        }

        // 根据熟度计算提交时的计分倍率
        private void resolveCookScore()
        {
            float overCookLimit = RequiredCookValue + 2f;
            if (CookProgress < RequiredCookValue)
            {
                ScoreMultiplier = 0.5f;
                CookStateText = "未熟";
            }
            else if (CookProgress > overCookLimit)
            {
                ScoreMultiplier = 0.4f;
                CookStateText = "煮烂";
            }
            else
            {
                ScoreMultiplier = 1f;
                CookStateText = "已熟";
            }

            FinalValue = CurrentValue * ScoreMultiplier;
        }
    }
}
