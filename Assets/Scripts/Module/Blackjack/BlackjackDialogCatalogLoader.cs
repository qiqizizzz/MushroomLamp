using Common;
using Common.Defines;

namespace Module.Blackjack
{
    public static class BlackjackDialogCatalogLoader
    {
        private const string DefaultDevilNormal = "再翻一张试试？";
        private const string DefaultDevilBusted = "嘿嘿，爆了吧！";
        private const string DefaultAngelNormal = "见好就收哦~";
        private const string DefaultAngelBusted = "唉，别贪心呀…";

        private static BlackjackDialogJsonConfig _config;

        public static void EnsureLoaded()
        {
            if (_config != null) return;
            _config = JsonConfigLoader.LoadFromConfig<BlackjackDialogJsonConfig>(AddressDefines.Config_BlackjackDialogCatalog);
        }

        public static string GetDevilText(bool busted)
        {
            EnsureLoaded();
            BlackjackSpeakerDialogJsonData devil = _config?.devil;
            if (busted)
                return pickText(devil?.bustedText, DefaultDevilBusted);

            return pickText(devil?.normalText, DefaultDevilNormal);
        }

        public static string GetAngelText(bool busted)
        {
            EnsureLoaded();
            BlackjackSpeakerDialogJsonData angel = _config?.angel;
            if (busted)
                return pickText(angel?.bustedText, DefaultAngelBusted);

            return pickText(angel?.normalText, DefaultAngelNormal);
        }

        private static string pickText(string configured, string fallback)
        {
            return string.IsNullOrWhiteSpace(configured) ? fallback : configured;
        }
    }
}
