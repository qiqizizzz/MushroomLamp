namespace Module.Item
{
    // 道具配置英文字段 → 界面展示中文（逻辑仍读 JSON 英文值）
    public static class ItemParamDisplayText
    {
        public static string FormatEffectType(string effectType)
        {
            if (string.IsNullOrWhiteSpace(effectType)) return string.Empty;

            return effectType switch
            {
                "prevent_first_overcook" => "首次糊锅减免",
                "modify_magic_box_option_count" => "魔盒选项数变更",
                "reroll_blackjack_card" => "21点重抽",
                "modify_round_count" => "回合数变更",
                "free_first_magic_box" => "首次魔盒免抽牌",
                _ => effectType
            };
        }

        public static string FormatEffectTarget(string effectTarget)
        {
            if (string.IsNullOrWhiteSpace(effectTarget)) return string.Empty;

            return effectTarget switch
            {
                "overcook" => "糊锅",
                "magicBoxOptionCount" => "魔盒选项数",
                "blackjack" => "21点",
                "roundCount" => "回合数",
                "blackjackCardDrawCount" => "21点抽牌数",
                _ => effectTarget
            };
        }

        public static string FormatTriggerType(string triggerType)
        {
            if (string.IsNullOrWhiteSpace(triggerType)) return string.Empty;

            return triggerType switch
            {
                "on_overcook" => "糊锅时",
                "passive" => "被动",
                "on_blackjack_draw" => "21点抽牌时",
                "on_stage_start" => "小关开始时",
                "on_magic_box" => "魔盒时",
                _ => triggerType
            };
        }

        public static string FormatDurationType(string durationType)
        {
            if (string.IsNullOrWhiteSpace(durationType)) return string.Empty;

            return durationType switch
            {
                "per_stage" => "每小关",
                "always" => "持续生效",
                _ => durationType
            };
        }
    }
}
