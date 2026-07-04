using System.Collections.Generic;
using System.Linq;
using Common.Defines;
using Module.Cook;
using Module.Item;
using Module.MagicBoxBuff;
using Module.Player;
using Module.Store;
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
                    {
                        var gmContext = new StoreOpenContext
                        {
                            boxId = "herb",
                            boxName = "草本药箱",
                            cardsIncludedInBoxPrice = true
                        };
                        GameApp.ControllerManager.ApplyFunc((int)ControllerType.Store, EventDefines.OpenStoreView, gmContext);
                    }

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
            drawItemColumn(panelWidth);
            drawBuffColumn(panelWidth);
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

        // 道具 GM：获得/移除任意道具
        private void drawItemColumn(float leftPanelWidth)
        {
            const float colWidth = 280f;
            var rect = new Rect(16f + leftPanelWidth + 12f + 200f + 12f, 16f, colWidth, 420f);
            GUILayout.BeginArea(rect, GUI.skin.box);

            ItemParamCatalogLoader.EnsureLoaded();
            IReadOnlyList<ItemParamJsonData> allItems = ItemParamCatalogLoader.GetAll();
            int totalCount = allItems.Count;

            GUILayout.Label("道具 GM");
            GUILayout.Label($"拥有：{PlayerDataManager.Instance.OwnedItemCount}/{totalCount}");
            GUILayout.Space(4f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("获得全部", GUILayout.Height(26f)))
            {
                gmAddAllItems(allItems);
                notifyItemInventoryChanged();
            }

            if (GUILayout.Button("清除全部", GUILayout.Height(26f)))
            {
                PlayerDataManager.Instance.ClearAllItems();
                notifyItemInventoryChanged();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            foreach (ItemParamJsonData item in allItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.id)) continue;

                bool owned = PlayerDataManager.Instance.HasItem(item.id);
                string label = owned ? $"[√] {item.name}" : $"[ ] {item.name}";

                GUILayout.BeginHorizontal();
                GUILayout.Label(label, GUILayout.Width(130f));

                GUI.enabled = !owned;
                if (GUILayout.Button("获得", GUILayout.Width(56f), GUILayout.Height(24f)))
                {
                    PlayerDataManager.Instance.AddItem(item.id);
                    notifyItemInventoryChanged();
                }
                GUI.enabled = true;

                GUI.enabled = owned;
                if (GUILayout.Button("移除", GUILayout.Width(56f), GUILayout.Height(24f)))
                {
                    PlayerDataManager.Instance.RemoveItem(item.id);
                    notifyItemInventoryChanged();
                }
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }

        private void drawBuffColumn(float leftPanelWidth)
        {
            const float colWidth = 280f;
            var rect = new Rect(16f + leftPanelWidth + 12f + 200f + 12f + 280f + 12f, 16f, colWidth, 420f);
            GUILayout.BeginArea(rect, GUI.skin.box);

            MagicBoxBuffCatalogLoader.EnsureLoaded();
            IReadOnlyList<MagicBoxBuffJsonData> allBuffs = MagicBoxBuffCatalogLoader.GetAll();
            int activeCount = MagicBoxBuffManager.RoundBuffIds.Count + MagicBoxBuffManager.SessionBuffIds.Count;

            GUILayout.Label("魔盒 Buff GM");
            GUILayout.Label($"生效中：{activeCount}");
            GUILayout.Space(4f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("获得全部", GUILayout.Height(26f)))
            {
                foreach (MagicBoxBuffJsonData buff in allBuffs)
                {
                    if (buff == null || string.IsNullOrWhiteSpace(buff.id)) continue;
                    MagicBoxBuffManager.GrantBuff(buff.id);
                }
            }

            if (GUILayout.Button("清除全部", GUILayout.Height(26f)))
            {
                MagicBoxBuffManager.ClearAll();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            foreach (MagicBoxBuffJsonData buff in allBuffs)
            {
                if (buff == null || string.IsNullOrWhiteSpace(buff.id)) continue;

                bool owned = MagicBoxBuffManager.HasBuff(buff.id);
                string label = owned ? $"[√] {buff.name}" : $"[ ] {buff.name}";

                GUILayout.BeginHorizontal();
                GUILayout.Label(label, GUILayout.Width(130f));

                GUI.enabled = !owned;
                if (GUILayout.Button("获得", GUILayout.Width(56f), GUILayout.Height(24f)))
                    MagicBoxBuffManager.GrantBuff(buff.id);
                GUI.enabled = true;

                GUI.enabled = owned;
                if (GUILayout.Button("移除", GUILayout.Width(56f), GUILayout.Height(24f)))
                    MagicBoxBuffManager.RemoveBuff(buff.id);
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }

        private static void gmAddAllItems(IReadOnlyList<ItemParamJsonData> allItems)
        {
            foreach (ItemParamJsonData item in allItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.id)) continue;
                PlayerDataManager.Instance.AddItem(item.id);
            }
        }

        private static void notifyItemInventoryChanged()
        {
            if (GameApp.ViewManager == null) return;
            if (!GameApp.ViewManager.IsOpen((int)ViewType.CookView)) return;

            CookView cookView = GameApp.ViewManager.GetView<CookView>(ViewType.CookView);
            cookView?.RefreshOwnedItemsDisplay();
        }
    }
}
