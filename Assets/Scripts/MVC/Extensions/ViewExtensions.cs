/*
* ┌──────────────────────────────────┐
* │  描    述: 视图扩展方法                      
* │  类    名: ViewExtensions.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Globalization;
using Common;
using Common.Defines;
using Module.Loading;
using MVC.View;
using UnityEngine;

namespace MVC.Extensions
{
    public static class ViewExtensions
    {
        public static void LoadScene(this IBaseView view, string sceneName , Action onComplete = null)
        {
            GameApp.ViewManager.Close(view.ViewId);
            
            LoadingModel model = new LoadingModel();
            model.SetSceneName(sceneName);
            model.Callback = () =>
            {
                QLog.Info($"[Extension] 场景 {sceneName} 加载完成");
                onComplete?.Invoke();
            };
            
            view.Controller.ApplyControllerFunc(ControllerType.Loading, EventDefines.LoadingScene, model);
        }
    }
}
