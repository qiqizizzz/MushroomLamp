/*
* ┌──────────────────────────────────┐
* │  描    述: 材料行 UI，负责显示图标、名称与数量
* │  类    名: TextLineItem.cs
* │  创    建: By qiqizizzz
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

        // 关闭自身射线阻挡，避免影响父级滚动列表拖拽
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

        // 绑定材料行显示数据
        public void Bind(SelectMaterialLineData data)
        {
            if (data == null) return;

            if (_txtLabel != null)
                _txtLabel.text = data.label ?? string.Empty;

            if (_txtNum != null)
                _txtNum.text = data.CountText;

            setIcon(data.icon);
        }

        // 设置材料图标显示
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

        // 确保 mushroom 节点在 UI 中拥有可显示的 Image 组件
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
