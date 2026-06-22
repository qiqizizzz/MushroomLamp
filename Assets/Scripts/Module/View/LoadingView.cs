/*
* ┌──────────────────────────────────┐
* │  描    述: 加载界面
* │  类    名: LoadingView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using MVC.View;
using UnityEngine;

namespace Module.View
{
    public class LoadingView : BaseView
    {
        private AsyncOperation _asyncOp;

        public float Progress => _asyncOp == null ? 0f : Mathf.Clamp01(_asyncOp.progress / 0.9f);

        // 初始化加载进度数据
        public void InitLoading(AsyncOperation asyncOp)
        {
            _asyncOp = asyncOp;
        }
    }
}
