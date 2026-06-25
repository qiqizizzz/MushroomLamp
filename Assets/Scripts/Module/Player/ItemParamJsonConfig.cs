using System;

namespace Module.Player
{
    [Serializable]
    public class ItemParamCatalogJsonConfig
    {
        public string defaultItemId;
        public ItemParamJsonData[] items;
    }

    [Serializable]
    public class ItemParamJsonData
    {
        public string id;
        public string name;
        public string iconPath;
        public string description;
        public int price;
    }
}
