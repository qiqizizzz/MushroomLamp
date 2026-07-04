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
        public string rarity;
        public string itemCategory;
        public string effectType;
        public string effectTarget;
        public float effectValue;
        public string triggerType;
        public string durationType;
        public string resetRule;
        public bool stackable;
    }
}
