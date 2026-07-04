/*
* ┌──────────────────────────────────┐
* │  描    述: 总结算界面视图，展示大局统计、评分与亮点
* │  类    名: SummaryView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections;
using Common.Defines;
using Common.UI;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Summary
{
    // 总结算界面视图，负责绑定节点并刷新结算内容
    public class SummaryView : BaseView
    {
        private TextMeshProUGUI _txtDeckName;
        private TextMeshProUGUI _txtRounds;
        private TextMeshProUGUI _txtTotalFlavor;
        private TextMeshProUGUI _txtMaxSingle;
        private TextMeshProUGUI _txtResonance;
        private TextMeshProUGUI _txtAngel;
        private TextMeshProUGUI _txtDevil;
        private TextMeshProUGUI _txtGold;
        private TextMeshProUGUI _txtScoreValue;

        private Transform _statsContainer;
        private Transform _scorePanel;
        private Transform _highlightsContainer;
        private GameObject _highlightTemplate;
        private GameObject _badgeAlmanac;

        private Button _btnAlmanac;
        private Button _btnBackMenu;
        private Button _btnCookAgain;

        private Coroutine _countUpCoroutine;

        // 初始化总结算界面节点引用
        public override void InitUI()
        {
            const string stats = "Middle/Left/Stats";
            _statsContainer = Find<Transform>(stats);
            _txtDeckName = Find<TextMeshProUGUI>($"{stats}/Row_DeckName/Txt_Value");
            _txtRounds = Find<TextMeshProUGUI>($"{stats}/Row_Rounds/Txt_Value");
            _txtTotalFlavor = Find<TextMeshProUGUI>($"{stats}/Row_Flavor/Txt_Value");
            _txtMaxSingle = Find<TextMeshProUGUI>($"{stats}/Row_MaxRound/Txt_Value");
            _txtResonance = Find<TextMeshProUGUI>($"{stats}/Row_Resonance/Txt_Value");
            _txtAngel = Find<TextMeshProUGUI>($"{stats}/Row_Angel/Txt_Value");
            _txtDevil = Find<TextMeshProUGUI>($"{stats}/Row_Devil/Txt_Value");
            _txtGold = Find<TextMeshProUGUI>($"{stats}/Row_Gold/Txt_Value");

            _txtScoreValue = Find<TextMeshProUGUI>("Middle/Right/ScorePanel/Txt_ScoreValue");

            _scorePanel = Find<Transform>("Middle/Right/ScorePanel");
            _highlightsContainer = Find<Transform>("Middle/Right/Highlights");
            _highlightTemplate = Find("Middle/Right/Highlights/HighlightTemplate");

            _badgeAlmanac = Find("Top/Badge_Almanac");

            _btnAlmanac = Find<Button>("Bottom/Btn_Almanac");
            _btnBackMenu = Find<Button>("Bottom/Btn_BackMenu");
            _btnCookAgain = Find<Button>("Bottom/Btn_CookAgain");

            setupButtonHovers();
            setupReadableTextStyle();
        }

        // 绑定总结算界面按钮事件
        public override void InitData()
        {
            base.InitData();
            _btnAlmanac.onClick.AddListener(() => ApplyFunc("Summary.ViewAlmanac"));
            _btnBackMenu.onClick.AddListener(() => ApplyFunc("Summary.BackMenu"));
            _btnCookAgain.onClick.AddListener(() => ApplyFunc("Summary.CookAgain"));
        }

        public override void Open(params object[] args)
        {
            SetVisible(true);
        }

        // 刷新总结算展示内容
        public void Refresh(SummaryModel model)
        {
            if (model == null) return;

            setText(_txtDeckName, model.DeckName);
            setText(_txtRounds, $"{model.RoundsDone}/{model.RoundsTotal}");
            setText(_txtTotalFlavor, model.TotalFlavorText);
            setText(_txtMaxSingle, model.MaxSingleRoundText);
            setText(_txtResonance, $"{model.ResonanceCount} 次");
            setText(_txtAngel, $"{model.AngelBlessCount} 次");
            setText(_txtDevil, $"{model.DevilDealCount} 次");
            setText(_txtGold, model.GoldEarned.ToString());

            if (_badgeAlmanac != null)
                _badgeAlmanac.SetActive(model.ShowAlmanacBadge);

            refreshHighlights(model);

            if (_countUpCoroutine != null)
                StopCoroutine(_countUpCoroutine);
            _countUpCoroutine = StartCoroutine(countUp(model.FinalScore));
        }

        // 设置按钮悬停贴图
        private void setupButtonHovers()
        {
            setupButtonHover(_btnCookAgain, AddressDefines.Art_SummaryNextRoundHover);
            setupButtonHover(_btnBackMenu, AddressDefines.Art_SummaryBackHomeHover);
        }

        // 为单个按钮绑定悬停贴图组件
        private static void setupButtonHover(Button button, string hoverSpriteAddress)
        {
            if (button == null) return;

            UIButtonHoverItem hover = button.GetComponent<UIButtonHoverItem>();
            if (hover == null)
                hover = button.gameObject.AddComponent<UIButtonHoverItem>();

            hover.Setup(button, hoverSpriteAddress);
        }

        // 刷新右侧亮点文本列表
        private void refreshHighlights(SummaryModel model)
        {
            if (_highlightsContainer == null || _highlightTemplate == null) return;
            _highlightTemplate.SetActive(false);
            setupRect(_highlightsContainer as RectTransform, new Vector2(0.16f, 0.08f), new Vector2(0.84f, 0.44f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

            for (int i = _highlightsContainer.childCount - 1; i >= 0; i--)
            {
                GameObject child = _highlightsContainer.GetChild(i).gameObject;
                if (child != _highlightTemplate)
                    Destroy(child);
            }

            int index = 0;
            foreach (string line in model.Highlights)
            {
                GameObject item = Instantiate(_highlightTemplate, _highlightsContainer);
                item.SetActive(true);
                RectTransform itemRect = item.transform as RectTransform;
                setupRect(itemRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -index * 32f), new Vector2(0f, 30f), new Vector2(0.5f, 1f));
                Image itemImage = item.GetComponent<Image>();
                if (itemImage != null)
                    itemImage.enabled = false;

                TextMeshProUGUI tmp = item.transform.Find("Txt_Highlight")?.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    setupText(tmp, 15, 18, new Color(0.24f, 0.16f, 0.10f, 1f), TextAlignmentOptions.Left);
                    setupRect(tmp.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
                    tmp.text = line;
                }

                index++;
            }
        }

        // 校正文本显示层级与可读样式
        private void setupReadableTextStyle()
        {
            setupStatsLayout();
            setupScoreLayout();

            _statsContainer?.SetAsLastSibling();
            _scorePanel?.SetAsLastSibling();
            _highlightsContainer?.SetAsLastSibling();
        }

        // 明确摆放左侧统计行，避免所有 Row 因尺寸为 0 堆叠
        private void setupStatsLayout()
        {
            RectTransform statsRect = _statsContainer as RectTransform;
            setupRect(statsRect, new Vector2(0.17f, 0.18f), new Vector2(0.83f, 0.72f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

            if (_statsContainer == null) return;

            const float rowHeight = 31f;
            const float rowGap = 5f;
            for (int i = 0; i < _statsContainer.childCount; i++)
            {
                Transform row = _statsContainer.GetChild(i);
                RectTransform rowRect = row as RectTransform;
                HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
                if (rowLayout != null)
                    rowLayout.enabled = false;

                setupRect(rowRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -i * (rowHeight + rowGap)), new Vector2(0f, rowHeight), new Vector2(0.5f, 1f));

                TextMeshProUGUI key = row.Find("Txt_Key")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI value = row.Find("Txt_Value")?.GetComponent<TextMeshProUGUI>();
                setupText(key, 13, 16, new Color(0.18f, 0.10f, 0.05f, 1f), TextAlignmentOptions.Left);
                setupText(value, 13, 16, new Color(0.18f, 0.10f, 0.05f, 1f), TextAlignmentOptions.Right);
                if (key != null)
                    setupRect(key.rectTransform, new Vector2(0f, 0f), new Vector2(0.58f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
                if (value != null)
                    setupRect(value.rectTransform, new Vector2(0.58f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            }
        }

        // 明确摆放右侧分数区域
        private void setupScoreLayout()
        {
            RectTransform scoreRect = _scorePanel as RectTransform;
            setupRect(scoreRect, new Vector2(0.2f, 0.58f), new Vector2(0.8f, 0.92f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

            TextMeshProUGUI label = _scorePanel?.Find("Txt_ScoreLabel")?.GetComponent<TextMeshProUGUI>();
            setupText(label, 14, 18, new Color(0.36f, 0.28f, 0.20f, 1f), TextAlignmentOptions.Center);
            setupText(_txtScoreValue, 34, 48, new Color(0.18f, 0.10f, 0.05f, 1f), TextAlignmentOptions.Center);

            if (label != null)
                setupRect(label.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            if (_txtScoreValue != null)
                setupRect(_txtScoreValue.rectTransform, new Vector2(0f, 0.1f), new Vector2(1f, 0.68f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        }

        // 设置单个文本的字号和颜色
        private static void setupText(TextMeshProUGUI text, int minSize, int maxSize, Color color, TextAlignmentOptions alignment)
        {
            if (text == null) return;

            text.color = color;
            text.enableAutoSizing = true;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
            text.fontSize = maxSize;
            text.enableWordWrapping = true;
            text.alignment = alignment;
            text.raycastTarget = false;
        }

        // 设置 RectTransform 锚点与尺寸
        private static void setupRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
        {
            if (rect == null) return;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.pivot = pivot;
            rect.localScale = Vector3.one;
        }

        // 播放最终评分递增动画
        private IEnumerator countUp(int target)
        {
            if (_txtScoreValue == null) yield break;

            int startValue = target > 0 ? 1 : 0;
            _txtScoreValue.text = startValue.ToString();
            const float duration = 2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _txtScoreValue.text = Mathf.RoundToInt(Mathf.Lerp(startValue, target, t)).ToString();
                yield return null;
            }

            _txtScoreValue.text = target.ToString();
        }

        // 设置文本内容
        private static void setText(TextMeshProUGUI text, string value)
        {
            if (text != null)
                text.text = value;
        }
    }
}
