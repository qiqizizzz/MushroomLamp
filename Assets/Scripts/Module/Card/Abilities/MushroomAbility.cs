using Module.Cook;

namespace Module.Card
{
    // 蘑菇：放入指定槽位（默认中心）时额外加分
    public sealed class MushroomAbility : CardAbility
    {
        private readonly int   _baseValue;
        private readonly float _requiredCookValue;
        private readonly string _tag;
        private readonly int   _processBonus;
        private readonly int   _centerSlotIndex;
        private readonly int   _centerSlotBonus;

        public MushroomAbility(CardDataEntry d)
        {
            _baseValue         = d.baseValue;
            _requiredCookValue = d.requiredCookValue;
            _tag               = d.tag;
            _processBonus      = d.processBonus;
            _centerSlotIndex   = d.centerSlotIndex;
            _centerSlotBonus   = d.centerSlotBonus;
        }

        public override int    GetBaseValue(string materialName)         => _baseValue;
        public override float  GetRequiredCookValue(string materialName) => _requiredCookValue;
        public override string GetTag(string materialName)               => _tag;
        public override int    GetProcessBonus()                         => _processBonus;

        public override void OnPlaced(CookModel model, int slotIndex)
        {
            if (_centerSlotBonus > 0 && slotIndex == _centerSlotIndex)
                model.AddBonus(_centerSlotBonus);
        }
    }
}
