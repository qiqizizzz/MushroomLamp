/*
* ┌──────────────────────────────────┐
* │  描    述: 通用循环复用网格列表（垂直滚动·环形对象池）
* │  类    名: LoopGridView.cs
* └──────────────────────────────────┘
*/

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Common.UI
{
    /// <summary>
    /// 通用循环网格：固定列数、垂直滚动。
    /// 只创建 columns * poolRows 个 item，向下滚动时把移出顶部的整行 item 复用到底部并刷新数据，
    /// 从而避免随数据量增长不断创建 item。可应用于任意 ScrollView。
    ///
    /// 用法：
    ///   var grid = gameObject.AddComponent&lt;LoopGridView&gt;();
    ///   grid.Init(scrollRect, itemPrefab, columns, poolRows, cellSize, spacing, OnUpdateItem);
    ///   grid.SetTotalCount(dataCount);
    /// 其中 OnUpdateItem(int dataIndex, GameObject item) 负责把第 dataIndex 条数据填到 item 上。
    /// </summary>
    public class LoopGridView : MonoBehaviour
    {
        private ScrollRect _scroll;
        private RectTransform _content;
        private GameObject _itemPrefab;
        private Action<int, GameObject> _onUpdateItem;

        private int _columns = 1;       // 列数（横向，固定）
        private int _poolRows = 1;      // 池子行数（含缓冲，建立的物理行数）
        private Vector2 _cellSize;      // 单元格尺寸
        private Vector2 _spacing;       // 间距 (x=列间距, y=行间距)
        private RectOffset _padding;    // 四周留白

        private int _totalCount;        // 数据总数
        private int _totalRows;         // 数据总行数 = ceil(total / columns)

        private RectTransform[] _items; // 物理 item 池，长度 = columns * poolRows
        private int[] _itemDataIndex;   // 每个物理 item 当前代表的数据索引（-1 表示空）
        private int _firstRow = -1;     // 当前池子覆盖的首个数据行
        private bool _inited;

        /// <summary>初始化（只需调用一次）。</summary>
        public void Init(ScrollRect scroll, GameObject itemPrefab, int columns, int poolRows,
                         Vector2 cellSize, Vector2 spacing, Action<int, GameObject> onUpdateItem,
                         RectOffset padding = null)
        {
            if (scroll == null || itemPrefab == null || onUpdateItem == null)
            {
                Debug.LogError("[LoopGridView] Init 参数不合法（scroll/itemPrefab/onUpdateItem 不能为空）");
                return;
            }

            _scroll = scroll;
            _content = scroll.content;
            _itemPrefab = itemPrefab;
            _columns = Mathf.Max(1, columns);
            _poolRows = Mathf.Max(1, poolRows);
            _cellSize = cellSize;
            _spacing = spacing;
            _padding = padding ?? new RectOffset(0, 0, 0, 0);
            _onUpdateItem = onUpdateItem;

            // Content 锚定到顶部、pivot 顶部，便于按行号往下排布（y 向下为负）
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(0f, 1f);
            _content.pivot = new Vector2(0f, 1f);

            buildPool();
            _scroll.onValueChanged.RemoveListener(onScroll);
            _scroll.onValueChanged.AddListener(onScroll);
            _inited = true;
        }

        /// <summary>设置数据总数并刷新（切换数据源时调用，如卡片/道具切页）。</summary>
        public void SetTotalCount(int count)
        {
            if (!_inited) return;

            _totalCount = Mathf.Max(0, count);
            _totalRows = Mathf.CeilToInt(_totalCount / (float)_columns);

            resizeContent();

            // 重置到顶部，强制全量重排
            _firstRow = -1;
            _content.anchoredPosition = Vector2.zero;
            relayout(0);
        }

        /// <summary>用当前数据重刷可见 item。</summary>
        public void Refresh()
        {
            if (!_inited) return;
            int first = calcFirstRow();
            _firstRow = -1; // 强制刷新
            relayout(first);
        }

        // 创建物理 item 池
        private void buildPool()
        {
            // 清空 Content 现有子物体（设计期占位 EmptyGo 等）
            for (int i = _content.childCount - 1; i >= 0; i--)
                DestroyImmediate(_content.GetChild(i).gameObject);

            int poolCount = _columns * _poolRows;
            _items = new RectTransform[poolCount];
            _itemDataIndex = new int[poolCount];

            for (int i = 0; i < poolCount; i++)
            {
                GameObject go = Instantiate(_itemPrefab, _content);
                go.name = $"{_itemPrefab.name}_{i}";
                var rt = go.GetComponent<RectTransform>();
                if (rt == null) rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = _cellSize;
                _items[i] = rt;
                _itemDataIndex[i] = -1;
            }
        }

        // 根据总行数撑开 Content 高度
        private void resizeContent()
        {
            float height = _padding.top + _padding.bottom;
            if (_totalRows > 0)
                height += _totalRows * _cellSize.y + (_totalRows - 1) * _spacing.y;

            float width = _padding.left + _padding.right
                          + _columns * _cellSize.x + (_columns - 1) * _spacing.x;

            _content.sizeDelta = new Vector2(width, height);
        }

        // 当前滚动位置对应的首个可见数据行
        private int calcFirstRow()
        {
            float y = _content.anchoredPosition.y; // 向下滚动 y 增大
            float rowH = _cellSize.y + _spacing.y;
            if (rowH <= 0f) return 0;

            int row = Mathf.FloorToInt((y - _padding.top) / rowH);
            return Mathf.Clamp(row, 0, Mathf.Max(0, _totalRows - _poolRows));
        }

        private void onScroll(Vector2 _)
        {
            int first = calcFirstRow();
            if (first != _firstRow)
                relayout(first);
        }

        // 把池子重新映射到从 firstRow 开始的若干行
        private void relayout(int firstRow)
        {
            _firstRow = firstRow;

            for (int r = 0; r < _poolRows; r++)
            {
                int dataRow = firstRow + r;
                // 物理槽位用 dataRow % poolRows 做环形映射：
                // 向下滚动 dataRow 增大时，移出顶部的物理行正好被复用为底部新行
                int slotRow = ((dataRow % _poolRows) + _poolRows) % _poolRows;

                for (int c = 0; c < _columns; c++)
                {
                    int slot = slotRow * _columns + c;
                    RectTransform item = _items[slot];

                    int dataIndex = dataRow * _columns + c;
                    bool valid = dataRow < _totalRows && dataIndex < _totalCount;

                    // 定位到该数据行/列
                    float x = _padding.left + c * (_cellSize.x + _spacing.x) + _cellSize.x * 0.5f;
                    float y = -(_padding.top + dataRow * (_cellSize.y + _spacing.y) + _cellSize.y * 0.5f);
                    item.anchoredPosition = new Vector2(x, y);

                    if (item.gameObject.activeSelf != valid)
                        item.gameObject.SetActive(valid);

                    // 数据变了才回调刷新
                    if (valid && _itemDataIndex[slot] != dataIndex)
                    {
                        _itemDataIndex[slot] = dataIndex;
                        _onUpdateItem(dataIndex, item.gameObject);
                    }
                    else if (!valid)
                    {
                        _itemDataIndex[slot] = -1;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_scroll != null)
                _scroll.onValueChanged.RemoveListener(onScroll);
        }
    }
}
