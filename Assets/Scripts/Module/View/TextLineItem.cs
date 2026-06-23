/*
 * ┌──────────────────────────────────┐
 * │  描    述: 材料行 UI（左图右文）
 * │  类    名: TextLineItem.cs
 * └──────────────────────────────────┘
 */

using Module.Select;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class TextLineItem : BaseItem
    {
        private TextMeshProUGUI _txtLabel;
        private TextMeshProUGUI _txtNum;
        private Image _imgIcon;

        protected override void OnAwake()
        {
            _txtLabel = Find<TextMeshProUGUI>("Txt_Label");
            _txtNum = Find<TextMeshProUGUI>("Txt_Num");
            _imgIcon = Find<Image>("mushroom");
            disableScrollBlockingRaycasts();
        }

        private void disableScrollBlockingRaycasts()
        {
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
                if (button.targetGraphic != null)
                    button.targetGraphic.raycastTarget = false;
            }

            Image bg = GetComponent<Image>();
            if (bg != null)
                bg.raycastTarget = false;
        }

        public void Bind(SelectMaterialLineData data)
        {
            if (data == null) return;

            if (_txtLabel != null)
                _txtLabel.text = data.label ?? string.Empty;

            if (_txtNum != null)
                _txtNum.text = data.CountText;

            setIcon(data.icon);
        }

        private void setIcon(Sprite sprite)
        {
            if (_imgIcon == null)
                ensureIconImage();

            if (_imgIcon != null)
            {
                _imgIcon.sprite = sprite;
                _imgIcon.enabled = sprite != null;
            }
        }

        // TextLine 预制体里 mushroom 默认是 SpriteRenderer，运行时补 Image 以便在 UI 中显示
        private void ensureIconImage()
        {
            GameObject iconGo = Find("mushroom");
            if (iconGo == null) return;

            _imgIcon = iconGo.GetComponent<Image>();
            if (_imgIcon != null) return;

            SpriteRenderer spriteRenderer = iconGo.GetComponent<SpriteRenderer>();
            _imgIcon = iconGo.AddComponent<Image>();
            _imgIcon.preserveAspect = true;
            _imgIcon.raycastTarget = false;

            if (spriteRenderer != null)
            {
                _imgIcon.sprite = spriteRenderer.sprite;
                spriteRenderer.enabled = false;
            }

            RectTransform rt = iconGo.GetComponent<RectTransform>();
            if (rt == null)
            {
                rt = iconGo.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(48f, 48f);
            }
        }
    }
}
