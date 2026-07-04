/*
* ┌──────────────────────────────────┐
* │  描    述: 项目 UI 中文字体加载（荆南缘默体 SDF）
* │  类    名: UIFontHelper.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Text;
using Common;
using Common.Defines;
using TMPro;
using UnityEngine;

namespace Common.UI
{
    public static class UIFontHelper
    {
        private const string ConfigResourceRoot = "Config";

        private static TMP_FontAsset _jingnanFont;
        private static bool _prewarmed;

        // Assets/Resources/Fonts/jingnan/荆南缘默体 SDF.asset
        public static TMP_FontAsset JingnanFont
        {
            get
            {
                if (_jingnanFont != null) return _jingnanFont;

                _jingnanFont = Resources.Load<TMP_FontAsset>(AddressDefines.Font_JingnanSdf);
                return _jingnanFont;
            }
        }

        // 启动时把配置与常用 UI 文案中的汉字写入动态图集，避免方块字
        public static void EnsurePrewarmed()
        {
            if (_prewarmed) return;

            TMP_FontAsset font = JingnanFont;
            if (font == null) return;

            collectCharsFromText(getBuiltinUiChars(), _charBuffer);
            TextAsset[] configs = Resources.LoadAll<TextAsset>(ConfigResourceRoot);
            for (int i = 0; i < configs.Length; i++)
            {
                if (configs[i] == null) continue;
                collectCharsFromText(configs[i].text, _charBuffer);
            }

            if (_charBuffer.Length == 0)
            {
                _prewarmed = true;
                return;
            }

            string allChars = _charBuffer.ToString();
            if (!font.TryAddCharacters(allChars, out string missing) && !string.IsNullOrEmpty(missing))
                QLog.Warning($"[{nameof(UIFontHelper)}] 字体缺少 {missing.Length} 个字符，部分文案可能显示为方块。");

            _charBuffer.Clear();
            _prewarmed = true;
        }

        public static void ApplyChineseFont(TextMeshProUGUI text, TMP_FontAsset preferred = null)
        {
            if (text == null) return;

            EnsurePrewarmed();

            TMP_FontAsset font = preferred ?? JingnanFont;
            if (font != null)
                text.font = font;
        }

        private static readonly StringBuilder _charBuffer = new StringBuilder(4096);
        private static readonly HashSet<char> _seenChars = new HashSet<char>();

        private static string getBuiltinUiChars()
        {
            return @"，。！？、；：""''（）【】《》—…·￥％＋－×÷0123456789" +
                   "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                   "确定取消返回购买出售价格金币材料道具商店烹饪回收设置图鉴开始游戏退出" +
                   "回合累计点数已翻魔盒天使恶魔";
        }

        private static void collectCharsFromText(string text, StringBuilder buffer)
        {
            if (string.IsNullOrEmpty(text)) return;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n' || c == '\r' || c == '\t') continue;
                if (_seenChars.Add(c))
                    buffer.Append(c);
            }
        }
    }
}
