/*
* ┌──────────────────────────────────┐
* │  描    述: 控制器管理器                      
* │  类    名: ControllerManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Linq;
using MVC.Controller;
using MVC.Model;
using Common;
using UnityEngine;

namespace MVC
{
    public class ControllerManager
    {
        private Dictionary<int, BaseController> _modules;

        public ControllerManager()
        {
            _modules = new Dictionary<int, BaseController>();
        }
        
        public void Init()
        {
            InitAllModules();
        }

        public void Destroy()
        {
            ClearAllModules();
            _modules.Clear();
            _modules = null;
        }
        
        //注册控制器
        public void Register(int controllerKey, BaseController ctl)
        {
            if (!_modules.ContainsKey(controllerKey))
                _modules.Add(controllerKey, ctl);
            else
                QLog.Info("Exit: 已经存在该控制器" + controllerKey);
        }
        public void Register(ControllerType ctlType, BaseController ctl)
        {
            Register((int)ctlType, ctl);
        }
        
        //注销控制器
        public void UnRegister(int controllerKey)
        {
            if (_modules.ContainsKey(controllerKey))
                _modules.Remove(controllerKey);
            else
                QLog.Info("Error: 不存在改控制器,id为: " + controllerKey);
        }
        
        //初始化所有控制器
        // 初始化所有控制器
        public void InitAllModules()
        {
            foreach (var mod in _modules)
            {
                mod.Value.Init();
            }
        }

        //清除所有控制器
        private void ClearAllModules()
        {
            if (_modules == null || _modules.Count == 0) return;
            
            var controllers = _modules.Values.ToList();//拷贝快照 防止报错
            
            foreach (var ctl in controllers)
            {
                if (ctl == null) continue;

                try
                {
                    ctl.Destroy();
                }
                catch (Exception ex)
                {
                    QLog.Error($"Error: Controller Destroy exception: {ex}");
                }
            }
        }

        // 触发其他控制器事件
        public void ApplyFunc(int controllerKey, string eventName, params object[] args)
        {
            if (_modules.ContainsKey(controllerKey))
                _modules[controllerKey].ApplyFunc(eventName, args);
            else
                QLog.Info("Error: 不存在该控制器,id为: " + controllerKey);
        }

        // 获取控制器的模型
        public BaseModel GetControllerModel(int controllerKey)
        {
            if (_modules.ContainsKey(controllerKey))
                return _modules[controllerKey].GetModel();
            
            return null;
        }
    }
}
