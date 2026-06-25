using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Defines;
using Module.Player;
using UnityEngine;

namespace Module.Shop
{
    public static class ShopCatalog
    {
        private static CardParamCatalogJsonConfig _cardConfig;
        private static ItemParamCatalogJsonConfig _itemConfig;

        public static IReadOnlyList<ShopSlotData> RandomCards(int count)
        {
            EnsureLoaded();
            return PickCards(count);
        }

        public static IReadOnlyList<ShopSlotData> RandomItems(int count)
        {
            EnsureLoaded();
            return PickItems(count);
        }

        private static IReadOnlyList<ShopSlotData> PickCards(int count)
        {
            var result = new List<ShopSlotData>();
            var source = _cardConfig?.cards;
            if (source == null || source.Length == 0 || count <= 0) return result;

            var pool = source.ToList();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int index = Random.Range(0, pool.Count);
                var data = pool[index];
                pool.RemoveAt(index);
                result.Add(new ShopSlotData
                {
                    id = data.id,
                    name = data.name,
                    iconPath = data.iconPath,
                    description = data.description,
                    price = data.price,
                    isCard = true
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
                int index = Random.Range(0, pool.Count);
                var data = pool[index];
                pool.RemoveAt(index);
                result.Add(new ShopSlotData
                {
                    id = data.id,
                    name = data.name,
                    iconPath = data.iconPath,
                    description = data.description,
                    price = data.price,
                    isCard = false
                });
            }

            return result;
        }

        private static void EnsureLoaded()
        {
            if (_cardConfig != null && _itemConfig != null) return;

            _cardConfig = JsonConfigLoader.LoadFromConfig<CardParamCatalogJsonConfig>(AddressDefines.Config_CardParamCatalog);
            _itemConfig = JsonConfigLoader.LoadFromConfig<ItemParamCatalogJsonConfig>(AddressDefines.Config_ItemParamCatalog);
        }
    }
}
