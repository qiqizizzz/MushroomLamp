/*
* ┌──────────────────────────────────┐
* │  描    述: 商店道具槽位绑定器，负责刷新道具图标、价格与详情入口
* │  类    名: ShopSlotBinder.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Shop
{
    public class ShopSlotBinder : MonoBehaviour
    {
        [SerializeField] private Image imgIcon;
        [SerializeField] private Image imgPriceTag;
        [SerializeField] private TextMeshProUGUI txtPrice;
        [SerializeField] private TextMeshProUGUI txtDesc;
        [SerializeField] private ShopHoverScaleItem hoverScale;

        // 绑定商店道具槽位显示与交互
        public void Bind(ShopSlotData data, Action<ShopSlotData> onBuy = null, ShopView view = null)
        {
            if (data == null) return;

            ensureReferences();
            ShopPriceRowHelper.PreparePropSlot(transform, ref txtPrice, ref imgPriceTag);
            if (txtPrice != null) txtPrice.text = data.price.ToString();
            if (txtDesc  != null) txtDesc.text  = data.description;

            if (imgIcon != null)
            {
                var sprite = ArtAssetLoader.LoadSprite(data.iconPath);
                imgIcon.sprite = sprite;
                imgIcon.preserveAspect = true;
                imgIcon.enabled = sprite != null;
            }

            if (hoverScale != null)
            {
                hoverScale.BindTooltip(view, data);
                hoverScale.SetInteractable(!data.isPurchased);
                RectTransform hitRt = imgIcon != null ? imgIcon.rectTransform : transform as RectTransform;
                if (hitRt != null)
                {
                    Vector3 scale = hitRt.localScale;
                    hoverScale.SetHitSize(hitRt.rect.width * scale.x, hitRt.rect.height * scale.y);
                }
            }

            bindBuyButton(data, onBuy);
        }

        // 确保预制体引用已经绑定
        private void ensureReferences()
        {
            if (imgIcon == null)
            {
                Transform iconTf = transform.Find("Img_Icon");
                if (iconTf != null) imgIcon = iconTf.GetComponent<Image>();
            }

            if (txtPrice == null)
            {
                Transform priceTf = transform.Find("PriceRow/Txt_Price") ?? transform.Find("Txt_Price");
                if (priceTf != null) txtPrice = priceTf.GetComponent<TextMeshProUGUI>();
            }

            if (imgPriceTag == null)
            {
                Transform tagTf = transform.Find("PriceRow/Img_PriceTag");
                if (tagTf != null) imgPriceTag = tagTf.GetComponent<Image>();
            }

            if (txtDesc == null)
            {
                Transform descTf = transform.Find("Txt_Desc");
                if (descTf != null) txtDesc = descTf.GetComponent<TextMeshProUGUI>();
            }

            if (hoverScale == null)
                hoverScale = GetComponent<ShopHoverScaleItem>();
            if (hoverScale == null)
                hoverScale = gameObject.AddComponent<ShopHoverScaleItem>();
        }

        // 整个卡槽即购买按钮，绑定根节点上的 Button
        private void bindBuyButton(ShopSlotData data, Action<ShopSlotData> onBuy)
        {
            Button btn = GetComponent<Button>();
            if (btn == null) return;

            btn.onClick.RemoveAllListeners();

            if (data.isPurchased)
            {
                btn.interactable = false;
                hoverScale?.SetInteractable(false);
            }
            else
            {
                btn.interactable = true;
                btn.onClick.AddListener(() => onBuy?.Invoke(data));
            }
        }
    }
}
