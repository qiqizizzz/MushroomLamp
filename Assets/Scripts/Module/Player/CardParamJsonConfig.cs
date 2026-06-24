/*
* ┌──────────────────────────────────┐
* │  描    述: 卡牌参数 JSON 配置结构
* │  类    名: CardParamJsonConfig.cs
* └──────────────────────────────────┘
*/

using System;

namespace Module.Player
{
    [Serializable]
    public class CardParamCatalogJsonConfig
    {
        public string defaultCardId;
        public CardParamJsonData[] cards;
    }

    [Serializable]
    public class CardParamJsonData
    {
        public string id;
        public string name;
        public string iconPath;
        public string description;
        public int price;
    }
}
