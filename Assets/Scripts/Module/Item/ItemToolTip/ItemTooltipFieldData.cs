/*
* ┌──────────────────────────────────┐
* │  描    述: 道具详情浮层字段行数据
* │  类    名: ItemTooltipFieldData.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Item
{
    // 道具详情浮层字段行数据
    public class ItemTooltipFieldData
    {
        public readonly string Label;
        public readonly string Value;

        // 创建字段行数据
        public ItemTooltipFieldData(string label, string value)
        {
            Label = label;
            Value = value;
        }
    }
}
