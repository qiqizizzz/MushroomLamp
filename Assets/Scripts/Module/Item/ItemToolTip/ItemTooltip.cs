/*
* ┌──────────────────────────────────┐
* │  描    述: 通用道具详情浮层，负责绑定预制体节点并刷新材料信息
* │  类    名: ItemTooltip.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Cook;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Item
{
    // 通用道具详情浮层，只负责填充数据与控制字段显隐
    public class ItemTooltip : BaseItem
    {
        private readonly Dictionary<string, TooltipFieldRow> _fixedFieldRows = new();
        private readonly List<TooltipFieldRow> _extraFieldRows = new();
        private readonly List<TooltipTagSlot> _tagSlots = new();

        private RectTransform _rectTransform;
        private Transform _tagRoot;
        private Transform _rowRoot;
        private RectTransform _descBlock;
        private Image _imgIcon;
        private TextMeshProUGUI _txtName;
        private TextMeshProUGUI _txtSubtitle;
        private TextMeshProUGUI _txtPrice;
        private TextMeshProUGUI _txtDesc;
        private GameObject _tagTemplate;
        private bool _isInitialized;

        protected override void OnAwake()
        {
            bindPrefabReferences();
            SetVisible(false);
        }

        // 绑定烹饪材料数据
        public void Bind(CookMaterialData material, ItemTooltipMode mode = ItemTooltipMode.Cook)
        {
            Bind(ItemTooltipData.FromMaterial(material, mode));
        }

        // 绑定通用详情数据
        public void Bind(ItemTooltipData data)
        {
            bindPrefabReferences();
            hideDynamicSections();

            if (data == null)
            {
                SetVisible(false);
                return;
            }

            setText(_txtName, string.IsNullOrWhiteSpace(data.Name) ? "未知材料" : data.Name);
            bindText(_txtSubtitle, data.Subtitle);
            bindText(_txtPrice, data.PriceText);
            bindIcon(data.Icon);
            bindTags(data.Tags);
            bindFields(data.Fields);
            bindBlock(_descBlock, _txtDesc, data.Desc);
            SetVisible(true);
        }

        // 保留外部调用入口，字体由预制体自身配置
        public void SetFontAsset(TMP_FontAsset fontAsset)
        {
        }

        // 设置 Tooltip 屏幕位置，并自动限制在画布内部
        public void SetScreenPosition(Vector2 screenPosition, RectTransform canvasRect, Vector2 offset)
        {
            bindPrefabReferences();
            if (_rectTransform == null) return;

            if (canvasRect == null)
            {
                _rectTransform.position = screenPosition + offset;
                return;
            }

            Camera eventCamera = resolveCanvasCamera(canvasRect);
            Vector2 resolvedOffset = resolveOffset(screenPosition, canvasRect, eventCamera, offset);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition + resolvedOffset, eventCamera, out Vector2 localPoint))
                return;

            Vector2 size = _rectTransform.rect.size;
            if (size.x <= 0f || size.y <= 0f)
                size = _rectTransform.sizeDelta;

            Rect rect = canvasRect.rect;
            localPoint.x = Mathf.Clamp(localPoint.x, rect.xMin, rect.xMax - size.x);
            localPoint.y = Mathf.Clamp(localPoint.y, rect.yMin + size.y, rect.yMax);
            _rectTransform.position = canvasRect.TransformPoint(localPoint);
        }

        // 隐藏详情浮层
        public void Hide()
        {
            SetVisible(false);
        }

        public override void SetVisible(bool isVisible)
        {
            if (gameObject.activeSelf != isVisible)
                gameObject.SetActive(isVisible);
        }

        // 绑定预制体内已有节点
        private void bindPrefabReferences()
        {
            if (_isInitialized) return;

            _rectTransform = GetComponent<RectTransform>();
            _imgIcon = Find<Image>("Content/Header/Img_Icon");
            _txtName = Find<TextMeshProUGUI>("Content/Header/TitleGroup/Txt_Name");
            _txtSubtitle = Find<TextMeshProUGUI>("Content/Header/TitleGroup/Txt_Subtitle");
            _txtPrice = Find<TextMeshProUGUI>("Content/Header/Txt_Price");
            _tagRoot = Find<Transform>("Content/TagRoot");
            _rowRoot = Find<Transform>("Content/RowRoot");
            _descBlock = Find<RectTransform>("Content/DescBlock");
            _txtDesc = Find<TextMeshProUGUI>("Content/DescBlock/Txt_Desc");
            _tagTemplate = Find("Content/TagRoot/Tag_Template");

            collectTagSlots();
            collectFieldRows();
            _isInitialized = true;
        }

        // 收集预制体中预留的标签显示位
        private void collectTagSlots()
        {
            _tagSlots.Clear();
            if (_tagRoot == null) return;

            for (int i = 0; i < _tagRoot.childCount; i++)
            {
                Transform child = _tagRoot.GetChild(i);
                if (child.name == "Tag_Template")
                    continue;

                TextMeshProUGUI label = child.Find("Txt_Label")?.GetComponent<TextMeshProUGUI>();
                _tagSlots.Add(new TooltipTagSlot(child.gameObject, label));
            }

            if (_tagTemplate != null)
                _tagTemplate.SetActive(false);
        }

        // 收集预制体中预留的字段行
        private void collectFieldRows()
        {
            _fixedFieldRows.Clear();
            _extraFieldRows.Clear();

            addFixedFieldRow(ItemTooltipData.FIELD_BASIC_SCORE, "Field_BasicScore");
            addFixedFieldRow(ItemTooltipData.FIELD_STATE, "Field_State");
            addFixedFieldRow(ItemTooltipData.FIELD_COOK_PROGRESS, "Field_CookProgress");
            addFixedFieldRow(ItemTooltipData.FIELD_CAN_PROCESS, "Field_CanProcess");
            addFixedFieldRow(ItemTooltipData.FIELD_PROCESS_METHOD, "Field_ProcessMethod");
            addFixedFieldRow(ItemTooltipData.FIELD_TRIGGER_CONDITION, "Field_TriggerCondition");
            addFixedFieldRow(ItemTooltipData.FIELD_EFFECT, "Field_Effect");
            addFixedFieldRow(ItemTooltipData.FIELD_MULTIPLIER, "Field_Multiplier");
            addFixedFieldRow(ItemTooltipData.FIELD_PROCESS_RESULT, "Field_ProcessResult");
            collectExtraFieldRows();
        }

        // 注册固定字段行
        private void addFixedFieldRow(string fieldKey, string rowName)
        {
            TooltipFieldRow row = findFieldRow(rowName);
            if (row.Root != null)
                _fixedFieldRows[fieldKey] = row;
        }

        // 收集额外字段行占位，不在运行时创建新行
        private void collectExtraFieldRows()
        {
            if (_rowRoot == null) return;

            for (int i = 0; i < _rowRoot.childCount; i++)
            {
                Transform child = _rowRoot.GetChild(i);
                if (!child.name.StartsWith("FieldRow_") || child.name == "FieldRow_Template")
                    continue;

                _extraFieldRows.Add(createFieldRow(child));
            }

            Transform template = _rowRoot.Find("FieldRow_Template");
            if (template != null)
                template.gameObject.SetActive(false);
        }

        // 查找字段行节点
        private TooltipFieldRow findFieldRow(string rowName)
        {
            if (_rowRoot == null) return TooltipFieldRow.Empty;

            Transform row = _rowRoot.Find(rowName);
            return row != null ? createFieldRow(row) : TooltipFieldRow.Empty;
        }

        // 创建字段行引用
        private static TooltipFieldRow createFieldRow(Transform row)
        {
            TextMeshProUGUI labelText = row.Find("Txt_Label")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI valueText = row.Find("Txt_Value")?.GetComponent<TextMeshProUGUI>();
            return new TooltipFieldRow(row.gameObject, labelText, valueText);
        }

        // 隐藏本次绑定前的可选区域
        private void hideDynamicSections()
        {
            if (_tagTemplate != null)
                _tagTemplate.SetActive(false);

            for (int i = 0; i < _tagSlots.Count; i++)
                _tagSlots[i].SetVisible(false);

            foreach (TooltipFieldRow row in _fixedFieldRows.Values)
                row.SetVisible(false);

            for (int i = 0; i < _extraFieldRows.Count; i++)
                _extraFieldRows[i].SetVisible(false);
        }

        // 绑定图标显示
        private void bindIcon(Sprite icon)
        {
            if (_imgIcon == null) return;

            _imgIcon.sprite = icon;
            _imgIcon.gameObject.SetActive(icon != null);
        }

        // 绑定标签文本
        private void bindTags(IReadOnlyList<string> tags)
        {
            int visibleCount = 0;
            for (int i = 0; tags != null && i < tags.Count && visibleCount < _tagSlots.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(tags[i])) continue;

                _tagSlots[visibleCount].Bind(tags[i]);
                visibleCount++;
            }

            if (_tagRoot != null)
                _tagRoot.gameObject.SetActive(visibleCount > 0);
        }

        // 绑定字段行
        private void bindFields(IReadOnlyList<ItemTooltipFieldData> fields)
        {
            int extraIndex = 0;
            for (int i = 0; fields != null && i < fields.Count; i++)
            {
                ItemTooltipFieldData field = fields[i];
                if (field == null || string.IsNullOrWhiteSpace(field.Value)) continue;

                if (!string.IsNullOrEmpty(field.Key) && _fixedFieldRows.TryGetValue(field.Key, out TooltipFieldRow fixedRow))
                {
                    fixedRow.Bind(field.Label, field.Value);
                    continue;
                }

                if (extraIndex >= _extraFieldRows.Count) continue;

                _extraFieldRows[extraIndex].Bind(field.Label, field.Value);
                extraIndex++;
            }

            if (_rowRoot != null)
                _rowRoot.gameObject.SetActive(hasVisibleFieldRow());
        }

        // 判断是否存在可见字段行
        private bool hasVisibleFieldRow()
        {
            foreach (TooltipFieldRow row in _fixedFieldRows.Values)
                if (row.IsVisible)
                    return true;

            for (int i = 0; i < _extraFieldRows.Count; i++)
                if (_extraFieldRows[i].IsVisible)
                    return true;

            return false;
        }

        // 绑定可选文本
        private static void bindText(TextMeshProUGUI text, string value)
        {
            bool visible = !string.IsNullOrWhiteSpace(value);
            if (text == null) return;

            text.text = visible ? value : string.Empty;
            text.gameObject.SetActive(visible);
        }

        // 绑定可选文本块
        private static void bindBlock(RectTransform block, TextMeshProUGUI text, string value)
        {
            bool visible = !string.IsNullOrWhiteSpace(value);
            if (text != null)
                text.text = visible ? value : string.Empty;

            if (block != null)
                block.gameObject.SetActive(visible);
        }

        // 设置文本内容
        private static void setText(TextMeshProUGUI text, string value)
        {
            if (text != null)
                text.text = value;
        }

        // 根据鼠标位置选择详情框显示方向，底部材料默认往上弹
        private Vector2 resolveOffset(Vector2 screenPosition, RectTransform canvasRect, Camera eventCamera, Vector2 fallbackOffset)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, eventCamera, out Vector2 cursorLocalPoint))
                return fallbackOffset;

            float tooltipHeight = Mathf.Max(_rectTransform.rect.height, _rectTransform.sizeDelta.y);
            float verticalOffset = cursorLocalPoint.y < canvasRect.rect.center.y
                ? tooltipHeight + Mathf.Abs(fallbackOffset.y)
                : -Mathf.Abs(fallbackOffset.y);

            return new Vector2(Mathf.Abs(fallbackOffset.x), verticalOffset);
        }

        // 解析画布相机
        private static Camera resolveCanvasCamera(RectTransform canvasRect)
        {
            Canvas canvas = canvasRect.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        // 固定字段行引用
        private class TooltipFieldRow
        {
            public static readonly TooltipFieldRow Empty = new TooltipFieldRow(null, null, null);

            public readonly GameObject Root;
            private readonly TextMeshProUGUI _labelText;
            private readonly TextMeshProUGUI _valueText;

            public bool IsVisible => Root != null && Root.activeSelf;

            public TooltipFieldRow(GameObject root, TextMeshProUGUI labelText, TextMeshProUGUI valueText)
            {
                Root = root;
                _labelText = labelText;
                _valueText = valueText;
            }

            // 更新字段行文本
            public void Bind(string label, string value)
            {
                if (_labelText != null)
                    _labelText.text = label;

                if (_valueText != null)
                    _valueText.text = value;

                SetVisible(true);
            }

            // 设置字段行显示状态
            public void SetVisible(bool isVisible)
            {
                if (Root != null)
                    Root.SetActive(isVisible);
            }
        }

        // 标签显示位引用
        private class TooltipTagSlot
        {
            private readonly GameObject _root;
            private readonly TextMeshProUGUI _labelText;

            public TooltipTagSlot(GameObject root, TextMeshProUGUI labelText)
            {
                _root = root;
                _labelText = labelText;
            }

            // 更新标签文本
            public void Bind(string value)
            {
                if (_labelText != null)
                    _labelText.text = value;

                SetVisible(true);
            }

            // 设置标签显示状态
            public void SetVisible(bool isVisible)
            {
                if (_root != null)
                    _root.SetActive(isVisible);
            }
        }
    }
}
