using System.Collections.Generic;
using System.Linq;
using Common.Defines;
using Module.Cook;
using Module.Item;
using Module.MagicBoxBuff;
using Module.Blackjack;
using Module.Player;
using Module.Level;
using Module.Select;
using Module.Store;
using Module.View;
using MVC;
using UnityEngine;

namespace Module.Debug
{
    public class GMPanelDebugger : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private bool visible;
        [SerializeField] private Vector2 scrollPosition;
        [SerializeField] private Vector2 detailTutorialScroll;

        private int _gmDetailViewTypeIndex;

        private void Update()
        {
            if (!GameDebugSettings.EnableGMPanel)
                return;

            if (Input.GetKeyDown(toggleKey))
                visible = !visible;
        }

        private void OnGUI()
        {
            if (!GameDebugSettings.EnableGMPanel || !visible || GameApp.ViewManager == null)
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
                else if (viewType == ViewType.CookView)
                {
                    if (GUILayout.Button($"{viewTypeName} (GM进入)", GUILayout.Height(28f)))
                        gmEnterCookFromPanel();
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
            drawLevelColumn(panelWidth);
            drawItemColumn(panelWidth);
            drawBuffColumn(panelWidth);
            drawBlackjackGmColumn();
            drawDetailTutorialColumn(panelWidth);
        }

        // 关卡 GM：重启 / 跳关（重新发初始手牌）
        private void drawLevelColumn(float leftPanelWidth)
        {
            const float colWidth = 220f;
            var rect = new Rect(16f + leftPanelWidth + 12f + 200f + 12f, 16f, colWidth, 300f);
            GUILayout.BeginArea(rect, GUI.skin.box);

            LevelFlow flow = LevelFlow.Instance;
            GUILayout.Label("关卡 GM");
            if (flow.HasFlow)
            {
                GUILayout.Label($"箱子：{flow.BoxName}");
                GUILayout.Label($"难度：{flow.Difficulty}  小局：{flow.StageIndex + 1}/{flow.StageCount}");
            }
            else
            {
                GUILayout.Label("当前无进行中的大局");
            }

            GUILayout.Space(6f);

            if (GUILayout.Button("重启大关(第一小关)", GUILayout.Height(28f)))
                gmRestartRun();

            GUILayout.Space(6f);
            GUILayout.Label("简单 · 跳关");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 3; i++)
            {
                int stageIndex = i;
                if (GUILayout.Button($"第{i + 1}关", GUILayout.Height(26f)))
                    gmJumpToStage(SelectDifficulty.Easy, stageIndex);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private static void gmRestartRun()
        {
            LevelRunBootstrap.EnsureDefaultRun(SelectDifficulty.Normal);
            LevelFlow.Instance.GmRestartFromFirstStage();
            LevelRunBootstrap.EnterCookRun();
        }

        private static void gmJumpToStage(SelectDifficulty difficulty, int stageIndex)
        {
            LevelRunBootstrap.EnsureDefaultRun(difficulty);
            LevelFlow.Instance.GmJumpToStage(difficulty, stageIndex);
            LevelRunBootstrap.EnterCookRun();
        }

        private static void gmEnterCookFromPanel()
        {
            if (!LevelFlow.Instance.HasFlow)
                LevelRunBootstrap.EnsureDefaultRun(SelectDifficulty.Normal);

            LevelRunBootstrap.EnterCookRun();
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
            const float levelColWidth = 220f;
            var rect = new Rect(16f + leftPanelWidth + 12f + 200f + 12f + levelColWidth + 12f, 16f, colWidth, 420f);
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
            const float levelColWidth = 220f;
            var rect = new Rect(16f + leftPanelWidth + 12f + 200f + 12f + levelColWidth + 12f + 280f + 12f, 16f, colWidth, 420f);
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

        // 21 点 GM：手动加点 / 模拟爆牌（测幸运兔脚重抽）
        private void drawBlackjackGmColumn()
        {
            if (GameApp.ViewManager == null || !GameApp.ViewManager.IsOpen((int)ViewType.BlackjackView))
                return;

            if (GameApp.ControllerManager.GetControllerModel((int)ControllerType.Blackjack) is not BlackjackModel model)
                return;

            const float colWidth = 240f;
            var rect = new Rect(16f, 16f + 420f + 12f, colWidth, 220f);
            GUILayout.BeginArea(rect, GUI.skin.box);

            GUILayout.Label("21 点 GM");
            GUILayout.Label($"累计：{BlackjackModel.FormatPoint(model.TotalPoint)} / {model.EffectiveBustLimit}");
            GUILayout.Label($"已翻：{model.RevealedCount}/{model.CardCount}  爆牌：{model.IsBusted}");
            GUILayout.Space(4f);
            GUILayout.Label("测兔脚：先翻1张牌 → 加点 → 检测爆牌", GUI.skin.label);

            GUILayout.BeginHorizontal();
            foreach (float delta in new[] { 1f, 3f, 5f, 10f })
            {
                float d = delta;
                if (GUILayout.Button($"+{BlackjackModel.FormatPoint(d)}", GUILayout.Height(26f)))
                    gmBlackjackAddPoint(d);
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button($"设为爆牌线 ({model.EffectiveBustLimit})", GUILayout.Height(26f)))
            {
                model.GmSetTotalPoint(model.EffectiveBustLimit);
                gmBlackjackRefresh();
            }

            if (GUILayout.Button("检测爆牌（触发兔脚/认栽）", GUILayout.Height(28f)))
                GameApp.ControllerManager.ApplyFunc((int)ControllerType.Blackjack, EventDefines.BlackjackGmCheckBust);

            if (GUILayout.Button("重置幸运兔脚(本小关)", GUILayout.Height(26f)))
                ItemPassiveManager.GmResetRabbitFoot();

            GUILayout.EndArea();
        }

        // 教程弹窗 GM：打开 DetailView 并切换各界面说明文案
        private void drawDetailTutorialColumn(float leftPanelWidth)
        {
            const float colWidth = 360f;
            var rect = new Rect(16f + leftPanelWidth + 12f, 16f + 420f + 12f, colWidth, 300f);
            GUILayout.BeginArea(rect, GUI.skin.box);

            ViewType current = (ViewType)_gmDetailViewTypeIndex;
            string previewTitle = current.ToString();
            if (DetailCatologJsonConfig.TryGetItem(current, out DetailItemJsonData item) && !string.IsNullOrEmpty(item.title))
                previewTitle = item.title;

            bool detailOpen = GameApp.ViewManager.IsOpen((int)ViewType.DetailView);
            GUILayout.Label("教程弹窗 GM");
            GUILayout.Label($"当前：{(int)current} · {current}");
            GUILayout.Label($"标题：{previewTitle}");
            GUILayout.Label($"状态：{(detailOpen ? "已打开" : "未打开")}");
            GUILayout.Space(4f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀ 上一个", GUILayout.Height(28f)))
                gmDetailShift(-1);
            if (GUILayout.Button("下一个 ▶", GUILayout.Height(28f)))
                gmDetailShift(1);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("打开教程弹窗", GUILayout.Height(28f)))
                gmOpenDetailTutorial(_gmDetailViewTypeIndex);
            if (GUILayout.Button("关闭教程弹窗", GUILayout.Height(28f)))
                GameApp.ViewManager.Close(ViewType.DetailView);
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.Label("快速切换（弹窗已开时即时刷新）");
            detailTutorialScroll = GUILayout.BeginScrollView(detailTutorialScroll, GUILayout.Height(120f));
            foreach (ViewType viewType in System.Enum.GetValues(typeof(ViewType)))
            {
                int index = (int)viewType;
                bool selected = index == _gmDetailViewTypeIndex;
                string label = selected ? $"▶ {(int)viewType} {viewType}" : $"  {(int)viewType} {viewType}";
                if (GUILayout.Button(label, GUILayout.Height(24f)))
                {
                    _gmDetailViewTypeIndex = index;
                    if (detailOpen)
                        gmOpenDetailTutorial(_gmDetailViewTypeIndex);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void gmDetailShift(int delta)
        {
            int count = System.Enum.GetValues(typeof(ViewType)).Length;
            _gmDetailViewTypeIndex = (_gmDetailViewTypeIndex + delta % count + count) % count;

            if (GameApp.ViewManager.IsOpen((int)ViewType.DetailView))
                gmOpenDetailTutorial(_gmDetailViewTypeIndex);
        }

        private static void gmOpenDetailTutorial(int viewTypeIndex)
        {
            if (GameApp.ViewManager == null) return;

            ViewType viewType = (ViewType)viewTypeIndex;
            if (DetailCatologJsonConfig.TryGetItem(viewType, out DetailItemJsonData item))
                GameApp.ViewManager.Open(ViewType.DetailView, item);
            else
                GameApp.ViewManager.Open(ViewType.DetailView, viewType);
        }

        private static void gmBlackjackAddPoint(float delta)
        {
            GameApp.ControllerManager.ApplyFunc((int)ControllerType.Blackjack, EventDefines.BlackjackGmAddPoint, delta);
        }

        private static void gmBlackjackRefresh()
        {
            GameApp.ControllerManager.ApplyFunc((int)ControllerType.Blackjack, EventDefines.BlackjackGmAddPoint, 0f);
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
