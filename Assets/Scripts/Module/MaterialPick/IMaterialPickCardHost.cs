/*
* ┌──────────────────────────────────┐
* │  描    述: 材料三选一候选卡 Tooltip 宿主接口
* │  类    名: IMaterialPickCardHost.cs
* └──────────────────────────────────┘
*/

using Module.Cook;
using UnityEngine;

namespace Module.MaterialPick
{
    public interface IMaterialPickCardHost
    {
        void ShowCardTooltip(object owner, CookMaterialData materialData, Vector2 screenPosition);
        void MoveCardTooltip(Vector2 screenPosition);
        void HideCardTooltip(object owner = null);
    }
}
