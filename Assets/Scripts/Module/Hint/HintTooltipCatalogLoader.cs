using System.Collections.Generic;
using Common;
using Common.Defines;

namespace Module.Hint
{
    public static class HintTooltipCatalogLoader
    {
        private static HintTooltipCatalogJsonConfig _config;
        private static Dictionary<string, HintTooltipJsonData> _lookup;

        public static void EnsureLoaded()
        {
            if (_config != null) return;

            _config = JsonConfigLoader.LoadFromConfig<HintTooltipCatalogJsonConfig>(AddressDefines.Config_HintTooltipCatalog);
            rebuildLookup();
        }

        public static HintTooltipJsonData GetById(string hintId)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(hintId) || _lookup == null)
                return null;

            _lookup.TryGetValue(hintId, out HintTooltipJsonData data);
            return data;
        }

        private static void rebuildLookup()
        {
            _lookup = new Dictionary<string, HintTooltipJsonData>();
            if (_config?.hints == null) return;

            foreach (HintTooltipJsonData hint in _config.hints)
            {
                if (hint == null || string.IsNullOrWhiteSpace(hint.id)) continue;
                _lookup[hint.id] = hint;
            }
        }
    }
}
