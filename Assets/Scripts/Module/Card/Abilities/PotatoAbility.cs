using Module.Cook;

namespace Module.Card
{
    // 土豆：研磨加值高于普通材料
    public sealed class PotatoAbility : CardAbility
    {
        private readonly int   _baseValue;
        private readonly float _requiredCookValue;
        private readonly string _tag;
        private readonly int   _processBonus;

        public PotatoAbility(CardDataEntry d)
        {
            _baseValue         = d.baseValue;
            _requiredCookValue = d.requiredCookValue;
            _tag               = d.tag;
            _processBonus      = d.processBonus;
        }

        public override int    GetBaseValue(string materialName)         => _baseValue;
        public override float  GetRequiredCookValue(string materialName) => _requiredCookValue;
        public override string GetTag(string materialName)               => _tag;
        public override int    GetProcessBonus()                         => _processBonus;
    }
}
