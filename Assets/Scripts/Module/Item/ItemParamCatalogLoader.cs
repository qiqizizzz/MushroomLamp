using System.Collections.Generic;
using Common;
using Common.Defines;
using Module.Player;

namespace Module.Item
{
    public static class ItemParamCatalogLoader
    {
        private static ItemParamCatalogJsonConfig _config;

        public static void EnsureLoaded()
        {
            if (_config != null) return;
            _config = JsonConfigLoader.LoadFromConfig<ItemParamCatalogJsonConfig>(AddressDefines.Config_ItemParamCatalog);
        }

        public static ItemParamJsonData GetById(string itemId)
        {
            EnsureLoaded();
            if (_config?.items == null || string.IsNullOrWhiteSpace(itemId)) return null;

            foreach (ItemParamJsonData item in _config.items)
            {
                if (item != null && item.id == itemId)
                    return item;
            }

            return null;
        }

        public static IReadOnlyList<ItemParamJsonData> GetAll()
        {
            EnsureLoaded();
            return _config?.items ?? System.Array.Empty<ItemParamJsonData>();
        }
    }
}
