namespace Module.MagicBoxBuff
{
    public static class MagicBoxBuffDisplayText
    {
        public static string FormatEffectType(string effectType)
        {
            if (string.IsNullOrWhiteSpace(effectType)) return string.Empty;

            return effectType switch
            {
                MagicBoxBuffManager.EffectAddRoundScoreFlat => "回合加分",
                MagicBoxBuffManager.EffectAddPerVegetableCap => "蔬菜计分加成",
                MagicBoxBuffManager.EffectModifyBustLimit => "爆牌阈值变更",
                MagicBoxBuffManager.EffectReduceBustPenalty => "爆牌惩罚减免",
                MagicBoxBuffManager.EffectPickMaterialReward => "材料三选一",
                _ => effectType
            };
        }

        public static string FormatEffectTarget(string effectTarget)
        {
            if (string.IsNullOrWhiteSpace(effectTarget)) return string.Empty;

            return effectTarget switch
            {
                "roundFinalScore" => "回合最终分",
                "roundVegetableMaterials" => "回合蔬菜材料",
                "magicBoxBlackjack" => "魔盒21点",
                "magicBoxBustPenalty" => "魔盒爆牌惩罚",
                "playerMaterialPool" => "玩家材料池",
                _ => effectTarget
            };
        }

        public static string FormatDurationType(string durationType)
        {
            if (string.IsNullOrWhiteSpace(durationType)) return string.Empty;

            return durationType switch
            {
                "per_round" => "本回合",
                "magic_box_session" => "本次魔盒",
                "immediate" => "立即生效",
                _ => durationType
            };
        }

        public static string FormatRarity(string rarity)
        {
            if (string.IsNullOrWhiteSpace(rarity)) return "无";

            return rarity switch
            {
                "silver" => "银",
                "gold" => "金",
                _ => rarity
            };
        }
    }
}
