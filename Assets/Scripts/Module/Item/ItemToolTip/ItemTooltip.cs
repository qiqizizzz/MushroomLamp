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
        [SerializeField] private float RowLabelWidth = 88f;

        private readonly List<TextMeshProUGUI> _tagTexts = new();
        private readonly List<GameObject> _fieldRows = new();

        private RectTransform _rectTransform;
        private RectTransform _contentRoot;
        private RectTransform _headerRoot;
        private RectTransform _titleRoot;
        private RectTransform _tagRoot;
        private RectTransform _rowRoot;
        private RectTransform _descBlock;
        private RectTransform _processBlock;
        private RectTransform _effectBlock;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private Image _imgBackground;
        private Image _imgIcon;
        private TextMeshProUGUI _txtName;
        private TextMeshProUGUI _txtSubtitle;
        private TextMeshProUGUI _txtPrice;
        private TextMeshProUGUI _txtDesc;
        private TextMeshProUGUI _txtProcess;
        private TextMeshProUGUI _txtEffect;
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
            bindBlock(_processBlock, _txtProcess, data.ProcessText);
            bindBlock(_effectBlock, _txtEffect, data.EffectText);
            SetVisible(true);
            Canvas.ForceUpdateCanvases();
            refreshDynamicLayoutSizes();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rowRoot);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
            clampHeight();
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
        }

        // 创建字段行容器
        private void createRowRoot()
        {
            _rowRoot = getOrCreateRect("RowRoot", _contentRoot);
            setupVerticalLayout(_rowRoot.gameObject, 4f, TextAnchor.UpperLeft);
            ensureFitter(_rowRoot.gameObject);
            _fieldRowTemplate = createFieldRow("FieldRow_Template", string.Empty, string.Empty);
            _fieldRowTemplate.SetActive(false);
        }

        // 创建描述、加工和效果文本块
        private void createTextBlocks()
        {
            _descBlock = createTextBlock("DescBlock", "Txt_Desc", out _txtDesc);
            _processBlock = createTextBlock("ProcessBlock", "Txt_Process", out _txtProcess);
            _effectBlock = createTextBlock("EffectBlock", "Txt_Effect", out _txtEffect);
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
            if (image == null)
                image = block.gameObject.AddComponent<Image>();

            image.color = new Color(0.95f, 0.78f, 0.52f, 0.14f);
            image.raycastTarget = false;
            VerticalLayoutGroup layout = setupVerticalLayout(block.gameObject, 0f, TextAnchor.UpperLeft);
            layout.padding = new RectOffset(
                Mathf.RoundToInt(TEXT_BLOCK_PADDING_X),
                Mathf.RoundToInt(TEXT_BLOCK_PADDING_X),
                Mathf.RoundToInt(TEXT_BLOCK_PADDING_Y),
                Mathf.RoundToInt(TEXT_BLOCK_PADDING_Y));
            removeFitter(block.gameObject);
            setLayout(block.gameObject, getContentWidth(), 32f, getContentWidth(), 32f);

            text = getOrCreateText(textName, block, 16f, new Color(0.94f, 0.86f, 0.73f, 1f), TextAlignmentOptions.Left);
            setupTopLeft(text.rectTransform, Vector2.zero, new Vector2(getTextBlockTextWidth(), 0f));
            setPreferredFlexibleLayout(text.gameObject, getTextBlockTextWidth(), 0f, getTextBlockTextWidth(), -1f);
            return block;
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
            for (int i = 0; fields != null && i < fields.Count; i++)
            {
                ItemTooltipFieldData field = fields[i];
                if (field == null || string.IsNullOrWhiteSpace(field.Value)) continue;

                GameObject row = createFieldRow($"FieldRow_{_fieldRows.Count}", field.Label, field.Value);
                row.SetActive(true);
                _fieldRows.Add(row);
            }

            _rowRoot.gameObject.SetActive(_fieldRows.Count > 0);
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
            RectTransform tagRoot = getOrCreateRect($"Tag_{_tagTexts.Count}", _tagRoot);
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

            TextMeshProUGUI labelText = getOrCreateText("Txt_Label", row, 15f, new Color(0.74f, 0.62f, 0.48f, 1f), TextAlignmentOptions.Left);
            labelText.text = label;
            setLayout(labelText.gameObject, RowLabelWidth, 24f, RowLabelWidth, -1f);

            TextMeshProUGUI valueText = getOrCreateText("Txt_Value", row, 16f, new Color(0.94f, 0.86f, 0.73f, 1f), TextAlignmentOptions.Left);
            valueText.text = value;
            setupTopLeft(valueText.rectTransform, Vector2.zero, new Vector2(getFieldValueWidth(), 0f));
            setPreferredFlexibleLayout(valueText.gameObject, getFieldValueWidth(), 24f, getFieldValueWidth(), -1f);
            return row.gameObject;
        }

        // 清理动态内容
        private void clearDynamicContent()
        {
            for (int i = 0; i < _tagTexts.Count; i++)
                if (_tagTexts[i] != null)
                    Destroy(_tagTexts[i].transform.parent.gameObject);

            _tagTexts.Clear();

            for (int i = 0; i < _fieldRows.Count; i++)
                if (_fieldRows[i] != null)
                    Destroy(_fieldRows[i]);

            _fieldRows.Clear();
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
            refreshTextBlockSize(_processBlock, _txtProcess);
            refreshTextBlockSize(_effectBlock, _txtEffect);
        }

        // 刷新字段行高度，避免多行字段压住下一行
        private void refreshFieldRowSizes()
        {
            for (int i = 0; i < _fieldRows.Count; i++)
            {
                if (_fieldRows[i] == null) continue;

                TextMeshProUGUI labelText = _fieldRows[i].transform.Find("Txt_Label")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI valueText = _fieldRows[i].transform.Find("Txt_Value")?.GetComponent<TextMeshProUGUI>();
                float labelHeight = getPreferredTextHeight(labelText, RowLabelWidth);
                float valueHeight = getPreferredTextHeight(valueText, getFieldValueWidth());
                float rowHeight = Mathf.Ceil(Mathf.Max(24f, labelHeight, valueHeight));
                setLayout(_fieldRows[i], getContentWidth(), rowHeight, getContentWidth(), rowHeight);
            }
        }

        // 刷新大段文本块高度
        private void refreshTextBlockSize(RectTransform block, TextMeshProUGUI text)
        {
            if (block == null || text == null || !block.gameObject.activeSelf) return;

            float textWidth = getTextBlockTextWidth();
            text.rectTransform.sizeDelta = new Vector2(textWidth, text.rectTransform.sizeDelta.y);
            float textHeight = getPreferredTextHeight(text, textWidth);
            float blockHeight = Mathf.Ceil(Mathf.Max(28f, textHeight + TEXT_BLOCK_PADDING_Y * 2f));
            setLayout(block.gameObject, getContentWidth(), blockHeight, getContentWidth(), blockHeight);
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

            return text.GetPreferredValues(text.text, width, 0f).y;
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
                Destroy(fitter);
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
    }

}
