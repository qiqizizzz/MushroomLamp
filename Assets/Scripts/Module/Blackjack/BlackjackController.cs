/*
* ┌──────────────────────────────────┐
* │  描    述: 21 点玩法控制器
* │  类    名: BlackjackController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Common.Defines;
using Module.Confirm;
using Module.Cook;
using Module.Item;
using Module.MagicBoxBuff;
using Module.Material;
using MVC;
using MVC.Controller;
using MVC.View;

namespace Module.Blackjack
{
    public class BlackjackController : BaseController
    {
        private const float DefaultBustDebuff = 5f;

        private enum SessionPhase
        {
            Intro,
            PlayItem,
            MaterialPick,
        }

        private BlackjackModel _model;
        private readonly BlackjackDialogSession _dialogSession = new();
        private SessionPhase _phase = SessionPhase.Intro;
        private List<MagicBoxBuffJsonData> _slotBuffs = new();
        private List<MaterialJsonData> _materialCandidates = new();
        private int _pendingMaterialSlot = -1;

        public BlackjackController()
        {
            _model = new BlackjackModel();
            SetModel(_model);

            GameApp.ViewManager.Register((int)ViewType.BlackjackView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_BlackjackView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 20,
                IsOverlay = true
            });

            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.OpenBlackjackView, onOpen);
            RegisterFunc(EventDefines.BlackjackUseItemSlot, onUseItemSlot);
            RegisterFunc(EventDefines.BlackjackPickMaterial, onPickMaterial);
            RegisterFunc(EventDefines.BlackjackRestart, onRestart);
            RegisterFunc(EventDefines.BlackjackReturn, onReturn);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenBlackjackView, onOpen);
            UnRegisterFunc(EventDefines.BlackjackUseItemSlot, onUseItemSlot);
            UnRegisterFunc(EventDefines.BlackjackPickMaterial, onPickMaterial);
            UnRegisterFunc(EventDefines.BlackjackRestart, onRestart);
            UnRegisterFunc(EventDefines.BlackjackReturn, onReturn);
        }

        private void onOpen(object[] args)
        {
            _dialogSession.Reset();
            _pendingMaterialSlot = -1;
            GameApp.ViewManager.Open((int)ViewType.BlackjackView, args);
            beginSession();
        }

        private void onRestart(object[] args)
        {
            _dialogSession.Reset();
            _pendingMaterialSlot = -1;
            beginSession();
        }

        private void onReturn(object[] args)
        {
            returnToCookView();
        }

        // 每个 Item 对应一个 Buff：点击即获得该 Buff 并翻对应小牌
        private void onUseItemSlot(object[] args)
        {
            if (_phase != SessionPhase.PlayItem) return;

            int slotIndex = resolveItemSlotIndex(args);
            if (!_model.IsItemSlotAvailable(slotIndex)) return;
            if (slotIndex < 0 || slotIndex >= _slotBuffs.Count) return;

            MagicBoxBuffJsonData buff = _slotBuffs[slotIndex];
            if (buff == null) return;

            MagicBoxBuffManager.GrantBuff(buff.id);

            if (buff.effectType == MagicBoxBuffManager.EffectPickMaterialReward)
            {
                _pendingMaterialSlot = slotIndex;
                beginMaterialPick(buff);
                return;
            }

            drawFromSlot(slotIndex);
        }

        private void onPickMaterial(object[] args)
        {
            if (_phase != SessionPhase.MaterialPick) return;

            int slotIndex = resolveItemSlotIndex(args);
            if (slotIndex < 0 || slotIndex >= _materialCandidates.Count) return;

            MaterialJsonData material = _materialCandidates[slotIndex];
            if (material == null) return;

            if (GameApp.ControllerManager.GetControllerModel((int)ControllerType.Cook) is CookModel cookModel)
                cookModel.TryGrantMaterialFromCatalog(material.id);

            int drawSlot = _pendingMaterialSlot;
            _pendingMaterialSlot = -1;
            _phase = SessionPhase.PlayItem;

            BlackjackView view = getBlackjackView();
            view?.RestorePlaySlotMode();
            drawFromSlot(drawSlot, showMaterialConfirm: material);
        }

        private void drawFromSlot(int slotIndex, MaterialJsonData showMaterialConfirm = null)
        {
            if (!_model.TryDrawFromSlot(slotIndex, out int cardIndex) || cardIndex < 0)
                return;

            BlackjackView view = getBlackjackView();
            if (view == null) return;

            view.MarkSlotUsed(slotIndex);
            int point = _model.GetRevealedPoint(cardIndex);
            string faceKey = _model.GetFaceSpriteKey(cardIndex);
            view.PlayCardFlipReveal(cardIndex, point, slotIndex, faceKey, onDrawFlipFinished);

            if (showMaterialConfirm == null) return;

            ConfirmController.Show(new ConfirmModel
            {
                mode = ConfirmModel.Mode.ConfirmOnly,
                title = "幸运三选一",
                message = $"已获得材料：{showMaterialConfirm.name}",
                confirmText = "继续"
            });
        }

        private void onDrawFlipFinished()
        {
            refreshView();

            if (_model.IsBusted)
            {
                if (ItemPassiveManager.TryConsumeRabbitFootReroll() && _model.UndoLastReveal())
                {
                    getBlackjackView()?.MarkSlotAvailable(_model.LastUndoneItemSlot);
                    refreshView();
                    ConfirmController.Show(new ConfirmModel
                    {
                        mode = ConfirmModel.Mode.ConfirmOnly,
                        title = "幸运兔脚",
                        message = "首次凑出 21 点，已为你重抽这张牌。\n请再选一张牌试试手气。",
                        confirmText = "继续"
                    });
                    return;
                }

                showBustResult();
            }
            else if (_model.AllRevealed)
            {
                showSafeResult();
            }
        }

        private void showBustResult()
        {
            float debuff = DefaultBustDebuff * MagicBoxBuffManager.GetBlackjackBustPenaltyMultiplier();
            string guardText = debuff < DefaultBustDebuff ? "\n（天使底护：惩罚减半）" : string.Empty;

            ConfirmController.Show(new ConfirmModel
            {
                mode = ConfirmModel.Mode.ConfirmOnly,
                title = "爆牌！",
                message = $"累计 {_model.TotalPoint} 点，达到/超过 {_model.EffectiveBustLimit} 点。\n恶魔风险 +{debuff:0.#}{guardText}",
                confirmText = "认栽",
                onConfirm = () =>
                {
                    if (GameApp.ControllerManager.GetControllerModel((int)ControllerType.Cook) is CookModel cookModel)
                        cookModel.AddDevil(debuff);
                    returnToCookView();
                }
            });
        }

        private void showSafeResult()
        {
            ConfirmController.Show(new ConfirmModel
            {
                mode = ConfirmModel.Mode.ConfirmOnly,
                title = "安全过关",
                message = $"全部牌翻完，累计 {_model.TotalPoint} 点，未爆牌。",
                confirmText = "收手",
                onConfirm = () => returnToCookView()
            });
        }

        private void returnToCookView()
        {
            ItemPassiveManager.EndMagicBoxSession();
            MagicBoxBuffManager.EndMagicBoxSession();
            GameApp.ViewManager.Close((int)ViewType.BlackjackView);
            ApplyControllerFunc(ControllerType.Cook, EventDefines.OpenCookView);
        }

        private void refreshView()
        {
            var view = getBlackjackView();
            if (view == null) return;

            view.RefreshGameplay(_model);

            if (!_dialogSession.DialogEnabled) return;

            _dialogSession.Refresh(_model, buildDialogContext());
            view.RefreshDialog(_dialogSession);
        }

        private void beginSession()
        {
            BlackjackView view = getBlackjackView();
            if (view == null) return;

            _phase = SessionPhase.Intro;
            _model.Reset(view.GetItemSlotCount());
            view.BeginSession(_model, onIntroFinished);
        }

        private void onIntroFinished()
        {
            _dialogSession.SetDialogEnabled(true);
            beginPlayItems();
        }

        private void beginPlayItems()
        {
            _phase = SessionPhase.PlayItem;
            _slotBuffs = MagicBoxBuffPicker.RollCandidates(_model.ItemSlotCount);

            BlackjackView view = getBlackjackView();
            view?.SetupSlotBuffs(_slotBuffs);
            refreshView();
        }

        private void beginMaterialPick(MagicBoxBuffJsonData buff)
        {
            _phase = SessionPhase.MaterialPick;
            _materialCandidates = MagicBoxBuffManager.RollMaterialRewardCandidates(buff);

            BlackjackView view = getBlackjackView();
            view?.SetupMaterialPick(_materialCandidates);
            refreshView();
        }

        private BlackjackView getBlackjackView()
        {
            return GameApp.ViewManager.GetView((int)ViewType.BlackjackView) as BlackjackView;
        }

        private static BlackjackDialogContext buildDialogContext()
        {
            if (GameApp.ControllerManager.GetControllerModel((int)ControllerType.Cook) is CookModel cookModel)
                return new BlackjackDialogContext(cookModel.CurrentScore, cookModel.TargetMin);

            return BlackjackDialogContext.Empty;
        }

        private static int resolveItemSlotIndex(object[] args)
        {
            if (args == null || args.Length == 0) return 0;
            return args[0] is int index ? index : 0;
        }
    }
}
