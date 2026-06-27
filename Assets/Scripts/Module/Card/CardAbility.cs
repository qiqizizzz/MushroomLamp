using Module.Cook;

namespace Module.Card
{
    public class CardAbility
    {
        public static readonly CardAbility Default = new CardAbility();

        // 材料属性 — base 一律返回 0，具体卡牌必须从数据表读取并重写
        public virtual int GetBaseValue(string materialName) => 0;
        public virtual float GetRequiredCookValue(string materialName) => 0f;
        public virtual string GetTag(string materialName) => "素材";
        public virtual int GetProcessBonus() => 0;

        // 事件时机 — 默认空实现
        public virtual void OnDrawn(CookModel model) { }
        public virtual void OnPlaced(CookModel model, int slotIndex) { }
        public virtual void OnProcessed(CookModel model) { }
        public virtual void OnSubmitToPot(CookModel model) { }
    }
}
