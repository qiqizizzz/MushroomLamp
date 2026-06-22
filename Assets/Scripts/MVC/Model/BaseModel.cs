/*
* ┌──────────────────────────────────┐
* │  描    述: 模型基类                      
* │  类    名: BaseModel.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using MVC.Controller;

namespace MVC.Model
{
    public class BaseModel
    {
        public BaseController Controller;

        public BaseModel(BaseController ctl)
        {
            this.Controller = ctl;
        }

        public BaseModel()
        {
            
        }

        public virtual void Init()
        {
            
        }

        public virtual void Destroy()
        {
            
        }
    }
}