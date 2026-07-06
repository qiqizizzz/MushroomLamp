/*
* ┌──────────────────────────────────┐
* │  描    述: 背包卡牌格子（图标 + 右下角数量）
* │  类    名: StoreBagItem.cs
* └──────────────────────────────────┘
*/

using Common;
using Module.Material;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Store
{
    public class StoreBagItem : MonoBehaviour
    {
        [SerializeField] private Image imgIcon;
        [SerializeField] private TextMeshProUGUI txtName;
        [SerializeField] private TextMeshProUGUI txtCount;

        private StoreBuyHoverItem _hover;

        public void Bind(StoreBagEntryData data, IStoreMaterialTooltipHost tooltipHost = null)
        {
            if (data == null) return;

            EnsureRefs();

            if (txtName != null) txtName.text = data.name;
            if (txtCount != null) txtCount.text = data.count.ToString();

            if (imgIcon != null)
            {
                Sprite sprite = MaterialIconLoader.LoadSprite(data.id, logOnFail: false);
                imgIcon.sprite = sprite;
                // 没有资源时保留白膜（保持可见的纯色占位）
                imgIcon.enabled = true;
            }

            setupHover(tooltipHost, data.id);
        }

        private void setupHover(IStoreMaterialTooltipHost tooltipHost, string materialId)
        {
            if (tooltipHost == null || string.IsNullOrWhiteSpace(materialId))
            {
                if (_hover != null) _hover.SetHoverEnabled(false);
                return;
            }

            if (_hover == null)
                _hover = GetComponent<StoreBuyHoverItem>() ?? gameObject.AddComponent<StoreBuyHoverItem>();

            _hover.Setup(tooltipHost, imgIcon != null ? imgIcon.rectTransform : null, materialId);
            _hover.SetHoverEnabled(true);
        }

        // 预制体若由代码生成，运行时按约定节点名补齐引用
        private void EnsureRefs()
        {
            if (imgIcon == null)
            {
                Transform iconTf = transform.Find("Img_Icon");
                if (iconTf != null) imgIcon = iconTf.GetComponent<Image>();
            }
            if (txtName == null)
            {
                Transform nameTf = transform.Find("Txt_Name");
                if (nameTf != null) txtName = nameTf.GetComponent<TextMeshProUGUI>();
            }
            if (txtCount == null)
            {
                Transform countTf = transform.Find("CountBadge/Txt_Count");
                if (countTf == null) countTf = transform.Find("Txt_Count");
                if (countTf != null) txtCount = countTf.GetComponent<TextMeshProUGUI>();
            }
        }
    }
}
