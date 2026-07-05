/*
* ┌──────────────────────────────────┐
* │  描    述: 制作人名单界面，负责名单自动滚动与结束后返回
* │  类    名: AlmanacView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections;
using Common.Defines;
using MVC.View;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Almanac
{
    // 制作人名单界面，进入后自动滚动并在播放完成前锁定返回
    public class AlmanacView : BaseView
    {
        // ==================== 常量与静态字段 ====================
        private const float FINISH_EPSILON = 0.5f;

        // ==================== 字段[外部设置] ====================
        [Header("滚动设置")]
        [Tooltip("制作人名单向上滚动速度")]
        [SerializeField, Min(1f)] private float ScrollSpeed = 85f;

        [Tooltip("名单完全离开视口后的额外滚动距离")]
        [SerializeField, Min(0f)] private float FinishPadding = 120f;

        // ==================== 字段[私有] ====================
        private Button _btnBack;
        private RectTransform _creditsViewport;
        private RectTransform _creditsContent;
        private GameObject _lockedOverlay;
        private Coroutine _scrollCoroutine;
        private bool _isScrollFinished;

        // ==================== Public Function ====================
        public override void InitUI()
        {
            _btnBack = Find<Button>("Btn_Back");
            _creditsViewport = Find<RectTransform>("CreditsViewport");
            _creditsContent = Find<RectTransform>("CreditsViewport/CreditsContent");
            _lockedOverlay = transform.Find("Btn_Back/LockedOverlay")?.gameObject;
        }

        public override void InitData()
        {
            base.InitData();
            _btnBack.onClick.RemoveAllListeners();
            _btnBack.onClick.AddListener(tryReturn);
        }

        // 打开界面时从头播放制作人名单
        public override void Open(params object[] args)
        {
            startCreditsScroll();
        }

        public override void Close(params object[] args)
        {
            stopCreditsScroll();
            base.Close(args);
        }

        // ==================== Private Function ====================
        // 从视口下方重置内容并启动自动滚动
        private void startCreditsScroll()
        {
            stopCreditsScroll();
            _isScrollFinished = false;
            setReturnEnabled(false);
            _scrollCoroutine = StartCoroutine(playCreditsCoroutine());
        }

        // 停止当前滚动协程
        private void stopCreditsScroll()
        {
            if (_scrollCoroutine == null) return;

            StopCoroutine(_scrollCoroutine);
            _scrollCoroutine = null;
        }

        // 名单未滚动结束时屏蔽返回操作
        private void tryReturn()
        {
            if (!_isScrollFinished) return;

            ApplyFunc(EventDefines.AlmanacReturn);
        }

        // 切换返回按钮可点击状态与锁定提示
        private void setReturnEnabled(bool isEnabled)
        {
            _btnBack.interactable = isEnabled;

            if (_lockedOverlay != null)
                _lockedOverlay.SetActive(!isEnabled);
        }

        // 刷新自动布局，确保新增姓名后也能正确计算滚动距离
        private float rebuildAndGetContentHeight()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_creditsContent);

            float preferredHeight = LayoutUtility.GetPreferredHeight(_creditsContent);
            float contentHeight = Mathf.Max(_creditsContent.rect.height, preferredHeight);
            _creditsContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            return contentHeight;
        }

        // ==================== Coroutine ====================
        // 播放制作人名单滚动，内容完全离开后解锁返回
        private IEnumerator playCreditsCoroutine()
        {
            yield return null;

            float viewportHeight = _creditsViewport.rect.height;
            float contentHeight = rebuildAndGetContentHeight();
            float startY = -viewportHeight;
            float targetY = contentHeight + FinishPadding;

            _creditsContent.anchoredPosition = new Vector2(_creditsContent.anchoredPosition.x, startY);

            while (_creditsContent.anchoredPosition.y < targetY - FINISH_EPSILON)
            {
                float nextY = Mathf.MoveTowards(
                    _creditsContent.anchoredPosition.y,
                    targetY,
                    ScrollSpeed * Time.unscaledDeltaTime);

                _creditsContent.anchoredPosition = new Vector2(_creditsContent.anchoredPosition.x, nextY);
                yield return null;
            }

            _creditsContent.anchoredPosition = new Vector2(_creditsContent.anchoredPosition.x, targetY);
            _isScrollFinished = true;
            _scrollCoroutine = null;
            setReturnEnabled(true);
        }
    }
}
