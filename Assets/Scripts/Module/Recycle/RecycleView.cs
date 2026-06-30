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
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MVC.View;

namespace Module.Recycle
{
    // 回收界面视图，负责展示候选材料、右侧清单与确认交互
    public class RecycleView : BaseView
    {
        private Button _btnBack;
        private Button _btnConfirm;
        private TextMeshProUGUI _txtGold;
        private TextMeshProUGUI _txtTip;
        private TextMeshProUGUI _txtSelected;
        private Transform _offerRoot;
        private Transform _inventoryRoot;
        private TMP_FontAsset _fontTemplate;

        private RecycleModel _model;
        private RecycleOfferData _selectedData;
        private RecycleOfferItem _selectedItem;
        private bool _isSelling;

        private readonly List<RecycleOfferItem> _offerItems = new();

        public override void InitUI()
        {
            _btnBack = Find<Button>("Btn_Back");
            _btnConfirm = Find<Button>("Center/RecycleBox/Btn_Confirm");
            _txtGold = Find<TextMeshProUGUI>("TopGold/Txt_GoldValue");
            _txtTip = Find<TextMeshProUGUI>("Bottom/Txt_Tip");
            _txtSelected = Find<TextMeshProUGUI>("Center/RecycleBox/Txt_Selected");
            _offerRoot = Find<Transform>("Center/OfferRoot");
            _inventoryRoot = Find<Transform>("Right/ScrollView/Viewport/Content");

            if (_txtTip != null) _fontTemplate = _txtTip.font;
            else if (_txtGold != null) _fontTemplate = _txtGold.font;
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
            clearSelection();
        }

        public void Refresh(RecycleModel model)
        {
            if (model == null) return;

            _model = model;
            if (_txtGold != null) _txtGold.text = model.Gold.ToString();
            if (_txtTip != null) _txtTip.text = "选择一个材料回收，只能卖出本次随机候选中的一个";

            refreshOffers(model.Offers);
            refreshInventory(model.InventoryEntries);
            clearSelection();
        }

        // 刷新中间候选材料
        private void refreshOffers(IReadOnlyList<RecycleOfferData> offers)
        {
            clearChildren(_offerRoot);
            _offerItems.Clear();

            if (_offerRoot == null || offers == null) return;

            for (int i = 0; i < offers.Count; i++)
            {
                RecycleOfferItem item = createOfferItem(_offerRoot, i);
                item.Bind(offers[i], selectOffer);
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
                _txtSelected.text = data == null ? "未选择" : $"{data.name}  +{data.price}";
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
            if (_txtSelected != null) _txtSelected.text = "未选择";
            if (_btnConfirm != null) _btnConfirm.interactable = false;
        }

        private RecycleOfferItem createOfferItem(Transform parent, int index)
        {
            GameObject root = new GameObject($"Offer_{index + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(RecycleOfferItem));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(180f, 220f);

            Image bg = root.GetComponent<Image>();
            bg.color = new Color(1f, 0.96f, 0.86f, 0.94f);

            createImage(rt, "Img_Selected", new Color(1f, 0.78f, 0.2f, 0.5f), new Vector2(0f, 0f), new Vector2(1f, 1f), true);
            createImage(rt, "Img_Icon", new Color(1f, 1f, 1f, 1f), new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.95f), false);
            createText(rt, "Txt_Name", new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.22f), 24f, "材料", TextAlignmentOptions.Center);
            createText(rt, "Txt_Price", new Vector2(0.04f, 0.0f), new Vector2(0.96f, 0.11f), 28f, "0", TextAlignmentOptions.Center);

            return root.GetComponent<RecycleOfferItem>();
        }

        private void createInventoryRow(Transform parent, RecycleInventoryEntryData data)
        {
            GameObject root = new GameObject($"Entry_{data.id}", typeof(RectTransform), typeof(Image));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(260f, 92f);

            Image bg = root.GetComponent<Image>();
            bg.color = data.isCard ? new Color(0.92f, 0.84f, 1f, 0.8f) : new Color(0.86f, 0.98f, 0.9f, 0.8f);

            Image icon = createImage(rt, "Img_Icon", Color.white, new Vector2(0.03f, 0.14f), new Vector2(0.28f, 0.86f), false);
            if (icon != null)
            {
                Sprite sprite = string.IsNullOrEmpty(data.iconPath) ? null : ArtAssetLoader.LoadSprite(data.iconPath, false);
                icon.sprite = sprite;
                icon.enabled = true;
            }

            createText(rt, "Txt_Name", new Vector2(0.32f, 0.48f), new Vector2(0.88f, 0.88f), 22f, data.name, TextAlignmentOptions.Left);
            createText(rt, "Txt_Count", new Vector2(0.72f, 0.08f), new Vector2(0.96f, 0.42f), 22f, "x" + data.count, TextAlignmentOptions.Right);
            createText(rt, "Txt_Type", new Vector2(0.32f, 0.08f), new Vector2(0.72f, 0.42f), 18f, data.category, TextAlignmentOptions.Left);
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
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = new Color(0.18f, 0.12f, 0.08f, 1f);
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
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
    }
}
