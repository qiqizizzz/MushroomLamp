/*
* ┌──────────────────────────────────┐
* │  描    述: 烹饪核心玩法界面，负责承接玩法状态刷新
* │  类    名: CookView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Cook;
using MVC.View;

namespace Module.View
{
    // 烹饪核心玩法界面，后续由预制体补充具体 UI
    public class CookView : BaseView
    {
        // 根据烹饪模型刷新界面
        public void Refresh(CookModel cookModel)
        {
            if (cookModel == null) return;
        }
    }
}
