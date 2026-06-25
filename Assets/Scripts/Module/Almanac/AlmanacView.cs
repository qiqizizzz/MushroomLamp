/*
* ┌──────────────────────────────────┐
* │  描    述: 图鉴界面（卡片/道具切换 + 循环网格）
* │  类    名: AlmanacView.cs
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Common;
using Common.Defines;
using Common.UI;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Almanac
{
    public class AlmanacView : BaseView
    {
        // 网格参数：4 列 × 可见 3 行（12），对象池 5 行（20，多 2 行缓冲防滚太快出空位）
        private const int Columns = 4;
        private const int PoolRows = 5;
        private static readonly Vector2 CellSize = new Vector2(160f, 200f);
        private static readonly Vector2 Spacing = new Vector2(20f, 20f);

        private Button _btnBack;
        private Button _btnTabCard;
        private Button _btnTabProp;
        private ScrollRect _scroll;

        private LoopGridView _grid;

        private bool _isCardTab = true;
        private readonly List<AlmanacEntry> _entries = new();

        // 运行时条目
        private class AlmanacEntry
        {
            public string name;
            public string iconPath;
        }

        // ---- 图鉴自带的 JSON 解析结构（与商店表结构同构，不依赖商店代码）----
        [Serializable]
        private class CatalogRow
        {
            public string id;
            public string name;
            public string iconPath;
            public string description;
            public int price;
        }

        [Serializable]
        private class CardCatalog
        {
            public CatalogRow[] cards;
        }

        [Serializable]
        private class ItemCatalog
        {
            public CatalogRow[] items;
        }
        // ----------------------------------------------------------------

        public override void InitUI()
        {
            _btnBack = Find<Button>("Btn_Back");
            _btnTabCard = Find<Button>("TopTabs/Btn_TabCard");
            _btnTabProp = Find<Button>("TopTabs/Btn_TabProp");
            _scroll = Find<ScrollRect>("ScrollView");
        }

        public override void InitData()
        {
            base.InitData();

            bindButton(_btnBack, () => ApplyFunc(EventDefines.AlmanacReturn));
            bindButton(_btnTabCard, () => ApplyFunc(EventDefines.AlmanacSwitchTab, true));
            bindButton(_btnTabProp, () => ApplyFunc(EventDefines.AlmanacSwitchTab, false));

            setupGrid();
        }

        public override void Open(params object[] args)
        {
            // 打开默认显示卡片页
            SwitchTab(true);
        }

        // 切换卡片/道具页（由 Controller 转发调用）
        public void SwitchTab(bool isCard)
        {
            _isCardTab = isCard;
            loadEntries(isCard);
            highlightTab(isCard);

            if (_grid != null)
                _grid.SetTotalCount(_entries.Count);
        }

        private void setupGrid()
        {
            if (_scroll == null)
            {
                QLog.Error($"[{nameof(AlmanacView)}] 未找到 ScrollView");
                return;
            }

            GameObject itemPrefab = ResManager.LoadAsset<GameObject>(AddressDefines.UI_ShopCardSlot);
            if (itemPrefab == null)
            {
                QLog.Error($"[{nameof(AlmanacView)}] 未找到 item 预制体：{AddressDefines.UI_ShopCardSlot}");
                return;
            }

            _grid = _scroll.gameObject.GetComponent<LoopGridView>();
            if (_grid == null) _grid = _scroll.gameObject.AddComponent<LoopGridView>();

            var padding = new RectOffset(20, 20, 20, 20);
            _grid.Init(_scroll, itemPrefab, Columns, PoolRows, CellSize, Spacing, onUpdateItem, padding);
        }

        // 网格刷新回调：把第 dataIndex 条数据填到 item 上
        private void onUpdateItem(int dataIndex, GameObject item)
        {
            if (dataIndex < 0 || dataIndex >= _entries.Count) return;
            AlmanacEntry entry = _entries[dataIndex];

            Transform iconTf = item.transform.Find("Img_Icon");
            if (iconTf != null)
            {
                Image img = iconTf.GetComponent<Image>();
                if (img != null)
                {
                    Sprite sprite = ArtAssetLoader.LoadSprite(entry.iconPath);
                    img.sprite = sprite;
                    img.enabled = sprite != null;
                }
            }

            Transform nameTf = item.transform.Find("Txt_Name");
            if (nameTf != null)
            {
                TextMeshProUGUI txt = nameTf.GetComponent<TextMeshProUGUI>();
                if (txt != null) txt.text = entry.name;
            }
        }

        // 读取卡片表 / 道具表
        private void loadEntries(bool isCard)
        {
            _entries.Clear();

            if (isCard)
            {
                var cfg = JsonConfigLoader.LoadFromConfig<CardCatalog>(AddressDefines.Config_CardParamCatalog);
                if (cfg?.cards != null)
                    foreach (var c in cfg.cards)
                        addEntry(c);
            }
            else
            {
                var cfg = JsonConfigLoader.LoadFromConfig<ItemCatalog>(AddressDefines.Config_ItemParamCatalog);
                if (cfg?.items != null)
                    foreach (var it in cfg.items)
                        addEntry(it);
            }
        }

        private void addEntry(CatalogRow row)
        {
            if (row == null) return;
            _entries.Add(new AlmanacEntry { name = row.name, iconPath = row.iconPath });
        }

        private void highlightTab(bool isCard)
        {
            if (_btnTabCard != null) _btnTabCard.transform.localScale = isCard ? Vector3.one * 1.1f : Vector3.one;
            if (_btnTabProp != null) _btnTabProp.transform.localScale = isCard ? Vector3.one : Vector3.one * 1.1f;
        }

        private static void bindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }
}
