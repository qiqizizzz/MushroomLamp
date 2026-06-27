using Module.Cook;

namespace Module.Item
{
    // 道具效果基类，每个道具必须实现 OnUse
    public abstract class ItemEffect
    {
        public abstract void OnUse(CookModel model);
    }
}
