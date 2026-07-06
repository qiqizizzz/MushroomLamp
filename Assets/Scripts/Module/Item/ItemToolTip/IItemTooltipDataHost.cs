using UnityEngine;

namespace Module.Item
{
    public interface IItemTooltipDataHost
    {
        void ShowItemTooltipData(object owner, ItemTooltipData data, Vector2 screenPosition);
        void MoveItemTooltipData(Vector2 screenPosition);
        void HideItemTooltipData(object owner = null);
    }
}
