using Module.Cook;

namespace Module.Card
{
    // 南瓜：入锅时固定额外加分
    public sealed class PumpkinAbility : CardAbility
    {
        private readonly int   _baseValue;
        private readonly float _requiredCookValue;
        private readonly string _tag;
        private readonly int   _processBonus;
        private readonly int   _submitBonus;

        public PumpkinAbility(CardDataEntry d)
        {
            _baseValue         = d.baseValue;
            _requiredCookValue = d.requiredCookValue;
            _tag               = d.tag;
            _processBonus      = d.processBonus;
            _submitBonus       = d.submitBonus;
        }

        public override int    GetBaseValue(string materialName)         => _baseValue;
        public override float  GetRequiredCookValue(string materialName) => _requiredCookValue;
        public override string GetTag(string materialName)               => _tag;
        public override int    GetProcessBonus()                         => _processBonus;

        public override void OnSubmitToPot(CookModel model)
        {
            if (_submitBonus > 0)
                model.AddBonus(_submitBonus);
        }
    }
}
