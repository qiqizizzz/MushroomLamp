/*
* ┌──────────────────────────────────┐
* │  描    述: 通用循环复用网格列表（垂直滚动·环形对象池·容器模式）
* │  类    名: LoopGridView.cs
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Common.UI
{
    /// <summary>
    /// 通用循环网格（容器模式）：固定列数、垂直滚动。
    ///
    /// Content 下预先摆好 columns*poolRows 个“EmptyGo”容器（设计期占位）；
    /// Init 时在每个 EmptyGo 容器下实例化一个 slot 作为子物体。
    /// 滚动时移动的是 EmptyGo 容器（slot 随父级一起走），再刷新该容器内 slot 的数据，
    /// 从而避免随数据量增长创建更多 item。可应用于任意 ScrollView。
    ///
    /// 用法：
    ///   grid.Init(scrollRect, slotPrefab, columns, poolRows, cellSize, spacing, OnUpdateSlot, padding);
    ///   grid.SetTotalCount(dataCount);
    /// OnUpdateSlot(int dataIndex, GameObject slot) 负责把第 dataIndex 条数据填到 slot 上。
    /// </summary>
    public class LoopGridView : MonoBehaviour
    {
        private ScrollRect _scroll;
        private RectTransform _content;
        private Action<int, GameObject> _onUpdateSlot;

        private int _columns = 1;
        private int _poolRows = 1;
        private Vector2 _cellSize;
        private Vector2 _spacing;
        private RectOffset _padding;

        private int _totalCount;
        private int _totalRows;

        private RectTransform[] _cells;     // EmptyGo 容器池（被搬运的对象），长度 = columns * poolRows
        private GameObject[] _slots;        // 每个容器下挂的 slot
        private int[] _cellDataIndex;       // 每个容器当前代表的数据索引（-1=空）
        private int _firstRow = -1;
        private bool _inited;

        /// <summary>
        /// 初始化。Content 下需已有 columns*poolRows 个容器（EmptyGo）；
        /// 容器数量不足时自动补建，多余的会被禁用。
        /// </summary>
        public void Init(ScrollRect scroll, GameObject slotPrefab, int columns, int poolRows,
                         Vector2 cellSize, Vector2 spacing, Action<int, GameObject> onUpdateSlot,
                         RectOffset padding = null)
        {
            if (scroll == null || slotPrefab == null || onUpdateSlot == null)
            {
                Debug.LogError("[LoopGridView] Init 参数不合法（scroll/slotPrefab/onUpdateSlot 不能为空）");
                return;
            }

            _scroll = scroll;
            _content = scroll.content;
            _columns = Mathf.Max(1, columns);
            _poolRows = Mathf.Max(1, poolRows);
            _cellSize = cellSize;
            _spacing = spacing;
            _padding = padding ?? new RectOffset(0, 0, 0, 0);
            _onUpdateSlot = onUpdateSlot;

            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(0f, 1f);
            _content.pivot = new Vector2(0f, 1f);

            prepareCells(slotPrefab);

            _scroll.onValueChanged.RemoveListener(onScroll);
            _scroll.onValueChanged.AddListener(onScroll);
            _inited = true;
        }

        /// <summary>设置数据总数并刷新（切换数据源时调用）。</summary>
        public void SetTotalCount(int count)
        {
            if (!_inited) return;

            _totalCount = Mathf.Max(0, count);
            _totalRows = Mathf.CeilToInt(_totalCount / (float)_columns);

            resizeContent();

            _firstRow = -1;
            _content.anchoredPosition = Vector2.zero;
            relayout(0);
        }

        /// <summary>用当前数据重刷可见 slot。</summary>
        public void Refresh()
        {
            if (!_inited) return;
            int first = calcFirstRow();
            _firstRow = -1;
            relayout(first);
        }

        // 复用 Content 下现有 EmptyGo 容器，并在每个容器下挂一个 slot
        private void prepareCells(GameObject slotPrefab)
        {
            int poolCount = _columns * _poolRows;

            // 收集 Content 下现有容器（设计期占位 EmptyGo）
            var existing = new List<RectTransform>();
            for (int i = 0; i < _content.childCount; i++)
            {
                var child = _content.GetChild(i) as RectTransform;
                if (child != null) existing.Add(child);
            }

            _cells = new RectTransform[poolCount];
            _slots = new GameObject[poolCount];
            _cellDataIndex = new int[poolCount];

            for (int i = 0; i < poolCount; i++)
            {
                RectTransform cell;
                if (i < existing.Count)
                {
                    cell = existing[i];           // 复用现有 EmptyGo
                }
                else
                {
                    var go = new GameObject("EmptyGo_" + i, typeof(RectTransform));
                    cell = go.GetComponent<RectTransform>();
                    cell.SetParent(_content, false);   // 数量不足时补建
                }

                cell.anchorMin = new Vector2(0f, 1f);
                cell.anchorMax = new Vector2(0f, 1f);
                cell.pivot = new Vector2(0.5f, 0.5f);
                cell.sizeDelta = _cellSize;
                cell.gameObject.SetActive(true);

                // 容器下若没有 slot 就实例化一个；保证 slot 始终挂在 EmptyGo 下
                GameObject slot = cell.childCount > 0 ? cell.GetChild(0).gameObject : null;
                if (slot == null)
                    slot = Instantiate(slotPrefab, cell);

                var slotRt = slot.GetComponent<RectTransform>();
                if (slotRt == null) slotRt = slot.AddComponent<RectTransform>();
                // slot 填满容器
                slotRt.anchorMin = Vector2.zero;
                slotRt.anchorMax = Vector2.one;
                slotRt.offsetMin = Vector2.zero;
                slotRt.offsetMax = Vector2.zero;
                slotRt.localScale = Vector3.one;

                _cells[i] = cell;
                _slots[i] = slot;
                _cellDataIndex[i] = -1;
            }

            // 多余的现有容器禁用
            for (int i = poolCount; i < existing.Count; i++)
                existing[i].gameObject.SetActive(false);
        }

        private void resizeContent()
        {
            float height = _padding.top + _padding.bottom;
            if (_totalRows > 0)
                height += _totalRows * _cellSize.y + (_totalRows - 1) * _spacing.y;

            float width = _padding.left + _padding.right
                          + _columns * _cellSize.x + (_columns - 1) * _spacing.x;

            _content.sizeDelta = new Vector2(width, height);
        }

        private int calcFirstRow()
        {
            float y = _content.anchoredPosition.y;
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

        // 把容器池重新映射到从 firstRow 开始的若干行
        private void relayout(int firstRow)
        {
            _firstRow = firstRow;

            for (int r = 0; r < _poolRows; r++)
            {
                int dataRow = firstRow + r;
                // 物理槽用 dataRow % poolRows 环形映射：
                // 向下滚动时，移出顶部的整行容器（EmptyGo）正好被复用为底部新行
                int slotRow = ((dataRow % _poolRows) + _poolRows) % _poolRows;

                for (int c = 0; c < _columns; c++)
                {
                    int cellIdx = slotRow * _columns + c;
                    RectTransform cell = _cells[cellIdx];

                    int dataIndex = dataRow * _columns + c;
                    bool valid = dataRow < _totalRows && dataIndex < _totalCount;

                    // 移动的是 EmptyGo 容器（slot 是它的子物体，随之一起移动）
                    float x = _padding.left + c * (_cellSize.x + _spacing.x) + _cellSize.x * 0.5f;
                    float y = -(_padding.top + dataRow * (_cellSize.y + _spacing.y) + _cellSize.y * 0.5f);
                    cell.anchoredPosition = new Vector2(x, y);

                    if (cell.gameObject.activeSelf != valid)
                        cell.gameObject.SetActive(valid);

                    // 只刷新 slot 数据
                    if (valid && _cellDataIndex[cellIdx] != dataIndex)
                    {
                        _cellDataIndex[cellIdx] = dataIndex;
                        _onUpdateSlot(dataIndex, _slots[cellIdx]);
                    }
                    else if (!valid)
                    {
                        _cellDataIndex[cellIdx] = -1;
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
