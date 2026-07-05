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
using Module.MaterialPick;
using MVC;
using MVC.Controller;
using MVC.View;

namespace Module.Blackjack
{
    public class BlackjackController : BaseController
    {
        private const float DefaultBustDebuff = 5f;
        private const string SfxMagicDebuff = "sfx_magic_debuff";

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
            RegisterFunc(EventDefines.BlackjackRestart, onRestart);
            RegisterFunc(EventDefines.BlackjackReturn, onReturn);
            RegisterFunc(EventDefines.BlackjackGmAddPoint, onGmAddPoint);
            RegisterFunc(EventDefines.BlackjackGmCheckBust, onGmCheckBust);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.OpenBlackjackView, onOpen);
            UnRegisterFunc(EventDefines.BlackjackUseItemSlot, onUseItemSlot);
            UnRegisterFunc(EventDefines.BlackjackRestart, onRestart);
            UnRegisterFunc(EventDefines.BlackjackReturn, onReturn);
            UnRegisterFunc(EventDefines.BlackjackGmAddPoint, onGmAddPoint);
            UnRegisterFunc(EventDefines.BlackjackGmCheckBust, onGmCheckBust);
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

            // 不可叠加 Buff 重复获得时仍允许翻牌，仅跳过再次生效（如兔脚重抽同槽）
            bool grantedNew = MagicBoxBuffManager.GrantBuff(buff.id);
            if (grantedNew)
                applyCookBuffEffects(buff);

            if (buff.effectType == MagicBoxBuffManager.EffectPickMaterialReward)
            {
                if (!grantedNew)
                {
                    drawFromSlot(slotIndex);
                    return;
                }

                _pendingMaterialSlot = slotIndex;
                beginMaterialPick(buff);
                return;
            }

            drawFromSlot(slotIndex);
        }

        private void onGmAddPoint(object[] args)
        {
            if (!GameApp.ViewManager.IsOpen((int)ViewType.BlackjackView)) return;

            float delta = 1f;
            if (args != null && args.Length > 0)
            {
                if (args[0] is float f) delta = f;
                else if (args[0] is int i) delta = i;
                else if (args[0] is double d) delta = (float)d;
            }

            _model.GmAddTotalPoint(delta);
            refreshView();
        }

        private void onGmCheckBust(object[] args)
        {
            if (!GameApp.ViewManager.IsOpen((int)ViewType.BlackjackView)) return;
            onDrawFlipFinished();
        }

        private void onMaterialPickComplete(int pickIndex)
        {
            if (_phase != SessionPhase.MaterialPick) return;
            if (pickIndex < 0 || pickIndex >= _materialCandidates.Count) return;

            MaterialJsonData material = _materialCandidates[pickIndex];
            if (material == null) return;

            int drawSlot = _pendingMaterialSlot;
            _pendingMaterialSlot = -1;
            _phase = SessionPhase.PlayItem;

            BlackjackView view = getBlackjackView();
            view?.RestorePlaySlotMode();
            view?.SetInteractionLocked(true);

            if (GameApp.ControllerManager.GetControllerModel((int)ControllerType.Cook) is CookModel cookModel)
                cookModel.TryGrantMaterialFromCatalog(material.id);

            MaterialJsonData picked = material;
            ConfirmController.Show(new ConfirmModel
            {
                mode = ConfirmModel.Mode.ConfirmOnly,
                title = "幸运三选一",
                message = $"已获得材料：{picked.name}",
                confirmText = "继续",
                onConfirm = () => drawFromSlot(drawSlot)
            });
        }

        private void drawFromSlot(int slotIndex)
        {
            if (!_model.TryDrawFromSlot(slotIndex, out int cardIndex) || cardIndex < 0)
                return;

            BlackjackView view = getBlackjackView();
            if (view == null) return;

            view.MarkSlotUsed(slotIndex);
            float point = _model.GetRevealedPoint(cardIndex);
            string faceKey = _model.GetFaceSpriteKey(cardIndex);
            view.PlayCardFlipReveal(cardIndex, point, slotIndex, faceKey, onDrawFlipFinished);
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
            GameApp.SoundManager?.PlayEffect(SfxMagicDebuff, UnityEngine.Vector3.zero);

            float debuff = DefaultBustDebuff * MagicBoxBuffManager.GetBlackjackBustPenaltyMultiplier();
            string guardText = debuff < DefaultBustDebuff ? "\n（天使底护：惩罚减半）" : string.Empty;

            ConfirmController.Show(new ConfirmModel
            {
                mode = ConfirmModel.Mode.ConfirmOnly,
                title = "爆牌！",
                message = $"累计 {BlackjackModel.FormatPoint(_model.TotalPoint)} 点，达到/超过 {_model.EffectiveBustLimit} 点。\n恶魔风险 +{debuff:0.#}{guardText}",
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
                message = $"全部牌翻完，累计 {BlackjackModel.FormatPoint(_model.TotalPoint)} 点，未爆牌。",
                confirmText = "收手",
                onConfirm = () => returnToCookView()
            });
        }

        private void returnToCookView()
        {
            ItemPassiveManager.EndMagicBoxSession();
            MagicBoxBuffManager.EndMagicBoxSession();
            GameApp.ViewManager.Close((int)ViewType.BlackjackView);

            // CookView 未关闭则只刷新状态，避免 Open 清空手牌 diff 导致重新发牌
            if (GameApp.ViewManager.IsOpen((int)ViewType.CookView))
            {
                CookView cookView = GameApp.ViewManager.GetView<CookView>(ViewType.CookView);
                if (GameApp.ControllerManager.GetControllerModel((int)ControllerType.Cook) is CookModel cookModel)
                    cookView?.Refresh(cookModel);
                return;
            }

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

            getBlackjackView()?.SetInteractionLocked(true);

            MaterialPickController.Show(new MaterialPickModel
            {
                title = "幸运三选一",
                candidates = _materialCandidates,
                onPicked = onMaterialPickComplete
            });
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

        private static void applyCookBuffEffects(MagicBoxBuffJsonData buff)
        {
            if (buff == null) return;
            if (GameApp.ControllerManager.GetControllerModel((int)ControllerType.Cook) is not CookModel cookModel)
                return;

            if (buff.effectType == MagicBoxBuffManager.EffectAddRoundScoreFlat && buff.roundScoreFlatBonus != 0f)
                cookModel.AddImmediateScore(buff.roundScoreFlatBonus);

            if (!GameApp.ViewManager.IsOpen((int)ViewType.CookView)) return;

            CookView cookView = GameApp.ViewManager.GetView<CookView>(ViewType.CookView);
            cookView?.Refresh(cookModel);
        }
    }
}
