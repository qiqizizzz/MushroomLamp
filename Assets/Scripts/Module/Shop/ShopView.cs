using System.Collections.Generic;
using Common;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Shop
{
    public class ShopView : BaseView
    {
        private TextMeshProUGUI _txtGold;
        private TextMeshProUGUI _txtTitle;
        private TextMeshProUGUI _txtSubtitle;
        private TextMeshProUGUI _txtInfo;
        private Button _btnRefresh;
        private Button _btnRecycle;
        private Button _btnContinue;

        private readonly List<Transform> _cardSlots = new();
        private readonly List<Transform> _itemSlots = new();

        public override void InitUI()
        {
            _txtGold = Find<TextMeshProUGUI>("TopGold/Txt_GoldValue");
            _txtTitle = Find<TextMeshProUGUI>("Top/Txt_Title");
            _txtSubtitle = Find<TextMeshProUGUI>("Subtitle/Txt_Subtitle");
            _txtInfo = Find<TextMeshProUGUI>("Right/Txt_Info");
            _btnRefresh = Find<Button>("Bottom/Btn_RefreshShelf");
            _btnRecycle = Find<Button>("Bottom/Btn_Recycle");
            _btnContinue = Find<Button>("Bottom/Btn_Continue");

            bindButtons();
            collectSlots();
        }

        public void Refresh(ShopModel model)
        {
            if (model == null) return;
            if (_txtGold != null) _txtGold.text = $"金币 {model.Gold}";
            if (_txtTitle != null) _txtTitle.text = "黑猫夜市";
            if (_txtSubtitle != null) _txtSubtitle.text = "夜市补给铺·精选材料箱（卡包）";
            if (_txtInfo != null) _txtInfo.text = "本轮补给\n上回合回味 42\n下轮目标 55\n剩余回合 3/9\n\n当前金币 26\n\n下轮幸运牌生效\n普通火候｜草本加成";

            refreshSlots(_cardSlots, model.CardSlots, true);
            refreshSlots(_itemSlots, model.ItemSlots, false);
        }

        private void bindButtons()
        {
            bind(_btnRefresh, () => ApplyFunc("Shop.Refresh"));
            bind(_btnRecycle, () => ApplyFunc("Shop.Recycle"));
            bind(_btnContinue, () => ApplyFunc("Shop.Continue"));
        }

        private static void bind(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }

        private void collectSlots()
        {
            _cardSlots.Clear();
            _itemSlots.Clear();
            var middle = Find<Transform>("Middle");
            if (middle == null) return;

            foreach (Transform group in middle)
            {
                foreach (Transform slot in group)
                {
                    if (group.name.Contains("Card")) _cardSlots.Add(slot);
                    else _itemSlots.Add(slot);
                }
            }
        }

        private void refreshSlots(List<Transform> slots, IReadOnlyList<ShopSlotData> data, bool isCard)
        {
            int showCount = Mathf.Min(slots.Count, data?.Count ?? 0);
            for (int i = 0; i < slots.Count; i++)
            {
                Transform slot = slots[i];
                for (int c = slot.childCount - 1; c >= 0; c--) Destroy(slot.GetChild(c).gameObject);
                if (i >= showCount) continue;

                string prefabPath = isCard ? "UI/Shop/ShopCardSlot" : "UI/Shop/ShopPropSlot";
                GameObject obj = ResManager.Instantiate(prefabPath, slot);
                if (obj == null) continue;

                var binder = obj.GetComponent<ShopSlotBinder>();
                if (binder != null)
                    binder.Bind(data[i]);
            }
        }
    }
}
