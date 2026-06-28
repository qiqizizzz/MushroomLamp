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
                var sprite = ArtAssetLoader.LoadSprite(data.iconPath);
                imgIcon.sprite  = sprite;
                imgIcon.enabled = sprite != null;
            }

            bindBuyButton(data, onBuy);
        }

        // 旧的私有 LoadSprite 已废弃（真机分支直接返回 null 导致图丢失），统一改走 ArtAssetLoader

        // 整个卡槽即购买按钮：绑根节点上的 Button
        private void bindBuyButton(ShopSlotData data, Action<ShopSlotData> onBuy)
        {
            Button btn = GetComponent<Button>();
            if (btn == null) return;

            btn.onClick.RemoveAllListeners();

            if (data.isPurchased)
            {
                btn.interactable = false;
            }
            else
            {
                btn.interactable = true;
                btn.onClick.AddListener(() => onBuy?.Invoke(data));
            }
        }
    }
}

