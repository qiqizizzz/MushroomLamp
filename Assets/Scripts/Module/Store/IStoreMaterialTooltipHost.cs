using UnityEngine;

namespace Module.Store
{
    public interface IStoreMaterialTooltipHost
    {
        void ShowMaterialTooltip(object owner, string materialId, Vector2 screenPosition);
        void MoveMaterialTooltip(Vector2 screenPosition);
        void HideMaterialTooltip(object owner = null);
    }
}
