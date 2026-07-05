/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪核心玩法界面，负责承接玩法状态刷新
* │  类    名: CookView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common;
using Common.UI;
using System.Collections.Generic;
using DG.Tweening;
using Module.Cook;
using Common.Defines;
using Module.Player;
using MVC.View;
using Module.Item;
using Module.View;
using MVC;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.Cook
{
    // 烹饪核心玩法界面，负责刷新一阶段玩法状态与转发操作事件
    public class CookView : BaseView
    {
        private const string ITEM_TOOLTIP_PATH = "UI/Cook/ItemTooltip";
        private const string POT_TRAY_PLUS_SPRITE = "Art/CookView/Pot/加号";
        private const string OWNED_ITEM_ICON_PATH = AddressDefines.Art_ShopItemSample;

        private static Sprite S_PotTrayPlusSprite;
        private static readonly Vector2 S_TooltipOffset = new Vector2(18f, -18f);

        private TextMeshProUGUI _txtTurn;
        private TextMeshProUGUI _txtScore;
        private TextMeshProUGUI _txtTarget;
        private TextMeshProUGUI _txtCoin;
        private TextMeshProUGUI _txtOrder;
        private TextMeshProUGUI _txtPreview;
        private TextMeshProUGUI _txtTip;

        private Image _imgHeatFill;
        private Image _imgHeatPreview;
        private Image _imgHeatBackground;
        private Tweener _heatPreviewTween;
        private Transform _slotRoot;
        private Transform _handArea;
        private Transform _handContent;
        private Transform _dragRoot;
        private Transform _processArea;
        private Transform _processedContent;
        private Transform _potArea;

        private TextMeshProUGUI _txtMagicBox;
        private Button _btnSetting;
        private Button _btnUndo;
        private Button _btnClear;
        private Button _btnSkip;
        private Button _btnSettle;
        private Button _btnMagicBox;
        private Button _btnMagicBoxLegacy;
        private UIButtonHoverItem _magicBoxHover;
        private GameObject _pauseDialogRoot;
        private Button _btnPauseConfirm;
        private Button _btnPauseCancel;
        private bool _isRiskSettleConfirmed;
        private CookRoundResultData _lastShownResult;

        private Transform _potTrayRoot;
        private Button _btnSubmitTray;
        private CookPotTrayItem[] _potTrayItems;

        // 右侧已拥有道具列表（LoopGridView 复用）
        private const int OwnedItemColumns = 1;
        private const int OwnedItemPoolRows = 4;
        private static readonly Vector2 OwnedItemCellSize = new Vector2(112f, 136f);
        private static readonly Vector2 OwnedItemSpacing = new Vector2(0f, 10f);

        private GameObject _ownedItemScrollRoot;
        private ScrollRect _ownedItemScroll;
        private LoopGridView _ownedItemGrid;
        private bool _ownedItemGridInited;
        private readonly List<CookOwnedItemEntry> _ownedItems = new();

        private class CookOwnedItemEntry
        {
            public string name;
            public string iconPath;
        }

        private CookModel _cookModel;
        private ItemTooltip _itemTooltip;
        private object _itemTooltipOwner;
        private RectTransform _tooltipCanvasRect;
        private readonly CookSlotItem[] _slotItems = new CookSlotItem[9];

        // ── 手牌对象池 + 发牌/出牌飞行动画 ──
        private RectTransform _imgAngel;          // 天使口袋（发牌起点）
        private RectTransform _imgDevil;          // 恶魔口袋（出牌终点）
        private SkeletonGraphic _angelSpine;      // 天使 Spine 展示（Img_Angel）
        private SkeletonGraphic _devilSpine;      // 恶魔 Spine 展示（Img_Devil）
        private readonly List<CookMaterialItem> _handPool = new();   // 复用，不销毁
        private readonly List<int> _lastHandIds = new();             // 上次显示的手牌 id（用于 diff）
        private readonly HashSet<CookMaterialItem> _discardingItems = new();   // 正飞向恶魔、由动画收尾隐藏的 item
        private bool _isHandAnimating;            // 发牌/出牌动画期间锁操作
        private bool _dealAnimating;              // 发牌动画进行中（与弃牌兜底解锁区分）
        private GameObject _imgBlock;             // ActionBar 上方的透明遮挡，卡牌飞行时挡住按钮点击
        private const float DealStagger = 0.07f;  // 依次发牌的间隔
        private const float DealDuration = 0.45f;  // 发牌飞行时长（飞久一点）
        private const float DiscardStagger = 0.06f;
        private const float DiscardDuration = 0.5f;   // 出牌飞行时长（飞久一点，飞到才淡出）
        [SerializeField] private float _dealEnterDelay = 0.3f; // 每次发牌飞行动画开始前的等待（秒），CookView 预制体 Inspector 可调
        private const string AngelIdleAnim = "Idle";
        private const string AngelLaunchAnim = "launch";
        private const string DevilIdleAnim = "idie";
        private const string DevilRecycleAnim = "recycle";
        private const string SfxDealAppear = "sfx_ingame_appear";
        private const string SfxDiscardDisappear = "sfx_ingame_disappear";
        private const string SfxHandSelect = "sfx_ingame_select";

        public bool IsHandAnimating => _isHandAnimating;
        public System.Action OnDiscardAnimationDone;

        public override void InitUI()
        {
            _txtTurn = Find<TextMeshProUGUI>("Top/Txt_Turn");
            _txtScore = Find<TextMeshProUGUI>("Top/Txt_Score");
            _txtTarget = Find<TextMeshProUGUI>("Top/Txt_Target");
            _txtCoin = Find<TextMeshProUGUI>("Top/Txt_Coin");
            _txtOrder = Find<TextMeshProUGUI>("Left/Txt_Order");
            if (_txtOrder != null) _txtOrder.gameObject.SetActive(false);
            _txtPreview = Find<TextMeshProUGUI>("Center/Pot/Txt_Preview");
            _txtTip = Find<TextMeshProUGUI>("Bottom/Txt_Tip");
            if (_txtTip != null) _txtTip.gameObject.SetActive(false);

            _imgHeatFill = Find<Image>("Left/HeatBar/Img_Fill");
            _imgHeatPreview = Find<Image>("Left/HeatBar/Img_HeatPreview");
            _imgHeatBackground = Find<Image>("Left/HeatBar/Img_Background");
            if (_imgHeatBackground != null)
            {
                _imgHeatBackground.raycastTarget = false;
                _imgHeatBackground.gameObject.SetActive(false);
            }
            if (_imgHeatFill != null)
            {
                setupHeatBarImage(_imgHeatFill);
                RectTransform fillRt = _imgHeatFill.rectTransform;
                fillRt.anchorMin = new Vector2(fillRt.anchorMin.x, 0f);
                fillRt.anchorMax = new Vector2(fillRt.anchorMax.x, 0f);
                resetHeatBarVerticalOffset(fillRt);
            }
            if (_imgHeatPreview != null)
            {
                setupHeatBarImage(_imgHeatPreview);
                RectTransform previewRt = _imgHeatPreview.rectTransform;
                previewRt.anchorMin = new Vector2(previewRt.anchorMin.x, 0f);
                previewRt.anchorMax = new Vector2(previewRt.anchorMax.x, 0f);
                resetHeatBarVerticalOffset(previewRt);
            }
            _slotRoot = Find<Transform>("Center/Grid");
            _handArea = Find<Transform>("Bottom/HandScroll");
            _handContent = Find<Transform>("Bottom/HandScroll/Viewport/Content");
            _dragRoot = Find<Transform>("DragRoot");
            _processArea = Find<Transform>("Right/Grinder");
            _potArea = Find<Transform>("Center/Pot");
            _potTrayRoot = Find<Transform>("Center/Pot/TrayRoot");
            _btnSubmitTray = Find<Button>("Center/Pot/Btn_SubmitTray");
            _txtMagicBox = Find<TextMeshProUGUI>("Right/MagicBox/Txt_Info");

            _btnSetting = Find<Button>("Top/Btn_Setting");
            _btnUndo = Find<Button>("Bottom/ActionBar/Btn_Undo");
            _btnClear = Find<Button>("Bottom/ActionBar/Btn_Clear");
            _btnSkip = Find<Button>("Bottom/ActionBar/Btn_Skip");
            _btnSettle = Find<Button>("Bottom/ActionBar/Btn_Settle");

            Image imgBlock = Find<Image>("Bottom/Img_Block");
            if (imgBlock != null)
            {
                _imgBlock = imgBlock.gameObject;
                _imgBlock.SetActive(false);   // 默认不挡，只有卡牌飞行时才激活
            }

            initMagicBoxButton();
            bindButtons();
            setupButtonText(_btnSettle, "结束本回合");
            hidePreviewText();
            initSlots();
            initHandArea();
            initProcessArea();
            initPotArea();
            initPauseDialog();
            initHandFlyNodes();
            initOwnedItemScroll();
        }

        // 查找天使发牌锚点 / 恶魔回收锚点（位置可在 prefab 里手动微调），递归按名字找
        private void initHandFlyNodes()
        {
            _imgAngel = findDeep(transform, "DealAnchor_Angel") as RectTransform;
            _imgDevil = findDeep(transform, "RecycleAnchor_Devil") as RectTransform;
            // 兜底：没配锚点时退回用天使/恶魔图片节点
            if (_imgAngel == null) _imgAngel = findDeep(transform, "Img_Angel") as RectTransform;
            if (_imgDevil == null) _imgDevil = findDeep(transform, "Img_Devil") as RectTransform;

            Transform angelVisual = findDeep(transform, "Img_Angel");
            _angelSpine = angelVisual != null ? angelVisual.GetComponent<SkeletonGraphic>() : null;

            Transform devilVisual = findDeep(transform, "Img_Devil");
            _devilSpine = devilVisual != null ? devilVisual.GetComponent<SkeletonGraphic>() : null;
        }

        // 魔盒 Spine 即按钮：悬停放大暂停 + 点击打开 Blackjack
        private void initMagicBoxButton()
        {
            Transform magicBoxTf = findDeep(transform, "Img_MagicBox");
            if (magicBoxTf == null) return;

            SkeletonGraphic spine = magicBoxTf.GetComponent<SkeletonGraphic>();
            _btnMagicBox = magicBoxTf.GetComponent<Button>();
            if (_btnMagicBox == null)
                _btnMagicBox = magicBoxTf.gameObject.AddComponent<Button>();

            _magicBoxHover = magicBoxTf.GetComponent<UIButtonHoverItem>();
            if (_magicBoxHover == null)
                _magicBoxHover = magicBoxTf.gameObject.AddComponent<UIButtonHoverItem>();

            _magicBoxHover.SetupSpineButton(_btnMagicBox, spine, 1.15f);

            _btnMagicBoxLegacy = Find<Button>("Right/MagicBox/Btn_Touch");
        }

        // 右侧已拥有道具 ScrollView（LoopGridView 复用 EmptyGo，无道具时隐藏 ItemScroll）
        private void initOwnedItemScroll()
        {
            if (_ownedItemGridInited) return;

            _ownedItemScroll = tryFindComponent<ScrollRect>("Right/ItemScroll/ScrollView");
            if (_ownedItemScroll == null)
            {
                QLog.Error($"[{nameof(CookView)}] 未找到 Right/ItemScroll/ScrollView，请在 CookView 预制体中配置");
                return;
            }

            _ownedItemScrollRoot = _ownedItemScroll.transform.parent != null
                ? _ownedItemScroll.transform.parent.gameObject
                : _ownedItemScroll.gameObject;

            GameObject slotPrefab = ResManager.LoadAsset<GameObject>(AddressDefines.UI_CookOwnedItemSlot);
            if (slotPrefab == null)
            {
                QLog.Error($"[{nameof(CookView)}] 未找到道具槽预制体：{AddressDefines.UI_CookOwnedItemSlot}");
                return;
            }

            _ownedItemGrid = _ownedItemScroll.gameObject.GetComponent<LoopGridView>();
            if (_ownedItemGrid == null)
                _ownedItemGrid = _ownedItemScroll.gameObject.AddComponent<LoopGridView>();

            stripLayoutComponents(_ownedItemScroll.content);

            var padding = new RectOffset(8, 8, 8, 8);
            _ownedItemGrid.Init(_ownedItemScroll, slotPrefab, OwnedItemColumns, OwnedItemPoolRows,
                OwnedItemCellSize, OwnedItemSpacing, onUpdateOwnedItemSlot, padding);
            _ownedItemGridInited = true;

            refreshOwnedItems();
        }

        private static void stripLayoutComponents(RectTransform content)
        {
            if (content == null) return;

            var layout = content.GetComponent<LayoutGroup>();
            if (layout != null) Destroy(layout);

            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter != null) Destroy(fitter);
        }

        private void refreshOwnedItems()
        {
            _ownedItems.Clear();
            ItemParamCatalogLoader.EnsureLoaded();

            foreach (string itemId in PlayerDataManager.Instance.GetOwnedItemIds())
            {
                ItemParamJsonData cfg = ItemParamCatalogLoader.GetById(itemId);
                if (cfg == null) continue;

                _ownedItems.Add(new CookOwnedItemEntry
                {
                    name = cfg.name,
                    iconPath = OWNED_ITEM_ICON_PATH
                });
            }

            bool hasItems = _ownedItems.Count > 0;
            if (_ownedItemScrollRoot != null)
                _ownedItemScrollRoot.SetActive(hasItems);

            if (!hasItems || !_ownedItemGridInited || _ownedItemGrid == null)
                return;

            _ownedItemGrid.SetTotalCount(_ownedItems.Count);
        }

        private void onUpdateOwnedItemSlot(int dataIndex, GameObject item)
        {
            if (dataIndex < 0 || dataIndex >= _ownedItems.Count) return;

            CookOwnedItemEntry entry = _ownedItems[dataIndex];

            Transform iconTf = item.transform.Find("Img_Icon");
            if (iconTf != null)
            {
                Image img = iconTf.GetComponent<Image>();
                if (img != null)
                {
                    Sprite sprite = ArtAssetLoader.LoadSprite(entry.iconPath);
                    img.sprite = sprite;
                    img.enabled = sprite != null;
                }
            }

            Transform nameTf = item.transform.Find("Txt_Name");
            if (nameTf != null)
            {
                TextMeshProUGUI txt = nameTf.GetComponent<TextMeshProUGUI>();
                if (txt != null) txt.text = entry.name ?? string.Empty;
            }
        }

        // 打开界面时关闭遗留弹窗
        public override void Open(params object[] args)
        {
            hidePauseDialog();
            _lastHandIds.Clear();   // 重置，确保 Open 后首次 refreshHand 把全部牌视为新牌
            playAngelIdleAnimation();
            playDevilIdleAnimation();
            Common.QLog.Info("[CookView] Open() lastHandIds cleared, dealEnterDelay=" + _dealEnterDelay + " angelReady=" + (_imgAngel != null));
        }

        // 关闭界面时恢复普通背景音乐轮播
        public override void Close(params object[] args)
        {
            HideItemTooltip();
            GameApp.SoundManager?.PlayRandomBGM();
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

        // 获取拖拽层
        public Transform GetDragRoot()
        {
            return _dragRoot == null ? transform : _dragRoot;
        }

        // 获取手牌容器（供放置后的池 item 收回隐藏）
        public Transform GetHandContent()
        {
            return _handContent;
        }

        // 获取当前界面字体
        public TMP_FontAsset GetFontAsset()
        {
            if (_txtTip != null && _txtTip.font != null)
                return _txtTip.font;

            return _txtTurn == null ? null : _txtTurn.font;
        }

        // 显示材料详情浮层
        public void ShowItemTooltip(CookMaterialData materialData, PointerEventData eventData)
        {
            if (materialData == null || eventData == null) return;

            ShowItemTooltip(materialData, eventData.position);
        }

        // 按屏幕坐标显示材料详情浮层
        public void ShowItemTooltip(CookMaterialData materialData, Vector2 screenPosition)
        {
            ShowItemTooltip(null, materialData, screenPosition);
        }

        // 按屏幕坐标显示材料详情浮层，并记录当前悬停来源（避免切换卡牌时被其它 item 误关）
        public void ShowItemTooltip(object owner, CookMaterialData materialData, Vector2 screenPosition)
        {
            if (materialData == null) return;
            if (!ensureItemTooltip()) return;

            _itemTooltipOwner = owner;
            _itemTooltip.transform.SetAsLastSibling();
            _itemTooltip.Bind(materialData);
            MoveItemTooltip(screenPosition);
        }

        // 跟随鼠标移动材料详情浮层
        public void MoveItemTooltip(PointerEventData eventData)
        {
            if (_itemTooltip == null || eventData == null) return;

            MoveItemTooltip(eventData.position);
        }

        // 按屏幕坐标移动材料详情浮层
        public void MoveItemTooltip(Vector2 screenPosition)
        {
            if (_itemTooltip == null) return;

            _itemTooltip.SetScreenPosition(screenPosition, _tooltipCanvasRect, S_TooltipOffset);
        }

        // 隐藏材料详情浮层；传入 owner 时仅当仍是该来源才关闭
        public void HideItemTooltip(object owner = null)
        {
            if (owner != null && _itemTooltipOwner != owner)
                return;

            _itemTooltipOwner = null;
            if (_itemTooltip != null)
                _itemTooltip.Hide();
        }

        // GM / 商店购买后刷新右侧道具列表
        public void RefreshOwnedItemsDisplay()
        {
            refreshOwnedItems();
        }

        // 根据烹饪模型刷新界面
        public void Refresh(CookModel cookModel)
        {
            if (cookModel == null) return;

            HideItemTooltip();
            _isRiskSettleConfirmed = false;
            _cookModel = cookModel;
            refreshTop(cookModel);
            refreshTarget(cookModel);
            refreshSlots(cookModel);
            refreshPotTray(cookModel);
            refreshHand(cookModel);
            refreshProcessedMaterials(cookModel);
            refreshActions(cookModel);
            refreshOwnedItems();

            if (cookModel.LastRoundResult != null && cookModel.LastRoundResult != _lastShownResult)
            {
                _lastShownResult = cookModel.LastRoundResult;
                showFloatingScore(cookModel.LastRoundResult);
            }
        }

        // 刷新 Pot 暂存槽与投入按钮显隐
        private void refreshPotTray(CookModel cookModel)
        {
            // 座位数以小局配置 PotTrayCapacity 为准；与当前不一致则重建
            int capacity = cookModel.PotTrayCapacity;
            if (_potTrayItems == null || _potTrayItems.Length != capacity)
                rebuildPotTray(capacity);

            if (_potTrayItems != null)
            {
                var traySlots = cookModel.PotTraySlots;
                bool isFull = cookModel.IsPotTrayFull;
                float previewScore = isFull ? cookModel.PotTrayPreviewScore : 0f;
                for (int i = 0; i < _potTrayItems.Length; i++)
                {
                    if (_potTrayItems[i] == null) continue;
                    CookMaterialData mat = (traySlots != null && i < traySlots.Count) ? traySlots[i] : null;
                    _potTrayItems[i].Bind(mat, isFull, previewScore);
                }
            }

            if (_btnSubmitTray != null)
            {
                _btnSubmitTray.gameObject.SetActive(true);
                _btnSubmitTray.interactable = cookModel.IsPotTrayFull;
            }
        }

        // 尝试将材料放入法阵槽位
        public bool TryPlaceMaterial(CookMaterialItem materialItem, int slotIndex)
        {
            if (materialItem == null) return false;
            if (!canPlaceMaterial(materialItem.MaterialId, slotIndex)) return false;

            ApplyFunc(EventDefines.CookPlaceMaterial, materialItem.MaterialId, slotIndex);
            GameApp.SoundManager?.PlayEffect(SfxHandSelect);
            return true;
        }

        // 尝试移动或交换法阵槽位材料
        public bool TryMoveSlotMaterial(int fromSlotIndex, int toSlotIndex)
        {
            if (_cookModel == null || !_cookModel.IsRunActive) return false;
            if (fromSlotIndex < 0 || fromSlotIndex >= _cookModel.Slots.Count) return false;
            if (toSlotIndex < 0 || toSlotIndex >= _cookModel.Slots.Count) return false;
            if (!_cookModel.Slots[fromSlotIndex].HasMaterial) return false;

            ApplyFunc(EventDefines.CookMoveSlotMaterial, fromSlotIndex, toSlotIndex);
            return true;
        }

        // 尝试将法阵槽位材料移到 Pot 暂存槽
        public bool TryMoveSlotToPotTray(int slotIndex, int trayIndex)
        {
            if (_cookModel == null || !_cookModel.IsRunActive) return false;
            if (slotIndex < 0 || slotIndex >= _cookModel.Slots.Count) return false;

            CookSlotData slot = _cookModel.Slots[slotIndex];
            if (!slot.HasMaterial) return false;
            if (trayIndex < 0 || trayIndex >= _cookModel.PotTrayCapacity) return false;

            // 必须煮过一轮才能入锅
            if (slot.Material.CookProgress <= 0f)
            {
                showTip("该材料还没煮过，先结束回合让它煮一轮");
                return false;
            }

            ApplyFunc(EventDefines.CookMoveToPotTray, slotIndex, trayIndex);
            return true;
        }

        // 尝试交换两个暂存槽
        public bool TrySwapPotTray(int fromTrayIndex, int toTrayIndex)
        {
            if (_cookModel == null || !_cookModel.IsRunActive) return false;
            ApplyFunc(EventDefines.CookSwapPotTray, fromTrayIndex, toTrayIndex);
            return true;
        }

        // 尝试从暂存槽撤回到法阵
        public bool TryReturnPotTray(int trayIndex)
        {
            if (_cookModel == null || !_cookModel.IsRunActive) return false;
            ApplyFunc(EventDefines.CookReturnPotTray, trayIndex);
            return true;
        }

        // 尝试加工材料
        public bool TryProcessMaterial(CookMaterialItem materialItem)
        {
            if (materialItem == null) return false;
            if (!canProcessMaterial(materialItem.MaterialId))
            {
                showTip(getProcessFailTip(materialItem.MaterialId));
                return false;
            }

            ApplyFunc(EventDefines.CookProcessMaterial, materialItem.MaterialId);
            return true;
        }

        // 尝试将本回合法阵材料撤回到可用区域
        public bool TryReturnSlotMaterial(int slotIndex)
        {
            if (_cookModel == null || !_cookModel.IsRunActive)
            {
                showTip("当前不能撤回材料");
                return false;
            }

            if (slotIndex < 0 || slotIndex >= _cookModel.Slots.Count)
            {
                showTip("法阵槽位不存在");
                return false;
            }

            if (!_cookModel.CanReturnSlotMaterial(slotIndex))
            {
                showTip("该材料已经进入持续烹饪，不能直接撤回");
                return false;
            }

            ApplyFunc(EventDefines.CookReturnSlotMaterial, slotIndex);
            return true;
        }

        private void bindButtons()
        {
            bindButton(_btnSetting, () => ApplyControllerFunc(ControllerType.Settings,EventDefines.OpenSettingsView));
            bindButton(_btnUndo, () => ApplyFunc(EventDefines.CookUndoMaterial));
            bindButton(_btnClear, () => ApplyFunc(EventDefines.CookClearMaterials));
            bindButton(_btnSkip, () => ApplyFunc(EventDefines.CookSkipTurn));
            bindButton(_btnSettle, onSettleClick);
            bindButton(_btnMagicBox, onMagicBoxClick);
            bindButton(_btnMagicBoxLegacy, onMagicBoxClick);
        }

        // 点击魔盒：由 Controller 打开 Blackjack 叠加层，CookView 保持打开
        private void onMagicBoxClick()
        {
            if (_cookModel != null && (!_cookModel.IsRunActive || _cookModel.IsMagicBoxUsed))
                return;

            ApplyFunc(EventDefines.CookTouchMagicBox);
        }

        private void initSlots()
        {
            if (_slotRoot == null) return;

            for (int i = 0; i < _slotItems.Length; i++)
            {
                // 槽位可能被分组到 Big/Middle/Small 等子节点下，递归查找
                Transform slotTf = findDeep(_slotRoot, $"Slot_{i}");
                if (slotTf == null)
                {
                    GameObject slotObj = new GameObject($"Slot_{i}", typeof(RectTransform));
                    slotObj.transform.SetParent(_slotRoot, false);
                    slotTf = slotObj.transform;
                }

                CookSlotItem slotItem = slotTf.GetComponent<CookSlotItem>();
                if (slotItem == null)
                    slotItem = slotTf.gameObject.AddComponent<CookSlotItem>();

                slotItem.Init(this, i);
                _slotItems[i] = slotItem;
            }
        }

        // 可选节点查找：不存在时不打 Error（与 BaseView.Find 区分）
        private T tryFindComponent<T>(string path) where T : Component
        {
            Transform tf = transform.Find(path);
            return tf != null ? tf.GetComponent<T>() : null;
        }

        // 在 root 的所有后代中按名查找（Transform.Find 只查直接子级，无法跨分组层）
        private static Transform findDeep(Transform root, string name)
        {
            if (root == null) return null;

            Transform direct = root.Find(name);
            if (direct != null) return direct;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = findDeep(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        // 初始化底部材料区拖拽接收组件
        private void initHandArea()
        {
            if (_handArea == null) return;

            CookHandAreaItem handAreaItem = _handArea.GetComponent<CookHandAreaItem>();
            if (handAreaItem == null)
                handAreaItem = _handArea.gameObject.AddComponent<CookHandAreaItem>();

            handAreaItem.Init(this);
        }

        private void initProcessArea()
        {
            if (_processArea == null) return;

            CookProcessAreaItem processAreaItem = _processArea.GetComponent<CookProcessAreaItem>();
            if (processAreaItem == null)
                processAreaItem = _processArea.gameObject.AddComponent<CookProcessAreaItem>();

            processAreaItem.Init(this);
            initProcessedContent();
        }

        // 初始化研磨器出口材料容器
        private void initProcessedContent()
        {
            if (_processArea == null) return;

            Transform contentTf = _processArea.Find("ProcessedContent");
            if (contentTf == null)
            {
                GameObject contentObj = new GameObject("ProcessedContent", typeof(RectTransform));
                contentObj.transform.SetParent(_processArea, false);
                contentTf = contentObj.transform;
            }

            _processedContent = contentTf;
            _processedContent.SetAsLastSibling();
            if (_processedContent is RectTransform rectTransform)
            {
                rectTransform.anchorMin = new Vector2(0.08f, 0.04f);
                rectTransform.anchorMax = new Vector2(0.92f, 0.38f);
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }

            HorizontalLayoutGroup layoutGroup = _processedContent.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
                layoutGroup = _processedContent.gameObject.AddComponent<HorizontalLayoutGroup>();

            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 8f;
        }

        // 初始化 Pot 暂存槽与投入按钮
        private void initPotArea()
        {
            // 座位数依赖小局配置（PotTrayCapacity），此时 _cookModel 尚未就绪，
            // 真正建座位放到 refreshPotTray（model 已传入）按真实容量建
            if (_btnSubmitTray != null)
            {
                _btnSubmitTray.onClick.RemoveAllListeners();
                _btnSubmitTray.onClick.AddListener(() => ApplyFunc(EventDefines.CookSubmitPotTray));
                _btnSubmitTray.gameObject.SetActive(true);
                _btnSubmitTray.interactable = false;
            }
        }

        // 按容量重建暂存槽：先即时清掉 TrayRoot 下旧座位，再建 N 个
        private void rebuildPotTray(int capacity)
        {
            if (_potTrayRoot == null) return;

            for (int i = _potTrayRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(_potTrayRoot.GetChild(i).gameObject);

            initPotTray(capacity);
        }

        // 在 TrayRoot 下创建 N 个暂存槽（全新建，不复用旧对象）
        private void initPotTray(int capacity)
        {
            if (_potTrayRoot == null) return;

            setupPotTrayLayout();

            _potTrayItems = new CookPotTrayItem[capacity];
            for (int i = 0; i < capacity; i++)
            {
                GameObject trayObj = new GameObject($"Tray_{i}", typeof(RectTransform));
                trayObj.transform.SetParent(_potTrayRoot, false);
                setupPotTraySlotLayout(trayObj, capacity);
                Transform trayTf = trayObj.transform;

                CookPotTrayItem trayItem = trayTf.GetComponent<CookPotTrayItem>();
                if (trayItem == null)
                    trayItem = trayTf.gameObject.AddComponent<CookPotTrayItem>();

                trayItem.Init(this, i);
                _potTrayItems[i] = trayItem;

                if (i < capacity - 1)
                    createPotTrayPlus(i, capacity);
            }
        }

        // 配置暂存槽横向布局
        private void setupPotTrayLayout()
        {
            HorizontalLayoutGroup layoutGroup = _potTrayRoot.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
                layoutGroup = _potTrayRoot.gameObject.AddComponent<HorizontalLayoutGroup>();

            layoutGroup.padding = new RectOffset(8, 8, 8, 8);
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.spacing = 0f;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
        }

        // 配置单个暂存槽在 LayoutGroup 中的尺寸约束
        private static void setupPotTraySlotLayout(GameObject trayObj, int capacity)
        {
            LayoutElement layoutElement = trayObj.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = trayObj.AddComponent<LayoutElement>();

            float slotSize = getPotTraySlotSize(capacity);
            layoutElement.minWidth = slotSize;
            layoutElement.minHeight = slotSize;
            layoutElement.preferredWidth = slotSize;
            layoutElement.preferredHeight = slotSize;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
        }

        // 创建暂存槽之间的加号分隔
        private void createPotTrayPlus(int index, int capacity)
        {
            GameObject plusObj = new GameObject($"Img_Plus_{index}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            plusObj.transform.SetParent(_potTrayRoot, false);

            Image image = plusObj.GetComponent<Image>();
            image.sprite = S_PotTrayPlusSprite ??= ArtAssetLoader.LoadSprite(POT_TRAY_PLUS_SPRITE, false);
            image.color = image.sprite == null ? Color.clear : Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;

            LayoutElement layoutElement = plusObj.GetComponent<LayoutElement>();
            Vector2 plusSize = getPotTrayPlusSize(capacity);
            layoutElement.minWidth = plusSize.x;
            layoutElement.minHeight = plusSize.y;
            layoutElement.preferredWidth = plusSize.x;
            layoutElement.preferredHeight = plusSize.y;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
        }

        // 根据暂存槽数量计算框尺寸，避免 4/5 格超出锅区域
        private static float getPotTraySlotSize(int capacity)
        {
            if (capacity >= 5)
                return 64f;

            if (capacity >= 4)
                return 82f;

            return 96f;
        }

        // 根据暂存槽数量计算加号尺寸，数量越多越紧凑
        private static Vector2 getPotTrayPlusSize(int capacity)
        {
            if (capacity >= 5)
                return new Vector2(28f, 44f);

            if (capacity >= 4)
                return new Vector2(32f, 50f);

            return new Vector2(36f, 54f);
        }

        // 初始化暂停确认弹窗
        private void initPauseDialog()
        {
            Transform dialogTf = transform.Find("PauseConfirmDialog");
            if (dialogTf == null)
            {
                GameObject dialogObj = new GameObject("PauseConfirmDialog", typeof(RectTransform), typeof(Image));
                dialogObj.transform.SetParent(transform, false);
                dialogTf = dialogObj.transform;
            }

            _pauseDialogRoot = dialogTf.gameObject;
            _pauseDialogRoot.transform.SetAsLastSibling();

            Image maskImage = _pauseDialogRoot.GetComponent<Image>();
            if (maskImage == null)
                maskImage = _pauseDialogRoot.AddComponent<Image>();

            maskImage.color = new Color(0f, 0f, 0f, 0.52f);
            maskImage.raycastTarget = true;

            if (dialogTf is RectTransform dialogRt)
                setupChildRect(dialogRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RectTransform panelRt = createRectChild("Panel", dialogTf);
            setupChildRect(panelRt, new Vector2(0.36f, 0.34f), new Vector2(0.64f, 0.66f), Vector2.zero, Vector2.zero);

            Image panelImage = panelRt.GetComponent<Image>();
            if (panelImage == null)
                panelImage = panelRt.gameObject.AddComponent<Image>();

            panelImage.color = new Color(0.98f, 0.9f, 0.72f, 1f);
            panelImage.raycastTarget = true;

            TextMeshProUGUI titleText = createDialogText("Txt_Title", panelRt, 34, TextAlignmentOptions.Center);
            titleText.text = "返回选择界面？";
            setupChildRect(titleText.rectTransform, new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI messageText = createDialogText("Txt_Message", panelRt, 24, TextAlignmentOptions.Center);
            messageText.text = "当前烹饪进度不会保留";
            setupChildRect(messageText.rectTransform, new Vector2(0.08f, 0.46f), new Vector2(0.92f, 0.66f), Vector2.zero, Vector2.zero);

            _btnPauseConfirm = createDialogButton("Btn_Confirm", panelRt, "是");
            setupChildRect(_btnPauseConfirm.GetComponent<RectTransform>(), new Vector2(0.14f, 0.12f), new Vector2(0.44f, 0.34f), Vector2.zero, Vector2.zero);

            _btnPauseCancel = createDialogButton("Btn_Cancel", panelRt, "否");
            setupChildRect(_btnPauseCancel.GetComponent<RectTransform>(), new Vector2(0.56f, 0.12f), new Vector2(0.86f, 0.34f), Vector2.zero, Vector2.zero);

            bindButton(_btnPauseConfirm, confirmReturnToSelect);
            bindButton(_btnPauseCancel, hidePauseDialog);
            hidePauseDialog();
        }

        // 隐藏中间锅区域原本的分数预览文字
        private void hidePreviewText()
        {
            if (_txtPreview != null)
                _txtPreview.gameObject.SetActive(false);
        }

        private void refreshTop(CookModel cookModel)
        {
            if (_txtTurn != null)
                _txtTurn.text = cookModel.GetTurnText();

            if (_txtScore != null)
                _txtScore.text = cookModel.GetScoreText();

            if (_txtTarget != null)
                _txtTarget.text = cookModel.GetTargetText();

            if (_txtCoin != null)
                _txtCoin.text = cookModel.GetCoinText();
        }

        private void refreshTarget(CookModel cookModel)
        {
            if (_txtOrder != null)
                _txtOrder.text = cookModel.GetPotText();

            if (_txtPreview != null && _txtPreview.gameObject.activeSelf)
                _txtPreview.text = cookModel.GetPreviewText();

            if (_txtTip != null)
                _txtTip.text = cookModel.LastTip;

            if (_txtMagicBox != null)
                _txtMagicBox.text = cookModel.MagicBoxStatusText;

            if (_imgHeatFill != null)
            {
                float denom = Mathf.Max(1f, cookModel.TargetMax);
                float fillRatio = Mathf.Clamp01(cookModel.CurrentScore / denom);
                RectTransform fillRt = _imgHeatFill.rectTransform;
                fillRt.anchorMin = new Vector2(fillRt.anchorMin.x, 0f);
                fillRt.anchorMax = new Vector2(fillRt.anchorMax.x, fillRatio);
                resetHeatBarVerticalOffset(fillRt);
                _imgHeatFill.color = cookModel.CurrentScore > cookModel.TargetMax
                    ? new Color(0.92f, 0.23f, 0.16f, 1f)
                    : new Color(0.98f, 0.62f, 0.22f, 1f);

                if (_imgHeatPreview != null)
                {
                    float previewScore = cookModel.IsPotTrayFull ? cookModel.PotTrayPreviewScore : 0f;
                    if (previewScore > 0f)
                    {
                        float previewRatio = Mathf.Clamp01(previewScore / denom);
                        RectTransform previewRt = _imgHeatPreview.rectTransform;
                        previewRt.anchorMin = new Vector2(previewRt.anchorMin.x, fillRatio);
                        previewRt.anchorMax = new Vector2(previewRt.anchorMax.x, Mathf.Clamp01(fillRatio + previewRatio));
                        resetHeatBarVerticalOffset(previewRt);

                        _heatPreviewTween?.Kill();
                        _imgHeatPreview.color = new Color(1f, 0.92f, 0.3f, 0f);
                        _heatPreviewTween = _imgHeatPreview
                            .DOFade(0.7f, 0.5f)
                            .SetLoops(-1, DG.Tweening.LoopType.Yoyo)
                            .SetEase(DG.Tweening.Ease.InOutSine);
                    }
                    else
                    {
                        _heatPreviewTween?.Kill();
                        _heatPreviewTween = null;
                        _imgHeatPreview.color = new Color(1f, 0.92f, 0.3f, 0f);
                        RectTransform previewRt = _imgHeatPreview.rectTransform;
                        previewRt.anchorMin = new Vector2(previewRt.anchorMin.x, 0f);
                        previewRt.anchorMax = new Vector2(previewRt.anchorMax.x, 0f);
                        resetHeatBarVerticalOffset(previewRt);
                    }
                }
            }
        }

        // 初始化温度条图像，避免遮挡素材文字和拖拽操作
        private void setupHeatBarImage(Image image)
        {
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
        }

        // 重置温度条纵向边距，保留预制体的横向布局
        private void resetHeatBarVerticalOffset(RectTransform rectTransform)
        {
            rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, 0f);
            rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, 0f);
        }

        private void refreshSlots(CookModel cookModel)
        {
            for (int i = 0; i < _slotItems.Length && i < cookModel.Slots.Count; i++)
            {
                if (_slotItems[i] != null)
                    _slotItems[i].Bind(cookModel.Slots[i]);
            }
        }

        // 刷新手牌：对象池复用 + 发牌/出牌飞行动画（不销毁重建 item）
        private void refreshHand(CookModel cookModel)
        {
            clearDragItems();
            if (_handContent == null) return;

            Common.QLog.Info("[CookView] refreshHand: handCount=" + cookModel.HandMaterials.Count + " lastHandIds=" + _lastHandIds.Count + " dealEnterDelay=" + _dealEnterDelay + " angelReady=" + (_imgAngel != null));

            // 1) 先处理"出牌作废"动画：放牌后剩余手牌飞向恶魔口袋（数据已清，此处只做表现）
            float discardEndTime = playDiscardAnimationIfNeeded(cookModel);

            var hand = cookModel.HandMaterials;
            int count = hand.Count;

            // 2) 用对象池绑定当前手牌，多余的隐藏
            ensureHandPool(count);

            // 找出本次新发的牌（上次 _lastHandIds 里没有的）
            var newIds = new HashSet<int>();
            for (int i = 0; i < count; i++)
            {
                int id = hand[i].RuntimeId;
                if (!_lastHandIds.Contains(id)) newIds.Add(id);
            }

            // 计算每张牌在 content 里的横向均匀目标位置
            for (int i = 0; i < _handPool.Count; i++)
            {
                CookMaterialItem item = _handPool[i];
                if (i < count)
                {
                    CookMaterialData mat = hand[i];
                    item.gameObject.SetActive(true);
                    item.Bind(mat, this);
                    layoutHandCard(item, i, count);
                    if (_imgAngel != null && newIds.Contains(mat.RuntimeId) && item.Group != null)
                        item.Group.alpha = 0f;
                }
                else
                {
                    // 正飞向恶魔的 item 由出牌动画收尾隐藏，这里不强制隐藏
                    if (!_discardingItems.Contains(item))
                        item.gameObject.SetActive(false);
                }
            }

            // 3) 新发的牌播放发牌飞入动画（依次从天使口袋飞出）
            if (_imgAngel == null) initHandFlyNodes();   // 防御：锚点未就绪时重查一次
            bool hasDeal = newIds.Count > 0 && _imgAngel != null;
            Common.QLog.Info("[CookView] refreshHand: newIds=" + newIds.Count + " hasDeal=" + hasDeal);
            if (hasDeal)
                playDealAnimation(hand, newIds);

            // 记录本次手牌 id，供下次 diff。
            // 若有新牌却因锚点未就绪没能播动画，则不记录这些新牌——留到下次 refresh 补播发牌动画，
            // 避免"第一次 refresh 抢先摆好牌、第二次就不再算新牌"导致永远无动画。
            if (newIds.Count > 0 && !hasDeal)
                return;

            _lastHandIds.Clear();
            for (int i = 0; i < count; i++)
                _lastHandIds.Add(hand[i].RuntimeId);
        }

        // 确保对象池至少有 count 个 item（复用，不销毁）
        private void ensureHandPool(int count)
        {
            while (_handPool.Count < count)
            {
                GameObject itemObj = new GameObject($"HandCard_{_handPool.Count}", typeof(RectTransform));
                itemObj.transform.SetParent(_handContent, false);
                CookMaterialItem item = itemObj.AddComponent<CookMaterialItem>();
                _handPool.Add(item);
            }
        }

        // 计算并设置第 index 张手牌在 content 里的位置（横向均匀排列，居中）
        private void layoutHandCard(CookMaterialItem item, int index, int total)
        {
            RectTransform rt = item.Rect;
            Vector2 size = CookMaterialItem.CardSize;
            float spacing = 16f;
            float step = size.x + spacing;
            float totalWidth = total > 0 ? (total * size.x + (total - 1) * spacing) : 0f;
            float startX = -totalWidth * 0.5f + size.x * 0.5f;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            rt.anchoredPosition = new Vector2(startX + index * step, 0f);

            if (item.Group != null) { item.Group.alpha = 1f; item.Group.blocksRaycasts = true; }
        }

        // 发牌动画：天使 launch 立即播放；卡牌飞行动画开始前等待 _dealEnterDelay
        private void playDealAnimation(System.Collections.Generic.IReadOnlyList<CookMaterialData> hand, HashSet<int> newIds)
        {
            setHandInteractable(false);
            _dealAnimating = true;
            Vector2 angelPos = worldToHandContent(_imgAngel.position);

            float enterDelay = Mathf.Max(0f, _dealEnterDelay);
            Common.QLog.Info("[CookView] playDealAnimation: enterDelay=" + enterDelay + " newIds=" + newIds.Count + " angelPos=" + worldToHandContent(_imgAngel.position));

            playAngelLaunchAnimation();
            if (enterDelay > 0f)
            {
                DOVirtual.DelayedCall(enterDelay, () =>
                    GameApp.SoundManager?.PlayEffect(SfxDealAppear));
            }
            else
            {
                GameApp.SoundManager?.PlayEffect(SfxDealAppear);
            }

            int order = 0;
            float lastEnd = 0f;
            for (int i = 0; i < hand.Count && i < _handPool.Count; i++)
            {
                if (!newIds.Contains(hand[i].RuntimeId)) continue;

                CookMaterialItem item = _handPool[i];
                RectTransform rt = item.Rect;
                Vector2 target = rt.anchoredPosition;

                // 起点：天使口袋；透明隐藏，飞入时渐显
                rt.anchoredPosition = angelPos;
                if (item.Group != null) item.Group.alpha = 0f;

                // 等待 enterDelay 后，第 order 张再依次起飞
                float delay = enterDelay + order * DealStagger;
                lastEnd = Mathf.Max(lastEnd, delay + DealDuration);

                DG.Tweening.Sequence seq = DOTween.Sequence().SetDelay(delay)
                    .Append(rt.DOAnchorPos(target, DealDuration).SetEase(Ease.OutCubic));
                if (item.Group != null)
                    seq.Join(item.Group.DOFade(1f, DealDuration));
                item.SetFlyTween(seq);
                order++;
            }

            // 全部飞完解锁
            DOVirtual.DelayedCall(lastEnd + 0.02f, () =>
            {
                _dealAnimating = false;
                setHandInteractable(true);
            });
        }

        private void playAngelLaunchAnimation()
        {
            if (_angelSpine == null) return;

            Spine.AnimationState state = _angelSpine.AnimationState;
            if (state == null) return;

            Spine.TrackEntry entry = state.SetAnimation(0, AngelLaunchAnim, false);
            entry.Complete += onAngelLaunchComplete;
        }

        private void onAngelLaunchComplete(Spine.TrackEntry trackEntry)
        {
            trackEntry.Complete -= onAngelLaunchComplete;
            playAngelIdleAnimation();
        }

        private void playAngelIdleAnimation()
        {
            if (_angelSpine == null) return;

            Spine.AnimationState state = _angelSpine.AnimationState;
            if (state == null) return;

            state.SetAnimation(0, AngelIdleAnim, true);
        }

        private void playDevilRecycleAnimation()
        {
            if (_devilSpine == null) return;

            Spine.AnimationState state = _devilSpine.AnimationState;
            if (state == null) return;

            Spine.TrackEntry entry = state.SetAnimation(0, DevilRecycleAnim, false);
            entry.Complete += onDevilRecycleComplete;
        }

        private void onDevilRecycleComplete(Spine.TrackEntry trackEntry)
        {
            trackEntry.Complete -= onDevilRecycleComplete;
            playDevilIdleAnimation();
        }

        private void playDevilIdleAnimation()
        {
            if (_devilSpine == null) return;

            Spine.AnimationState state = _devilSpine.AnimationState;
            if (state == null) return;

            state.SetAnimation(0, DevilIdleAnim, true);
        }

        // 出牌动画：把 model 标记作废的手牌从当前位置飞向恶魔口袋后隐藏
        private float playDiscardAnimationIfNeeded(CookModel cookModel)
        {
            var discarded = cookModel.DiscardedHandThisTurn;
            if (discarded == null || discarded.Count == 0 || _imgDevil == null)
            {
                if (discarded != null) discarded.Clear();
                return 0f;
            }

            setHandInteractable(false);
            playDevilRecycleAnimation();
            GameApp.SoundManager?.PlayEffect(SfxDiscardDisappear);
            Vector2 devilPos = worldToHandContent(_imgDevil.position);

            // 作废的牌对应池中当前显示这些 id 的 item
            int order = 0;
            float lastEnd = 0f;
            for (int d = 0; d < discarded.Count; d++)
            {
                int id = discarded[d].RuntimeId;
                CookMaterialItem item = findPoolItemById(id);
                if (item == null) continue;

                RectTransform rt = item.Rect;
                float delay = order * DiscardStagger;
                lastEnd = Mathf.Max(lastEnd, delay + DiscardDuration);

                CookMaterialItem captured = item;
                _discardingItems.Add(captured);
                // 位移与 scale 同步同时长：每张卡刚好飞到恶魔口袋时 scale=0（只用 scale，不改透明度）
                DG.Tweening.Sequence seq = DOTween.Sequence().SetDelay(delay)
                    .Append(rt.DOAnchorPos(devilPos, DiscardDuration).SetEase(Ease.InCubic))
                    .Join(rt.DOScale(0f, DiscardDuration).SetEase(Ease.InCubic));
                seq.OnComplete(() =>
                {
                    captured.gameObject.SetActive(false);
                    _discardingItems.Remove(captured);
                });
                item.SetFlyTween(seq);
                order++;
            }

            discarded.Clear();

            // 全部弃牌飞完后：通知 model 发新牌，再刷新 View，并触发外部回调
            float endTime = lastEnd + 0.05f;
            DOVirtual.DelayedCall(endTime, () =>
            {
                cookModel.DealNewHand();
                Refresh(cookModel);
                // Refresh 内若有新牌会触发发牌动画（_dealAnimating=true），由发牌结束自行解锁；
                // 若没有新牌（如整局最后一回合只回收不发牌），这里兜底解锁，避免遮挡残留
                if (_isHandAnimating && !_dealAnimating)
                    setHandInteractable(true);
                OnDiscardAnimationDone?.Invoke();
                OnDiscardAnimationDone = null;
            });

            return endTime;
        }

        // 在对象池里找当前绑定了指定 id 且在显示中的 item
        private CookMaterialItem findPoolItemById(int runtimeId)
        {
            for (int i = 0; i < _handPool.Count; i++)
            {
                if (_handPool[i] != null && _handPool[i].gameObject.activeSelf && _handPool[i].MaterialId == runtimeId)
                    return _handPool[i];
            }
            return null;
        }

        // 世界坐标转换到 handContent 的局部坐标（直接用 InverseTransformPoint，
        // 不经屏幕坐标，避开 WorldSpace/Overlay 相机差异导致的错位）
        private Vector2 worldToHandContent(Vector3 worldPos)
        {
            RectTransform contentRt = _handContent as RectTransform;
            if (contentRt == null) return Vector2.zero;

            Vector3 local = contentRt.InverseTransformPoint(worldPos);
            return new Vector2(local.x, local.y);
        }

        // 锁/解锁全部手牌的交互（动画期间禁拖拽，并用透明遮挡挡住按钮点击）
        private void setHandInteractable(bool value)
        {
            _isHandAnimating = !value;
            // value=false（卡牌正在飞行）→ 激活遮挡挡住按钮；飞行结束 → 关闭遮挡
            if (_imgBlock != null)
                _imgBlock.SetActive(_isHandAnimating);
            for (int i = 0; i < _handPool.Count; i++)
                if (_handPool[i] != null) _handPool[i].SetInteractable(value);
        }

        // 刷新研磨器出口等待取走的材料
        private void refreshProcessedMaterials(CookModel cookModel)
        {
            clearProcessedMaterials();
            if (_processedContent == null) return;

            for (int i = 0; i < cookModel.ProcessedMaterials.Count; i++)
            {
                CookMaterialData materialData = cookModel.ProcessedMaterials[i];
                GameObject itemObj = new GameObject($"ProcessedMaterial_{materialData.RuntimeId}", typeof(RectTransform));
                itemObj.transform.SetParent(_processedContent, false);

                CookMaterialItem item = itemObj.AddComponent<CookMaterialItem>();
                item.Bind(materialData, this);
                item.SetDisplaySize(104f, 124f);
            }

            if (_processedContent is RectTransform contentRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        }

        private void refreshActions(CookModel cookModel)
        {
            if (_btnUndo != null)
                _btnUndo.interactable = cookModel.HasPlacedMaterial;

            if (_btnClear != null)
                _btnClear.interactable = cookModel.HasPlacedMaterial;

            if (_btnSkip != null)
                _btnSkip.interactable = cookModel.IsRunActive;

            if (_btnSettle != null)
                _btnSettle.interactable = cookModel.CanSettle;

            if (_btnMagicBox != null)
                _btnMagicBox.interactable = cookModel.IsRunActive && !cookModel.IsMagicBoxUsed;

            if (_btnMagicBoxLegacy != null)
                _btnMagicBoxLegacy.interactable = cookModel.IsRunActive && !cookModel.IsMagicBoxUsed;

            if (_magicBoxHover != null)
                _magicBoxHover.SetInteractable(cookModel.IsRunActive && !cookModel.IsMagicBoxUsed);
        }

        private void clearHand()
        {
            HideItemTooltip();
            if (_handContent == null) return;

            for (int i = _handContent.childCount - 1; i >= 0; i--)
                Destroy(_handContent.GetChild(i).gameObject);
        }

        // 清空研磨器出口材料 UI
        private void clearProcessedMaterials()
        {
            HideItemTooltip();
            if (_processedContent == null) return;

            for (int i = _processedContent.childCount - 1; i >= 0; i--)
                Destroy(_processedContent.GetChild(i).gameObject);
        }

        private void clearDragItems()
        {
            HideItemTooltip();
            if (_dragRoot == null) return;

            for (int i = _dragRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _dragRoot.GetChild(i);
                CookMaterialItem item = child.GetComponent<CookMaterialItem>();
                if (item == null) continue;

                // 对象池里的 item 不能销毁（会留下野指针导致 MissingReference）；
                // 拖拽残留在 DragRoot 的池对象，收回手牌容器、隐藏待下次复用
                if (_handPool.Contains(item))
                {
                    item.transform.SetParent(_handContent, false);
                    item.gameObject.SetActive(false);
                }
                else
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private bool canPlaceMaterial(int materialId, int slotIndex)
        {
            if (_cookModel == null || !_cookModel.IsRunActive) return false;
            if (!_cookModel.CanPlaceHandThisTurn) return false;
            if (slotIndex < 0 || slotIndex >= _cookModel.Slots.Count) return false;
            if (_cookModel.Slots[slotIndex].HasMaterial) return false;

            for (int i = 0; i < _cookModel.HandMaterials.Count; i++)
            {
                if (_cookModel.HandMaterials[i].RuntimeId == materialId)
                    return true;
            }

            for (int i = 0; i < _cookModel.ProcessedMaterials.Count; i++)
            {
                if (_cookModel.ProcessedMaterials[i].RuntimeId == materialId)
                    return true;
            }

            return false;
        }

        // 获取加工失败提示
        private string getProcessFailTip(int materialId)
        {
            if (_cookModel == null || !_cookModel.IsRunActive)
                return "当前不能研磨";

            for (int i = 0; i < _cookModel.HandMaterials.Count; i++)
            {
                CookMaterialData materialData = _cookModel.HandMaterials[i];
                if (materialData.RuntimeId != materialId) continue;

                if (materialData.IsProcessed)
                    return $"{materialData.Config.name} 已研磨";

                if (!materialData.Config.canProcess)
                    return $"{materialData.Config.name} 不可研磨";
            }

            for (int i = 0; i < _cookModel.ProcessedMaterials.Count; i++)
            {
                CookMaterialData materialData = _cookModel.ProcessedMaterials[i];
                if (materialData.RuntimeId == materialId)
                    return $"{materialData.Config.name} 已在研磨器出口，请拖入法阵";
            }

            return "材料不在可研磨区域";
        }

        // 确保材料详情浮层已经实例化到场景 Canvas
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

            if (_itemTooltip != null)
            {
                _itemTooltip.SetFontAsset(GetFontAsset());
                _itemTooltip.Hide();
            }

            return _itemTooltip != null;
        }

        // 点击结算按钮
        private void onSettleClick()
        {
            if (_cookModel == null || !_cookModel.CanSettle)
            {
                showTip("当前不能结算");
                return;
            }

            if (_cookModel.IsOverHeatRisk && !_isRiskSettleConfirmed)
            {
                _isRiskSettleConfirmed = true;
                showTip("当前火候可能爆锅，再点一次结束本回合");
                return;
            }

            ApplyFunc(EventDefines.CookSettleTurn);
        }

        private bool canProcessMaterial(int materialId)
        {
            if (_cookModel == null || !_cookModel.IsRunActive) return false;

            for (int i = 0; i < _cookModel.HandMaterials.Count; i++)
            {
                CookMaterialData materialData = _cookModel.HandMaterials[i];
                if (materialData.RuntimeId != materialId) continue;

                return materialData.Config.canProcess && !materialData.IsProcessed;
            }

            return false;
        }

        // 显示暂停确认弹窗
        private void showPauseDialog()
        {
            if (_pauseDialogRoot == null) return;

            _pauseDialogRoot.transform.SetAsLastSibling();
            _pauseDialogRoot.SetActive(true);
        }

        // 隐藏暂停确认弹窗
        private void hidePauseDialog()
        {
            if (_pauseDialogRoot != null)
                _pauseDialogRoot.SetActive(false);
        }

        // 确认返回选择界面
        private void confirmReturnToSelect()
        {
            hidePauseDialog();
            ApplyFunc(EventDefines.CookReturnToSelect);
        }

        // 显示底部操作提示
        public void ShowTip(string tip)
        {
            showTip(tip);
        }

        // 刷新底部操作提示
        private void showTip(string tip)
        {
            if (_txtTip != null)
                _txtTip.text = tip;
        }

        private void showFloatingScore(CookRoundResultData result)
        {
            string text = result.RoundScore > 0
                ? $"+{CookRoundResultData.FormatScore(result.RoundScore)}"
                : CookRoundResultData.FormatScore(result.RoundScore);

            // 在 Canvas 正中心创建飘字对象
            GameObject floatObj = new GameObject("FloatingScore", typeof(RectTransform));
            floatObj.transform.SetParent(transform, false);
            floatObj.transform.SetAsLastSibling();

            RectTransform rt = floatObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(400f, 100f);

            TextMeshProUGUI txt = floatObj.AddComponent<TextMeshProUGUI>();
            txt.font = GetFontAsset();
            txt.fontSize = 60;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.text = text;
            txt.color = result.IsOverHeat
                ? new Color(0.92f, 0.23f, 0.16f, 1f)
                : new Color(0.1f, 0.7f, 0.15f, 1f);
            txt.raycastTarget = false;

            // 向上飘 + 渐隐
            rt.DOAnchorPosY(200f, 1.2f).SetEase(Ease.OutCubic);
            txt.DOFade(0f, 1.2f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                if (floatObj != null) floatObj.SetActive(false);
            });
        }

        private static void bindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null) return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        // 设置按钮显示文本
        private static void setupButtonText(Button button, string text)
        {
            if (button == null) return;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = text;
        }

        // 创建或获取 RectTransform 子节点
        private static RectTransform createRectChild(string childName, Transform parent)
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

        // 创建弹窗文本
        private TextMeshProUGUI createDialogText(
            string childName,
            Transform parent,
            int fontSize,
            TextAlignmentOptions alignment)
        {
            RectTransform rectTransform = createRectChild(childName, parent);
            TextMeshProUGUI text = rectTransform.GetComponent<TextMeshProUGUI>();
            if (text == null)
                text = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();

            text.font = GetFontAsset();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.18f, 0.11f, 0.07f, 1f);
            text.enableWordWrapping = false;
            return text;
        }

        // 创建弹窗按钮
        private Button createDialogButton(string childName, Transform parent, string label)
        {
            RectTransform buttonRt = createRectChild(childName, parent);
            Image buttonImage = buttonRt.GetComponent<Image>();
            if (buttonImage == null)
                buttonImage = buttonRt.gameObject.AddComponent<Image>();

            buttonImage.color = new Color(0.92f, 0.5f, 0.18f, 1f);
            buttonImage.raycastTarget = true;

            Button button = buttonRt.GetComponent<Button>();
            if (button == null)
                button = buttonRt.gameObject.AddComponent<Button>();

            TextMeshProUGUI text = createDialogText("Txt_Label", buttonRt, 26, TextAlignmentOptions.Center);
            text.text = label;
            setupChildRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        // 设置子节点 RectTransform 拉伸范围
        private static void setupChildRect(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            if (rectTransform == null) return;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
    }
}
