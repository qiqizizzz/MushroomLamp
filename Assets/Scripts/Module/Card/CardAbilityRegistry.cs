using System.Collections.Generic;
using Common;

namespace Module.Card
{
    public static class CardAbilityRegistry
    {
        private static readonly Dictionary<string, CardAbility> _map = new();

        public static void Register(string key, CardAbility ability) => _map[key] = ability;

        public static CardAbility Get(string key)
        {
            if (!string.IsNullOrEmpty(key) && _map.TryGetValue(key, out CardAbility ability))
                return ability;
            return CardAbility.Default;
        }

        // 从 CardConfig_Data.json 加载所有卡牌数据并注册
        public static void RegisterAll()
        {
            CardDataCatalog catalog = JsonConfigLoader.LoadFromConfig<CardDataCatalog>("CardConfig_Data");
            if (catalog?.cards == null)
            {
                QLog.Error("[CardAbilityRegistry] 加载 CardConfig_Data.json 失败");
                return;
            }

            foreach (CardDataEntry d in catalog.cards)
            {
                if (string.IsNullOrEmpty(d.name)) continue;

                CardAbility ability = d.abilityType switch
                {
                    "carrot"   => new CarrotAbility(d),
                    "potato"   => new PotatoAbility(d),
                    "mushroom" => new MushroomAbility(d),
                    "pumpkin"  => new PumpkinAbility(d),
                    "mineral"  => new MineralAbility(d),
                    "spice"    => new SpiceAbility(d),
                    _          => CardAbility.Default
                };

                Register(d.name, ability);
            }
        }
    }
}
