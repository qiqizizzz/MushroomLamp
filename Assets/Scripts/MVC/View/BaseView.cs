/*
* ┌──────────────────────────────────┐
* │  描    述: 视图基类                      
* │  类    名: BaseView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using MVC.Controller;
using MVC.Model;
using UnityEngine;

namespace MVC.View
{
    public class BaseView : MonoBehaviour, IBaseView
    {
        public BaseController Controller { get; set; }
        public int ViewId { get; set; }

        protected Canvas _canvas;
        protected Dictionary<string, GameObject> _cacheGos = new Dictionary<string, GameObject>();

        private bool _isInit = false;

        public bool IsInit() => _isInit;

        public bool IsShow() => _canvas.enabled;
        
        private void Awake()
        {
            _canvas = gameObject.GetComponent<Canvas>();
            OnAwake();
        }

        private void Start()
        {
            OnStart();
        }

        private void Update()
        {
            OnUpdate();
        }

        protected virtual void OnDestroy()
        {
            _cacheGos.Clear();
        }

        protected virtual void OnAwake()
        {
            
        }

        protected virtual void OnStart()
        {
            
        }
        
        protected virtual void OnEnable()
        {
            
        }

        protected virtual void OnUpdate()
        {
            
        }

        protected virtual void OnDisable()
        {
            
        }
        
        public virtual void InitUI()
        {
            
        }

        public virtual void InitData()
        {
            _isInit = true;
        }

        public virtual void Open(params object[] args)
        {
            
        }

        public virtual void Close(params object[] args)
        {
            SetVisible(false);
        }

        public void DestroyView()
        {
            Controller = null;
            Destroy(gameObject);
        }

        public void ApplyFunc(string eventName, params object[] args)
        {
            Controller?.ApplyFunc(eventName, args ?? Array.Empty<object>());
        }

        public void ApplyControllerFunc(int controllerKey, string eventName, params object[] args)
        {
            Controller?.ApplyControllerFunc(controllerKey, eventName, args ?? Array.Empty<object>());
        }
        public void ApplyControllerFunc(ControllerType type, string eventName, params object[] args)
        {
            ApplyControllerFunc((int)type, eventName, args);
        }

        public void SetVisible(bool isVisible)
        {
            if (_canvas != null)
                _canvas.enabled = isVisible;
        }

        public GameObject Find(string res, Transform parent = null)
        {
            if (_cacheGos.ContainsKey(res))
                return _cacheGos[res];

            Transform target = (parent != null ? parent : transform).Find(res);
            if (target == null)
            {
                Common.QLog.Error($"[{nameof(BaseView)}] 节点未找到：{res}");
                return null;
            }

            _cacheGos.Add(res, target.gameObject);
            return _cacheGos[res];
        }

        public T Find<T>(string res, Transform parent = null) where T : Component
        {
            GameObject obj = Find(res, parent);
            if (obj == null) return null;

            return obj.GetComponent<T>();
        }
    }
}
