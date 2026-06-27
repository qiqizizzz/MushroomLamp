using System.Collections.Generic;

namespace Module.Item
{
    public static class ItemEffectRegistry
    {
        private static readonly Dictionary<string, ItemEffect> _map = new();

        public static void Register(string key, ItemEffect effect) => _map[key] = effect;

        public static ItemEffect Get(string key)
        {
            if (!string.IsNullOrEmpty(key) && _map.TryGetValue(key, out ItemEffect effect))
                return effect;
            return null;
        }

        // 在此集中注册所有道具 Effect，例如：
        // Register("草本茶", new HerbalTeaEffect());
        public static void RegisterAll() { }
    }
}
