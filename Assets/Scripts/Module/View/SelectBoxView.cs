/*
 * ┌──────────────────────────────────┐
 * │  描    述: 材料箱选择界面
 * │  类    名: SelectBoxView.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Common;
using Common.Defines;
using Module.Select;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class SelectBoxView : BaseView
    {
        private const float LineHeight = 72f;

        private Transform _lineContent;
        private TextMeshProUGUI _txtTitle;
        private readonly List<Transform> _emptySlots = new();
        private readonly List<TextLineItem> _lineItems = new();

        public override void InitUI()
        {
            _txtTitle = Find<TextMeshProUGUI>("Left/Txt_Title");
            _lineContent = Find<Transform>("Left/ScrollView/Viewport/Content");
            collectEmptySlots();
        }

        public override void Open(params object[] args)
        {
            SelectBoxJsonConfig config = resolveConfig(args);
            refreshLeftPanel(config);
        }

        public override void Close(params object[] args)
        {
            clearLines();
            base.Close(args);
        }

        private SelectBoxJsonConfig resolveConfig(object[] args)
        {
            if (args != null && args.Length > 0 && args[0] is SelectBoxJsonConfig config)
                return config;

            return JsonConfigLoader.LoadFromConfig<SelectBoxJsonConfig>(AddressDefines.Config_SelectBox);
        }

        private void refreshLeftPanel(SelectBoxJsonConfig config)
        {
            if (config == null)
            {
                QLog.Warning($"[{nameof(SelectBoxView)}] 未找到 JSON 配置，请检查 Assets/Config/{AddressDefines.Config_SelectBox}.json");
                return;
            }

            if (_txtTitle != null)
                _txtTitle.text = string.IsNullOrEmpty(config.summaryTitle) ? "简介" : config.summaryTitle;

            refreshLines(config.ToRuntimeLines());
        }

        private void collectEmptySlots()
        {
            _emptySlots.Clear();
            if (_lineContent == null) return;

            for (int i = 0; i < _lineContent.childCount; i++)
            {
                Transform child = _lineContent.GetChild(i);
                if (child.name.StartsWith("EmptyGo"))
                    _emptySlots.Add(child);
            }

            _emptySlots.Sort((a, b) => getEmptyGoOrder(a).CompareTo(getEmptyGoOrder(b)));
        }

        private static int getEmptyGoOrder(Transform transform)
        {
            const string prefix = "EmptyGo_";
            string name = transform.name;
            if (name.StartsWith(prefix) && int.TryParse(name.Substring(prefix.Length), out int index))
                return index;

            return int.MaxValue;
        }

        private void refreshLines(IReadOnlyList<SelectMaterialLineData> lines)
        {
            clearLines();
            if (_emptySlots.Count == 0)
            {
                QLog.Error($"[{nameof(SelectBoxView)}] Content 下未找到 EmptyGo 节点");
                return;
            }

            if (lines == null) return;

            int showCount = Mathf.Min(lines.Count, _emptySlots.Count);
            if (lines.Count > _emptySlots.Count)
                QLog.Warning($"[{nameof(SelectBoxView)}] 配置 {lines.Count} 条，EmptyGo 仅 {_emptySlots.Count} 个，超出部分不显示");

            for (int i = 0; i < showCount; i++)
            {
                SelectMaterialLineData lineData = lines[i];
                if (lineData == null) continue;

                Transform slot = _emptySlots[i];
                slot.gameObject.SetActive(true);

                GameObject lineObj = ResManager.Instantiate(AddressDefines.UI_TextLine, slot);
                if (lineObj == null) continue;

                setupLineInSlot(slot, lineObj);

                TextLineItem item = lineObj.GetComponent<TextLineItem>();
                if (item == null)
                    item = lineObj.AddComponent<TextLineItem>();

                item.Bind(lineData);
                _lineItems.Add(item);
            }

            for (int i = showCount; i < _emptySlots.Count; i++)
                _emptySlots[i].gameObject.SetActive(false);

            rebuildScrollContent();
        }

        // EmptyGo 只是占位容器，VerticalLayoutGroup 只统计它的高度，不会读子节点 TextLine
        private static void setupLineInSlot(Transform slot, GameObject lineObj)
        {
            LayoutElement slotLayout = slot.GetComponent<LayoutElement>();
            if (slotLayout == null)
                slotLayout = slot.gameObject.AddComponent<LayoutElement>();

            slotLayout.minHeight = LineHeight;
            slotLayout.preferredHeight = LineHeight;

            RectTransform lineRt = lineObj.GetComponent<RectTransform>();
            if (lineRt == null) return;

            lineRt.anchorMin = Vector2.zero;
            lineRt.anchorMax = Vector2.one;
            lineRt.offsetMin = Vector2.zero;
            lineRt.offsetMax = Vector2.zero;
            lineRt.localScale = Vector3.one;
        }

        private void rebuildScrollContent()
        {
            if (_lineContent is not RectTransform contentRt) return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);

            Transform viewport = contentRt.parent;
            ScrollRect scrollRect = viewport != null ? viewport.parent?.GetComponent<ScrollRect>() : null;
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
        }

        private void clearLines()
        {
            for (int i = 0; i < _lineItems.Count; i++)
            {
                if (_lineItems[i] != null)
                    Destroy(_lineItems[i].gameObject);
            }

            _lineItems.Clear();

            for (int i = 0; i < _emptySlots.Count; i++)
            {
                Transform slot = _emptySlots[i];
                for (int c = slot.childCount - 1; c >= 0; c--)
                    Destroy(slot.GetChild(c).gameObject);

                slot.gameObject.SetActive(true);
            }
        }
    }
}
