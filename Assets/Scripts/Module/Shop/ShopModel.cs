/*
* ┌──────────────────────────────────┐
* │  描    述: 商店数据模型，负责货架内容与本轮回收状态
* │  类    名: ShopModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Text;
using Module.Cook;
using Module.Level;
using Module.Player;
using MVC.Model;

namespace Module.Shop
{
    [Serializable]
    public class ShopSlotData
    {
        public string id;
        public string name;
        public string iconPath;
        public string description;
        public int price;
        public bool isBox;
        public bool isCard;
        public bool isPurchased;
    }

    public class ShopModel : BaseModel
    {
        public const int RefreshShelfCost = 5;

        // 金币直接读玩家单例，不再随机
        public int Gold => PlayerDataManager.Instance.Money;
        public bool HasRecycled { get; private set; }
        public bool CanRecycle => !HasRecycled;

        // 上排：材料箱；下排：道具
        public readonly List<ShopSlotData> BoxSlots = new();
        public readonly List<ShopSlotData> ItemSlots = new();

        // 刷新商店货架
        public void Refresh()
        {
            BoxSlots.Clear();
            ItemSlots.Clear();
            BoxSlots.AddRange(ShopCatalog.RandomBoxes(3));
            ItemSlots.AddRange(ShopCatalog.RandomItems(3));
        }

        // 重置本次进入商店的回收状态
        public void ResetRecycleState()
        {
            HasRecycled = false;
        }

        // 标记本次商店已经完成回收
        public void MarkRecycled()
        {
            HasRecycled = true;
        }

        public string BuildInfoText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("本轮补给");
            sb.AppendLine();
            appendProgressSection(sb);
            sb.AppendLine();
            appendStatusSection(sb);
            return sb.ToString().TrimEnd();
        }

        private static void appendProgressSection(StringBuilder sb)
        {
            sb.AppendLine("【当前进度】");
            LevelFlow flow = LevelFlow.Instance;
            if (!flow.HasFlow)
            {
                sb.AppendLine("尚未开始大局");
                return;
            }

            sb.AppendLine($"大局：{flow.BoxName}");
            sb.AppendLine($"难度：{formatDifficulty(flow.Difficulty)}");
            sb.AppendLine($"小关：{flow.StageIndex + 1} / {flow.StageCount}");
            sb.AppendLine($"牌组材料：{countMaterialKinds(flow)} 种");
        }

        private void appendStatusSection(StringBuilder sb)
        {
            sb.AppendLine("【状态】");
            sb.AppendLine($"当前金币：{Gold}");
            sb.AppendLine($"回收机会：{(CanRecycle ? "可用" : "已使用")}");
            sb.AppendLine($"已拥有道具：{countOwnedItems()}");
            sb.AppendLine($"刷新货架：{RefreshShelfCost} 金币/次");
        }

        private static int countMaterialKinds(LevelFlow flow)
        {
            int kinds = 0;
            foreach (CookMaterialSeedData seed in flow.MaterialPool)
            {
                if (seed != null && seed.Count > 0)
                    kinds++;
            }

            return kinds;
        }

        private static int countOwnedItems()
        {
            int count = 0;
            foreach (string _ in PlayerDataManager.Instance.GetOwnedItemIds())
                count++;
            return count;
        }

        private static string formatDifficulty(Module.Select.SelectDifficulty difficulty)
        {
            return difficulty switch
            {
                Module.Select.SelectDifficulty.Easy => "简单",
                Module.Select.SelectDifficulty.Normal => "普通",
                Module.Select.SelectDifficulty.Hard => "困难",
                _ => difficulty.ToString()
            };
        }
    }
}
