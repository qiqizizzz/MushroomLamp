/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪材料图标视觉校正工具，统一处理材料图标偏移与旋转
* │  类    名: CookMaterialIconVisual.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    // 烹饪材料图标视觉校正工具，统一处理材料图标偏移与旋转
    public static class CookMaterialIconVisual
    {
        private struct IconVisualAdjust
        {
            public Vector2 OffsetRatio;
            public float Scale;
            public float RotationZ;

            public IconVisualAdjust(Vector2 offsetRatio, float scale = 1f, float rotationZ = 0f)
            {
                OffsetRatio = offsetRatio;
                Scale = scale;
                RotationZ = rotationZ;
            }
        }

        // 按显示盒尺寸应用图标视觉校正
        public static void Apply(Image image, Vector2 boxSize)
        {
            if (image == null) return;

            RectTransform iconRt = image.rectTransform;
            IconVisualAdjust adjust = getIconVisualAdjust(image.sprite);
            float width = Mathf.Max(1f, boxSize.x);
            float height = Mathf.Max(1f, boxSize.y);
            float scale = Mathf.Max(0.01f, adjust.Scale);

            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(width * scale, height * scale);
            iconRt.anchoredPosition = new Vector2(width * adjust.OffsetRatio.x, height * adjust.OffsetRatio.y);
            iconRt.localScale = Vector3.one;
            iconRt.localRotation = Quaternion.Euler(0f, 0f, adjust.RotationZ);
        }

        // 按当前 RectTransform 尺寸应用图标视觉校正
        public static void Apply(Image image, Vector2 fallbackSize, RectTransform sourceRect)
        {
            Vector2 boxSize = fallbackSize;
            if (sourceRect != null && sourceRect.rect.size.sqrMagnitude > 0f)
                boxSize = sourceRect.rect.size;

            Apply(image, boxSize);
        }

        // 获取不同材料图标的视觉校正参数
        private static IconVisualAdjust getIconVisualAdjust(Sprite sprite)
        {
            if (sprite == null)
                return new IconVisualAdjust(Vector2.zero);

            string spriteName = sprite.name;
            if (spriteName == "carrot")
                return new IconVisualAdjust(new Vector2(0.02f, -0.04f), 1f, -12f);

            if (spriteName == "potato")
                return new IconVisualAdjust(new Vector2(-0.02f, -0.03f), 1f, -20f);

            if (spriteName == "pumpkin")
                return new IconVisualAdjust(new Vector2(-0.035f, 0.03f));

            return new IconVisualAdjust(Vector2.zero);
        }
    }
}
