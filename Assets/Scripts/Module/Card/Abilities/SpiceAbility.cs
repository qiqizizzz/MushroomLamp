using Module.Cook;

namespace Module.Card
{
    // 香料：入锅时，锅中每有一个非同标签材料得额外分（鼓励多样性组合）
    public sealed class SpiceAbility : CardAbility
    {
        private readonly int   _baseValue;
        private readonly float _requiredCookValue;
        private readonly string _tag;
        private readonly int   _processBonus;
        private readonly int   _crossTagBonus;

        public SpiceAbility(CardDataEntry d)
        {
            _baseValue         = d.baseValue;
            _requiredCookValue = d.requiredCookValue;
            _tag               = d.tag;
            _processBonus      = d.processBonus;
            _crossTagBonus     = d.crossTagBonus;
        }

        public override int    GetBaseValue(string materialName)         => _baseValue;
        public override float  GetRequiredCookValue(string materialName) => _requiredCookValue;
        public override string GetTag(string materialName)               => _tag;
        public override int    GetProcessBonus()                         => _processBonus;

        public override void OnSubmitToPot(CookModel model)
        {
            if (_crossTagBonus <= 0) return;
            int count = 0;
            foreach (var entry in model.PotEntries)
            {
                string primary = primaryTag(entry.TagText);
                if (primary != _tag)
                    count++;
            }
            if (count > 0)
                model.AddBonus(count * _crossTagBonus);
        }

        private static string primaryTag(string tagText) =>
            string.IsNullOrEmpty(tagText) ? string.Empty
                : tagText.Contains("/") ? tagText[..tagText.IndexOf('/')] : tagText;
    }
}
