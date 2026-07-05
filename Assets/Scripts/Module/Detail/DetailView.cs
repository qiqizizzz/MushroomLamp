/*
* ┌──────────────────────────────────┐
* │  描    述: 详细信息界面，负责展示 JSON 配置中的标题与内容
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
    // 详细信息界面，负责展示标题、内容并处理关闭
    public class DetailView : BaseView
    {
        // ==================== 字段[私有] ====================
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _content;
        private Button _closeBtn;

        // ==================== Public Function ====================
        public override void InitUI()
        {
            _title = Find<TextMeshProUGUI>("Txt_title");
            _content = Find<TextMeshProUGUI>("Txt_content");
            _closeBtn = Find<Button>("Btn_close");
        }

        public override void InitData()
        {
            base.InitData();
            _closeBtn.onClick.RemoveAllListeners();
            _closeBtn.onClick.AddListener(closeDetailView);
        }

        // 打开详情界面并刷新显示内容
        public override void Open(params object[] args)
        {
            DetailItemJsonData item = resolveDetailItem(args);
            setText(item);
        }

        // ==================== Private Function ====================
        // 关闭详情界面
        private void closeDetailView()
        {
            GameApp.ViewManager.Close(ViewType.DetailView);
        }

        // 解析详情数据，兼容配置对象、ViewType 和旧版字符串参数
        private DetailItemJsonData resolveDetailItem(object[] args)
        {
            if (args != null && args.Length > 0)
            {
                if (args[0] is DetailItemJsonData item)
                    return item;

                if (args[0] is ViewType viewType && DetailCatologJsonConfig.TryGetItem(viewType, out item))
                    return item;

                if (args[0] is string title)
                {
                    string content = args.Length > 1 && args[1] is string text ? text : string.Empty;
                    return new DetailItemJsonData
                    {
                        viewType = ViewType.DetailView,
                        title = title,
                        content = content
                    };
                }
            }

            return new DetailItemJsonData
            {
                viewType = ViewType.DetailView,
                title = "详情",
                content = "暂无详情配置"
            };
        }

        // 设置标题与正文文本
        private void setText(DetailItemJsonData item)
        {
            _title.text = string.IsNullOrEmpty(item.title) ? "详情" : item.title;
            _content.text = string.IsNullOrEmpty(item.content) ? "暂无详情配置" : item.content;
        }
    }
}
