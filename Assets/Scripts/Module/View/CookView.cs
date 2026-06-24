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

        private Button _btnUndo;
        private Button _btnClear;
        private Button _btnSkip;
        private Button _btnSettle;

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

            _btnUndo = Find<Button>("Bottom/ActionBar/Btn_Undo");
            _btnClear = Find<Button>("Bottom/ActionBar/Btn_Clear");
            _btnSkip = Find<Button>("Bottom/ActionBar/Btn_Skip");
            _btnSettle = Find<Button>("Bottom/ActionBar/Btn_Settle");

            bindButtons();
            initSlots();
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

        private void bindButtons()
        {
            bindButton(_btnUndo, () => ApplyFunc(EventDefines.CookUndoMaterial));
            bindButton(_btnClear, () => ApplyFunc(EventDefines.CookClearMaterials));
            bindButton(_btnSkip, () => ApplyFunc(EventDefines.CookSkipTurn));
            bindButton(_btnSettle, () => ApplyFunc(EventDefines.CookSettleTurn));
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
                _txtOrder.text = $"{cookModel.BoxName}\n{cookModel.GetTargetText()}\n拖拽材料到法阵后投入锅中";

            if (_txtPreview != null)
                _txtPreview.text = $"预估火候\n{cookModel.PreviewValue}";

            if (_txtTip != null)
                _txtTip.text = cookModel.LastTip;

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
        }

        private void clearHand()
        {
            if (_handContent == null) return;

            for (int i = _handContent.childCount - 1; i >= 0; i--)
                Destroy(_handContent.GetChild(i).gameObject);
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
            if (slotIndex < 0 || slotIndex >= _cookModel.Slots.Count) return false;
            if (_cookModel.Slots[slotIndex].HasMaterial) return false;

            for (int i = 0; i < _cookModel.HandMaterials.Count; i++)
            {
                if (_cookModel.HandMaterials[i].RuntimeId == materialId)
                    return true;
            }

            return false;
        }

        private static void bindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null) return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }
}
