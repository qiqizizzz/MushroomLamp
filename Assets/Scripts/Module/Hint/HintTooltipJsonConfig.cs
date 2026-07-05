using System;

namespace Module.Hint
{
    [Serializable]
    public class HintTooltipCatalogJsonConfig
    {
        public HintTooltipJsonData[] hints;
    }

    [Serializable]
    public class HintTooltipJsonData
    {
        public string id;
        public string title;
        public string description;
    }
}
