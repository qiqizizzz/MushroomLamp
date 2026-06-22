/*
* ┌──────────────────────────────────┐
* │  描    述: 加载界面控制器
* │  类    名: LoadingController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Common.Defines;
using Module.View;
using MVC;
using MVC.Controller;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Module.Loading
{
    public class LoadingController : BaseController
    {
        private readonly float _delayTime = 0.1f;
        private AsyncOperation _asyncOp;

        public LoadingController()
        {
            GameApp.ViewManager.Register(ViewType.LoadingView, new ViewInfo
            {
                PrefabName = AddressDefines.UI_LoadingView,
                parentTf = GameApp.ViewManager.canvasTf,
                controller = this,
                Sorting_Order = 999
            });

            InitModuleEvent();
        }

        public override void InitModuleEvent()
        {
            RegisterFunc(EventDefines.LoadingScene, loadSceneCallback);
        }

        public override void RemoveModuleEvent()
        {
            UnRegisterFunc(EventDefines.LoadingScene, loadSceneCallback);
        }

        // 加载场景回调
        private void loadSceneCallback(object[] args)
        {
            if (args == null || args.Length == 0) return;

            LoadingModel model = args[0] as LoadingModel;
            if (model == null)
                return;

            SetModel(model);
            GameApp.ViewManager.Open(ViewType.LoadingView);
            _asyncOp = SceneManager.LoadSceneAsync(model.SceneName);

            if (_asyncOp == null) return;

            syncProcess();
            _asyncOp.completed += onLoadEndCallback;
        }

        // 加载后回调
        private void onLoadEndCallback(AsyncOperation op)
        {
            if (_asyncOp != null)
                _asyncOp.completed -= onLoadEndCallback;

            GameApp.TimerManager.Register(_delayTime, invokeLoadComplete);
        }

        // 执行加载完成回调
        private void invokeLoadComplete()
        {
            GetModel<LoadingModel>()?.Callback?.Invoke();
            GameApp.ViewManager.Close(ViewType.LoadingView);
        }

        // 同步进度到加载视图
        private void syncProcess()
        {
            LoadingView view = GameApp.ViewManager.GetView<LoadingView>(ViewType.LoadingView);
            view?.InitLoading(_asyncOp);
        }
    }
}
