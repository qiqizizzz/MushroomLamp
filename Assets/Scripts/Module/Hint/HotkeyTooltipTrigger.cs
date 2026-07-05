using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Module.Hint
{
    // 挂载在可悬停的 UI 上，悬停后按 K 显示对应提示
    [DisallowMultipleComponent]
    public class HotkeyTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string _hintId;

        public string HintId => _hintId;

        private void Awake()
        {
            ensureRaycastTarget();
        }

        private void OnEnable()
        {
            ensureRaycastTarget();
            HotkeyTooltipService.EnsureRunning();
        }

        private void ensureRaycastTarget()
        {
            Graphic graphic = GetComponent<Graphic>();
            if (graphic == null) return;

            if (!graphic.raycastTarget)
                graphic.raycastTarget = true;
        }

        private void OnDisable()
        {
            HotkeyTooltipService.NotifyTriggerDisabled(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            HotkeyTooltipService.NotifyHoverEnter(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HotkeyTooltipService.NotifyHoverExit(this);
        }
    }
}
