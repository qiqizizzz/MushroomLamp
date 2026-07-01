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
        public int price;
        public string description;
    }

    [Serializable]
    public class ShopCatalogJsonConfig
    {
        public string defaultBoxIconPath = "Art/ShopView/材料箱样本";
        public ShopBoxCatalogEntryJson[] entries;
    }

    public static class ShopCatalog
    {
        private const string DefaultBoxIconPath = AddressDefines.Art_ShopMaterialBoxSample;

        private static ItemParamCatalogJsonConfig _itemConfig;
        private static ShopCatalogJsonConfig _shopConfig;
        private static SelectBoxCatalogJsonConfig _boxCatalog;

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

            var pool = shopConfig.entries.ToList();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                ShopBoxCatalogEntryJson data = pool[index];
                pool.RemoveAt(index);
                if (data == null || string.IsNullOrWhiteSpace(data.boxId)) continue;

                SelectBoxCatalogEntry boxEntry = GetBoxEntry(data.boxId);
                result.Add(new ShopSlotData
                {
                    id = data.boxId,
                    name = boxEntry?.displayName ?? data.boxId,
                    iconPath = iconPath,
                    description = data.description,
                    price = data.price,
                    isBox = true,
                    isCard = false
                });
            }

            return result;
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
                    new ShopBoxCatalogEntryJson { boxId = "herb", price = 8, description = "草本材料箱" },
                    new ShopBoxCatalogEntryJson { boxId = "mineral", price = 10, description = "矿物材料箱" },
                    new ShopBoxCatalogEntryJson { boxId = "spice", price = 12, description = "香料材料箱" }
                }
            };
        }
    }
}
