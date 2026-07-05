/*
 * ┌──────────────────────────────────┐
 * │  描    述: 详细信息界面                      
 * │  类    名: DetailView.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using MVC;
using MVC.View;
using TMPro;
using UnityEngine.UI;

namespace Module.View
{
    //json数据格式
    public class DetailViewData
    {
        public string title;
        public string content;
        public ViewType viewType;
    }
    
    public class DetailView : BaseView
    {
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _content;
        private Button _closeBtn;

        protected override void OnAwake()
        {
            _title = Find<TextMeshProUGUI>("Txt_title");
            _content = Find<TextMeshProUGUI>("Txt_content");
            _closeBtn = Find<Button>("Btn_close");
        }

        protected override void OnStart()
        {
            _closeBtn.onClick.RemoveAllListeners();
            _closeBtn.onClick.AddListener(() =>
            {
                GameApp.ViewManager.Close(ViewType.DetailView);
            });
        }

        public override void Open(params object[] args)
        {
            //参数1 _title 参数2 _content
            string title = args.Length > 0 ? args[0] as string : "Title";
            string content = args.Length > 1 ? args[1] as string : "Content";
            _title.text = title;
            _content.text = content;
        }
    }
}