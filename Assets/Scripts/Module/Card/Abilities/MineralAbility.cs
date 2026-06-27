using Module.Cook;

namespace Module.Card
{
    // 矿物：入锅时扩展目标上限
    public sealed class MineralAbility : CardAbility
    {
        private readonly int   _baseValue;
        private readonly float _requiredCookValue;
        private readonly string _tag;
        private readonly int   _processBonus;
        private readonly int   _targetExpand;

        public MineralAbility(CardDataEntry d)
        {
            _baseValue         = d.baseValue;
            _requiredCookValue = d.requiredCookValue;
            _tag               = d.tag;
            _processBonus      = d.processBonus;
            _targetExpand      = d.targetExpand;
        }

        public override int    GetBaseValue(string materialName)         => _baseValue;
        public override float  GetRequiredCookValue(string materialName) => _requiredCookValue;
        public override string GetTag(string materialName)               => _tag;
        public override int    GetProcessBonus()                         => _processBonus;

        public override void OnSubmitToPot(CookModel model)
        {
            if (_targetExpand > 0)
                model.ExpandTarget(_targetExpand);
        }
    }
}
