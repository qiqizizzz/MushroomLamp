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

        public void Bind(ShopSlotData data)
        {
            if (data == null) return;

            if (txtName != null) txtName.text = data.name;
            if (txtPrice != null) txtPrice.text = data.price.ToString();
            if (txtDesc != null) txtDesc.text = data.description;

            if (imgIcon != null)
            {
                var sprite = LoadSprite(data.iconPath);
                imgIcon.sprite = sprite;
                imgIcon.enabled = sprite != null;
            }
        }

        private static Sprite LoadSprite(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath)) return null;
#if UNITY_EDITOR
            // 仅编辑器：JSON 表里 iconPath 形如 "Art/Card_img/carrot"（共享给其他面板，不改）。
            // 这里拼成完整 asset 路径加载，图片放在 Assets/Art 下、无需放进 Resources。
            string assetPath = $"Assets/{iconPath}.png";
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
            return null;
#endif
        }
    }
}
