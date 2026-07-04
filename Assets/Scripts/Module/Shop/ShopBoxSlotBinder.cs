using System;
using Common;
using Common.UI;
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
            Action<ShopSlotData> onBuy = null)
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
                hoverScale.SetInteractable(!data.isPurchased);
                if (imgIcon != null)
                {
                    RectTransform iconRt = imgIcon.rectTransform;
                    hoverScale.SetHitSize(iconRt.rect.width, iconRt.rect.height);
                }
            }

            bindBuyButton(data, onBuy);
        }

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
