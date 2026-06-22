/*
* ┌──────────────────────────────────┐
* │  描    述: 加载模块数据模型
* │  类    名: LoadingModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using MVC.Model;

namespace Module.Loading
{
    public class LoadingModel : BaseModel
    {
        public string SceneName { get; private set; }
        public Action Callback { get; set; }

        // 设置目标场景名称
        public void SetSceneName(string name)
        {
            SceneName = name;
        }
    }
}
