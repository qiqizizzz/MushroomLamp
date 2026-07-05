/*
* ┌──────────────────────────────────┐
* │  描    述: 商店材料箱槽位绑定器，负责刷新箱子图标、价格与详情入口
* │  类    名: ShopBoxSlotBinder.cs
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
    // 材料箱货架槽：展示箱子图标 + 价格，悬停时放大箱子图
    public class ShopBoxSlotBinder : MonoBehaviour
    {
        [SerializeField] private Image imgIcon;
        [SerializeField] private Image imgPriceTag;
        [SerializeField] private TextMeshProUGUI txtPrice;
        [SerializeField] private ShopHoverScaleItem hoverScale;

        public void Bind(
            ShopSlotData data,
            string boxIconPath,
            Sprite fallbackSprite,
            TMP_FontAsset font,
            Action<ShopSlotData> onBuy = null,
            ShopView view = null)
        {
            if (data == null) return;
            ensureReferences();
            ShopPriceRowHelper.ApplySlotLayout(transform, ref txtPrice, ref imgPriceTag, font);

            if (txtPrice != null) txtPrice.text = data.price.ToString();

            if (imgIcon != null)
            {
                Sprite sprite = ArtAssetLoader.LoadSprite(boxIconPath, logOnFail: false);
                if (sprite == null)
                    sprite = fallbackSprite;

                imgIcon.sprite = sprite;
                imgIcon.preserveAspect = true;
                imgIcon.enabled = sprite != null;
            }

            if (hoverScale != null)
            {
                hoverScale.BindTooltip(view, data);
                hoverScale.SetInteractable(!data.isPurchased);
                if (imgIcon != null)
                {
                    RectTransform iconRt = imgIcon.rectTransform;
                    hoverScale.SetHitSize(iconRt.rect.width, iconRt.rect.height);
                }
            }

            applyPurchasedVisual(data);
            bindBuyButton(data, onBuy);
        }

        // 已购材料箱：置灰且不可交互
        private void applyPurchasedVisual(ShopSlotData data)
        {
            if (data == null || !data.isPurchased) return;

            if (imgIcon != null)
                imgIcon.color = new Color(0.55f, 0.55f, 0.55f, 0.88f);

            if (txtPrice != null)
                txtPrice.color = new Color(0.45f, 0.45f, 0.45f, 1f);

            if (imgPriceTag != null)
                imgPriceTag.color = new Color(0.65f, 0.65f, 0.65f, 0.9f);
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

            if (hoverScale == null)
                hoverScale = GetComponent<ShopHoverScaleItem>();
        }

        // 绑定槽位购买按钮
        private void bindBuyButton(ShopSlotData data, Action<ShopSlotData> onBuy)
        {
            Button btn = GetComponent<Button>();
            if (btn == null) return;

            btn.onClick.RemoveAllListeners();

            if (data.isPurchased)
            {
                btn.interactable = false;
                btn.enabled = false;
                hoverScale?.SetInteractable(false);
            }
            else
            {
                btn.enabled = true;
                btn.interactable = true;
                btn.onClick.AddListener(() => onBuy?.Invoke(data));
            }
        }
    }
}
