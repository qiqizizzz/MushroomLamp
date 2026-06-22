/*
* ┌──────────────────────────────────┐
* │  描    述: 控制器基类                       
* │  类    名: BaseController.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using MVC.Model;
using MVC.View;
using Common;
using UnityEngine;

namespace MVC.Controller
{
    public class BaseController
    {
        private Dictionary<string, Action<object[]>> message;
        protected BaseModel model;

        public BaseController()
        {
            message = new Dictionary<string, Action<object[]>>();
        }

        public virtual void Init()
        {
            
        }

        public virtual void Destroy()
        {
            RemoveGlobalEvent();
            RemoveModuleEvent();
        }

        public BaseModel GetModel() => model;
        public T GetModel<T>() where T : BaseModel => model as T;

        // 注册事件
        public void RegisterFunc(string eventName, Action<object[]> callback)
        {
            if (message.ContainsKey(eventName))
                message[eventName] += callback;
            else
                message.Add(eventName, callback);
        }

        // 注销事件
        public void UnRegisterFunc(string eventName)
        {
            if (message.ContainsKey(eventName))
                message.Remove(eventName);
        }

        // 新增: 注销某个事件的回调函数
        public void UnRegisterFunc(string eventName, Action<object[]> callback)
        {
            if (message.ContainsKey(eventName))
            {
                message[eventName] -= callback;
                
                if (message[eventName] == null)
                    message.Remove(eventName);
            }
        }

        // 触发事件
        public void ApplyFunc(string eventName, params object[] args)
        {
            if (message.ContainsKey(eventName))
                message[eventName]?.Invoke(args);
            else
                QLog.Info("Error: 不存在事件" + eventName);
        }

        // 触发其他控制器事件
        public void ApplyControllerFunc(int controllerKey, string eventName, params object[] args)
        {
            GameApp.ControllerManager.ApplyFunc(controllerKey, eventName, args);
        }
        public void ApplyControllerFunc(ControllerType ctlType, string eventName, params object[] args)
        {
            ApplyControllerFunc((int)ctlType, eventName, args);
        }
        
        // 设置模型数据
        public void SetModel(BaseModel mod)
        {
            this.model = mod;
            this.model.Controller = this;
        }
        
        // 获取其他控制器模型
        public BaseModel GetControllerModel(int controllerKey) =>
            GameApp.ControllerManager.GetControllerModel(controllerKey);

        // 获取其他控制器模型
        public BaseModel GetControllerModel(ControllerType ctlType) =>
            GetControllerModel((int)ctlType);

        public virtual void InitModuleEvent()
        {
            
        }
        
        public virtual void InitGlobalEvent()
        {
            
        }
        
        public virtual void RemoveModuleEvent()
        {
            
        }
        
        public virtual void RemoveGlobalEvent()
        {
            
        }

        public virtual void OnLoadView(IBaseView view)
        {
            
        }
        
        public virtual void OpenView(IBaseView view)
        {
            
        }
        
        public virtual void CloseView(IBaseView view)
        {
            
        }
    }
}
