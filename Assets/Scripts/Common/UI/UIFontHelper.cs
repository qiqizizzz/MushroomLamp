/*
* ┌──────────────────────────────────┐
* │  描    述: 项目 UI 中文字体加载（荆南缘默体 SDF）
* │  类    名: UIFontHelper.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using TMPro;
using UnityEngine;

namespace Common.UI
{
    public static class UIFontHelper
    {
        private static TMP_FontAsset _jingnanFont;

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

        public static void ApplyChineseFont(TextMeshProUGUI text, TMP_FontAsset preferred = null)
        {
            if (text == null) return;

            TMP_FontAsset font = preferred ?? JingnanFont;
            if (font != null)
                text.font = font;
        }
    }
}
