/*
* ┌──────────────────────────────────┐
* │  描    述: 商店购买卡牌格子（图标 + 下方材料介绍框）
* │  类    名: StoreBuyItem.cs
* └──────────────────────────────────┘
*/

using Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Store
{
    public class StoreBuyItem : MonoBehaviour
    {
        [SerializeField] private Image imgIcon;
        [SerializeField] private TextMeshProUGUI txtDesc;
        [SerializeField] private Button button;

        private StoreBuyHoverItem _hover;

        public Image Icon => imgIcon;
        public TextMeshProUGUI Desc => txtDesc;
        public Button Button => button;

        public StoreBuyHoverItem Hover
        {
            get
            {
                if (_hover == null) _hover = GetComponent<StoreBuyHoverItem>();
                return _hover;
            }
        }

        private void Awake()
        {
            EnsureRefs();
            makeBackgroundTransparent();
        }

        public void Bind(StoreBuySlotData slot)
        {
            if (slot == null) return;
            EnsureRefs();

            if (imgIcon != null)
            {
                Sprite sprite = string.IsNullOrEmpty(slot.iconPath) ? null : ArtAssetLoader.LoadSprite(slot.iconPath);
                imgIcon.sprite = sprite;
                imgIcon.preserveAspect = true;
                imgIcon.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
                imgIcon.enabled = sprite != null;
            }

            SetDescription(resolveDescription(slot));
        }

        private static string resolveDescription(StoreBuySlotData slot)
        {
            if (!string.IsNullOrEmpty(slot.description)) return slot.description;
            if (!string.IsNullOrEmpty(slot.name)) return slot.name;
            return "暂无介绍";
        }

        public void SetDescription(string text)
        {
            if (txtDesc != null) txtDesc.text = text ?? string.Empty;
        }

        public void ClearDescription()
        {
            SetDescription(string.Empty);
        }

        private void EnsureRefs()
        {
            if (imgIcon == null)
            {
                Transform t = transform.Find("Img_Icon");
                if (t != null) imgIcon = t.GetComponent<Image>();
            }

            if (txtDesc == null)
            {
                Transform t = transform.Find("MaterialDesc/Txt_Desc");
                if (t != null) txtDesc = t.GetComponent<TextMeshProUGUI>();
            }

            if (button == null) button = GetComponent<Button>();
        }

        private void makeBackgroundTransparent()
        {
            Image bg = GetComponent<Image>();
            if (bg == null) return;
            bg.sprite = null;
            bg.color = new Color(1f, 1f, 1f, 0f);
        }
    }
}
