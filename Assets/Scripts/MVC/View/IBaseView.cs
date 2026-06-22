/*
* ┌──────────────────────────────────┐
* │  描    述: 视图接口                      
* │  类    名: IBaseView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using MVC.Controller;

namespace MVC.View
{
    public interface IBaseView
    {
        BaseController Controller { get; set; }//面板所属控制器
        
        int ViewId { get; set; }//面板ID
        
        bool IsInit();
        
        bool IsShow();//是否显示

        void InitUI();//初始化面板

        void InitData();//初始化数据

        void Open(params object[] args);//打开面板 -> params object表示可以传入任意数量参数
        
        void Close(params object[] args);//关闭面板

        void DestroyView();//删除面板

        void ApplyFunc(string eventName, params object[] args);//触发本模块事件

        void ApplyControllerFunc(int controllerKey, string eventName, params object[] args);//触发其他控制器模块事件
        
        void SetVisible(bool isVisible);//设置显示/隐藏
    }
}