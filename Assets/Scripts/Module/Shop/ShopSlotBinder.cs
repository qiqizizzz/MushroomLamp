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
                var sprite = Resources.Load<Sprite>(data.iconPath);
                imgIcon.sprite = sprite;
                imgIcon.enabled = sprite != null;
            }
        }
    }
}
