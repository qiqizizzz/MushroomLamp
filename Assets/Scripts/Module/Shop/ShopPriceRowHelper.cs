using Common;
using Common.Defines;
using Common.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Shop
{
    // 商店卡槽价格行：左侧价格贴 + 右侧金币数量
    public static class ShopPriceRowHelper
    {
        public const string RowName = "PriceRow";
        public const string TagName = "Img_PriceTag";

        private const float IconAnchorY = 0.58f;
        private const float IconScale = 1.4f;
        private const float PriceTextPosX = 67.4f;
        private const float PriceTextPosY = -4f;
        private const float PriceTextWidth = 113.7983f;
        private const float PriceTextHeight = 44f;
        private const float PriceTextFontSize = 30f;

        public static void ApplySlotLayout(
            Transform root,
            ref TextMeshProUGUI txtPrice,
            ref Image imgPriceTag,
            TMP_FontAsset font = null)
        {
            if (root == null) return;

            hideNameLabel(root);
            layoutIcon(root.Find("Img_Icon"));
            EnsureLayout(root, ref txtPrice, ref imgPriceTag, font);
        }

        // 道具槽：保留预制体手调布局，仅隐藏名称并清理背景
        public static void PreparePropSlot(
            Transform root,
            ref TextMeshProUGUI txtPrice,
            ref Image imgPriceTag)
        {
            if (root == null) return;

            hideNameLabel(root);
            clearRootBackground(root);
            hideDecorBuyButton(root);
            resolvePriceRowRefs(root, ref txtPrice, ref imgPriceTag);
            applyTagSpriteIfNeeded(imgPriceTag);

            if (txtPrice != null)
                applyPriceFont(txtPrice);
        }

        public static void EnsureLayout(Transform root, ref TextMeshProUGUI txtPrice, ref Image imgPriceTag, TMP_FontAsset font = null)
        {
            if (root == null) return;

            Transform row = root.Find(RowName);
            if (row == null && txtPrice != null)
                row = wrapExistingPriceText(root, txtPrice);

            if (row == null) return;

            if (txtPrice == null)
            {
                Transform priceTf = row.Find("Txt_Price");
                if (priceTf != null) txtPrice = priceTf.GetComponent<TextMeshProUGUI>();
            }

            if (imgPriceTag == null)
            {
                Transform tagTf = row.Find(TagName);
                if (tagTf != null) imgPriceTag = tagTf.GetComponent<Image>();
            }

            if (imgPriceTag == null)
                imgPriceTag = createPriceTag(row);

            applyTagSpriteIfNeeded(imgPriceTag);
            layoutPriceText(txtPrice, font);
        }

        public static RectTransform CreatePriceRow(RectTransform parent, Vector2 anchorY, Vector2 size)
        {
            GameObject row = new GameObject(RowName, typeof(RectTransform));
            RectTransform rowRt = row.GetComponent<RectTransform>();
            rowRt.SetParent(parent, false);
            rowRt.anchorMin = new Vector2(0.5f, anchorY.y);
            rowRt.anchorMax = new Vector2(0.5f, anchorY.y);
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.sizeDelta = size;
            rowRt.anchoredPosition = Vector2.zero;
            return rowRt;
        }

        public static Image CreatePriceTag(RectTransform rowRt)
        {
            if (rowRt == null) return null;

            Transform existing = rowRt.Find(TagName);
            if (existing != null)
                return existing.GetComponent<Image>();

            Image img = createPriceTag(rowRt);
            applyTagSpriteIfNeeded(img);
            return img;
        }

        public static TextMeshProUGUI CreatePriceText(RectTransform rowRt, TMP_FontAsset font, float fontSize)
        {
            GameObject go = new GameObject("Txt_Price", typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(rowRt, false);
            applyPriceTextLayout(rt);

            TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
            applyPriceFont(txt);
            txt.fontSize = fontSize > 0f ? fontSize : PriceTextFontSize;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.black;
            txt.raycastTarget = false;
            return txt;
        }

        private static Transform wrapExistingPriceText(Transform root, TextMeshProUGUI txtPrice)
        {
            RectTransform priceRt = txtPrice.rectTransform;
            GameObject row = new GameObject(RowName, typeof(RectTransform));
            RectTransform rowRt = row.GetComponent<RectTransform>();
            rowRt.SetParent(root, false);
            rowRt.anchorMin = priceRt.anchorMin;
            rowRt.anchorMax = priceRt.anchorMax;
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = priceRt.anchoredPosition;
            rowRt.sizeDelta = new Vector2(170f, 52f);

            priceRt.SetParent(rowRt, false);
            return rowRt;
        }

        private static Image createPriceTag(Transform row)
        {
            GameObject go = new GameObject(TagName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(row, false);

            Image img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }

        private static void applyTagSpriteIfNeeded(Image imgPriceTag)
        {
            if (imgPriceTag == null || imgPriceTag.sprite != null) return;

            Sprite sprite = ArtAssetLoader.LoadSprite(AddressDefines.Art_ShopPriceTag, logOnFail: false);
            if (sprite == null) return;

            imgPriceTag.sprite = sprite;
            imgPriceTag.enabled = true;
            imgPriceTag.SetNativeSize();
        }

        private static void layoutPriceText(TextMeshProUGUI txtPrice, TMP_FontAsset font)
        {
            if (txtPrice == null) return;

            applyPriceFont(txtPrice);
            applyPriceTextLayout(txtPrice.rectTransform);
            txtPrice.fontSize = PriceTextFontSize;
            txtPrice.alignment = TextAlignmentOptions.Center;
            txtPrice.color = Color.black;
        }

        private static void applyPriceTextLayout(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(PriceTextPosX, PriceTextPosY);
            rt.sizeDelta = new Vector2(PriceTextWidth, PriceTextHeight);
        }

        private static void applyPriceFont(TextMeshProUGUI txtPrice)
        {
            UIFontHelper.ApplyChineseFont(txtPrice, UIFontHelper.JingnanFont);
        }

        private static void hideNameLabel(Transform root)
        {
            Transform nameTf = root.Find("Txt_Name");
            if (nameTf != null)
                nameTf.gameObject.SetActive(false);
        }

        private static void clearRootBackground(Transform root)
        {
            Image bg = root.GetComponent<Image>();
            if (bg == null) return;

            bg.sprite = null;
            bg.color = new Color(1f, 1f, 1f, 0f);
            bg.raycastTarget = true;
        }

        private static void hideDecorBuyButton(Transform root)
        {
            Transform buyTf = root.Find("Btn_Buy");
            if (buyTf != null)
                buyTf.gameObject.SetActive(false);
        }

        private static void resolvePriceRowRefs(
            Transform root,
            ref TextMeshProUGUI txtPrice,
            ref Image imgPriceTag)
        {
            Transform row = root.Find(RowName);
            if (row == null) return;

            if (txtPrice == null)
            {
                Transform priceTf = row.Find("Txt_Price");
                if (priceTf != null) txtPrice = priceTf.GetComponent<TextMeshProUGUI>();
            }

            if (imgPriceTag == null)
            {
                Transform tagTf = row.Find(TagName);
                if (tagTf != null) imgPriceTag = tagTf.GetComponent<Image>();
            }
        }

        private static void layoutIcon(Transform iconTf)
        {
            if (iconTf is not RectTransform rt) return;
            setAnchored(rt, IconAnchorY, new Vector2(200f, 200f));
            iconTf.localScale = new Vector3(IconScale, IconScale, IconScale);
        }

        private static void setAnchored(RectTransform rt, float anchorY, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, anchorY);
            rt.anchorMax = new Vector2(0.5f, anchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }
    }
}
