using Common;
using UnityEngine;

namespace Module.Hint
{
    public static class HotkeyTooltipService
    {
        internal const string HintTooltipPath = "UI/Common/HintTooltip";
        internal static readonly Vector2 TooltipOffset = new Vector2(18f, -18f);

        private static HotkeyTooltipRuntime _runtime;

        public static void EnsureRunning()
        {
            if (_runtime != null) return;

            GameObject hostObj = new GameObject(nameof(HotkeyTooltipRuntime));
            if (GameApp.RootTf != null)
                hostObj.transform.SetParent(GameApp.RootTf, false);

            _runtime = hostObj.AddComponent<HotkeyTooltipRuntime>();
        }

        internal static void NotifyHoverEnter(HotkeyTooltipTrigger trigger)
        {
            EnsureRunning();
            _runtime?.SetHoveredTrigger(trigger);
        }

        internal static void NotifyHoverExit(HotkeyTooltipTrigger trigger)
        {
            _runtime?.ClearHoveredTrigger(trigger);
        }

        internal static void NotifyTriggerDisabled(HotkeyTooltipTrigger trigger)
        {
            _runtime?.HandleTriggerDisabled(trigger);
        }
    }

    internal class HotkeyTooltipRuntime : MonoBehaviour
    {
        private HotkeyTooltipTrigger _hoveredTrigger;
        private HintTooltip _hintTooltip;
        private RectTransform _canvasRect;
        private Vector2 _visibleMousePosition;
        private bool _isVisible;

        private void Update()
        {
            if (_isVisible)
            {
                Vector2 mousePosition = Input.mousePosition;
                if (mousePosition != _visibleMousePosition)
                {
                    hideTooltip();
                    return;
                }
            }

            if (!Input.GetKeyDown(KeyCode.K)) return;

            if (_isVisible)
            {
                hideTooltip();
                return;
            }

            if (_hoveredTrigger == null || string.IsNullOrWhiteSpace(_hoveredTrigger.HintId))
                return;

            showTooltip(_hoveredTrigger.HintId);
        }

        public void SetHoveredTrigger(HotkeyTooltipTrigger trigger)
        {
            _hoveredTrigger = trigger;
        }

        public void ClearHoveredTrigger(HotkeyTooltipTrigger trigger)
        {
            if (_hoveredTrigger == trigger)
                _hoveredTrigger = null;
        }

        public void HandleTriggerDisabled(HotkeyTooltipTrigger trigger)
        {
            if (_hoveredTrigger == trigger)
                _hoveredTrigger = null;

            if (_isVisible)
                hideTooltip();
        }

        private void showTooltip(string hintId)
        {
            HintTooltipJsonData data = HintTooltipCatalogLoader.GetById(hintId);
            if (data == null)
            {
                QLog.Error($"[{nameof(HotkeyTooltipService)}] 未找到提示配置：{hintId}");
                return;
            }

            if (!ensureHintTooltip()) return;

            _hintTooltip.Bind(data);
            _hintTooltip.SetScreenPosition(Input.mousePosition, _canvasRect, HotkeyTooltipService.TooltipOffset);
            _visibleMousePosition = Input.mousePosition;
            _isVisible = true;
        }

        private void hideTooltip()
        {
            _isVisible = false;
            _hintTooltip?.Hide();
        }

        private bool ensureHintTooltip()
        {
            if (_hintTooltip != null)
                return true;

            Transform parent = GameApp.ViewManager?.canvasTf;
            if (parent == null)
                return false;

            GameObject tooltipObj = ResManager.Instantiate(HotkeyTooltipService.HintTooltipPath, parent);
            if (tooltipObj == null) return false;

            _hintTooltip = tooltipObj.GetComponent<HintTooltip>();
            if (_hintTooltip == null)
                _hintTooltip = tooltipObj.AddComponent<HintTooltip>();

            tooltipObj.name = "HintTooltip";
            tooltipObj.transform.SetAsLastSibling();
            _canvasRect = parent as RectTransform;
            if (_canvasRect == null)
                _canvasRect = tooltipObj.GetComponentInParent<Canvas>()?.transform as RectTransform;

            _hintTooltip.Hide();
            return _hintTooltip != null;
        }
    }
}
