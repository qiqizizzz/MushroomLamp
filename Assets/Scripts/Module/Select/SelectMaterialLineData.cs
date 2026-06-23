/*
 * ┌──────────────────────────────────┐
 * │  描    述: 选择界面材料行数据
 * │  类    名: SelectMaterialLineData.cs
 * └──────────────────────────────────┘
 */

using System;
using UnityEngine;

namespace Module.Select
{
    [Serializable]
    public class SelectMaterialLineData
    {
        public Sprite icon;
        public string label;
        public int count = 1;

        public string CountText => $"x{Mathf.Max(0, count)}";
    }
}
