using Module.Player;

namespace Module.Item
{
    // 被动道具运行时：按小关刷新 per_stage 标记，大关内持久
    public static class ItemPassiveManager
    {
        private const string EffectPreventFirstOvercook = "prevent_first_overcook";
        private const string EffectModifyMagicBoxOptionCount = "modify_magic_box_option_count";
        private const string EffectRerollBlackjackCard = "reroll_blackjack_card";
        private const string EffectModifyRoundCount = "modify_round_count";
        private const string EffectFreeFirstMagicBox = "free_first_magic_box";

        private static bool _overcookPadUsed;
        private static bool _rabbitFootUsed;
        private static bool _pandoraKeyUsed;
        private static bool _pandoraSafeSession;

        public static void ResetRun()
        {
            ResetStageState();
        }

        public static void ResetStageState()
        {
            _overcookPadUsed = false;
            _rabbitFootUsed = false;
            _pandoraKeyUsed = false;
            _pandoraSafeSession = false;
        }

        public static void BeginMagicBoxSession()
        {
            if (_pandoraKeyUsed || !ownsEffect(EffectFreeFirstMagicBox))
            {
                _pandoraSafeSession = false;
                return;
            }

            _pandoraSafeSession = true;
        }

        public static void EndMagicBoxSession()
        {
            if (!_pandoraSafeSession) return;

            _pandoraKeyUsed = true;
            _pandoraSafeSession = false;
        }

        public static bool IsPandoraSafeDrawActive => _pandoraSafeSession;

        // 防糊锅垫：本小关首次糊掉改为微焦（保留 80% 分）
        public static bool TryConvertOvercookToSlightBurn(out float scoreMultiplier, out string cookStateText)
        {
            scoreMultiplier = 1f;
            cookStateText = "微焦";

            if (_overcookPadUsed || !ownsEffect(EffectPreventFirstOvercook))
                return false;

            scoreMultiplier = getOwnedEffectValue(EffectPreventFirstOvercook);
            if (scoreMultiplier <= 0f) scoreMultiplier = 0.8f;

            _overcookPadUsed = true;
            return true;
        }

        public static int GetMagicBoxOptionBonus()
        {
            if (!ownsEffect(EffectModifyMagicBoxOptionCount)) return 0;
            return toIntBonus(getOwnedEffectValue(EffectModifyMagicBoxOptionCount));
        }

        public static int GetRoundCountBonus()
        {
            if (!ownsEffect(EffectModifyRoundCount)) return 0;
            return toIntBonus(getOwnedEffectValue(EffectModifyRoundCount));
        }

        // 幸运兔脚：本小关首次爆牌可重抽
        public static bool TryConsumeRabbitFootReroll()
        {
            if (_rabbitFootUsed || !ownsEffect(EffectRerollBlackjackCard))
                return false;

            _rabbitFootUsed = true;
            return true;
        }

        // GM：重置幸运兔脚本小关使用标记
        public static void GmResetRabbitFoot()
        {
            _rabbitFootUsed = false;
        }

        private static bool ownsEffect(string effectType)
        {
            foreach (string itemId in PlayerDataManager.Instance.GetOwnedItemIds())
            {
                ItemParamJsonData cfg = ItemParamCatalogLoader.GetById(itemId);
                if (cfg != null && cfg.effectType == effectType)
                    return true;
            }

            return false;
        }

        private static float getOwnedEffectValue(string effectType)
        {
            foreach (string itemId in PlayerDataManager.Instance.GetOwnedItemIds())
            {
                ItemParamJsonData cfg = ItemParamCatalogLoader.GetById(itemId);
                if (cfg != null && cfg.effectType == effectType)
                    return cfg.effectValue;
            }

            return 0f;
        }

        private static int toIntBonus(float value)
        {
            return UnityEngine.Mathf.RoundToInt(value);
        }
    }
}
