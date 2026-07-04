/*
* ┌──────────────────────────────────┐
* │  描    述: 商店界面视图，负责展示货架、金币与回收状态
* │  类    名: ShopView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Common;
using Common.Defines;
using Common.UI;
using Module.Item;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Shop
{
    public class ShopView : BaseView
    {
        private const string ITEM_TOOLTIP_PATH = "UI/Cook/ItemTooltip";

        private static readonly Vector2 S_TooltipOffset = new Vector2(18f, -18f);

        private TextMeshProUGUI _txtGold;
        private TextMeshProUGUI _txtTitle;
        private TextMeshProUGUI _txtSubtitle;
        private TextMeshProUGUI _txtInfo;
        private Button _btnRefresh;
        private Button _btnRecycle;
        private Button _btnContinue;
        private Button _btnStore;
        private TextMeshProUGUI _txtRecycleButton;
        private Color _recycleButtonNormalColor;

        private readonly List<Transform> _boxSlots = new();
        private readonly List<Transform> _itemSlots = new();
        private ItemTooltip _itemTooltip;
        private object _itemTooltipOwner;
        private RectTransform _tooltipCanvasRect;

        // 预制体设计参考图（Img_Background 下），运行时隐藏并复用 sprite
        private Sprite _boxSprite;
        private TMP_FontAsset _fontTemplate;

        public override void InitUI()
        {
            _txtGold = Find<TextMeshProUGUI>("TopGold/Txt_GoldValue");
            _txtTitle = Find<Transform>("Top")?.GetComponentInChildren<TextMeshProUGUI>();
            _txtSubtitle = Find<TextMeshProUGUI>("Subtitle/Txt_Subtitle");
            _txtInfo = Find<TextMeshProUGUI>("Right/Txt_Info");
            _btnRefresh = Find<Button>("Bottom/Btn_RefreshShelf");
            _btnRecycle = Find<Button>("Bottom/Btn_Recycle");
            _btnContinue = Find<Button>("Bottom/Btn_Continue");
            _btnStore = findOptional<Button>("Bottom/Btn_Store");
            if (_btnRecycle != null)
            {
                _txtRecycleButton = _btnRecycle.GetComponentInChildren<TextMeshProUGUI>(true);
                Image recycleImage = _btnRecycle.GetComponent<Image>();
                _recycleButtonNormalColor = recycleImage != null ? recycleImage.color : Color.white;
            }

            resolveUIFont();

            Image materialSample = findOptional<Image>("Img_Background/Img_MaterialSample");
            if (materialSample != null)
            {
                _boxSprite = materialSample.sprite;
                materialSample.gameObject.SetActive(false);
            }

            if (_boxSprite == null)
                _boxSprite = ArtAssetLoader.LoadSprite(AddressDefines.Art_ShopMaterialBoxSample, logOnFail: false);

            Image itemSample = findOptional<Image>("Img_Background/Img_ItemSample");
            if (itemSample != null)
                itemSample.gameObject.SetActive(false);

            bindButtons();
            setupButtonHovers();
            collectSlots();
        }

        private void setupButtonHovers()
        {
            setupButtonHover(_btnContinue, AddressDefines.Art_ShopContinueHover);
            setupButtonHover(_btnRefresh, null);
        }

        private static void setupButtonHover(Button button, string hoverSpriteAddress)
        {
            if (button == null) return;

            UIButtonHoverItem hover = button.GetComponent<UIButtonHoverItem>();
            if (hover == null)
                hover = button.gameObject.AddComponent<UIButtonHoverItem>();

            hover.Setup(button, hoverSpriteAddress);
        }

        private void resolveUIFont()
        {
            if (_txtGold?.font != null) _fontTemplate = _txtGold.font;
            else if (_txtSubtitle?.font != null) _fontTemplate = _txtSubtitle.font;
            else if (_txtInfo?.font != null) _fontTemplate = _txtInfo.font;
            else _fontTemplate = UIFontHelper.JingnanFont;
        }

        public override void Close(params object[] args)
        {
            HideShopTooltip();
            base.Close(args);
        }

        // 销毁界面时同步清理挂在全局 Canvas 下的详情浮层
        protected override void OnDestroy()
        {
            if (_itemTooltip != null)
            {
                Destroy(_itemTooltip.gameObject);
                _itemTooltip = null;
            }

            base.OnDestroy();
        }

        public void Refresh(ShopModel model)
        {
            if (model == null) return;
            HideShopTooltip();

            if (_txtGold != null) _txtGold.text = model.Gold.ToString();
            if (_txtTitle != null) _txtTitle.text = "黑猫夜市";
            if (_txtSubtitle != null) _txtSubtitle.text = "夜市补给铺·精选材料箱（卡包）";
            if (_txtInfo != null)
            {
                _txtInfo.text = "本轮补给\n\n" +
                                "上排: 购买材料箱，进入选卡界面挑选卡牌\n\n" +
                                "下排: 购买道具\n\n" +
                                $"当前金币:{model.Gold}\n\n" +
                                $"回收机会:{(model.CanRecycle ? "可用" : "已使用")}\n\n" +
                                "购箱后可免费选卡加入牌组";
            }

            refreshRecycleState(model.CanRecycle);
            refreshBoxSlots(model.BoxSlots);
            refreshItemSlots(model.ItemSlots);
        }

        // 刷新回收按钮的状态表现
        private void refreshRecycleState(bool canRecycle)
        {
            if (_txtRecycleButton != null)
                _txtRecycleButton.text = canRecycle ? "回收" : "已回收";

            if (_btnRecycle == null) return;

            _btnRecycle.interactable = true;
            Image recycleImage = _btnRecycle.GetComponent<Image>();
            if (recycleImage != null)
                recycleImage.color = canRecycle ? _recycleButtonNormalColor : new Color(0.55f, 0.55f, 0.55f, 0.9f);
        }

        private void bindButtons()
        {
            bind(_btnRefresh, () => ApplyFunc("Shop.Refresh"));
            bind(_btnRecycle, () => ApplyFunc("Shop.Recycle"));
            bind(_btnContinue, () => ApplyFunc("Shop.Continue"));

            if (_btnStore != null)
                _btnStore.gameObject.SetActive(false);
        }

        private T findOptional<T>(string path) where T : Component
        {
            Transform t = transform.Find(path);
            return t != null ? t.GetComponent<T>() : null;
        }

        private static void bind(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }

        private void collectSlots()
        {
            _boxSlots.Clear();
            _itemSlots.Clear();
            var middle = Find<Transform>("Middle");
            if (middle == null) return;

            foreach (Transform group in middle)
            {
                foreach (Transform slot in group)
                {
                    if (group.name.Contains("Card") || group.name.Contains("Box"))
                        _boxSlots.Add(slot);
                    else
                        _itemSlots.Add(slot);
                }
            }
        }

        private void refreshBoxSlots(IReadOnlyList<ShopSlotData> data)
        {
            string iconPath = ShopCatalog.DefaultBoxIconPathValue;
            int showCount = Mathf.Min(_boxSlots.Count, data?.Count ?? 0);

            for (int i = 0; i < _boxSlots.Count; i++)
            {
                Transform slot = _boxSlots[i];
                clearChildren(slot);
                if (i >= showCount) continue;

                buildBoxSlot(slot, data[i], iconPath);
            }
        }

        private void refreshItemSlots(IReadOnlyList<ShopSlotData> data)
        {
            int showCount = Mathf.Min(_itemSlots.Count, data?.Count ?? 0);
            for (int i = 0; i < _itemSlots.Count; i++)
            {
                Transform slot = _itemSlots[i];
                clearChildren(slot);
                if (i >= showCount) continue;

                GameObject obj = ResManager.Instantiate("UI/Shop/ShopPropSlot", slot);
                if (obj == null) continue;

                var binder = obj.GetComponent<ShopSlotBinder>();
                if (binder != null)
                    binder.Bind(data[i], onBuySlot, this);
            }
        }

        private void buildBoxSlot(Transform slot, ShopSlotData data, string iconPath)
        {
            GameObject root = new GameObject("ShopBoxSlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ShopBoxSlotBinder));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.SetParent(slot, false);
            stretchFill(rootRt);

            Image rootBg = root.GetComponent<Image>();
            rootBg.color = new Color(1f, 1f, 1f, 0f);
            rootBg.raycastTarget = true;

            Button button = root.GetComponent<Button>();
            button.targetGraphic = rootBg;
            button.transition = Selectable.Transition.ColorTint;

            ShopHoverScaleItem hover = root.AddComponent<ShopHoverScaleItem>();

            RectTransform iconRt = createChildRect(rootRt, "Img_Icon", new Vector2(0.5f, 0.58f), new Vector2(200f, 200f));
            Image icon = iconRt.gameObject.AddComponent<Image>();
            applyBoxSprite(icon);
            icon.raycastTarget = false;

            RectTransform priceRowRt = ShopPriceRowHelper.CreatePriceRow(rootRt, new Vector2(0.5f, 0.06f), new Vector2(170f, 52f));
            ShopPriceRowHelper.CreatePriceTag(priceRowRt);
            TextMeshProUGUI priceText = ShopPriceRowHelper.CreatePriceText(priceRowRt, _fontTemplate, 30);

            hover.SetHitSize(rootRt.rect.width > 1f ? rootRt.rect.width : 200f,
                rootRt.rect.height > 1f ? rootRt.rect.height : 240f);

            var binder = root.GetComponent<ShopBoxSlotBinder>();
            binder.Bind(data, iconPath, _boxSprite, _fontTemplate, onBuySlot, this);
        }

        private void applyBoxSprite(Image target)
        {
            if (target == null) return;

            target.sprite = _boxSprite;
            target.preserveAspect = true;
            target.enabled = _boxSprite != null;
            if (_boxSprite == null)
                target.color = new Color(0.92f, 0.88f, 0.82f, 1f);
        }

        private TextMeshProUGUI createChildText(RectTransform parent, string name, Vector2 anchorY, Vector2 size, float fontSize)
        {
            RectTransform rt = createChildRect(parent, name, anchorY, size);
            TextMeshProUGUI txt = rt.gameObject.AddComponent<TextMeshProUGUI>();
            UIFontHelper.ApplyChineseFont(txt, _fontTemplate);
            txt.fontSize = fontSize;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.2f, 0.15f, 0.1f, 1f);
            txt.raycastTarget = false;
            return txt;
        }

        private static RectTransform createChildRect(RectTransform parent, string name, Vector2 anchorY, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, anchorY.y);
            rt.anchorMax = new Vector2(0.5f, anchorY.y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        private static void stretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static void clearChildren(Transform slot)
        {
            for (int c = slot.childCount - 1; c >= 0; c--)
                Destroy(slot.GetChild(c).gameObject);
        }

        // 显示商店商品详情浮层
        public void ShowShopTooltip(object owner, ShopSlotData slotData, Vector2 screenPosition)
        {
            if (slotData == null) return;
            if (!ensureItemTooltip()) return;

            _itemTooltipOwner = owner;
            _itemTooltip.transform.SetAsLastSibling();
            _itemTooltip.Bind(slotData);
            MoveShopTooltip(screenPosition);
        }

        // 跟随鼠标移动商店商品详情浮层
        public void MoveShopTooltip(Vector2 screenPosition)
        {
            if (_itemTooltip == null) return;

            _itemTooltip.SetScreenPosition(screenPosition, _tooltipCanvasRect, S_TooltipOffset);
        }

        // 隐藏商店商品详情浮层；传入 owner 时仅关闭当前来源
        public void HideShopTooltip(object owner = null)
        {
            if (owner != null && _itemTooltipOwner != owner)
                return;

            _itemTooltipOwner = null;
            if (_itemTooltip != null)
                _itemTooltip.Hide();
        }

        // 确保商店商品详情浮层已经实例化到场景 Canvas
        private bool ensureItemTooltip()
        {
            if (_itemTooltip != null)
                return true;

            Transform parent = GameApp.ViewManager?.canvasTf ?? transform;
            GameObject tooltipObj = ResManager.Instantiate(ITEM_TOOLTIP_PATH, parent);
            if (tooltipObj == null) return false;

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

        private void onBuySlot(ShopSlotData slotData) => ApplyFunc("Shop.BuyItem", slotData);
    }
}
