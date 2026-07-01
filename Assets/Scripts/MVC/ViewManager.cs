/*
* ┌──────────────────────────────────┐
* │  描    述: 视图管理器
* │  类    名: ViewManager.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using MVC.Controller;
using MVC.View;
using Sound;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MVC
{
    public class ViewInfo
    {
        public string PrefabName;
        public Transform parentTf;
        public BaseController controller;
        public int Sorting_Order;
        // 叠加层（如 Confirm、小局结算）：打开时不关闭其它面板
        public bool IsOverlay;
    }

    public class ViewManager
    {
        public Transform canvasTf;
        public Transform worldCanvasTf;

        private readonly Dictionary<int, IBaseView> _opens;
        private readonly Dictionary<int, IBaseView> _viewCache;
        private readonly Dictionary<int, ViewInfo> _viewInfos;
        private readonly Stack<int> _viewStack;

        public IReadOnlyDictionary<int, ViewInfo> ViewInfos => _viewInfos;

        public ViewManager()
        {
            Transform rootTf = GameApp.RootTf;
            canvasTf = getOrCreateCanvas("Canvas", RenderMode.ScreenSpaceOverlay, rootTf).transform;
            worldCanvasTf = getOrCreateCanvas("WorldCanvas", RenderMode.WorldSpace, rootTf).transform;
            ensureEventSystem(rootTf);

            _opens = new Dictionary<int, IBaseView>();
            _viewCache = new Dictionary<int, IBaseView>();
            _viewInfos = new Dictionary<int, ViewInfo>();
            _viewStack = new Stack<int>();
        }

        public bool IsOpen(int key) => _opens.ContainsKey(key);

        // 注册视图信息
        public void Register(int key, ViewInfo viewInfo)
        {
            if (viewInfo == null) return;

            if (viewInfo.parentTf == null)
                viewInfo.parentTf = canvasTf;

            if (!_viewInfos.ContainsKey(key))
                _viewInfos.Add(key, viewInfo);
            else
                _viewInfos[key] = viewInfo;
        }

        // 注册视图信息
        public void Register(ViewType type, ViewInfo viewInfo)
        {
            Register((int)type, viewInfo);
        }

        // 注销视图信息
        public void UnRegister(int key)
        {
            if (_viewInfos.ContainsKey(key))
                _viewInfos.Remove(key);
        }

        // 移除视图
        public void RemoveView(int key)
        {
            _opens.Remove(key);
            _viewCache.Remove(key);
            _viewInfos.Remove(key);
        }

        // 移除控制器关联的所有视图
        public void RemoveControllerView(BaseController ctl)
        {
            List<int> keys = _viewInfos
                .Where(item => item.Value.controller == ctl)
                .Select(item => item.Key)
                .ToList();

            foreach (int key in keys)
                RemoveView(key);
        }

        // 获取视图
        public IBaseView GetView(int key)
        {
            if (_opens.TryGetValue(key, out IBaseView openView))
                return openView;

            if (_viewCache.TryGetValue(key, out IBaseView cacheView))
                return cacheView;

            return null;
        }

        // 获取视图
        public IBaseView GetView(ViewType type)
        {
            return GetView((int)type);
        }

        // 获取指定类型视图
        public T GetView<T>(int key) where T : class, IBaseView
        {
            return GetView(key) as T;
        }

        // 获取指定类型视图
        public T GetView<T>(ViewType type) where T : class, IBaseView
        {
            return GetView<T>((int)type);
        }

        // 销毁视图
        public void DestroyView(int key)
        {
            IBaseView oldView = GetView(key);
            if (oldView == null) return;

            UnRegister(key);
            oldView.DestroyView();
            _opens.Remove(key);
            _viewCache.Remove(key);
        }

        // 关闭视图
        public void Close(int key, params object[] args)
        {
            if (!IsOpen(key)) return;

            IBaseView view = GetView(key);
            if (view == null) return;

            _opens.Remove(key);
            view.Close(args);

            if (_viewInfos.TryGetValue(key, out ViewInfo viewInfo))
                viewInfo.controller?.CloseView(view);

            if (_viewStack.Count > 0 && _viewStack.Peek() == key)
                _viewStack.Pop();
        }

        // 关闭视图
        public void Close(ViewType viewType)
        {
            Close((int)viewType);
        }

        // 关闭所有视图
        public void CloseAll()
        {
            List<IBaseView> list = _opens.Values.ToList();
            for (int i = list.Count - 1; i >= 0; i--)
                Close(list[i].ViewId);

            _viewStack.Clear();
        }

        // 打开视图（非叠加层会先关闭其它已开面板，避免多层 Canvas 叠在一起）
        public void Open(int key, params object[] args)
        {
            if (!_viewInfos.TryGetValue(key, out ViewInfo viewInfo))
            {
                QLog.Error($"[{nameof(ViewManager)}] 未注册视图：{key}");
                return;
            }

            IBaseView view = GetView(key);
            if (view == null)
                view = createView(key, viewInfo);

            if (view == null) return;
            if (_opens.ContainsKey(key)) return;

            if (!viewInfo.IsOverlay)
                closeNonOverlayViewsExcept(key);

            _opens.Add(key, view);
            _viewStack.Push(key);

            if (view.IsInit())
            {
                view.SetVisible(true);
                view.Open(args);
            }
            else
            {
                view.InitUI();
                view.InitData();
                view.Open(args);
            }

            UISoundAutoBinder.Bind(view);
            viewInfo.controller?.OpenView(view);
        }

        // 打开视图
        public void Open(ViewType viewType, params object[] args)
        {
            Open((int)viewType, args);
        }

        // 关闭除 keepKey 与叠加层以外的所有已开面板
        private void closeNonOverlayViewsExcept(int keepKey)
        {
            List<int> openKeys = _opens.Keys.ToList();
            for (int i = 0; i < openKeys.Count; i++)
            {
                int openKey = openKeys[i];
                if (openKey == keepKey) continue;

                if (_viewInfos.TryGetValue(openKey, out ViewInfo info) && info.IsOverlay)
                    continue;

                Close(openKey);
            }
        }

        // 返回上一个视图
        public void NavigateBack()
        {
            if (_viewStack.Count <= 1) return;

            int currentViewKey = _viewStack.Pop();
            if (IsOpen(currentViewKey))
                Close(currentViewKey);

            int previousViewKey = _viewStack.Peek();
            if (!IsOpen(previousViewKey))
                Open(previousViewKey);
        }

        // 创建视图实例
        private IBaseView createView(int key, ViewInfo viewInfo)
        {
            GameObject uiObj = createViewObject(key, viewInfo);
            if (uiObj == null) return null;

            Canvas canvas = uiObj.GetComponent<Canvas>();
            if (canvas == null)
                canvas = uiObj.AddComponent<Canvas>();

            if (uiObj.GetComponent<GraphicRaycaster>() == null)
                uiObj.AddComponent<GraphicRaycaster>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = viewInfo.Sorting_Order;

            string typeName = ((ViewType)key).ToString();
            Type viewType = findType(typeName);
            if (viewType == null)
            {
                QLog.Error($"[{nameof(ViewManager)}] 未找到视图脚本：{typeName}");
                UnityEngine.Object.Destroy(uiObj);
                return null;
            }

            IBaseView view = uiObj.GetComponent(viewType) as IBaseView;
            if (view == null)
                view = uiObj.AddComponent(viewType) as IBaseView;
            if (view == null)
            {
                QLog.Error($"[{nameof(ViewManager)}] 脚本未实现 IBaseView：{typeName}");
                UnityEngine.Object.Destroy(uiObj);
                return null;
            }

            view.ViewId = key;
            view.Controller = viewInfo.controller;
            _viewCache.Add(key, view);
            viewInfo.controller?.OnLoadView(view);
            return view;
        }

        // 创建视图对象
        private GameObject createViewObject(int key, ViewInfo viewInfo)
        {
            GameObject uiObj = null;
            if (!string.IsNullOrEmpty(viewInfo.PrefabName))
                uiObj = ResManager.Instantiate(viewInfo.PrefabName, viewInfo.parentTf);

            if (uiObj != null) return uiObj;

            string typeName = ((ViewType)key).ToString();
            uiObj = new GameObject(typeName, typeof(RectTransform));
            uiObj.transform.SetParent(viewInfo.parentTf, false);
            return uiObj;
        }

        // 获取或创建画布
        private Canvas getOrCreateCanvas(string canvasName, RenderMode renderMode, Transform rootTf)
        {
            GameObject canvasObj = findFrameworkChild(canvasName, rootTf);
            if (canvasObj == null)
                canvasObj = new GameObject(canvasName, typeof(RectTransform));

            parentToRoot(canvasObj.transform, rootTf);
            resetLocalTransform(canvasObj.transform);

            Canvas canvas = canvasObj.GetComponent<Canvas>();
            if (canvas == null)
                canvas = canvasObj.AddComponent<Canvas>();

            canvas.renderMode = renderMode;
            canvas.worldCamera = renderMode == RenderMode.WorldSpace ? Camera.main : null;
            canvas.overrideSorting = false;

            CanvasScaler canvasScaler = canvasObj.GetComponent<CanvasScaler>();
            if (canvasScaler == null)
                canvasScaler = canvasObj.AddComponent<CanvasScaler>();

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.matchWidthOrHeight = 0.5f;

            if (canvasObj.GetComponent<GraphicRaycaster>() == null)
                canvasObj.AddComponent<GraphicRaycaster>();

            RectTransform rectTf = canvasObj.GetComponent<RectTransform>();
            rectTf.sizeDelta = new Vector2(1920f, 1080f);

            return canvas;
        }

        // 确保场景存在事件系统
        private void ensureEventSystem(Transform rootTf)
        {
            GameObject eventSystemObj = findFrameworkChild("EventSystem", rootTf);
            if (eventSystemObj == null && EventSystem.current != null)
                eventSystemObj = EventSystem.current.gameObject;

            if (eventSystemObj == null)
                eventSystemObj = new GameObject("EventSystem");

            parentToRoot(eventSystemObj.transform, rootTf);
            resetLocalTransform(eventSystemObj.transform);

            if (eventSystemObj.GetComponent<EventSystem>() == null)
                eventSystemObj.AddComponent<EventSystem>();

            if (eventSystemObj.GetComponent<StandaloneInputModule>() == null)
                eventSystemObj.AddComponent<StandaloneInputModule>();
        }

        // 查找框架根节点下的对象
        private GameObject findFrameworkChild(string objName, Transform rootTf)
        {
            if (rootTf != null)
            {
                Transform child = rootTf.Find(objName);
                if (child != null)
                    return child.gameObject;
            }

            GameObject obj = GameObject.Find(objName);
            if (obj != null && (rootTf == null || obj.transform.parent == null))
                return obj;

            return null;
        }

        // 挂载到框架根节点
        private void parentToRoot(Transform targetTf, Transform rootTf)
        {
            if (targetTf == null || rootTf == null || targetTf == rootTf) return;

            targetTf.SetParent(rootTf, false);
        }

        // 重置本地变换
        private void resetLocalTransform(Transform targetTf)
        {
            targetTf.localPosition = Vector3.zero;
            targetTf.localRotation = Quaternion.identity;
            targetTf.localScale = Vector3.one;
        }

        // 查找对应类型的脚本
        private Type findType(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(type => type.Name == typeName);
        }
    }
}
