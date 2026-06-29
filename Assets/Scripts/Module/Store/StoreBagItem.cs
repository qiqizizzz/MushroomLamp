/*
* ┌──────────────────────────────────┐
* │  描    述: 背包卡牌格子（图标 + 右下角数量）
* │  类    名: StoreBagItem.cs
* └──────────────────────────────────┘
*/

using Common;
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

        public void Bind(StoreBagEntryData data)
        {
            if (data == null) return;

            EnsureRefs();

            if (txtName != null) txtName.text = data.name;
            if (txtCount != null) txtCount.text = "x" + data.count;

            if (imgIcon != null)
            {
                Sprite sprite = string.IsNullOrEmpty(data.iconPath) ? null : ArtAssetLoader.LoadSprite(data.iconPath);
                imgIcon.sprite = sprite;
                // 没有资源时保留白膜（保持可见的纯色占位）
                imgIcon.enabled = true;
            }
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
                Transform countTf = transform.Find("Txt_Count");
                if (countTf != null) txtCount = countTf.GetComponent<TextMeshProUGUI>();
            }
        }
    }
}
