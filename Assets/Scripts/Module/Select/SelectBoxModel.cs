/*
 * ┌──────────────────────────────────┐
 * │  描    述: 材料箱选择页 Model
 * │  类    名: SelectBoxModel.cs
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Common;
using Common.Defines;
using MVC.Model;

namespace Module.Select
{
    public class SelectBoxModel : BaseModel
    {
        private readonly Dictionary<string, SelectBoxDetailJsonConfig> _detailCache = new();

        public SelectBoxCatalogJsonConfig Catalog { get; private set; }
        public SelectDifficulty Difficulty { get; set; } = SelectDifficulty.Normal;
        public int SelectedBoxIndex { get; private set; }

        public int BoxCount => Catalog?.boxes?.Length ?? 0;

        public void EnsureCatalogLoaded()
        {
            if (Catalog != null) return;

            Catalog = JsonConfigLoader.LoadFromConfig<SelectBoxCatalogJsonConfig>(
                AddressDefines.Config_SelectBoxCatalog);

            if (Catalog?.boxes == null || Catalog.boxes.Length == 0)
            {
                QLog.Error($"[{nameof(SelectBoxModel)}] 主表无可用 box，请检查 {AddressDefines.Config_SelectBoxCatalog}.json");
                return;
            }

            SelectedBoxIndex = resolveDefaultBoxIndex();
        }

        public SelectBoxCatalogEntry GetCurrentBoxEntry()
        {
            if (BoxCount == 0) return null;
            return Catalog.boxes[SelectedBoxIndex];
        }

        public SelectBoxDetailJsonConfig GetCurrentBoxDetail()
        {
            SelectBoxCatalogEntry entry = GetCurrentBoxEntry();
            if (entry == null || string.IsNullOrEmpty(entry.configFile))
                return null;

            if (_detailCache.TryGetValue(entry.configFile, out SelectBoxDetailJsonConfig cached))
                return cached;

            SelectBoxDetailJsonConfig detail = JsonConfigLoader.LoadFromConfig<SelectBoxDetailJsonConfig>(
                entry.configFile);

            if (detail != null)
                _detailCache[entry.configFile] = detail;

            return detail;
        }

        public void SetDifficulty(SelectDifficulty difficulty)
        {
            Difficulty = difficulty;
        }

        public void ChangeBoxIndex(int delta)
        {
            if (BoxCount == 0) return;
            SelectedBoxIndex = (SelectedBoxIndex + delta % BoxCount + BoxCount) % BoxCount;
        }

        private int resolveDefaultBoxIndex()
        {
            if (string.IsNullOrEmpty(Catalog.defaultBoxId))
                return 0;

            for (int i = 0; i < Catalog.boxes.Length; i++)
            {
                if (string.Equals(Catalog.boxes[i].id, Catalog.defaultBoxId, StringComparison.Ordinal))
                    return i;
            }

            QLog.Warning($"[{nameof(SelectBoxModel)}] 未找到 defaultBoxId={Catalog.defaultBoxId}，使用第一个 box");
            return 0;
        }
    }
}
