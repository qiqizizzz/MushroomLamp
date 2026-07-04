using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Defines;
using Module.Player;
using Module.Select;
using UnityEngine;

namespace Module.Shop
{
    [Serializable]
    public class ShopBoxCatalogEntryJson
    {
        public string boxId;
        public string name;
        public int price;
        public string description;
        public string poolFile;
        public int baseWeight = 1;
        public bool enabled = true;
        public string unlockStage;
        public int minMaterialCount;
        public int maxMaterialCount;
    }

    [Serializable]
    public class ShopCatalogJsonConfig
    {
        public string defaultBoxIconPath = "Art/ShopView/材料箱样本";
        public int pickCount = 3;
        public ShopBoxCatalogEntryJson[] entries;
    }

    [Serializable]
    public class ShopBoxPoolJsonConfig
    {
        public string boxId;
        public string[] materialIds;
    }

    public static class ShopCatalog
    {
        private const string DefaultBoxIconPath = AddressDefines.Art_ShopMaterialBoxSample;

        private static ItemParamCatalogJsonConfig _itemConfig;
        private static ShopCatalogJsonConfig _shopConfig;
        private static SelectBoxCatalogJsonConfig _boxCatalog;
        private static readonly Dictionary<string, ShopBoxPoolJsonConfig> _poolCache = new();

        public static string DefaultBoxIconPathValue => getShopConfig()?.defaultBoxIconPath ?? DefaultBoxIconPath;

        public static IReadOnlyList<ShopSlotData> RandomBoxes(int count)
        {
            EnsureLoaded();
            return PickBoxes(count);
        }

        public static IReadOnlyList<ShopSlotData> RandomItems(int count)
        {
            EnsureLoaded();
            return PickItems(count);
        }

        public static ShopBoxCatalogEntryJson GetShopEntry(string boxId)
        {
            EnsureLoaded();
            if (_shopConfig?.entries == null || string.IsNullOrWhiteSpace(boxId)) return null;

            foreach (ShopBoxCatalogEntryJson entry in _shopConfig.entries)
            {
                if (entry != null && entry.boxId == boxId)
                    return entry;
            }

            return null;
        }

        public static ShopBoxPoolJsonConfig LoadBoxPool(string poolFile)
        {
            if (string.IsNullOrWhiteSpace(poolFile)) return null;

            if (_poolCache.TryGetValue(poolFile, out ShopBoxPoolJsonConfig cached))
                return cached;

            ShopBoxPoolJsonConfig pool = JsonConfigLoader.LoadFromConfig<ShopBoxPoolJsonConfig>(poolFile);
            if (pool != null)
                _poolCache[poolFile] = pool;

            return pool;
        }

        public static ShopBoxPoolJsonConfig LoadBoxPoolByBoxId(string boxId)
        {
            ShopBoxCatalogEntryJson entry = GetShopEntry(boxId);
            if (entry == null || string.IsNullOrWhiteSpace(entry.poolFile))
                return LoadBoxPool($"Shop/Pools/{boxId}");

            return LoadBoxPool(entry.poolFile);
        }

        // 兼容旧 SelectBox 查询（初始选箱等非商店流程）
        public static SelectBoxCatalogEntry GetBoxEntry(string boxId)
        {
            EnsureLoaded();
            if (_boxCatalog?.boxes == null || string.IsNullOrWhiteSpace(boxId)) return null;

            foreach (SelectBoxCatalogEntry entry in _boxCatalog.boxes)
            {
                if (entry != null && entry.id == boxId)
                    return entry;
            }

            return null;
        }

        public static SelectBoxDetailJsonConfig LoadBoxDetail(SelectBoxCatalogEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.configFile)) return null;
            return JsonConfigLoader.LoadFromConfig<SelectBoxDetailJsonConfig>(entry.configFile);
        }

        private static IReadOnlyList<ShopSlotData> PickBoxes(int count)
        {
            var result = new List<ShopSlotData>();
            ShopCatalogJsonConfig shopConfig = getShopConfig();
            if (shopConfig?.entries == null || shopConfig.entries.Length == 0 || count <= 0)
                return result;

            string iconPath = string.IsNullOrWhiteSpace(shopConfig.defaultBoxIconPath)
                ? DefaultBoxIconPath
                : shopConfig.defaultBoxIconPath;

            var pool = shopConfig.entries
                .Where(entry => entry != null && entry.enabled && !string.IsNullOrWhiteSpace(entry.boxId))
                .ToList();

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int index = pickWeightedIndex(pool);
                ShopBoxCatalogEntryJson data = pool[index];
                pool.RemoveAt(index);

                result.Add(new ShopSlotData
                {
                    id = data.boxId,
                    name = string.IsNullOrWhiteSpace(data.name) ? data.boxId : data.name,
                    iconPath = iconPath,
                    description = data.description,
                    price = data.price,
                    isBox = true,
                    isCard = false
                });
            }

            return result;
        }

        private static int pickWeightedIndex(List<ShopBoxCatalogEntryJson> pool)
        {
            int total = 0;
            foreach (ShopBoxCatalogEntryJson entry in pool)
                total += Math.Max(0, entry.baseWeight);

            if (total <= 0)
                return UnityEngine.Random.Range(0, pool.Count);

            int roll = UnityEngine.Random.Range(0, total);
            int acc = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                acc += Math.Max(0, pool[i].baseWeight);
                if (roll < acc)
                    return i;
            }

            return pool.Count - 1;
        }

        private static IReadOnlyList<ShopSlotData> PickItems(int count)
        {
            var result = new List<ShopSlotData>();
            var source = _itemConfig?.items;
            if (source == null || source.Length == 0 || count <= 0) return result;

            var pool = source.ToList();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                var data = pool[index];
                pool.RemoveAt(index);
                result.Add(new ShopSlotData
                {
                    id = data.id,
                    name = data.name,
                    iconPath = data.iconPath,
                    description = data.description,
                    price = data.price,
                    isBox = false,
                    isCard = false
                });
            }

            return result;
        }

        private static ShopCatalogJsonConfig getShopConfig()
        {
            EnsureLoaded();
            return _shopConfig;
        }

        private static void EnsureLoaded()
        {
            if (_itemConfig == null)
                _itemConfig = JsonConfigLoader.LoadFromConfig<ItemParamCatalogJsonConfig>(AddressDefines.Config_ItemParamCatalog);

            if (_boxCatalog == null)
                _boxCatalog = JsonConfigLoader.LoadFromConfig<SelectBoxCatalogJsonConfig>(AddressDefines.Config_SelectBoxCatalog);

            if (_shopConfig != null) return;

            _shopConfig = JsonConfigLoader.LoadFromConfig<ShopCatalogJsonConfig>(AddressDefines.Config_ShopCatalog);
            if (_shopConfig != null) return;

            QLog.Warning($"[{nameof(ShopCatalog)}] 未找到 {AddressDefines.Config_ShopCatalog}，使用默认材料箱价格");
            _shopConfig = new ShopCatalogJsonConfig
            {
                defaultBoxIconPath = DefaultBoxIconPath,
                entries = new[]
                {
                    new ShopBoxCatalogEntryJson
                    {
                        boxId = "shop_box_veg_basic",
                        name = "蔬菜基础材料箱",
                        price = 5,
                        description = "蔬菜基础材料箱",
                        poolFile = "Shop/Pools/shop_box_veg_basic",
                        baseWeight = 90,
                        enabled = true
                    }
                }
            };
        }
    }
}
