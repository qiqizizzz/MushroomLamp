/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪玩法运行时材料数据，保存数值、标签与界面图标
* │  类    名: CookMaterialData.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using UnityEngine;

namespace Module.Cook
{
    // 烹饪玩法运行时材料数据，保存数值、标签与界面图标
    public class CookMaterialData
    {
        public int RuntimeId { get; private set; }
        public string MaterialName { get; private set; }
        public int BaseValue { get; private set; }
        public int CurrentValue { get; private set; }
        public string TagText { get; private set; }
        public bool CanProcess { get; private set; }
        public bool IsProcessed { get; private set; }
        public Sprite Icon { get; private set; }

        public string ValueText => IsProcessed ? $"{CurrentValue}*" : CurrentValue.ToString();

        public CookMaterialData(
            int runtimeId,
            string materialName,
            int baseValue,
            string tagText,
            bool canProcess,
            Sprite icon)
        {
            RuntimeId = runtimeId;
            MaterialName = materialName;
            BaseValue = baseValue;
            CurrentValue = baseValue;
            TagText = tagText;
            CanProcess = canProcess;
            Icon = icon;
        }

        // 标记材料进入加工状态
        public void MarkProcessed(int valueDelta, string extraTag)
        {
            if (!CanProcess || IsProcessed) return;

            IsProcessed = true;
            CurrentValue = Mathf.Max(0, CurrentValue + valueDelta);
            if (!string.IsNullOrWhiteSpace(extraTag))
                TagText = $"{TagText}/{extraTag}";
        }
    }
}
