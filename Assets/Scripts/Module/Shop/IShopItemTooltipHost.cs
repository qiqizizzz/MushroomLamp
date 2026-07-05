using UnityEngine;

namespace Module.Shop
{
    public interface IShopItemTooltipHost
    {
        void ShowShopTooltip(object owner, ShopSlotData slotData, Vector2 screenPosition);
        void MoveShopTooltip(Vector2 screenPosition);
        void HideShopTooltip(object owner = null);
    }
}
