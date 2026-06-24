/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪法阵槽位 UI 项，负责展示槽位状态并接收材料拖拽
* │  类    名: CookSlotItem.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Cook;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.View
{
    // 烹饪法阵槽位 UI 项，负责展示槽位状态并接收材料拖拽
    public class CookSlotItem : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private readonly Color _emptyColor = new Color(0.96f, 0.82f, 0.58f, 0.92f);
        private readonly Color _highlightColor = new Color(0.99f, 0.94f, 0.42f, 1f);
        private readonly Color _occupiedColor = new Color(0.78f, 0.55f, 0.32f, 1f);

        private CookView _view;
        private int _slotIndex;
        private bool _hasMaterial;

        private Image _imgBackground;
        private Image _imgIcon;
        private TextMeshProUGUI _txtOrder;
        private TextMeshProUGUI _txtName;
        private TextMeshProUGUI _txtValue;

        private void Awake()
        {
            ensureReferences();
        }

        // 初始化槽位索引
        public void Init(CookView view, int slotIndex)
        {
            _view = view;
            _slotIndex = slotIndex;
            applyFont(view == null ? null : view.GetFontAsset());
        }

        // 绑定槽位数据
        public void Bind(CookSlotData slotData)
        {
            ensureReferences();

            _hasMaterial = slotData != null && slotData.HasMaterial;
            CookMaterialData material = slotData?.Material;

            if (_imgBackground != null)
                _imgBackground.color = _hasMaterial ? _occupiedColor : _emptyColor;

            if (_imgIcon != null)
            {
                _imgIcon.sprite = material?.Icon;
                _imgIcon.enabled = material?.Icon != null;
            }

            if (_txtOrder != null)
                _txtOrder.text = _hasMaterial ? slotData.Order.ToString() : string.Empty;

            if (_txtName != null)
                _txtName.text = material?.MaterialName ?? "空槽";

            if (_txtValue != null)
                _txtValue.text = material == null ? string.Empty : material.ValueText;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_view == null || eventData.pointerDrag == null) return;

            CookMaterialItem materialItem = eventData.pointerDrag.GetComponent<CookMaterialItem>();
            if (materialItem == null) return;

            if (_view.TryPlaceMaterial(materialItem, _slotIndex))
                materialItem.AcceptDropAndDestroy();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_imgBackground != null && !_hasMaterial)
                _imgBackground.color = _highlightColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_imgBackground != null && !_hasMaterial)
                _imgBackground.color = _emptyColor;
        }

        private void ensureReferences()
        {
            _imgBackground = getOrCreateImage("Img_Background", transform, _emptyColor);
            _imgIcon = getOrCreateImage("Img_Icon", transform, Color.white);
            _txtOrder = getOrCreateText("Txt_Order", transform, 26, TextAlignmentOptions.Center);
            _txtName = getOrCreateText("Txt_Name", transform, 18, TextAlignmentOptions.Center);
            _txtValue = getOrCreateText("Txt_Value", transform, 22, TextAlignmentOptions.Center);

            setupChildRect(_imgBackground.rectTransform, Vector2.zero, Vector2.one);
            setupChildRect(_imgIcon.rectTransform, new Vector2(0.22f, 0.3f), new Vector2(0.78f, 0.78f));
            setupChildRect(_txtOrder.rectTransform, new Vector2(0.02f, 0.72f), new Vector2(0.28f, 0.98f));
            setupChildRect(_txtName.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.3f));
            setupChildRect(_txtValue.rectTransform, new Vector2(0.72f, 0.72f), new Vector2(0.98f, 0.98f));
        }

        private static Image getOrCreateImage(string childName, Transform parent, Color color)
        {
            Transform child = parent.Find(childName);
            if (child == null)
            {
                GameObject obj = new GameObject(childName, typeof(RectTransform));
                obj.transform.SetParent(parent, false);
                child = obj.transform;
            }

            Image image = child.GetComponent<Image>();
            if (image == null)
                image = child.gameObject.AddComponent<Image>();

            image.color = color;
            return image;
        }

        private static TextMeshProUGUI getOrCreateText(
            string childName,
            Transform parent,
            int fontSize,
            TextAlignmentOptions alignment)
        {
            Transform child = parent.Find(childName);
            if (child == null)
            {
                GameObject obj = new GameObject(childName, typeof(RectTransform));
                obj.transform.SetParent(parent, false);
                child = obj.transform;
            }

            TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
            if (text == null)
                text = child.gameObject.AddComponent<TextMeshProUGUI>();

            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.16f, 0.09f, 0.05f, 1f);
            text.enableWordWrapping = false;
            return text;
        }

        private void applyFont(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return;

            if (_txtOrder != null)
                _txtOrder.font = fontAsset;

            if (_txtName != null)
                _txtName.font = fontAsset;

            if (_txtValue != null)
                _txtValue.font = fontAsset;
        }

        private static void setupChildRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rectTransform == null) return;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
