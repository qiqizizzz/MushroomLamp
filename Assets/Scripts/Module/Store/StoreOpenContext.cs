/*
* ┌──────────────────────────────────┐
* │  描    述: 打开 StoreView 时的上下文（购箱后选卡）
* │  类    名: StoreOpenContext.cs
* └──────────────────────────────────┘
*/

using System;

namespace Module.Store
{
    [Serializable]
    public class StoreOpenContext
    {
        public string boxId;
        public string boxName;
        // 购箱后选卡：卡牌已含在箱价内，不再额外扣金币
        public bool cardsIncludedInBoxPrice = true;
    }
}
