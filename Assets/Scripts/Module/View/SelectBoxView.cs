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
        private const float SelectedDifficultyScale = 1.08f;

        private Transform _lineContent;
        private TextMeshProUGUI _txtTitle;
        private TextMeshProUGUI _txtBoxName;
        private Image _imgBackground;

        private Button _btnEasy;
        private Button _btnNormal;
        private Button _btnHard;
        private Button _btnStart;
        private Button _btnLeft;
        private Button _btnRight;

        private Button _btnReturn;

        private readonly List<Transform> _emptySlots = new();
        private readonly List<TextLineItem> _lineItems = new();

        public override void InitUI()
        {
            _txtTitle = Find<TextMeshProUGUI>("Left/Txt_Title");
            _txtBoxName = Find<TextMeshProUGUI>("Left/Txt_Info");
            _imgBackground = Find<Image>("Img_Background");
            _lineContent = Find<Transform>("Left/ScrollView/Viewport/Content");

            _btnEasy = Find<Button>("Right/ButtonGroup/Btn_Easy");
            _btnNormal = Find<Button>("Right/ButtonGroup/Btn_Normal");
            _btnHard = Find<Button>("Right/ButtonGroup/Btn_Hard");
            _btnStart = Find<Button>("Right/Btn_Start");
            _btnLeft = Find<Button>("Bottom/Btn_Left");
            _btnRight = Find<Button>("Bottom/Btn_Right");

            _btnReturn = Find<Button>("Left/Btn_Return");
            
            bindButtons();
            collectEmptySlots();
        }

        public override void Open(params object[] args)
        {
            if (args != null && args.Length > 0 && args[0] is SelectBoxModel model)
                Refresh(model);
        }

        public override void Close(params object[] args)
        {
            clearLines();
            base.Close(args);
        }

        public void Refresh(SelectBoxModel model)
        {
            if (model == null) return;

            SelectBoxCatalogEntry entry = model.GetCurrentBoxEntry();
            SelectBoxDetailJsonConfig detail = model.GetCurrentBoxDetail();

            if (detail == null)
            {
                QLog.Warning($"[{nameof(SelectBoxView)}] 未找到 box 子表配置 index={model.SelectedBoxIndex}");
                return;
            }

            refreshBackground(detail.backgroundPath);
            refreshHeader(entry, detail);
            refreshLines(detail.ToRuntimeLines());
            refreshDifficultyButtons(model.Difficulty);
        }

        private void bindButtons()
        {
            bindButton(_btnEasy, () => ApplyFunc(EventDefines.SelectBoxSetDifficulty, SelectDifficulty.Easy));
            bindButton(_btnNormal, () => ApplyFunc(EventDefines.SelectBoxSetDifficulty, SelectDifficulty.Normal));
            bindButton(_btnHard, () => ApplyFunc(EventDefines.SelectBoxSetDifficulty, SelectDifficulty.Hard));
            bindButton(_btnStart, () => ApplyFunc(EventDefines.SelectBoxStart));
            bindButton(_btnLeft, () => ApplyFunc(EventDefines.SelectBoxChangeBox, -1));
            bindButton(_btnRight, () => ApplyFunc(EventDefines.SelectBoxChangeBox, 1));
            bindButton(_btnReturn, () => ApplyFunc(EventDefines.SelectBoxReturn));
        }

        private static void bindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void refreshBackground(string backgroundPath)
        {
            if (_imgBackground == null) return;

            Sprite sprite = ArtAssetLoader.LoadSprite(backgroundPath);
            if (sprite == null)
            {
                QLog.Warning($"[{nameof(SelectBoxView)}] 背景图加载失败：{backgroundPath}");
                return;
            }

            _imgBackground.sprite = sprite;
        }

        private void refreshHeader(SelectBoxCatalogEntry entry, SelectBoxDetailJsonConfig detail)
        {
            if (_txtBoxName != null)
                _txtBoxName.text = entry?.displayName ?? string.Empty;

            if (_txtTitle != null)
                _txtTitle.text = string.IsNullOrEmpty(detail.summaryTitle) ? "简介" : detail.summaryTitle;
        }

        private void refreshDifficultyButtons(SelectDifficulty difficulty)
        {
            setDifficultySelected(_btnEasy, difficulty == SelectDifficulty.Easy);
            setDifficultySelected(_btnNormal, difficulty == SelectDifficulty.Normal);
            setDifficultySelected(_btnHard, difficulty == SelectDifficulty.Hard);
        }

        private static void setDifficultySelected(Button button, bool selected)
        {
            if (button == null) return;
            button.transform.localScale = selected
                ? Vector3.one * SelectedDifficultyScale
                : Vector3.one;
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
