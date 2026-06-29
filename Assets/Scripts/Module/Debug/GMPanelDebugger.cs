using System.Linq;
using Common.Defines;
using Module.Player;
using MVC;
using UnityEngine;

namespace Module.Debug
{
    public class GMPanelDebugger : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private bool visible;
        [SerializeField] private Vector2 scrollPosition;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible || GameApp.ViewManager == null)
                return;

            const float panelWidth = 320f;
            const float panelHeight = 420f;
            var rect = new Rect(16f, 16f, panelWidth, panelHeight);
            GUILayout.BeginArea(rect, GUI.skin.box);

            GUILayout.Label("GM 面板工具");
            GUILayout.Label("F1 开/关");
            GUILayout.Space(6f);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (var viewInfo in GameApp.ViewManager.ViewInfos.OrderBy(item => item.Key))
            {
                var viewType = (ViewType)viewInfo.Key;
                var viewTypeName = viewType.ToString();
                bool isOpen = GameApp.ViewManager.IsOpen(viewInfo.Key);
                string label = $"{viewTypeName} [{(isOpen ? "Open" : "Close")}]";

                if (viewType == ViewType.ShopView)
                {
                    if (GUILayout.Button($"{viewTypeName} (GM进入)", GUILayout.Height(28f)))
                        GameApp.ControllerManager.ApplyFunc((int)ControllerType.Shop, "OpenShopView");
                }
                else if (viewType == ViewType.SummaryView)
                {
                    if (GUILayout.Button($"{viewTypeName} (GM进入)", GUILayout.Height(28f)))
                        GameApp.ControllerManager.ApplyFunc((int)ControllerType.Summary, EventDefines.OpenSummaryView);
                }
                else if (viewType == ViewType.StageSettleView)
                {
                    if (GUILayout.Button($"{viewTypeName} (GM进入)", GUILayout.Height(28f)))
                        GameApp.ControllerManager.ApplyFunc((int)ControllerType.StageSettle, EventDefines.OpenStageSettleView);
                }
                else if (viewType == ViewType.BlackjackView)
                {
                    if (GUILayout.Button($"{viewTypeName} (GM进入)", GUILayout.Height(28f)))
                        GameApp.ControllerManager.ApplyFunc((int)ControllerType.Blackjack, EventDefines.OpenBlackjackView);
                }
                else if (viewType == ViewType.StoreView)
                {
                    if (GUILayout.Button($"{viewTypeName} (GM进入)", GUILayout.Height(28f)))
                        GameApp.ControllerManager.ApplyFunc((int)ControllerType.Store, EventDefines.OpenStoreView);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("背包数量", GUILayout.Width(60f));
                    foreach (int n in new[] { 5, 12, 30, 100 })
                    {
                        if (GUILayout.Button(n.ToString(), GUILayout.Height(24f)))
                            GameApp.ControllerManager.ApplyFunc((int)ControllerType.Store, EventDefines.StoreSetBagCount, n);
                    }
                    GUILayout.EndHorizontal();
                }
                else if (GUILayout.Button(label, GUILayout.Height(28f)))
                {
                    if (isOpen)
                        GameApp.ViewManager.Close(viewInfo.Key);
                    else
                        GameApp.ViewManager.Open(viewInfo.Key);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(6f);
            if (GUILayout.Button("Close All", GUILayout.Height(30f)))
                GameApp.ViewManager.CloseAll();

            GUILayout.EndArea();

            drawMoneyColumn(panelWidth);
        }

        // 独立的金币列，放在面板右侧（不与 View 按钮同列）
        private void drawMoneyColumn(float leftPanelWidth)
        {
            const float colWidth = 200f;
            var rect = new Rect(16f + leftPanelWidth + 12f, 16f, colWidth, 300f);
            GUILayout.BeginArea(rect, GUI.skin.box);

            GUILayout.Label("金币 GM");
            GUILayout.Label($"当前：{PlayerDataManager.Instance.Money}");
            GUILayout.Space(6f);

            if (GUILayout.Button("+10", GUILayout.Height(28f)))
                PlayerDataManager.Instance.AddMoney(10);
            if (GUILayout.Button("+50", GUILayout.Height(28f)))
                PlayerDataManager.Instance.AddMoney(50);
            if (GUILayout.Button("+100", GUILayout.Height(28f)))
                PlayerDataManager.Instance.AddMoney(100);
            if (GUILayout.Button("清零", GUILayout.Height(28f)))
                PlayerDataManager.Instance.AddMoney(-PlayerDataManager.Instance.Money);

            GUILayout.Space(10f);
            GUILayout.Label("测试");
            bool forceWin = GUILayout.Toggle(Module.Cook.CookModel.ForceStageWin, " 永不失败(恒达标)");
            Module.Cook.CookModel.ForceStageWin = forceWin;

            GUILayout.EndArea();
        }
    }
}
