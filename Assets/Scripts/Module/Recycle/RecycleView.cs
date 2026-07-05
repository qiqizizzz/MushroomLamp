/*
* ┌──────────────────────────────────┐
* │  描    述: 回收界面视图，负责展示候选材料、右侧清单与确认交互
* │  类    名: RecycleView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Common;
using Common.Defines;
using Common.UI;
using Module.Item;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MVC.View;

namespace Module.Recycle
{
    // 回收界面视图，负责展示候选材料、右侧清单与确认交互
    public class RecycleView : BaseView
    {
        private const string ITEM_TOOLTIP_PATH = "UI/Cook/ItemTooltip";
        private const float OFFER_CARD_WIDTH = 160f;
        private const float OFFER_CARD_HEIGHT = 220f;
        private const float OFFER_ICON_SCALE = 1.28f;
        private const float OFFER_RANDOM_ROTATION = 60f;
        private const float OFFER_MAX_INTERACTION_SCALE = 1.18f;
        private const float OFFER_AREA_PADDING = 8f;
        private const float INVENTORY_ROW_WIDTH = 142f;
        private const float INVENTORY_ROW_HEIGHT = 70f;

        private static readonly Vector2 S_TooltipOffset = new Vector2(18f, -18f);
        private static readonly string[] S_OfferAreaNames =
        {
            "Left",
            "Right",
            "Bottom"
        };
        private struct OfferPlacement
        {
            public RectTransform areaRoot;
            public Vector2 anchorPreset;
        }

        private Button _btnBack;
        private Button _btnConfirm;
        private TextMeshProUGUI _txtGold;
        private TextMeshProUGUI _txtTip;
        private TextMeshProUGUI _txtSelected;
        private Transform _offerRoot;
        private Transform _inventoryRoot;
        private ScrollRect _inventoryScroll;
        private TMP_FontAsset _fontTemplate;
        private ItemTooltip _itemTooltip;
        private object _itemTooltipOwner;
        private RectTransform _tooltipCanvasRect;

        private RecycleModel _model;
        private RecycleOfferData _selectedData;
        private RecycleOfferItem _selectedItem;
        private bool _isSelling;

        private readonly List<RecycleOfferItem> _offerItems = new();

        public override void InitUI()
        {
            _btnBack = Find<Button>("Btn_Back");
            _btnConfirm = findFirst<Button>("Center/RecycleBox/Btn_Confirm", "Center/Btn_Confirm");
            _txtGold = findFirst<TextMeshProUGUI>("TopGold/Txt_GoldValue");
            _txtTip = findFirst<TextMeshProUGUI>("Bottom/Txt_Tip");
            _txtSelected = findFirst<TextMeshProUGUI>("Center/RecycleBox/Txt_Selected", "Center/Txt_Selected");
            _offerRoot = Find<Transform>("Center/OfferRoot");
            _inventoryRoot = Find<Transform>("Right/ScrollView/Viewport/Content");
            _inventoryScroll = findOptional<ScrollRect>("Right/ScrollView");

            if (_txtTip != null) _fontTemplate = _txtTip.font;
            else if (_txtGold != null) _fontTemplate = _txtGold.font;

            TMP_FontAsset chineseFont = UIFontHelper.JingnanFont;
            if (chineseFont != null)
                _fontTemplate = chineseFont;

            applyChineseFont(_txtGold);
            applyChineseFont(_txtTip);
            applyChineseFont(_txtSelected);
        }

        public override void InitData()
        {
            base.InitData();
            bindButton(_btnBack, () => ApplyFunc(EventDefines.RecycleReturn));
            bindButton(_btnConfirm, confirmSell);
        }

        public override void Open(params object[] args)
        {
            _isSelling = false;
            HideRecycleTooltip();
            clearSelection();
        }

        public override void Close(params object[] args)
        {
            HideRecycleTooltip();
            base.Close(args);
        }

        // 销毁界面时同步清理挂在画布下的详情浮层
        protected override void OnDestroy()
        {
            if (_itemTooltip != null)
            {
                Destroy(_itemTooltip.gameObject);
                _itemTooltip = null;
            }

            base.OnDestroy();
        }

        public void Refresh(RecycleModel model)
        {
            if (model == null) return;

            _model = model;
            HideRecycleTooltip();
            if (_txtGold != null) _txtGold.text = model.Gold.ToString();
            if (_txtTip != null) _txtTip.text = "选择一个材料回收，只能卖出本次随机候选中的一个";

            refreshOffers(model.Offers);
            refreshInventory(model.InventoryEntries);
            clearSelection();
        }

        // 刷新中间候选材料
        private void refreshOffers(IReadOnlyList<RecycleOfferData> offers)
        {
            _offerItems.Clear();
            if (_offerRoot == null || offers == null) return;

            disableOfferRootLayout();
            clearGeneratedOfferItems(_offerRoot);
            Dictionary<string, RectTransform> areaRoots = collectOfferAreaRoots();
            if (areaRoots.Count == 0) return;

            refreshOffersInAreas(offers, areaRoots);
        }

        // 按指定区域与九宫格位置摆放候选材料
        private void refreshOffersInAreas(IReadOnlyList<RecycleOfferData> offers, Dictionary<string, RectTransform> areaRoots)
        {
            foreach (RectTransform areaRoot in areaRoots.Values)
                clearChildren(areaRoot);

            List<OfferPlacement> placements = buildOfferPlacements(areaRoots);
            if (placements.Count == 0) return;

            for (int i = 0; i < offers.Count; i++)
            {
                OfferPlacement placement = placements[i % placements.Count];
                RectTransform areaRoot = placement.areaRoot;
                Vector2 itemSize = resolveOfferItemSize(areaRoot.rect);
                float rotation = Random.Range(-OFFER_RANDOM_ROTATION, OFFER_RANDOM_ROTATION);
                Vector2 anchorPreset = placement.anchorPreset;
                Vector2 anchoredPosition = getAnchorInsetPosition(areaRoot.rect, itemSize, rotation, anchorPreset);

                RecycleOfferItem item = createOfferItem(areaRoot, i, anchorPreset, anchoredPosition, itemSize, rotation);
                item.Bind(offers[i], this, selectOffer);
                _offerItems.Add(item);
            }
        }

        // 刷新右侧卡牌与材料清单
        private void refreshInventory(IReadOnlyList<RecycleInventoryEntryData> entries)
        {
            clearChildren(_inventoryRoot);
            if (_inventoryRoot == null || entries == null) return;

            for (int i = 0; i < entries.Count; i++)
                createInventoryRow(_inventoryRoot, entries[i]);

            if (_inventoryRoot is RectTransform contentRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
            if (_inventoryScroll != null)
                _inventoryScroll.verticalNormalizedPosition = 1f;
        }

        // 选中材料候选
        private void selectOffer(RecycleOfferData data, RecycleOfferItem item)
        {
            if (_isSelling) return;

            _selectedData = data;
            _selectedItem = item;

            for (int i = 0; i < _offerItems.Count; i++)
                if (_offerItems[i] != null)
                    _offerItems[i].SetSelected(_offerItems[i] == item);

            if (_txtSelected != null)
                _txtSelected.text = data == null ? "未选择" : $"{data.name}  ￥{data.price}";
            if (_btnConfirm != null)
                _btnConfirm.interactable = data != null;
        }

        // 确认卖出当前选中项
        private void confirmSell()
        {
            if (_isSelling || _selectedData == null || _selectedItem == null) return;

            _isSelling = true;
            RecycleOfferData data = _selectedData;
            _selectedItem.PlaySold(() =>
            {
                _isSelling = false;
                ApplyFunc(EventDefines.RecycleSellSelected, data);
            });
        }

        private void clearSelection()
        {
            _selectedData = null;
            _selectedItem = null;
            for (int i = 0; i < _offerItems.Count; i++)
                if (_offerItems[i] != null)
                    _offerItems[i].SetSelected(false);

            if (_txtSelected != null) _txtSelected.text = "未选择";
            if (_btnConfirm != null) _btnConfirm.interactable = false;
        }

        // 创建候选材料格子，使用与烹饪手牌接近的图标式表现
        private RecycleOfferItem createOfferItem(Transform parent, int index, Vector2 anchorPreset, Vector2 anchoredPosition, Vector2 itemSize, float rotation)
        {
            GameObject root = new GameObject($"Offer_{index + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(RecycleOfferItem));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorPreset;
            rt.anchorMax = anchorPreset;
            rt.pivot = anchorPreset;
            rt.sizeDelta = itemSize;
            rt.anchoredPosition = anchoredPosition;
            rt.localRotation = Quaternion.Euler(0f, 0f, rotation);

            Image bg = root.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0f);
            bg.raycastTarget = true;

            Image icon = createImage(rt, "Img_Icon", Color.white, Vector2.zero, Vector2.one, false);
            if (icon.transform is RectTransform iconRt)
            {
                iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.sizeDelta = itemSize;
                iconRt.anchoredPosition = Vector2.zero;
            }
            icon.preserveAspect = true;

            return root.GetComponent<RecycleOfferItem>();
        }

        // 创建右侧背包和卡组清单行
        private void createInventoryRow(Transform parent, RecycleInventoryEntryData data)
        {
            GameObject root = new GameObject($"Entry_{data.id}", typeof(RectTransform), typeof(LayoutElement));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(INVENTORY_ROW_WIDTH, INVENTORY_ROW_HEIGHT);

            LayoutElement layoutElement = root.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = INVENTORY_ROW_WIDTH;
            layoutElement.preferredHeight = INVENTORY_ROW_HEIGHT;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            Image icon = createImage(rt, "Img_Icon", Color.white, new Vector2(0.04f, 0.22f), new Vector2(0.24f, 0.80f), false);
            if (icon != null)
            {
                Sprite sprite = string.IsNullOrEmpty(data.iconPath) ? null : ArtAssetLoader.LoadSprite(data.iconPath, false);
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.enabled = true;
            }

            createText(rt, "Txt_Name", new Vector2(0.31f, 0.54f), new Vector2(0.74f, 0.88f), 14f, data.name, TextAlignmentOptions.Left);
            createText(rt, "Txt_Count", new Vector2(0.74f, 0.54f), new Vector2(0.94f, 0.88f), 14f, "x" + data.count, TextAlignmentOptions.Right);
            createText(rt, "Txt_Type", new Vector2(0.31f, 0.18f), new Vector2(0.94f, 0.44f), 12f, data.category, TextAlignmentOptions.Left);
        }

        // 显示回收材料详情浮层
        public void ShowRecycleTooltip(object owner, RecycleOfferData data, Vector2 screenPosition)
        {
            if (data == null) return;
            if (!ensureItemTooltip()) return;

            _itemTooltipOwner = owner;
            _itemTooltip.transform.SetAsLastSibling();
            _itemTooltip.Bind(ItemTooltipData.FromRecycleOffer(data));
            MoveRecycleTooltip(screenPosition);
        }

        // 跟随鼠标移动回收材料详情浮层
        public void MoveRecycleTooltip(Vector2 screenPosition)
        {
            if (_itemTooltip == null) return;

            _itemTooltip.SetScreenPosition(screenPosition, _tooltipCanvasRect, S_TooltipOffset);
        }

        // 隐藏回收材料详情浮层；传入 owner 时仅关闭当前来源
        public void HideRecycleTooltip(object owner = null)
        {
            if (owner != null && _itemTooltipOwner != owner)
                return;

            _itemTooltipOwner = null;
            if (_itemTooltip != null)
                _itemTooltip.Hide();
        }

        // 关闭预制体上旧的横向布局，让候选物品可以自由散落
        private void disableOfferRootLayout()
        {
            if (_offerRoot == null) return;

            HorizontalLayoutGroup layoutGroup = _offerRoot.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null)
                layoutGroup.enabled = false;
        }

        // 收集预制体中标记散落范围的区域节点
        private Dictionary<string, RectTransform> collectOfferAreaRoots()
        {
            Dictionary<string, RectTransform> areaRoots = new Dictionary<string, RectTransform>(S_OfferAreaNames.Length);
            if (_offerRoot == null) return areaRoots;

            for (int i = 0; i < S_OfferAreaNames.Length; i++)
            {
                string areaName = S_OfferAreaNames[i];
                RectTransform areaRoot = findOptional<RectTransform>(_offerRoot, areaName);
                if (areaRoot != null)
                    areaRoots[areaName] = areaRoot;
            }

            return areaRoots;
        }

        // 构建固定摆放点：Left 左上/正右，Right 中间，Bottom 最左/最右
        private static List<OfferPlacement> buildOfferPlacements(Dictionary<string, RectTransform> areaRoots)
        {
            List<OfferPlacement> placements = new List<OfferPlacement>(RecycleModel.OfferCount);
            addOfferPlacement(placements, areaRoots, "Left", new Vector2(0f, 1f));
            addOfferPlacement(placements, areaRoots, "Left", new Vector2(1f, 0.5f));
            addOfferPlacement(placements, areaRoots, "Right", new Vector2(0.5f, 0.5f));
            addOfferPlacement(placements, areaRoots, "Bottom", new Vector2(0f, 0.5f));
            addOfferPlacement(placements, areaRoots, "Bottom", new Vector2(1f, 0.5f));
            return placements;
        }

        // 添加一个可用区域中的固定摆放点
        private static void addOfferPlacement(List<OfferPlacement> placements, Dictionary<string, RectTransform> areaRoots, string areaName, Vector2 anchorPreset)
        {
            if (placements == null || areaRoots == null) return;
            if (!areaRoots.TryGetValue(areaName, out RectTransform areaRoot) || areaRoot == null) return;

            placements.Add(new OfferPlacement
            {
                areaRoot = areaRoot,
                anchorPreset = anchorPreset
            });
        }

        // 根据区域大小限制材料尺寸，确保完整显示在区域内
        private static Vector2 resolveOfferItemSize(Rect areaRect)
        {
            Vector2 desiredSize = new Vector2(OFFER_CARD_WIDTH * OFFER_ICON_SCALE, OFFER_CARD_HEIGHT * OFFER_ICON_SCALE);
            Vector2 maxRotatedBounds = getRotatedBounds(desiredSize, OFFER_RANDOM_ROTATION);
            float maxWidth = Mathf.Max(24f, areaRect.width - OFFER_AREA_PADDING * 2f);
            float maxHeight = Mathf.Max(24f, areaRect.height - OFFER_AREA_PADDING * 2f);
            float scale = Mathf.Min(1f, maxWidth / (maxRotatedBounds.x * OFFER_MAX_INTERACTION_SCALE), maxHeight / (maxRotatedBounds.y * OFFER_MAX_INTERACTION_SCALE));
            return desiredSize * scale;
        }

        // 根据锚点和旋转后的四角，把材料向区域内轻推，避免旋转后越界
        private static Vector2 getAnchorInsetPosition(Rect areaRect, Vector2 itemSize, float rotation, Vector2 pivot)
        {
            getRotatedCornerRange(itemSize, rotation, pivot, out Vector2 min, out Vector2 max);
            Vector2 anchorPoint = new Vector2(
                Mathf.Lerp(areaRect.xMin, areaRect.xMax, pivot.x),
                Mathf.Lerp(areaRect.yMin, areaRect.yMax, pivot.y));

            float minOffsetX = areaRect.xMin + OFFER_AREA_PADDING - anchorPoint.x - min.x;
            float maxOffsetX = areaRect.xMax - OFFER_AREA_PADDING - anchorPoint.x - max.x;
            float minOffsetY = areaRect.yMin + OFFER_AREA_PADDING - anchorPoint.y - min.y;
            float maxOffsetY = areaRect.yMax - OFFER_AREA_PADDING - anchorPoint.y - max.y;

            return new Vector2(clampZero(minOffsetX, maxOffsetX), clampZero(minOffsetY, maxOffsetY));
        }

        // 计算绕 pivot 旋转后的四角范围
        private static void getRotatedCornerRange(Vector2 size, float rotation, Vector2 pivot, out Vector2 min, out Vector2 max)
        {
            float radians = rotation * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            Vector2[] corners =
            {
                new Vector2(-pivot.x * size.x, -pivot.y * size.y),
                new Vector2((1f - pivot.x) * size.x, -pivot.y * size.y),
                new Vector2((1f - pivot.x) * size.x, (1f - pivot.y) * size.y),
                new Vector2(-pivot.x * size.x, (1f - pivot.y) * size.y)
            };

            min = new Vector2(float.MaxValue, float.MaxValue);
            max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 rotated = new Vector2(
                    corners[i].x * cos - corners[i].y * sin,
                    corners[i].x * sin + corners[i].y * cos);
                min = Vector2.Min(min, rotated);
                max = Vector2.Max(max, rotated);
            }
        }

        // 优先保持锚点位置，只有越界时才向内修正
        private static float clampZero(float min, float max)
        {
            if (min > max)
                return (min + max) * 0.5f;

            return Mathf.Clamp(0f, min, max);
        }

        // 计算旋转后的视觉包围盒
        private static Vector2 getRotatedBounds(Vector2 size, float rotation)
        {
            float radians = rotation * Mathf.Deg2Rad;
            float sin = Mathf.Abs(Mathf.Sin(radians));
            float cos = Mathf.Abs(Mathf.Cos(radians));
            return new Vector2(size.x * cos + size.y * sin, size.x * sin + size.y * cos);
        }

        private Image createImage(RectTransform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, bool hidden)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            image.enabled = !hidden;
            return image;
        }

        private TextMeshProUGUI createText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float fontSize, string text, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (_fontTemplate != null) tmp.font = _fontTemplate;
            applyChineseFont(tmp);
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = new Color(0.18f, 0.12f, 0.08f, 1f);
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = fontSize;
            tmp.fontSizeMin = Mathf.Max(10f, fontSize - 6f);
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        // 确保回收材料详情浮层已经实例化到场景 Canvas
        private bool ensureItemTooltip()
        {
            if (_itemTooltip != null)
                return true;

            Transform parent = GameApp.ViewManager?.canvasTf ?? transform;
            GameObject tooltipObj = ResManager.Instantiate(ITEM_TOOLTIP_PATH, parent);
            if (tooltipObj == null) return false;

            Canvas viewCanvas = GetComponent<Canvas>();
            Canvas tooltipCanvas = tooltipObj.GetComponent<Canvas>();
            if (tooltipCanvas != null && viewCanvas != null)
            {
                tooltipCanvas.overrideSorting = true;
                tooltipCanvas.sortingOrder = viewCanvas.sortingOrder + 1;
            }

            CanvasGroup tooltipGroup = tooltipObj.GetComponent<CanvasGroup>();
            if (tooltipGroup == null)
                tooltipGroup = tooltipObj.AddComponent<CanvasGroup>();

            tooltipGroup.interactable = false;
            tooltipGroup.blocksRaycasts = false;

            _itemTooltip = tooltipObj.GetComponent<ItemTooltip>();
            if (_itemTooltip == null)
                _itemTooltip = tooltipObj.AddComponent<ItemTooltip>();

            tooltipObj.name = "ItemTooltip";
            tooltipObj.transform.SetAsLastSibling();
            _tooltipCanvasRect = parent as RectTransform;
            if (_tooltipCanvasRect == null)
                _tooltipCanvasRect = tooltipObj.GetComponentInParent<Canvas>()?.transform as RectTransform;

            _itemTooltip.SetFontAsset(_fontTemplate);
            _itemTooltip.Hide();
            return true;
        }

        // 应用项目中文字体，避免运行时创建的 TMP 文本出现方块字
        private void applyChineseFont(TextMeshProUGUI text)
        {
            if (text == null) return;
            UIFontHelper.ApplyChineseFont(text, _fontTemplate);
        }

        private static T findOptional<T>(Transform root, string path) where T : Component
        {
            if (root == null || string.IsNullOrEmpty(path)) return null;
            Transform target = root.Find(path);
            return target != null ? target.GetComponent<T>() : null;
        }

        private T findOptional<T>(string path) where T : Component => findOptional<T>(transform, path);

        private T findFirst<T>(params string[] paths) where T : Component
        {
            if (paths == null || paths.Length == 0) return null;

            for (int i = 0; i < paths.Length; i++)
            {
                T found = findOptional<T>(paths[i]);
                if (found != null) return found;
            }

            QLog.Error($"[{nameof(RecycleView)}] 节点未找到：{string.Join(" 或 ", paths)}");
            return null;
        }

        private static void bindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static void clearChildren(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        // 清理旧逻辑遗留在 OfferRoot 直下的候选材料，不删除 Left/Right/Bottom 区域节点
        private static void clearGeneratedOfferItems(Transform root)
        {
            if (root == null) return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (!child.name.StartsWith("Offer_") && child.GetComponent<RecycleOfferItem>() == null)
                    continue;

                Destroy(child.gameObject);
            }
        }
    }
}
