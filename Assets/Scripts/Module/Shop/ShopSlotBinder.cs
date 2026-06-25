using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Shop
{
    public class ShopSlotBinder : MonoBehaviour
    {
        [SerializeField] private Image imgIcon;
        [SerializeField] private TextMeshProUGUI txtName;
        [SerializeField] private TextMeshProUGUI txtPrice;
        [SerializeField] private TextMeshProUGUI txtDesc;

        public void Bind(ShopSlotData data, Action<ShopSlotData> onBuy = null)
        {
            if (data == null) return;

            if (txtName  != null) txtName.text  = data.name;
            if (txtPrice != null) txtPrice.text = data.price.ToString();
            if (txtDesc  != null) txtDesc.text  = data.description;

            if (imgIcon != null)
            {
                var sprite = LoadSprite(data.iconPath);
                imgIcon.sprite  = sprite;
                imgIcon.enabled = sprite != null;
            }

            bindBuyButton(data, onBuy);
        }

        private void bindBuyButton(ShopSlotData data, Action<ShopSlotData> onBuy)
        {
            Transform buyTf = transform.Find("Btn_Buy");
            if (buyTf == null) return;

            Button btn = buyTf.GetComponent<Button>();
            if (btn == null) return;

            btn.onClick.RemoveAllListeners();

            TextMeshProUGUI label = buyTf.Find("Txt_Label")?.GetComponent<TextMeshProUGUI>();

            if (data.isPurchased)
            {
                btn.interactable = false;
                if (label != null) label.text = "已购买";
            }
            else
            {
                btn.interactable = true;
                if (label != null) label.text = "购买";
                btn.onClick.AddListener(() => onBuy?.Invoke(data));
            }
        }

        private static Sprite LoadSprite(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath)) return null;
#if UNITY_EDITOR
            string assetPath = $"Assets/{iconPath}.png";
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
            return null;
#endif
        }
    }
}

