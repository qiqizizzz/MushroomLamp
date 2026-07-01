/*
* ┌──────────────────────────────────┐
* │  描    述: 项目 UI 中文字体加载（思源黑体 SDF）
* │  类    名: UIFontHelper.cs
* └──────────────────────────────────┘
*/

using Common.Defines;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Common.UI
{
    public static class UIFontHelper
    {
        private static TMP_FontAsset _sourceHanSans;

        // Assets/Fonts/siyuan/SourceHanSansSC-Normal SDF.asset
        public static TMP_FontAsset SourceHanSans
        {
            get
            {
                if (_sourceHanSans != null) return _sourceHanSans;

                AsyncOperationHandle<TMP_FontAsset> handle =
                    Addressables.LoadAssetAsync<TMP_FontAsset>(AddressDefines.Font_SourceHanSansSdf);
                handle.WaitForCompletion();

                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                    _sourceHanSans = handle.Result;

                return _sourceHanSans;
            }
        }

        public static void ApplyChineseFont(TextMeshProUGUI text, TMP_FontAsset preferred = null)
        {
            if (text == null) return;

            TMP_FontAsset font = preferred ?? SourceHanSans;
            if (font != null)
                text.font = font;
        }
    }
}
