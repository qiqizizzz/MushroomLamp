using Module.Cook;
using UnityEngine;

namespace Module.Card
{
    // 胡萝卜：入锅时，锅中每已有一个同标签（根茎）材料额外得分
    public sealed class CarrotAbility : CardAbility
    {
        private readonly int   _baseValue;
        private readonly float _requiredCookValue;
        private readonly string _tag;
        private readonly int   _bonusPerRootInPot;

        public CarrotAbility(CardDataEntry d)
        {
            _baseValue         = d.baseValue;
            _requiredCookValue = d.requiredCookValue;
            _tag               = d.tag;
            _bonusPerRootInPot = d.bonusPerRootInPot;
        }

        public override int    GetBaseValue(string materialName)         => _baseValue;
        public override float  GetRequiredCookValue(string materialName) => _requiredCookValue;
        public override string GetTag(string materialName)               => _tag;
        public override int    GetProcessBonus()                         => 0;

        public override void OnSubmitToPot(CookModel model)
        {
            if (_bonusPerRootInPot <= 0) return;
            int count = 0;
            foreach (var entry in model.PotEntries)
            {
                string primary = primaryTag(entry.TagText);
                if (primary == _tag) count++;
            }
            // 减去自身（刚刚入锅时已计入 PotEntries）
            count = Mathf.Max(0, count - 1);
            if (count > 0)
                model.AddBonus(count * _bonusPerRootInPot);
        }

        private static string primaryTag(string tagText) =>
            string.IsNullOrEmpty(tagText) ? string.Empty
                : tagText.Contains("/") ? tagText[..tagText.IndexOf('/')] : tagText;
    }
}
