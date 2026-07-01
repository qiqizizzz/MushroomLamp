/*
* ┌──────────────────────────────────┐
* │  描    述: 商店子页面视图
* │           左上：返回按钮  左下：信息文本  右上：金币
* │           中间：三个定位点（购买卡牌）  底部：背包横向滚动列表
* │  类    名: StoreView.cs
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Common;
using Common.Defines;
using Common.UI;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Store
{
    public class StoreView : BaseView
    {
        private Button _btnBack;
        private TextMeshProUGUI _txtInfo;
        private TextMeshProUGUI _txtGold;

        // 运行时生成白膜文本时复用的中文字体（取自预制体已带字体的文本）
        private TMP_FontAsset _fontTemplate;

        // 中间三个购买卡牌定位点
        private readonly List<Transform> _buyAnchors = new();
        private readonly List<StoreBuyCard> _buyCards = new();

        // 底部背包：水平循环复用列表
        private const int BagRows = 1;                 // 背包为单行
        private const int BagPoolColumns = 8;          // 池中常驻列数（可视列数 + 缓冲，足够覆盖横向可视区）
        private static readonly Vector2 BagCellSize = new Vector2(140f, 180f);
        private static readonly Vector2 BagSpacing = new Vector2(20f, 0f);

        private ScrollRect _bagScroll;
        private LoopGridView _bagGrid;
        private GameObject _bagItemPrefab;
        private StoreModel _model;

        // 单张购买卡牌的运行时绑定（卡面 + 名称 + 价格 + 整卡按钮）
        private class StoreBuyCard
        {
            public GameObject root;
            public Image icon;
            public TextMeshProUGUI name;
            public TextMeshProUGUI price;
            public Button button;
        }

        public override void InitUI()
        {
            _btnBack = Find<Button>("Btn_Back");
            _txtInfo = Find<TextMeshProUGUI>("InfoPanel/Txt_Info");
            _txtGold = Find<TextMeshProUGUI>("TopGold/Txt_GoldValue");

            collectBuyAnchors();
            collectBagScroll();

            // 记录预制体里中文文本的字体，供运行时白膜文本复用
            if (_txtInfo != null) _fontTemplate = _txtInfo.font;
            else if (_txtGold != null) _fontTemplate = _txtGold.font;
        }

        public override void InitData()
        {
            base.InitData();
            bindButton(_btnBack, () => ApplyFunc(EventDefines.StoreReturn));
        }

        public void Refresh(StoreModel model)
        {
            if (model == null) return;
            _model = model;

            if (_txtGold != null) _txtGold.text = model.Gold.ToString();
            if (_txtInfo != null) _txtInfo.text = buildInfoText(model);

            refreshBuyCards(model);
            refreshBag(model);
        }

        // ---------------- 中间购买卡牌 ----------------

        private void collectBuyAnchors()
        {
            _buyAnchors.Clear();
            Transform middle = Find<Transform>("Middle");
            if (middle == null) return;

            // Middle 下每个子节点即一个定位点（设计图为 3 个）
            foreach (Transform anchor in middle)
                _buyAnchors.Add(anchor);
        }

        private void refreshBuyCards(StoreModel model)
        {
            ensureBuyCards();

            for (int i = 0; i < _buyCards.Count; i++)
            {
                StoreBuyCard card = _buyCards[i];
                bool valid = model.BuySlots != null && i < model.BuySlots.Count;

                if (card.root != null) card.root.SetActive(valid);
                if (!valid) continue;

                StoreBuySlotData slot = model.BuySlots[i];

                if (card.name != null) card.name.text = slot.name;
                if (card.price != null)
                    card.price.text = slot.price <= 0 && model.CardsIncludedInBoxPrice ? "免费" : slot.price.ToString();

                if (card.icon != null)
                {
                    Sprite sprite = string.IsNullOrEmpty(slot.iconPath) ? null : ArtAssetLoader.LoadSprite(slot.iconPath);
                    card.icon.sprite = sprite;
                    card.icon.enabled = true; // 无资源时保留白膜
                }

                if (card.button != null)
                {
                    card.button.onClick.RemoveAllListeners();
                    bool canPick = !slot.isPurchased
                        && !(model.CardsIncludedInBoxPrice && model.HasBoxPickCompleted());
                    card.button.interactable = canPick;
                    StoreBuySlotData captured = slot;
                    card.button.onClick.AddListener(() => ApplyFunc(EventDefines.StoreBuy, captured));
                }
            }
        }

        // 在每个定位点下挂一张购买卡牌（无美术资源时用白膜占位）
        private void ensureBuyCards()
        {
            if (_buyCards.Count == _buyAnchors.Count) return;
            _buyCards.Clear();

            foreach (Transform anchor in _buyAnchors)
            {
                StoreBuyCard card = anchor.childCount > 0
                    ? bindExistingCard(anchor.GetChild(0).gameObject)
                    : buildPlaceholderCard(anchor);
                _buyCards.Add(card);
            }
        }

        private static StoreBuyCard bindExistingCard(GameObject root)
        {
            return new StoreBuyCard
            {
                root = root,
                icon = findImage(root.transform, "Img_Icon"),
                name = findText(root.transform, "Txt_Name"),
                price = findText(root.transform, "Txt_Price"),
                button = root.GetComponent<Button>()
            };
        }

        // 运行时生成的白膜卡牌（设计期无预制体时兜底）
        private StoreBuyCard buildPlaceholderCard(Transform anchor)
        {
            GameObject root = new GameObject("BuyCard", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.SetParent(anchor, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bg = root.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.92f);

            Image icon = createChildImage(rt, "Img_Icon", new Color(0.85f, 0.85f, 0.85f, 1f),
                new Vector2(0.15f, 0.32f), new Vector2(0.85f, 0.9f));

            TextMeshProUGUI name = createChildText(rt, "Txt_Name", new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.3f), 26, "卡牌");
            TextMeshProUGUI price = createChildText(rt, "Txt_Price", new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.16f), 30, "0");

            return new StoreBuyCard
            {
                root = root,
                icon = icon,
                name = name,
                price = price,
                button = root.GetComponent<Button>()
            };
        }

        // ---------------- 底部背包：水平循环复用 ----------------

        private void collectBagScroll()
        {
            _bagScroll = Find<ScrollRect>("BagScrollView");
            _bagItemPrefab = ResManager.LoadAsset<GameObject>(AddressDefines.UI_StoreBagItem);

            if (_bagScroll == null) return;

            // 水平滚动配置：屏蔽垂直，避免内容被纵向拖动
            _bagScroll.horizontal = true;
            _bagScroll.vertical = false;

            // 清理设计期可能残留的布局组件（与 LoopGridView 的手动定位冲突）
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
            _bagGrid.SetTotalCount(model.BagEntries?.Count ?? 0);
        }

        // LoopGridView 回调：把第 dataIndex 条背包数据填到 slot 上
        private void onUpdateBagSlot(int dataIndex, GameObject slot)
        {
            if (_model == null || _model.BagEntries == null) return;
            if (dataIndex < 0 || dataIndex >= _model.BagEntries.Count) return;

            StoreBagItem item = slot.GetComponent<StoreBagItem>();
            if (item == null) item = slot.AddComponent<StoreBagItem>();
            item.Bind(_model.BagEntries[dataIndex]);
        }

        // 移除 Content 上设计期布局组件（HorizontalLayoutGroup / ContentSizeFitter 等）
        private static void stripLayoutComponents(RectTransform content)
        {
            if (content == null) return;
            var layout = content.GetComponent<LayoutGroup>();
            if (layout != null) Destroy(layout);
            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter != null) Destroy(fitter);
        }

        // 设计期无预制体时，构建一个白膜背包格子模板供 LoopGridView 复制（模板本身不参与显示）
        private GameObject buildBagSlotTemplate()
        {
            GameObject root = new GameObject("StoreBagItemTemplate", typeof(RectTransform), typeof(Image), typeof(StoreBagItem));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.SetParent(transform, false);   // 挂在 View 根下，不放进 Content，避免被当成容器
            rt.sizeDelta = BagCellSize;

            root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.92f);

            createChildImage(rt, "Img_Icon", new Color(0.85f, 0.85f, 0.85f, 1f),
                new Vector2(0.1f, 0.22f), new Vector2(0.9f, 0.95f));
            createChildText(rt, "Txt_Name", new Vector2(0.02f, 0.0f), new Vector2(0.98f, 0.2f), 22, "");

            TextMeshProUGUI countTxt = createChildText(rt, "Txt_Count", new Vector2(0.5f, 0.02f), new Vector2(0.98f, 0.28f), 24, "x0");
            countTxt.alignment = TextAlignmentOptions.BottomRight;
            countTxt.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            root.SetActive(false);   // 模板隐藏，仅作为 Instantiate 源
            return root;
        }

        // ---------------- 信息文本 ----------------

        private static string buildInfoText(StoreModel model)
        {
            int bagKinds = model.BagEntries?.Count ?? 0;
            string boxLine = string.IsNullOrEmpty(model.CurrentBoxName)
                ? "材料箱：—"
                : $"材料箱：{model.CurrentBoxName}";

            string pickHint = model.CardsIncludedInBoxPrice
                ? "三选一：点击一张卡牌加入牌组（选完自动返回夜市）"
                : "点击中间卡牌购买";

            return "选卡界面\n" +
                   "————————\n" +
                   $"{boxLine}\n" +
                   $"当前金币：{model.Gold}\n" +
                   $"可选卡牌：{model.BuySlots.Count}\n" +
                   $"背包种类：{bagKinds}\n\n" +
                   pickHint + "\n滑动下方查看背包";
        }

        // ---------------- 小工具 ----------------

        private static void bindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static Image findImage(Transform root, string path)
        {
            Transform t = root.Find(path);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private static TextMeshProUGUI findText(Transform root, string path)
        {
            Transform t = root.Find(path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
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
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
            if (_fontTemplate != null) txt.font = _fontTemplate;
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = new Color(0.2f, 0.15f, 0.1f, 1f);
            txt.alignment = TextAlignmentOptions.Center;
            txt.raycastTarget = false;
            return txt;
        }
    }
}
