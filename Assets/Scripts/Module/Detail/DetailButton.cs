/*
* ┌──────────────────────────────────┐
* │  描    述: 通用详情按钮，负责按目标界面类型打开详情界面
* │  类    名: DetailButton.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using MVC;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    // 通用详情按钮，点击后按目标界面类型读取并打开详情内容
    [RequireComponent(typeof(Button))]
    public class DetailButton : MonoBehaviour
    {
        // ==================== 字段[外部设置] ====================
        [Tooltip("点击按钮后读取该界面类型对应的详情配置")]
        public ViewType TargetViewType = ViewType.DetailView;

        // ==================== 字段[私有] ====================
        private Button _button;

        // ==================== 生命周期 ====================
        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            _button.onClick.AddListener(openDetailView);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(openDetailView);
        }

        // ==================== Private Function ====================
        // 打开当前目标界面类型对应的详情内容
        private void openDetailView()
        {
            if (DetailCatologJsonConfig.TryGetItem(TargetViewType, out DetailItemJsonData item))
                GameApp.ViewManager.Open(ViewType.DetailView, item);
            else
                GameApp.ViewManager.Open(ViewType.DetailView, TargetViewType);
        }
    }
}
