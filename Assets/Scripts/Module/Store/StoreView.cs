/*

* ┌──────────────────────────────────┐

* │  描    述: 商店子页面视图

* │           中间：三个购买卡牌 + 各卡牌下方材料介绍框

* │           底部：背包横向滚动列表

* │  类    名: StoreView.cs

* └──────────────────────────────────┘

*/



using System.Collections.Generic;

using Common;

using Common.Defines;

using Common.UI;

using Module.Cook;

using Module.Item;

using Module.Material;

using MVC.View;

using TMPro;

using UnityEngine;

using UnityEngine.UI;



namespace Module.Store

{

    public class StoreView : BaseView, IStoreMaterialTooltipHost

    {

        private const string ITEM_TOOLTIP_PATH = "UI/Cook/ItemTooltip";

        private static readonly Vector2 TooltipOffset = new Vector2(18f, -18f);

        private Button _btnBack;

        private TextMeshProUGUI _txtGold;



        private GameObject _buyItemPrefab;

        private readonly List<Transform> _buyAnchors = new();

        private readonly List<StoreBuyItem> _buyItems = new();



        private const int BagRows = 1;

        private const int BagPoolColumns = 8;

        private static readonly Vector2 BagCellSize = new Vector2(140f, 180f);

        private static readonly Vector2 BagSpacing = new Vector2(50f, 0f);



        private ScrollRect _bagScroll;

        private LoopGridView _bagGrid;

        private GameObject _bagItemPrefab;

        private StoreModel _model;

        private ItemTooltip _itemTooltip;

        private object _itemTooltipOwner;

        private RectTransform _tooltipCanvasRect;



        public override void InitUI()

        {

            _btnBack = Find<Button>("Btn_Back");

            _txtGold = Find<TextMeshProUGUI>("TopGold/Txt_GoldValue");



            _buyItemPrefab = ResManager.LoadAsset<GameObject>(AddressDefines.UI_StoreBuyItem);

            collectBuyAnchors();

            ensureBuyItems();

            collectBagScroll();

        }



        public override void InitData()

        {

            base.InitData();

            bindButton(_btnBack, () => ApplyFunc(EventDefines.StoreReturn));

        }



        public void Refresh(StoreModel model)

        {

            if (model == null) return;

            HideMaterialTooltip();

            _model = model;



            if (_txtGold != null) _txtGold.text = model.Gold.ToString();



            refreshBuyCards(model);

            refreshBag(model);

        }

        public override void Open(params object[] args)
        {
            gameObject.SetActive(true);
            HideMaterialTooltip();
        }

        public override void Close(params object[] args)
        {
            disableAllHovers();
            HideMaterialTooltip();
            hideTooltipObject();
            _model = null;
            gameObject.SetActive(false);
        }

        private void disableAllHovers()
        {
            for (int i = 0; i < _buyItems.Count; i++)
            {
                StoreBuyItem item = _buyItems[i];
                item?.Hover?.SetHoverEnabled(false);
            }
        }

        private void hideTooltipObject()
        {
            if (_itemTooltip == null) return;
            _itemTooltip.Hide();
            _itemTooltip.gameObject.SetActive(false);
        }



        private void collectBuyAnchors()

        {

            _buyAnchors.Clear();

            Transform middle = Find<Transform>("Middle");

            if (middle == null) return;



            foreach (Transform child in middle)

            {

                if (child.name.StartsWith("BuyAnchor"))

                    _buyAnchors.Add(child);

            }

        }



        private void ensureBuyItems()

        {

            if (_buyItems.Count == _buyAnchors.Count) return;



            _buyItems.Clear();

            foreach (Transform anchor in _buyAnchors)

            {

                clearChildren(anchor);



                if (_buyItemPrefab == null)

                {

                    _buyItems.Add(null);

                    continue;

                }



                GameObject go = Instantiate(_buyItemPrefab, anchor);

                go.name = "StoreBuyItem";



                RectTransform rt = go.GetComponent<RectTransform>();

                rt.anchorMin = Vector2.zero;

                rt.anchorMax = Vector2.one;

                rt.offsetMin = Vector2.zero;

                rt.offsetMax = Vector2.zero;



                StoreBuyItem item = go.GetComponent<StoreBuyItem>();

                _buyItems.Add(item);

            }

        }



        private void refreshBuyCards(StoreModel model)

        {

            ensureBuyItems();



            for (int i = 0; i < _buyItems.Count; i++)

            {

                StoreBuyItem item = _buyItems[i];

                bool valid = item != null && model.BuySlots != null && i < model.BuySlots.Count;



                if (item != null) item.gameObject.SetActive(valid);

                if (!valid) continue;



                StoreBuySlotData slot = model.BuySlots[i];
                item.Bind(slot);

                bool canPick = !slot.isPurchased
                    && !(model.CardsIncludedInBoxPrice && model.HasBoxPickCompleted());

                if (item.Button != null)
                {
                    item.Button.onClick.RemoveAllListeners();
                    item.Button.interactable = canPick;
                    StoreBuySlotData captured = slot;
                    item.Button.onClick.AddListener(() => ApplyFunc(EventDefines.StoreBuy, captured));
                }

                setupBuyCardHover(item, slot, canPick);

            }

        }



        private void setupBuyCardHover(StoreBuyItem item, StoreBuySlotData slot, bool canPick)

        {

            StoreBuyHoverItem hover = item.Hover;

            if (hover == null) return;

            hover.Setup(this, item.Icon != null ? item.Icon.rectTransform : null, slot?.id);

            hover.SetHoverEnabled(true);

        }



        private void collectBagScroll()

        {

            _bagScroll = Find<ScrollRect>("BagScrollView");

            _bagItemPrefab = ResManager.LoadAsset<GameObject>(AddressDefines.UI_StoreBagItem);

            if (_bagScroll == null) return;



            _bagScroll.horizontal = true;

            _bagScroll.vertical = false;



            stripLayoutComponents(_bagScroll.content);



            GameObject slotPrefab = _bagItemPrefab != null ? _bagItemPrefab : buildBagSlotTemplate();



            _bagGrid = _bagScroll.gameObject.GetComponent<LoopGridView>();

            if (_bagGrid == null) _bagGrid = _bagScroll.gameObject.AddComponent<LoopGridView>();



            var padding = new RectOffset(20, 20, 20, 20);

            _bagGrid.InitHorizontal(_bagScroll, slotPrefab, BagRows, BagPoolColumns,

                BagCellSize, BagSpacing, onUpdateBagSlot, padding);

        }



        private void refreshBag(StoreModel model)

        {

            if (_bagGrid == null) return;

            int count = model.BagEntries?.Count ?? 0;

            _bagGrid.SetTotalCount(count);

            if (count > 0)

                _bagGrid.Refresh();

        }



        private void onUpdateBagSlot(int dataIndex, GameObject slot)

        {

            if (_model == null || _model.BagEntries == null) return;

            if (dataIndex < 0 || dataIndex >= _model.BagEntries.Count) return;



            StoreBagItem item = slot.GetComponent<StoreBagItem>();

            if (item == null) item = slot.AddComponent<StoreBagItem>();

            item.Bind(_model.BagEntries[dataIndex], this);

        }



        public void ShowMaterialTooltip(object owner, string materialId, Vector2 screenPosition)

        {

            if (!isActiveAndEnabled) return;

            if (string.IsNullOrWhiteSpace(materialId)) return;

            MaterialJsonData config = MaterialCatalogLoader.GetById(materialId);

            if (config == null || !ensureItemTooltip()) return;

            Sprite icon = ArtAssetLoader.LoadSprite(config.iconPath, logOnFail: false);

            CookMaterialData preview = new CookMaterialData(0, config, icon);

            _itemTooltipOwner = owner;

            _itemTooltip.gameObject.SetActive(true);

            _itemTooltip.transform.SetAsLastSibling();

            _itemTooltip.Bind(preview, ItemTooltipMode.Cook);

            MoveMaterialTooltip(screenPosition);

        }



        public void MoveMaterialTooltip(Vector2 screenPosition)

        {

            if (_itemTooltip == null) return;

            _itemTooltip.SetScreenPosition(screenPosition, _tooltipCanvasRect, TooltipOffset);

        }



        public void HideMaterialTooltip(object owner = null)

        {

            if (owner != null && _itemTooltipOwner != owner) return;

            _itemTooltipOwner = null;

            if (_itemTooltip != null)

                _itemTooltip.Hide();

        }



        protected override void OnDestroy()

        {

            HideMaterialTooltip();

            if (_itemTooltip != null)

            {

                Destroy(_itemTooltip.gameObject);

                _itemTooltip = null;

            }

            base.OnDestroy();

        }



        private bool ensureItemTooltip()

        {

            if (_itemTooltip != null) return true;

            // 挂在 StoreView 下，避免被 sortingOrder=20 的 Store Canvas 挡住（根 Canvas 上的 tooltip 不可见）
            Transform parent = transform;

            GameObject tooltipObj = ResManager.Instantiate(ITEM_TOOLTIP_PATH, parent);

            if (tooltipObj == null) return false;

            _itemTooltip = tooltipObj.GetComponent<ItemTooltip>();

            if (_itemTooltip == null)

                _itemTooltip = tooltipObj.AddComponent<ItemTooltip>();

            tooltipObj.name = "StoreItemTooltip";

            configureItemTooltipCanvas(tooltipObj);

            tooltipObj.transform.SetAsLastSibling();

            _tooltipCanvasRect = parent as RectTransform;

            if (_tooltipCanvasRect == null)

                _tooltipCanvasRect = tooltipObj.GetComponentInParent<Canvas>()?.transform as RectTransform;

            if (_itemTooltip != null)

            {

                if (_txtGold != null)

                    _itemTooltip.SetFontAsset(_txtGold.font);

                _itemTooltip.Hide();

            }

            return _itemTooltip != null;

        }



        private static void configureItemTooltipCanvas(GameObject tooltipObj)

        {

            Canvas canvas = tooltipObj.GetComponent<Canvas>();

            if (canvas == null) return;

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvas.overrideSorting = true;

            canvas.sortingOrder = 100;

        }



        private static void clearChildren(Transform parent)

        {

            if (parent == null) return;

            for (int i = parent.childCount - 1; i >= 0; i--)

                Destroy(parent.GetChild(i).gameObject);

        }



        private static void stripLayoutComponents(RectTransform content)

        {

            if (content == null) return;

            var layout = content.GetComponent<LayoutGroup>();

            if (layout != null) DestroyImmediate(layout);

            var fitter = content.GetComponent<ContentSizeFitter>();

            if (fitter != null) DestroyImmediate(fitter);

        }



        private GameObject buildBagSlotTemplate()

        {

            GameObject root = new GameObject("StoreBagItemTemplate", typeof(RectTransform), typeof(Image), typeof(StoreBagItem));

            RectTransform rt = root.GetComponent<RectTransform>();

            rt.SetParent(transform, false);

            rt.sizeDelta = BagCellSize;



            root.GetComponent<Image>().enabled = false;



            createChildImage(rt, "Img_Icon", Color.white,

                new Vector2(0.1f, 0.26f), new Vector2(0.9f, 0.92f));

            createChildText(rt, "Txt_Name", new Vector2(0.02f, 0.0f), new Vector2(0.98f, 0.2f), 22, "");

            createCountBadge(rt);

            root.SetActive(false);

            return root;

        }



        private void createCountBadge(RectTransform parent)
        {
            GameObject badgeGo = new GameObject("CountBadge", typeof(RectTransform), typeof(Image));
            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.SetParent(parent, false);
            badgeRt.anchorMin = new Vector2(0.62f, 0.58f);
            badgeRt.anchorMax = new Vector2(0.98f, 0.94f);
            badgeRt.offsetMin = Vector2.zero;
            badgeRt.offsetMax = Vector2.zero;

            Image badgeImg = badgeGo.GetComponent<Image>();
            Sprite badgeSprite = ArtAssetLoader.LoadSprite("Art/StoreView/数量小标签", logOnFail: false);
            badgeImg.sprite = badgeSprite;
            badgeImg.preserveAspect = true;
            badgeImg.raycastTarget = false;
            badgeImg.enabled = badgeSprite != null;

            TextMeshProUGUI countTxt = createChildText(badgeRt, "Txt_Count", Vector2.zero, Vector2.one, 22, "0");
            countTxt.alignment = TextAlignmentOptions.Center;
            countTxt.fontStyle = FontStyles.Bold;
            countTxt.color = Color.white;
        }

        private static void bindButton(Button button, UnityEngine.Events.UnityAction action)

        {

            if (button == null || action == null) return;

            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(action);

        }



        private static Image createChildImage(RectTransform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)

        {

            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));

            RectTransform rt = go.GetComponent<RectTransform>();

            rt.SetParent(parent, false);

            rt.anchorMin = anchorMin;

            rt.anchorMax = anchorMax;

            rt.offsetMin = Vector2.zero;

            rt.offsetMax = Vector2.zero;

            Image img = go.GetComponent<Image>();

            img.color = color;

            img.raycastTarget = false;

            return img;

        }



        private TextMeshProUGUI createChildText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float fontSize, string text)

        {

            var fontSource = Find<TextMeshProUGUI>("TopGold/Txt_GoldValue");



            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));

            RectTransform rt = go.GetComponent<RectTransform>();

            rt.SetParent(parent, false);

            rt.anchorMin = anchorMin;

            rt.anchorMax = anchorMax;

            rt.offsetMin = Vector2.zero;

            rt.offsetMax = Vector2.zero;

            TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();

            if (fontSource != null) txt.font = fontSource.font;

            txt.text = text;

            txt.fontSize = fontSize;

            txt.color = new Color(0.2f, 0.15f, 0.1f, 1f);

            txt.alignment = TextAlignmentOptions.Center;

            txt.raycastTarget = false;

            return txt;

        }

    }

}

