using System.Collections.Generic;
using Common;
using Common.Defines;

namespace Module.MagicBoxBuff
{
    public static class MagicBoxBuffCatalogLoader
    {
        private static MagicBoxBuffCatalogJsonConfig _config;
        private static Dictionary<string, MagicBoxBuffJsonData> _byId;

        public static void EnsureLoaded()
        {
            if (_config != null) return;
            _config = JsonConfigLoader.LoadFromConfig<MagicBoxBuffCatalogJsonConfig>(
                AddressDefines.Config_MagicBoxBuffCatalog);

            _byId = new Dictionary<string, MagicBoxBuffJsonData>();
            if (_config?.buffs == null) return;

            foreach (MagicBoxBuffJsonData buff in _config.buffs)
            {
                if (buff == null || string.IsNullOrWhiteSpace(buff.id)) continue;
                _byId[buff.id] = buff;
            }
        }

        public static MagicBoxBuffJsonData GetById(string buffId)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(buffId) || _byId == null) return null;
            return _byId.TryGetValue(buffId, out MagicBoxBuffJsonData data) ? data : null;
        }

        public static IReadOnlyList<MagicBoxBuffJsonData> GetAll()
        {
            EnsureLoaded();
            return _config?.buffs ?? System.Array.Empty<MagicBoxBuffJsonData>();
        }

        public static int PickCandidateCount =>
            _config != null && _config.pickCandidateCount > 0 ? _config.pickCandidateCount : 3;
    }
}
