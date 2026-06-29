/*
* ┌──────────────────────────────────┐
* │  描    述: 通用道具详情浮层，负责按字段数量伸缩展示材料信息
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
    // 通用道具详情浮层，支持简单材料与复杂材料按字段数量自动伸缩
    public class ItemTooltip : BaseItem
    {
        private const int TOOLTIP_SORTING_ORDER = 5000;
        private const float CONTENT_PADDING_X = 14f;
        private const float CONTENT_PADDING_Y = 14f;
        private const float FIELD_ROW_SPACING = 8f;
        private const float TEXT_BLOCK_PADDING_X = 8f;
        private const float TEXT_BLOCK_PADDING_Y = 6f;

        [SerializeField] private float TooltipWidth = 380f;
        [SerializeField] private float RowLabelWidth = 96f;

        private readonly List<TextMeshProUGUI> _tagTexts = new();
        private readonly List<GameObject> _dynamicFieldRows = new();
        private readonly Dictionary<string, TooltipFieldRow> _fixedFieldRows = new();

        private RectTransform _rectTransform;
        private RectTransform _contentRoot;
        private RectTransform _headerRoot;
        private RectTransform _titleRoot;
        private RectTransform _tagRoot;
        private RectTransform _rowRoot;
        private RectTransform _descBlock;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private Image _imgBackground;
        private Image _imgIcon;
        private TextMeshProUGUI _txtName;
        private TextMeshProUGUI _txtSubtitle;
        private TextMeshProUGUI _txtPrice;
        private TextMeshProUGUI _txtDesc;
        private GameObject _tagTemplate;
        private GameObject _fieldRowTemplate;
        private TMP_FontAsset _fontAsset;

        protected override void OnAwake()
        {
            ensureHierarchy();
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
            ensureHierarchy();
            clearDynamicContent();

            if (data == null)
            {
                SetVisible(false);
                return;
            }

            _txtName.text = string.IsNullOrWhiteSpace(data.Name) ? "未知材料" : data.Name;
            _txtSubtitle.text = data.Subtitle ?? string.Empty;
            _txtSubtitle.gameObject.SetActive(!string.IsNullOrWhiteSpace(_txtSubtitle.text));
            _txtPrice.text = data.PriceText ?? string.Empty;
            _txtPrice.gameObject.SetActive(!string.IsNullOrWhiteSpace(_txtPrice.text));

            _imgIcon.sprite = data.Icon;
            _imgIcon.enabled = data.Icon != null;

            bindTags(data.Tags);
            bindFields(data.Fields);
            bindBlock(_descBlock, _txtDesc, data.Desc);
            SetVisible(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
            refreshDynamicLayoutSizes();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rowRoot);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
            clampHeight();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
        }

        // 设置详情浮层字体
        public void SetFontAsset(TMP_FontAsset fontAsset)
        {
            _fontAsset = fontAsset;
            applyFontAsset();
        }

        // 设置 Tooltip 屏幕位置，并自动限制在画布内部
        public void SetScreenPosition(Vector2 screenPosition, RectTransform canvasRect, Vector2 offset)
        {
            ensureHierarchy();
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

        // 设置显示状态，保持根物体激活以便后续悬停可以立刻重新显示
        public override void SetVisible(bool isVisible)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            ensureHierarchy();
            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        // 确保基础 UI 层级存在
        private void ensureHierarchy()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
                _rectTransform = gameObject.AddComponent<RectTransform>();

            _rectTransform.sizeDelta = new Vector2(TooltipWidth, _rectTransform.sizeDelta.y <= 0 ? 240f : _rectTransform.sizeDelta.y);
            _rectTransform.anchorMin = new Vector2(0f, 1f);
            _rectTransform.anchorMax = new Vector2(0f, 1f);
            _rectTransform.pivot = new Vector2(0f, 1f);

            _imgBackground = GetComponent<Image>();
            if (_imgBackground == null)
                _imgBackground = gameObject.AddComponent<Image>();

            _imgBackground.color = new Color(0.18f, 0.13f, 0.10f, 0.94f);
            _imgBackground.raycastTarget = false;

            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
                _canvas = gameObject.AddComponent<Canvas>();

            _canvas.overrideSorting = true;
            _canvas.sortingOrder = TOOLTIP_SORTING_ORDER;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            _contentRoot = getOrCreateRect("Content", transform);
            setupTopLeft(_contentRoot, new Vector2(CONTENT_PADDING_X, -CONTENT_PADDING_Y), new Vector2(getContentWidth(), 0f));
            setupVerticalLayout(_contentRoot.gameObject, 8f, TextAnchor.UpperLeft);
            ensureFitter(_contentRoot.gameObject);

            createHeader();
            createTagRoot();
            createRowRoot();
            createTextBlocks();
        }

        // 创建标题区
        private void createHeader()
        {
            _headerRoot = getOrCreateRect("Header", _contentRoot);
            setupHorizontalLayout(_headerRoot.gameObject, 10f, TextAnchor.MiddleLeft);
            setLayout(_headerRoot.gameObject, getContentWidth(), 58f, getContentWidth(), 58f);

            _imgIcon = getOrCreateImage("Img_Icon", _headerRoot, new Color(0.92f, 0.83f, 0.66f, 1f));
            setLayout(_imgIcon.gameObject, 54f, 54f, 54f, 54f);
            _imgIcon.preserveAspect = true;
            _imgIcon.raycastTarget = false;

            _titleRoot = getOrCreateRect("TitleGroup", _headerRoot);
            setupVerticalLayout(_titleRoot.gameObject, 2f, TextAnchor.MiddleLeft);
            setFlexibleLayout(_titleRoot.gameObject);

            _txtName = getOrCreateText("Txt_Name", _titleRoot, 24f, new Color(1f, 0.91f, 0.72f, 1f), TextAlignmentOptions.Left);
            _txtName.fontStyle = FontStyles.Bold;

            _txtSubtitle = getOrCreateText("Txt_Subtitle", _titleRoot, 16f, new Color(0.82f, 0.72f, 0.58f, 1f), TextAlignmentOptions.Left);
            _txtPrice = getOrCreateText("Txt_Price", _headerRoot, 18f, new Color(1f, 0.79f, 0.36f, 1f), TextAlignmentOptions.Right);
            setLayout(_txtPrice.gameObject, 72f, 24f, 72f, 24f);
        }

        // 创建标签容器
        private void createTagRoot()
        {
            _tagRoot = getOrCreateRect("TagRoot", _contentRoot);
            HorizontalLayoutGroup layout = setupHorizontalLayout(_tagRoot.gameObject, 6f, TextAnchor.UpperLeft);
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            setLayout(_tagRoot.gameObject, getContentWidth(), 28f, getContentWidth(), 28f);
            ContentSizeFitter fitter = ensureFitter(_tagRoot.gameObject);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            _tagTemplate = getOrCreateTagTemplate();
        }

        // 创建字段行容器
        private void createRowRoot()
        {
            _rowRoot = getOrCreateRect("RowRoot", _contentRoot);
            setupVerticalLayout(_rowRoot.gameObject, 2f, TextAnchor.UpperLeft);
            ensureFitter(_rowRoot.gameObject);
            _fieldRowTemplate = createFieldRow("FieldRow_Template", string.Empty, string.Empty);
            _fieldRowTemplate.SetActive(false);
            ensureFixedFieldRows();
        }

        // 创建描述文本块
        private void createTextBlocks()
        {
            _descBlock = createTextBlock("DescBlock", "Txt_Desc", out _txtDesc);
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

        // 获取内容区固定宽度
        private float getContentWidth()
        {
            return Mathf.Max(1f, TooltipWidth - CONTENT_PADDING_X * 2f);
        }

        // 创建文本块
        private RectTransform createTextBlock(string blockName, string textName, out TextMeshProUGUI text)
        {
            RectTransform block = getOrCreateRect(blockName, _contentRoot);
            Image image = block.GetComponent<Image>();
            if (image != null)
                image.enabled = false;

            removeVerticalLayout(block.gameObject);
            removeFitter(block.gameObject);
            setLayout(block.gameObject, getContentWidth(), 32f, getContentWidth(), 32f);

            text = getOrCreateText(textName, block, 16f, new Color(0.94f, 0.86f, 0.73f, 1f), TextAlignmentOptions.TopLeft);
            setupTextBlockTextRect(text.rectTransform, getTextBlockTextWidth(), 20f);
            return block;
        }

        // 获取或创建标签模板，运行时复制模板生成动态标签
        private GameObject getOrCreateTagTemplate()
        {
            RectTransform tagRoot = getOrCreateRect("Tag_Template", _tagRoot);
            Image image = tagRoot.GetComponent<Image>();
            if (image == null)
                image = tagRoot.gameObject.AddComponent<Image>();

            image.color = new Color(0.67f, 0.43f, 0.26f, 0.85f);
            image.raycastTarget = false;
            setLayout(tagRoot.gameObject, -1f, 28f, -1f, 28f);

            TextMeshProUGUI text = getOrCreateText("Txt_Label", tagRoot, 15f, new Color(1f, 0.89f, 0.68f, 1f), TextAlignmentOptions.Center);
            text.text = string.Empty;
            text.enableWordWrapping = false;
            setPadding(text.rectTransform, 10f, 2f);
            tagRoot.gameObject.SetActive(false);
            return tagRoot.gameObject;
        }

        // 确保策划字段在预制体内有固定行
        private void ensureFixedFieldRows()
        {
            _fixedFieldRows.Clear();
            addFixedFieldRow(ItemTooltipData.FIELD_BASIC_SCORE, "Field_BasicScore", "基础分值");
            addFixedFieldRow(ItemTooltipData.FIELD_STATE, "Field_State", "状态");
            addFixedFieldRow(ItemTooltipData.FIELD_COOK_PROGRESS, "Field_CookProgress", "熟度");
            addFixedFieldRow(ItemTooltipData.FIELD_CAN_PROCESS, "Field_CanProcess", "是否可加工");
            addFixedFieldRow(ItemTooltipData.FIELD_PROCESS_METHOD, "Field_ProcessMethod", "加工方式");
            addFixedFieldRow(ItemTooltipData.FIELD_TRIGGER_CONDITION, "Field_TriggerCondition", "触发条件");
            addFixedFieldRow(ItemTooltipData.FIELD_EFFECT, "Field_Effect", "效果");
            addFixedFieldRow(ItemTooltipData.FIELD_MULTIPLIER, "Field_Multiplier", "倍率");
            addFixedFieldRow(ItemTooltipData.FIELD_PROCESS_RESULT, "Field_ProcessResult", "加工结果");
        }

        // 注册固定字段行
        private void addFixedFieldRow(string fieldKey, string rowName, string fallbackLabel)
        {
            Transform existingRow = _rowRoot.Find(rowName);
            GameObject row = existingRow != null ? existingRow.gameObject : createFieldRow(rowName, fallbackLabel, string.Empty);
            TextMeshProUGUI labelText = row.transform.Find("Txt_Label")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI valueText = row.transform.Find("Txt_Value")?.GetComponent<TextMeshProUGUI>();
            if (labelText == null || valueText == null)
            {
                row = createFieldRow(rowName, fallbackLabel, string.Empty);
                labelText = row.transform.Find("Txt_Label")?.GetComponent<TextMeshProUGUI>();
                valueText = row.transform.Find("Txt_Value")?.GetComponent<TextMeshProUGUI>();
            }

            if (existingRow == null)
                row.SetActive(true);

            _fixedFieldRows[fieldKey] = new TooltipFieldRow(row, labelText, valueText);
        }

        // 绑定标签
        private void bindTags(IReadOnlyList<string> tags)
        {
            for (int i = 0; tags != null && i < tags.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(tags[i])) continue;

                TextMeshProUGUI tagText = createTagText(tags[i]);
                _tagTexts.Add(tagText);
            }

            _tagRoot.gameObject.SetActive(_tagTexts.Count > 0);
        }

        // 绑定字段行
        private void bindFields(IReadOnlyList<ItemTooltipFieldData> fields)
        {
            hideFixedFieldRows();

            for (int i = 0; fields != null && i < fields.Count; i++)
            {
                ItemTooltipFieldData field = fields[i];
                if (field == null || string.IsNullOrWhiteSpace(field.Value)) continue;

                if (!string.IsNullOrEmpty(field.Key) && _fixedFieldRows.TryGetValue(field.Key, out TooltipFieldRow fixedRow))
                {
                    fixedRow.Bind(field.Label, field.Value);
                    continue;
                }

                GameObject row = createFieldRow($"FieldRow_{_dynamicFieldRows.Count}", field.Label, field.Value);
                row.SetActive(true);
                _dynamicFieldRows.Add(row);
            }

            _rowRoot.gameObject.SetActive(hasVisibleFixedRow() || _dynamicFieldRows.Count > 0);
        }

        // 隐藏固定字段行，等待本次绑定重新填值
        private void hideFixedFieldRows()
        {
            foreach (TooltipFieldRow row in _fixedFieldRows.Values)
                row.SetVisible(false);
        }

        // 判断是否存在已显示的固定字段行
        private bool hasVisibleFixedRow()
        {
            foreach (TooltipFieldRow row in _fixedFieldRows.Values)
                if (row.Root != null && row.Root.activeSelf)
                    return true;

            return false;
        }

        // 绑定可选文本块
        private static void bindBlock(RectTransform block, TextMeshProUGUI text, string value)
        {
            bool visible = !string.IsNullOrWhiteSpace(value);
            block.gameObject.SetActive(visible);
            if (visible)
                text.text = value;
        }

        // 创建标签文本
        private TextMeshProUGUI createTagText(string tag)
        {
            GameObject tagObj = _tagTemplate != null
                ? Instantiate(_tagTemplate, _tagRoot, false)
                : getOrCreateRect($"Tag_{_tagTexts.Count}", _tagRoot).gameObject;
            tagObj.name = $"Tag_{_tagTexts.Count}";
            tagObj.SetActive(true);

            RectTransform tagRoot = tagObj.GetComponent<RectTransform>();
            Image image = tagRoot.GetComponent<Image>();
            if (image == null)
                image = tagRoot.gameObject.AddComponent<Image>();

            image.color = new Color(0.67f, 0.43f, 0.26f, 0.85f);
            image.raycastTarget = false;
            setLayout(tagRoot.gameObject, -1f, 28f, -1f, 28f);

            TextMeshProUGUI text = getOrCreateText("Txt_Label", tagRoot, 15f, new Color(1f, 0.89f, 0.68f, 1f), TextAlignmentOptions.Center);
            text.text = tag;
            text.enableWordWrapping = false;
            setPadding(text.rectTransform, 10f, 2f);
            return text;
        }

        // 创建字段行
        private GameObject createFieldRow(string rowName, string label, string value)
        {
            RectTransform row = getOrCreateRect(rowName, _rowRoot);
            setupHorizontalLayout(row.gameObject, FIELD_ROW_SPACING, TextAnchor.UpperLeft);
            removeFitter(row.gameObject);
            setLayout(row.gameObject, getContentWidth(), 24f, getContentWidth(), 24f);

            TextMeshProUGUI labelText = getOrCreateText("Txt_Label", row, 15f, new Color(0.74f, 0.62f, 0.48f, 1f), TextAlignmentOptions.TopLeft);
            labelText.text = label;
            setLayout(labelText.gameObject, RowLabelWidth, 24f, RowLabelWidth, -1f);

            TextMeshProUGUI valueText = getOrCreateText("Txt_Value", row, 16f, new Color(0.94f, 0.86f, 0.73f, 1f), TextAlignmentOptions.TopLeft);
            valueText.text = value;
            setPreferredFlexibleLayout(valueText.gameObject, getFieldValueWidth(), 24f, getFieldValueWidth(), -1f);
            return row.gameObject;
        }

        // 清理动态内容
        private void clearDynamicContent()
        {
            for (int i = 0; i < _tagTexts.Count; i++)
                if (_tagTexts[i] != null)
                    destroyDynamicObject(_tagTexts[i].transform.parent != null ? _tagTexts[i].transform.parent.gameObject : _tagTexts[i].gameObject);

            _tagTexts.Clear();

            for (int i = 0; i < _dynamicFieldRows.Count; i++)
                if (_dynamicFieldRows[i] != null)
                    destroyDynamicObject(_dynamicFieldRows[i]);

            _dynamicFieldRows.Clear();
            clearPreviewTags();
            clearPreviewDynamicRows();
        }

        // 清理预制体中用于预览的标签节点
        private void clearPreviewTags()
        {
            if (_tagRoot == null) return;

            for (int i = _tagRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _tagRoot.GetChild(i);
                if (child.name.StartsWith("Tag_") && child.name != "Tag_Template")
                    destroyDynamicObject(child.gameObject);
            }
        }

        // 清理预制体中可能残留的动态字段行
        private void clearPreviewDynamicRows()
        {
            if (_rowRoot == null) return;

            for (int i = _rowRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _rowRoot.GetChild(i);
                if (child.name.StartsWith("FieldRow_") && child.name != "FieldRow_Template")
                    destroyDynamicObject(child.gameObject);
            }
        }

        // 根据动态内容刷新高度
        private void clampHeight()
        {
            float preferredHeight = LayoutUtility.GetPreferredHeight(_contentRoot);
            float contentHeight = Mathf.Max(160f, preferredHeight + CONTENT_PADDING_Y * 2f);
            _rectTransform.sizeDelta = new Vector2(TooltipWidth, contentHeight);
            _contentRoot.sizeDelta = new Vector2(getContentWidth(), preferredHeight);
        }

        // 刷新动态文本块与字段行尺寸
        private void refreshDynamicLayoutSizes()
        {
            refreshFieldRowSizes();
            refreshTextBlockSize(_descBlock, _txtDesc);
        }

        // 刷新字段行高度，避免多行字段压住下一行
        private void refreshFieldRowSizes()
        {
            foreach (TooltipFieldRow row in _fixedFieldRows.Values)
                refreshFieldRowSize(row.Root, row.LabelText, row.ValueText);

            for (int i = 0; i < _dynamicFieldRows.Count; i++)
                if (_dynamicFieldRows[i] != null)
                    refreshFieldRowSize(_dynamicFieldRows[i],
                        _dynamicFieldRows[i].transform.Find("Txt_Label")?.GetComponent<TextMeshProUGUI>(),
                        _dynamicFieldRows[i].transform.Find("Txt_Value")?.GetComponent<TextMeshProUGUI>());
        }

        // 刷新单个字段行高度
        private void refreshFieldRowSize(GameObject row, TextMeshProUGUI labelText, TextMeshProUGUI valueText)
        {
            if (row == null || !row.activeSelf || labelText == null || valueText == null) return;

            float labelHeight = getPreferredTextHeight(labelText, RowLabelWidth);
            float valueHeight = getPreferredTextHeight(valueText, getFieldValueWidth());
            float rowHeight = Mathf.Ceil(Mathf.Max(24f, labelHeight, valueHeight));
            setLayout(row, getContentWidth(), rowHeight, getContentWidth(), rowHeight);
            setLayout(labelText.gameObject, RowLabelWidth, rowHeight, RowLabelWidth, rowHeight);
            setPreferredFlexibleLayout(valueText.gameObject, getFieldValueWidth(), rowHeight, getFieldValueWidth(), rowHeight);
        }

        // 刷新大段文本块高度
        private void refreshTextBlockSize(RectTransform block, TextMeshProUGUI text)
        {
            if (block == null || text == null || !block.gameObject.activeSelf) return;

            float textWidth = getTextBlockTextWidth();
            float textHeight = getPreferredTextHeight(text, textWidth);
            float blockHeight = Mathf.Ceil(Mathf.Max(28f, textHeight + TEXT_BLOCK_PADDING_Y * 2f + 2f));
            setLayout(block.gameObject, getContentWidth(), blockHeight, getContentWidth(), blockHeight);
            setRectSize(block, getContentWidth(), blockHeight);
            setupTextBlockTextRect(text.rectTransform, textWidth, Mathf.Max(1f, blockHeight - TEXT_BLOCK_PADDING_Y * 2f));
        }

        // 获取字段值文本宽度
        private float getFieldValueWidth()
        {
            return Mathf.Max(1f, getContentWidth() - RowLabelWidth - FIELD_ROW_SPACING);
        }

        // 获取文本块内部文字宽度
        private float getTextBlockTextWidth()
        {
            return Mathf.Max(1f, getContentWidth() - TEXT_BLOCK_PADDING_X * 2f);
        }

        // 获取 TMP 文本在指定宽度下的首选高度
        private static float getPreferredTextHeight(TextMeshProUGUI text, float width)
        {
            if (text == null || string.IsNullOrWhiteSpace(text.text))
                return 0f;

            text.enableAutoSizing = false;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.ForceMeshUpdate();
            Vector2 preferredValues = text.GetPreferredValues(text.text, width, Mathf.Infinity);
            return Mathf.Max(preferredValues.y, getEstimatedWrappedTextHeight(text, width));
        }

        // 估算中文长句换行高度，兜住 TMP 字体 fallback 导致的首选高度偏小
        private static float getEstimatedWrappedTextHeight(TextMeshProUGUI text, float width)
        {
            string value = text.text;
            if (string.IsNullOrWhiteSpace(value))
                return 0f;

            float lineHeight = Mathf.Max(1f, text.fontSize * 1.2f);
            float fullWidthCharCount = Mathf.Max(1f, width / Mathf.Max(1f, text.fontSize));
            string[] lines = value.Split('\n');
            int lineCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                float weightedLength = getWeightedTextLength(lines[i]);
                lineCount += Mathf.Max(1, Mathf.CeilToInt(weightedLength / fullWidthCharCount));
            }

            return lineHeight * lineCount;
        }

        // 统计文本显示宽度权重，中文按全角，英文数字按半角估算
        private static float getWeightedTextLength(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0f;

            float length = 0f;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsWhiteSpace(c))
                {
                    length += 0.35f;
                    continue;
                }

                length += c <= 127 ? 0.6f : 1f;
            }

            return length;
        }

        // 移除动态节点并从布局树摘除，避免同一帧重复绑定时旧节点影响布局
        private static void destroyDynamicObject(GameObject target)
        {
            if (target == null) return;

            target.transform.SetParent(null, false);
            Destroy(target);
        }

        // 将当前字体应用到所有已存在文本
        private void applyFontAsset()
        {
            if (_fontAsset == null) return;

            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
                texts[i].font = _fontAsset;
        }

        // 获取或创建 RectTransform 节点
        private static RectTransform getOrCreateRect(string childName, Transform parent)
        {
            Transform child = parent.Find(childName);
            if (child == null)
            {
                GameObject childObj = new GameObject(childName, typeof(RectTransform));
                childObj.transform.SetParent(parent, false);
                child = childObj.transform;
            }

            RectTransform rectTransform = child.GetComponent<RectTransform>();
            if (rectTransform == null)
                rectTransform = child.gameObject.AddComponent<RectTransform>();

            return rectTransform;
        }

        // 获取或创建 Image 节点
        private static Image getOrCreateImage(string childName, Transform parent, Color color)
        {
            RectTransform rectTransform = getOrCreateRect(childName, parent);
            Image image = rectTransform.GetComponent<Image>();
            if (image == null)
                image = rectTransform.gameObject.AddComponent<Image>();

            image.color = color;
            return image;
        }

        // 获取或创建文本节点
        private TextMeshProUGUI getOrCreateText(string childName, Transform parent, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            RectTransform rectTransform = getOrCreateRect(childName, parent);
            TextMeshProUGUI text = rectTransform.GetComponent<TextMeshProUGUI>();
            if (text == null)
                text = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();

            if (_fontAsset != null)
                text.font = _fontAsset;

            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.enableAutoSizing = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        // 设置左上角固定布局
        private static void setupTopLeft(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }

        // 设置文本内边距
        private static void setPadding(RectTransform rectTransform, float horizontal, float vertical)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(horizontal, vertical);
            rectTransform.offsetMax = new Vector2(-horizontal, -vertical);
        }

        // 固定说明块文本区域，避免 Stretch 子节点被父级高度压缩
        private static void setupTextBlockTextRect(RectTransform rectTransform, float width, float height)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(TEXT_BLOCK_PADDING_X, -TEXT_BLOCK_PADDING_Y);
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        // 立即同步 RectTransform 尺寸，避免等待布局系统刷新时出现单行高度底图
        private static void setRectSize(RectTransform rectTransform, float width, float height)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        // 设置垂直布局
        private static VerticalLayoutGroup setupVerticalLayout(GameObject target, float spacing, TextAnchor childAlignment)
        {
            VerticalLayoutGroup layout = target.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = target.AddComponent<VerticalLayoutGroup>();

            layout.childAlignment = childAlignment;
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        // 设置水平布局
        private static HorizontalLayoutGroup setupHorizontalLayout(GameObject target, float spacing, TextAnchor childAlignment)
        {
            HorizontalLayoutGroup layout = target.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
                layout = target.AddComponent<HorizontalLayoutGroup>();

            layout.childAlignment = childAlignment;
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        // 确保自适应尺寸组件
        private static ContentSizeFitter ensureFitter(GameObject target)
        {
            ContentSizeFitter fitter = target.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = target.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return fitter;
        }

        // 移除受父级 LayoutGroup 管理的自适应尺寸组件
        private static void removeFitter(GameObject target)
        {
            ContentSizeFitter fitter = target.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                fitter.enabled = false;
                Destroy(fitter);
            }
        }

        // 移除文本块内部的垂直布局，避免文本高度被压缩
        private static void removeVerticalLayout(GameObject target)
        {
            VerticalLayoutGroup layout = target.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.enabled = false;
                Destroy(layout);
            }
        }

        // 设置固定布局尺寸
        private static void setLayout(GameObject target, float minWidth, float minHeight, float preferredWidth, float preferredHeight)
        {
            LayoutElement layout = target.GetComponent<LayoutElement>();
            if (layout == null)
                layout = target.AddComponent<LayoutElement>();

            layout.minWidth = minWidth;
            layout.minHeight = minHeight;
            layout.preferredWidth = preferredWidth;
            layout.preferredHeight = preferredHeight;
        }

        // 设置弹性布局
        private static void setFlexibleLayout(GameObject target)
        {
            LayoutElement layout = target.GetComponent<LayoutElement>();
            if (layout == null)
                layout = target.AddComponent<LayoutElement>();

            layout.flexibleWidth = 1f;
        }

        // 设置带宽高约束的弹性布局
        private static void setPreferredFlexibleLayout(GameObject target, float minWidth, float minHeight, float preferredWidth, float preferredHeight)
        {
            setLayout(target, minWidth, minHeight, preferredWidth, preferredHeight);
            LayoutElement layout = target.GetComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
        }

        // 解析画布相机
        private Camera resolveCanvasCamera(RectTransform canvasRect)
        {
            Canvas canvas = canvasRect.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        // 固定字段行引用
        private class TooltipFieldRow
        {
            public readonly GameObject Root;
            public readonly TextMeshProUGUI LabelText;
            public readonly TextMeshProUGUI ValueText;

            public TooltipFieldRow(GameObject root, TextMeshProUGUI labelText, TextMeshProUGUI valueText)
            {
                Root = root;
                LabelText = labelText;
                ValueText = valueText;
            }

            // 更新字段行文本
            public void Bind(string label, string value)
            {
                if (LabelText != null)
                    LabelText.text = label;

                if (ValueText != null)
                    ValueText.text = value;

                SetVisible(true);
            }

            // 设置字段行显示状态
            public void SetVisible(bool isVisible)
            {
                if (Root != null)
                    Root.SetActive(isVisible);
            }
        }
    }

}
