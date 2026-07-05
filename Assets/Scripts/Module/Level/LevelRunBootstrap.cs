using System.Collections.Generic;
using Common;
using Common.Defines;
using Module.Cook;
using Module.Item;
using Module.MagicBoxBuff;
using Module.Player;
using Module.Select;
using MVC;

namespace Module.Level
{
    // 从选择页 / GM 启动大局时的统一入口
    public static class LevelRunBootstrap
    {
        public const string DefaultBoxId = "herb";
        public const string DefaultBoxName = "草本药箱";

        public static void BeginNewRun(
            string boxId,
            string boxName,
            SelectDifficulty difficulty,
            IEnumerable<CookMaterialSeedData> materials)
        {
            PlayerDataManager.Instance.ClearItemsForNewRun();
            ItemPassiveManager.ResetRun();
            MagicBoxBuffManager.ClearAll();
            LevelFlow.Instance.Begin(boxId, boxName, difficulty, materials);
        }

        public static bool TryLoadBoxMaterials(
            string boxId,
            out string displayName,
            out List<CookMaterialSeedData> materials)
        {
            displayName = DefaultBoxName;
            materials = new List<CookMaterialSeedData>();

            SelectBoxCatalogJsonConfig catalog = JsonConfigLoader.LoadFromConfig<SelectBoxCatalogJsonConfig>(
                AddressDefines.Config_SelectBoxCatalog);
            if (catalog?.boxes == null || catalog.boxes.Length == 0)
                return false;

            SelectBoxCatalogEntry entry = null;
            foreach (SelectBoxCatalogEntry box in catalog.boxes)
            {
                if (box == null || box.id != boxId) continue;
                entry = box;
                break;
            }

            if (entry == null)
                entry = catalog.boxes[0];

            if (entry == null || string.IsNullOrEmpty(entry.configFile))
                return false;

            displayName = entry.displayName ?? DefaultBoxName;
            SelectBoxDetailJsonConfig detail = JsonConfigLoader.LoadFromConfig<SelectBoxDetailJsonConfig>(entry.configFile);
            materials = SelectBoxMaterialHelper.CollectMaterials(detail);
            return materials.Count > 0;
        }

        public static void EnsureDefaultRun(SelectDifficulty difficulty = SelectDifficulty.Normal)
        {
            if (LevelFlow.Instance.HasFlow) return;

            if (!TryLoadBoxMaterials(DefaultBoxId, out string boxName, out List<CookMaterialSeedData> materials))
                materials = new List<CookMaterialSeedData>();

            BeginNewRun(DefaultBoxId, boxName, difficulty, materials);
        }

        public static void EnterCookRun()
        {
            GameApp.ViewManager.Close((int)ViewType.ShopView);
            GameApp.ViewManager.Close((int)ViewType.StageSettleView);
            GameApp.ViewManager.Close((int)ViewType.SummaryView);
            GameApp.ViewManager.Close((int)ViewType.BlackjackView);
            GameApp.ViewManager.Close((int)ViewType.SelectBoxView);
            GameApp.ViewManager.Close((int)ViewType.MainMenuView);

            GameApp.ControllerManager.ApplyFunc(
                (int)ControllerType.Cook,
                EventDefines.StartCookRun,
                LevelFlow.Instance.BuildStartData());
        }
    }
}
