/*
* ┌──────────────────────────────────┐
* │  描    述: 二次确认弹窗视图
* │  类    名: ConfirmView.cs
* └──────────────────────────────────┘
*/

using Common.Defines;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Module.Confirm
{
    public class ConfirmView : BaseView
    {
        private const string HitCancelName = "RuntimeHitCancel";
        private const string HitConfirmName = "RuntimeHitConfirm";

        private GameObject _goTitle;
        private TextMeshProUGUI _txtTitle;
        private TextMeshProUGUI _txtMessage;
        private Button _btnConfirm;
        private TextMeshProUGUI _txtConfirm;
        private Button _btnCancel;
        private TextMeshProUGUI _txtCancel;
        private Button _btnBlocker;
        private Transform _buttonGroup;

        private Button _hitCancel;
        private Button _hitConfirm;

        private bool _handlingChoice;

        public override void InitUI()
        {
            _goTitle     = Find("Window/Text_Title");
            _txtTitle    = Find<TextMeshProUGUI>("Window/Text_Title");
            _txtMessage  = Find<TextMeshProUGUI>("Window/Text_Message");
            _btnConfirm  = Find<Button>("Window/ButtonGroup/Btn_Confirm");
            _txtConfirm  = Find<TextMeshProUGUI>("Window/ButtonGroup/Btn_Confirm/Text");
            _btnCancel   = Find<Button>("Window/ButtonGroup/Btn_Cancel");
            _txtCancel   = Find<TextMeshProUGUI>("Window/ButtonGroup/Btn_Cancel/Text");
            _btnBlocker  = Find<Button>("Blocker");
            _buttonGroup = Find<Transform>("Window/ButtonGroup");

            ensureBlockerButton();
            setupChoiceHitLayer();
        }

        public override void InitData()
        {
            base.InitData();
            bindButtonHandlers();
        }

        public override void Open(params object[] args)
        {
            SetVisible(true);
            _handlingChoice = false;
            bindButtonHandlers();
            setButtonsInteractable(true);
            if (args == null || args.Length == 0 || args[0] is not ConfirmModel model) return;
            refresh(model);
        }

        private void ensureBlockerButton()
        {
            if (_btnBlocker != null) return;

            Transform blockerTf = transform.Find("Blocker");
            if (blockerTf == null) return;

            _btnBlocker = blockerTf.GetComponent<Button>();
            if (_btnBlocker == null)
                _btnBlocker = blockerTf.gameObject.AddComponent<Button>();

            Image blockerImage = blockerTf.GetComponent<Image>();
            if (blockerImage != null)
                _btnBlocker.targetGraphic = blockerImage;
        }

        // prefab 按钮 scale=0.2、size 很大，且 TMP 开了 raycast；Confirm 层级更高，点取消文字会误触购买
        private void setupChoiceHitLayer()
        {
            stripPrefabRaycasts();
            removeLegacyHitTargets(_btnCancel);
            removeLegacyHitTargets(_btnConfirm);

            if (_buttonGroup == null) return;

            // 与 prefab Btn_Cancel / Btn_Confirm 的 anchoredPosition 对齐（ButtonGroup 本地坐标）
            _hitCancel  = ensureGroupHitButton(HitCancelName,  new Vector2(148.3f, -18f));
            _hitConfirm = ensureGroupHitButton(HitConfirmName, new Vector2(344.9f, -18f));
        }

        private void stripPrefabRaycasts()
        {
            disableButtonRaycast(_btnCancel);
            disableButtonRaycast(_btnConfirm);
            disableTextRaycast(_txtTitle);
            disableTextRaycast(_txtMessage);
            disableTextRaycast(_txtCancel);
            disableTextRaycast(_txtConfirm);
        }

        private static void disableButtonRaycast(Button button)
        {
            if (button == null) return;

            button.onClick.RemoveAllListeners();
            if (button.TryGetComponent(out Image image))
                image.raycastTarget = false;
        }

        private static void disableTextRaycast(TextMeshProUGUI text)
        {
            if (text != null)
                text.raycastTarget = false;
        }

        private static void removeLegacyHitTargets(Button owner)
        {
            if (owner == null) return;

            Transform legacy = owner.transform.Find("RuntimeHitTarget");
            if (legacy != null)
                Destroy(legacy.gameObject);
        }

        private Button ensureGroupHitButton(string name, Vector2 anchoredPosition)
        {
            Transform existing = _buttonGroup.Find(name);
            if (existing != null)
                return existing.GetComponent<Button>();

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Transform tf = go.transform;
            tf.SetParent(_buttonGroup, false);

            var rt = (RectTransform)tf;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = new Vector2(140f, 44f);

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            return button;
        }

        private void bindButtonHandlers()
        {
            ensureBlockerButton();
            bindHitButton(_hitCancel, onCancelClick);
            bindHitButton(_hitConfirm, onConfirmClick);
            bindHitButton(_btnBlocker, onCancelClick);
        }

        private static void bindHitButton(Button button, UnityAction handler)
        {
            if (button == null) return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(handler);
        }

        private void refresh(ConfirmModel model)
        {
            bool hasTitle = !string.IsNullOrEmpty(model.title);
            _goTitle?.SetActive(hasTitle);
            if (hasTitle && _txtTitle != null)
                _txtTitle.text = model.title;

            if (_txtMessage != null)
                _txtMessage.text = model.message ?? string.Empty;

            if (_txtConfirm != null)
                _txtConfirm.text = model.confirmText;

            bool showCancel = model.mode == ConfirmModel.Mode.ConfirmCancel;
            _btnCancel?.gameObject.SetActive(showCancel);
            if (_hitCancel != null)
                _hitCancel.gameObject.SetActive(showCancel);
            if (_btnBlocker != null)
                _btnBlocker.interactable = showCancel;
            if (showCancel && _txtCancel != null)
                _txtCancel.text = model.cancelText;

            if (_hitConfirm != null)
                _hitConfirm.gameObject.SetActive(true);
        }

        private void onConfirmClick()
        {
            if (_handlingChoice) return;

            _handlingChoice = true;
            setButtonsInteractable(false);
            ApplyFunc(EventDefines.ConfirmViewConfirm);
        }

        private void onCancelClick()
        {
            if (_handlingChoice) return;

            _handlingChoice = true;
            setButtonsInteractable(false);
            ApplyFunc(EventDefines.ConfirmViewCancel);
        }

        private void setButtonsInteractable(bool interactable)
        {
            if (_hitConfirm != null) _hitConfirm.interactable = interactable;
            if (_hitCancel != null) _hitCancel.interactable = interactable;
            if (_btnBlocker != null) _btnBlocker.interactable = interactable;
        }
    }
}
