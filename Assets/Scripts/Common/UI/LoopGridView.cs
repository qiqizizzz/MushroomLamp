/*
* ┌──────────────────────────────────┐
* │  描    述: 通用循环复用网格列表（垂直/水平滚动·环形对象池·容器模式）
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
    /// 通用循环网格（容器模式）：支持垂直或水平滚动。
    ///
    /// 概念抽象（与方向无关）：
    ///   - 交叉轴（cross）：与滚动方向垂直的那一维，数量固定 = crossCount。
    ///     垂直滚动时交叉轴是“列”（columns）；水平滚动时交叉轴是“行”（rows）。
    ///   - 主轴（line）：滚动方向那一维，按需环形复用，池中常驻 poolLines 条线。
    ///     垂直滚动时主轴是“行”；水平滚动时主轴是“列”。
    ///
    /// Content 下预先摆好 crossCount*poolLines 个“EmptyGo”容器（设计期占位，不足自动补建）；
    /// Init 时在每个容器下挂一个 slot。滚动时移动容器（slot 随父级一起走）并刷新数据，
    /// 主轴上滚出可视区的整条线（容器）会被复用到另一端，从而数量恒定。
    ///
    /// 垂直用法（向后兼容）：
    ///   grid.Init(scroll, slotPrefab, columns, poolRows, cellSize, spacing, OnUpdateSlot, padding);
    /// 水平用法：
    ///   grid.InitHorizontal(scroll, slotPrefab, rows, poolColumns, cellSize, spacing, OnUpdateSlot, padding);
    ///
    /// 两者都通过 SetTotalCount(dataCount) 设置数据总数。
    /// OnUpdateSlot(int dataIndex, GameObject slot) 负责把第 dataIndex 条数据填到 slot 上。
    /// </summary>
    public class LoopGridView : MonoBehaviour
    {
        public enum Direction { Vertical, Horizontal }

        private ScrollRect _scroll;
        private RectTransform _content;
        private Action<int, GameObject> _onUpdateSlot;

        private Direction _direction = Direction.Vertical;
        private int _crossCount = 1;   // 交叉轴格子数（垂直=列数；水平=行数）
        private int _poolLines = 1;     // 主轴常驻线数（垂直=行；水平=列）
        private Vector2 _cellSize;
        private Vector2 _spacing;
        private RectOffset _padding;

        private int _totalCount;
        private int _totalLines;        // 主轴总线数

        private RectTransform[] _cells;   // EmptyGo 容器池，长度 = crossCount * poolLines
        private GameObject[] _slots;      // 每个容器下挂的 slot
        private int[] _cellDataIndex;     // 每个容器当前代表的数据索引（-1=空）
        private int _firstLine = -1;
        private bool _inited;

        /// <summary>初始化（垂直滚动，向后兼容旧签名）。columns=列数，poolRows=池中行数。</summary>
        public void Init(ScrollRect scroll, GameObject slotPrefab, int columns, int poolRows,
                         Vector2 cellSize, Vector2 spacing, Action<int, GameObject> onUpdateSlot,
                         RectOffset padding = null)
        {
            InitInternal(Direction.Vertical, scroll, slotPrefab, columns, poolRows, cellSize, spacing, onUpdateSlot, padding);
        }

        /// <summary>初始化（水平滚动）。rows=行数（通常 1），poolColumns=池中列数。</summary>
        public void InitHorizontal(ScrollRect scroll, GameObject slotPrefab, int rows, int poolColumns,
                                   Vector2 cellSize, Vector2 spacing, Action<int, GameObject> onUpdateSlot,
                                   RectOffset padding = null)
        {
            InitInternal(Direction.Horizontal, scroll, slotPrefab, rows, poolColumns, cellSize, spacing, onUpdateSlot, padding);
        }

        private void InitInternal(Direction direction, ScrollRect scroll, GameObject slotPrefab,
                                  int crossCount, int poolLines, Vector2 cellSize, Vector2 spacing,
                                  Action<int, GameObject> onUpdateSlot, RectOffset padding)
        {
            if (scroll == null || slotPrefab == null || onUpdateSlot == null)
            {
                Debug.LogError("[LoopGridView] Init 参数不合法（scroll/slotPrefab/onUpdateSlot 不能为空）");
                return;
            }

            _direction = direction;
            _scroll = scroll;
            _content = scroll.content;
            _crossCount = Mathf.Max(1, crossCount);
            _poolLines = Mathf.Max(1, poolLines);
            _cellSize = cellSize;
            _spacing = spacing;
            _padding = padding ?? new RectOffset(0, 0, 0, 0);
            _onUpdateSlot = onUpdateSlot;

            // Content 锚点/轴心置于左上，所有定位以左上为原点（X 向右为正，Y 向下为负）
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
            _totalLines = Mathf.CeilToInt(_totalCount / (float)_crossCount);

            resizeContent();

            _firstLine = -1;
            _content.anchoredPosition = Vector2.zero;
            relayout(0);
        }

        /// <summary>用当前数据重刷可见 slot。</summary>
        public void Refresh()
        {
            if (!_inited) return;
            int first = calcFirstLine();
            _firstLine = -1;
            relayout(first);
        }

        // 复用 Content 下现有 EmptyGo 容器，并在每个容器下挂一个 slot
        private void prepareCells(GameObject slotPrefab)
        {
            int poolCount = _crossCount * _poolLines;

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
                    cell = existing[i];
                }
                else
                {
                    var go = new GameObject("EmptyGo_" + i, typeof(RectTransform));
                    cell = go.GetComponent<RectTransform>();
                    cell.SetParent(_content, false);
                }

                cell.anchorMin = new Vector2(0f, 1f);
                cell.anchorMax = new Vector2(0f, 1f);
                cell.pivot = new Vector2(0.5f, 0.5f);
                cell.sizeDelta = _cellSize;
                cell.gameObject.SetActive(true);

                GameObject slot = cell.childCount > 0 ? cell.GetChild(0).gameObject : null;
                if (slot == null)
                    slot = Instantiate(slotPrefab, cell);
                if (!slot.activeSelf) slot.SetActive(true);   // 模板可能为禁用态，实例化后需激活

                var slotRt = slot.GetComponent<RectTransform>();
                if (slotRt == null) slotRt = slot.AddComponent<RectTransform>();
                slotRt.anchorMin = Vector2.zero;
                slotRt.anchorMax = Vector2.one;
                slotRt.offsetMin = Vector2.zero;
                slotRt.offsetMax = Vector2.zero;
                slotRt.localScale = Vector3.one;

                _cells[i] = cell;
                _slots[i] = slot;
                _cellDataIndex[i] = -1;
            }

            for (int i = poolCount; i < existing.Count; i++)
                existing[i].gameObject.SetActive(false);
        }

        // 主轴方向上每条线的步进（cell + 间距）
        private float lineStride()
        {
            return _direction == Direction.Vertical
                ? _cellSize.y + _spacing.y
                : _cellSize.x + _spacing.x;
        }

        private void resizeContent()
        {
            if (_direction == Direction.Vertical)
            {
                float height = _padding.top + _padding.bottom;
                if (_totalLines > 0)
                    height += _totalLines * _cellSize.y + (_totalLines - 1) * _spacing.y;

                float width = _padding.left + _padding.right
                              + _crossCount * _cellSize.x + (_crossCount - 1) * _spacing.x;

                _content.sizeDelta = new Vector2(width, height);
            }
            else
            {
                float width = _padding.left + _padding.right;
                if (_totalLines > 0)
                    width += _totalLines * _cellSize.x + (_totalLines - 1) * _spacing.x;

                float height = _padding.top + _padding.bottom
                               + _crossCount * _cellSize.y + (_crossCount - 1) * _spacing.y;

                _content.sizeDelta = new Vector2(width, height);
            }
        }

        // 计算当前可视区主轴上的“首行/首列”索引
        private int calcFirstLine()
        {
            float stride = lineStride();
            if (stride <= 0f) return 0;

            float offset;
            if (_direction == Direction.Vertical)
                offset = _content.anchoredPosition.y - _padding.top; // 向下滚 content.y 增大
            else
                offset = -_content.anchoredPosition.x - _padding.left; // 向左滚 content.x 减小

            int line = Mathf.FloorToInt(offset / stride);
            return Mathf.Clamp(line, 0, Mathf.Max(0, _totalLines - _poolLines));
        }

        private void onScroll(Vector2 _)
        {
            int first = calcFirstLine();
            if (first != _firstLine)
                relayout(first);
        }

        // 把容器池重新映射到从 firstLine 开始的若干条主轴线
        private void relayout(int firstLine)
        {
            _firstLine = firstLine;

            for (int l = 0; l < _poolLines; l++)
            {
                int dataLine = firstLine + l;
                // 环形映射：滚出一端的整条线（容器）被复用到另一端
                int slotLine = ((dataLine % _poolLines) + _poolLines) % _poolLines;

                for (int c = 0; c < _crossCount; c++)
                {
                    int cellIdx = slotLine * _crossCount + c;
                    RectTransform cell = _cells[cellIdx];

                    int dataIndex = dataLine * _crossCount + c;
                    bool valid = dataLine < _totalLines && dataIndex < _totalCount;

                    cell.anchoredPosition = cellPosition(dataLine, c);

                    if (cell.gameObject.activeSelf != valid)
                        cell.gameObject.SetActive(valid);

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

        // 计算容器在 Content 内的锚定位置（左上为原点）
        private Vector2 cellPosition(int dataLine, int crossIndex)
        {
            if (_direction == Direction.Vertical)
            {
                // 交叉轴=列(crossIndex)，主轴=行(dataLine)
                float x = _padding.left + crossIndex * (_cellSize.x + _spacing.x) + _cellSize.x * 0.5f;
                float y = -(_padding.top + dataLine * (_cellSize.y + _spacing.y) + _cellSize.y * 0.5f);
                return new Vector2(x, y);
            }
            else
            {
                // 交叉轴=行(crossIndex)，主轴=列(dataLine)
                float x = _padding.left + dataLine * (_cellSize.x + _spacing.x) + _cellSize.x * 0.5f;
                float y = -(_padding.top + crossIndex * (_cellSize.y + _spacing.y) + _cellSize.y * 0.5f);
                return new Vector2(x, y);
            }
        }

        private void OnDestroy()
        {
            if (_scroll != null)
                _scroll.onValueChanged.RemoveListener(onScroll);
        }
    }
}
