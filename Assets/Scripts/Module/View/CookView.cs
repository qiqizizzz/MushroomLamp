/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪核心玩法界面，负责承接玩法状态刷新
* │  类    名: CookView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Cook;
using Common.Defines;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    // 烹饪核心玩法界面，负责刷新一阶段玩法状态与转发操作事件
    public class CookView : BaseView
    {
        private TextMeshProUGUI _txtTurn;
        private TextMeshProUGUI _txtScore;
        private TextMeshProUGUI _txtTarget;
        private TextMeshProUGUI _txtCoin;
        private TextMeshProUGUI _txtOrder;
        private TextMeshProUGUI _txtPreview;
        private TextMeshProUGUI _txtTip;

        private Image _imgHeatFill;
        private Transform _slotRoot;
        private Transform _handContent;
        private Transform _dragRoot;
        private Transform _processArea;
        private Transform _processedContent;
        private Transform _potArea;

        private TextMeshProUGUI _txtMagicBox;
        private Image _imgPotBody;
        private Button _btnUndo;
        private Button _btnClear;
        private Button _btnSkip;
        private Button _btnSettle;
        private Button _btnMagicBox;

        private CookModel _cookModel;
        private readonly CookSlotItem[] _slotItems = new CookSlotItem[9];

        public override void InitUI()
        {
            _txtTurn = Find<TextMeshProUGUI>("Top/Txt_Turn");
            _txtScore = Find<TextMeshProUGUI>("Top/Txt_Score");
            _txtTarget = Find<TextMeshProUGUI>("Top/Txt_Target");
            _txtCoin = Find<TextMeshProUGUI>("Top/Txt_Coin");
            _txtOrder = Find<TextMeshProUGUI>("Left/Txt_Order");
            _txtPreview = Find<TextMeshProUGUI>("Center/Pot/Txt_Preview");
            _txtTip = Find<TextMeshProUGUI>("Bottom/Txt_Tip");

            _imgHeatFill = Find<Image>("Left/HeatBar/Img_Fill");
            _slotRoot = Find<Transform>("Center/Grid");
            _handContent = Find<Transform>("Bottom/HandScroll/Viewport/Content");
            _dragRoot = Find<Transform>("DragRoot");
            _processArea = Find<Transform>("Right/Grinder");
            _potArea = Find<Transform>("Center/Pot");
            _txtMagicBox = Find<TextMeshProUGUI>("Right/MagicBox/Txt_Info");

            _btnUndo = Find<Button>("Bottom/ActionBar/Btn_Undo");
            _btnClear = Find<Button>("Bottom/ActionBar/Btn_Clear");
            _btnSkip = Find<Button>("Bottom/ActionBar/Btn_Skip");
            _btnSettle = Find<Button>("Bottom/ActionBar/Btn_Settle");
            _btnMagicBox = Find<Button>("Right/MagicBox/Btn_Touch");

            bindButtons();
            setupButtonText(_btnSettle, "结束本回合");
            hidePreviewText();
            initSlots();
            initProcessArea();
            initPotArea();
            initPotVisual();
        }

        // 获取拖拽层
        public Transform GetDragRoot()
        {
            return _dragRoot == null ? transform : _dragRoot;
        }

        // 获取当前界面字体
        public TMP_FontAsset GetFontAsset()
        {
            if (_txtTip != null && _txtTip.font != null)
                return _txtTip.font;

            return _txtOrder == null ? null : _txtOrder.font;
        }

        // 根据烹饪模型刷新界面
        public void Refresh(CookModel cookModel)
        {
            if (cookModel == null) return;

            _cookModel = cookModel;
            refreshTop(cookModel);
            refreshTarget(cookModel);
            refreshSlots(cookModel);
            refreshHand(cookModel);
            refreshProcessedMaterials(cookModel);
            refreshActions(cookModel);
        }

        // 尝试将材料放入法阵槽位
        public bool TryPlaceMaterial(CookMaterialItem materialItem, int slotIndex)
        {
            if (materialItem == null) return false;
            if (!canPlaceMaterial(materialItem.MaterialId, slotIndex)) return false;

            ApplyFunc(EventDefines.CookPlaceMaterial, materialItem.MaterialId, slotIndex);
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

        // 尝试将法阵槽位材料提交到锅中
        public bool TrySubmitSlotToPot(int slotIndex)
        {
            if (_cookModel == null || !_cookModel.IsRunActive) return false;
            if (slotIndex < 0 || slotIndex >= _cookModel.Slots.Count) return false;
            if (!_cookModel.Slots[slotIndex].HasMaterial) return false;

            ApplyFunc(EventDefines.CookSubmitToPot, slotIndex);
            return true;
        }

        // 尝试加工材料
        public bool TryProcessMaterial(CookMaterialItem materialItem)
        {
            if (materialItem == null) return false;
            if (!canProcessMaterial(materialItem.MaterialId)) return false;

            ApplyFunc(EventDefines.CookProcessMaterial, materialItem.MaterialId);
            return true;
        }

        private void bindButtons()
        {
            bindButton(_btnUndo, () => ApplyFunc(EventDefines.CookUndoMaterial));
            bindButton(_btnClear, () => ApplyFunc(EventDefines.CookClearMaterials));
            bindButton(_btnSkip, () => ApplyFunc(EventDefines.CookSkipTurn));
            bindButton(_btnSettle, () => ApplyFunc(EventDefines.CookSettleTurn));
            bindButton(_btnMagicBox, () => ApplyFunc(EventDefines.CookTouchMagicBox));
        }

        private void initSlots()
        {
            if (_slotRoot == null) return;

            for (int i = 0; i < _slotItems.Length; i++)
            {
                Transform slotTf = _slotRoot.Find($"Slot_{i}");
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

        // 初始化锅区域拖拽接收组件
        private void initPotArea()
        {
            if (_potArea == null) return;

            CookPotAreaItem potAreaItem = _potArea.GetComponent<CookPotAreaItem>();
            if (potAreaItem == null)
                potAreaItem = _potArea.gameObject.AddComponent<CookPotAreaItem>();

            potAreaItem.Init(this);
        }

        // 初始化锅的临时视觉占位
        private void initPotVisual()
        {
            if (_potArea == null) return;

            Transform potBodyTf = _potArea.Find("Img_PotBody");
            if (potBodyTf == null)
            {
                GameObject potBodyObj = new GameObject("Img_PotBody", typeof(RectTransform));
                potBodyObj.transform.SetParent(_potArea, false);
                potBodyTf = potBodyObj.transform;
            }

            _imgPotBody = potBodyTf.GetComponent<Image>();
            if (_imgPotBody == null)
                _imgPotBody = potBodyTf.gameObject.AddComponent<Image>();

            _imgPotBody.color = new Color(0.95f, 0.5f, 0.16f, 0.9f);
            _imgPotBody.raycastTarget = false;

            if (potBodyTf is RectTransform rectTransform)
            {
                rectTransform.anchorMin = new Vector2(0.16f, 0.18f);
                rectTransform.anchorMax = new Vector2(0.84f, 0.78f);
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }

            _imgPotBody.transform.SetAsFirstSibling();
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
                float denominator = Mathf.Max(1f, cookModel.TargetMax + 4f);
                _imgHeatFill.fillAmount = Mathf.Clamp01(cookModel.PreviewValue / denominator);
                _imgHeatFill.color = cookModel.IsOverHeatRisk
                    ? new Color(0.92f, 0.23f, 0.16f, 1f)
                    : new Color(0.98f, 0.62f, 0.22f, 1f);
            }
        }

        private void refreshSlots(CookModel cookModel)
        {
            for (int i = 0; i < _slotItems.Length && i < cookModel.Slots.Count; i++)
            {
                if (_slotItems[i] != null)
                    _slotItems[i].Bind(cookModel.Slots[i]);
            }
        }

        private void refreshHand(CookModel cookModel)
        {
            clearDragItems();
            clearHand();
            if (_handContent == null) return;

            for (int i = 0; i < cookModel.HandMaterials.Count; i++)
            {
                CookMaterialData materialData = cookModel.HandMaterials[i];
                GameObject itemObj = new GameObject($"Material_{materialData.RuntimeId}", typeof(RectTransform));
                itemObj.transform.SetParent(_handContent, false);

                CookMaterialItem item = itemObj.AddComponent<CookMaterialItem>();
                item.Bind(materialData, this);
            }

            if (_handContent is RectTransform contentRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
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
        }

        private void clearHand()
        {
            if (_handContent == null) return;

            for (int i = _handContent.childCount - 1; i >= 0; i--)
                Destroy(_handContent.GetChild(i).gameObject);
        }

        // 清空研磨器出口材料 UI
        private void clearProcessedMaterials()
        {
            if (_processedContent == null) return;

            for (int i = _processedContent.childCount - 1; i >= 0; i--)
                Destroy(_processedContent.GetChild(i).gameObject);
        }

        private void clearDragItems()
        {
            if (_dragRoot == null) return;

            for (int i = _dragRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _dragRoot.GetChild(i);
                if (child.GetComponent<CookMaterialItem>() != null)
                    Destroy(child.gameObject);
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

        private bool canProcessMaterial(int materialId)
        {
            if (_cookModel == null || !_cookModel.IsRunActive) return false;

            for (int i = 0; i < _cookModel.HandMaterials.Count; i++)
            {
                CookMaterialData materialData = _cookModel.HandMaterials[i];
                if (materialData.RuntimeId != materialId) continue;

                return materialData.CanProcess && !materialData.IsProcessed;
            }

            return false;
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
    }
}
