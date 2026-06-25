using System.Collections;
using MVC.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Module.Summary
{
    public class SummaryView : BaseView
    {
        // Left stats
        private TextMeshProUGUI _txtDeckName;
        private TextMeshProUGUI _txtRounds;
        private TextMeshProUGUI _txtTotalFlavor;
        private TextMeshProUGUI _txtMaxSingle;
        private TextMeshProUGUI _txtResonance;
        private TextMeshProUGUI _txtAngel;
        private TextMeshProUGUI _txtDevil;
        private TextMeshProUGUI _txtGold;

        // Center
        private TextMeshProUGUI _txtScoreValue;

        // Right highlights
        private Transform _highlightsContainer;
        private GameObject _highlightTemplate;

        // Top
        private GameObject _badgeAlmanac;

        // Buttons
        private Button _btnAlmanac;
        private Button _btnBackMenu;
        private Button _btnCookAgain;

        private Coroutine _countUpCoroutine;

        public override void InitUI()
        {
            const string stats = "Middle/Left/Stats";
            _txtDeckName    = Find<TextMeshProUGUI>($"{stats}/Row_DeckName/Txt_Value");
            _txtRounds      = Find<TextMeshProUGUI>($"{stats}/Row_Rounds/Txt_Value");
            _txtTotalFlavor = Find<TextMeshProUGUI>($"{stats}/Row_Flavor/Txt_Value");
            _txtMaxSingle   = Find<TextMeshProUGUI>($"{stats}/Row_MaxRound/Txt_Value");
            _txtResonance   = Find<TextMeshProUGUI>($"{stats}/Row_Resonance/Txt_Value");
            _txtAngel       = Find<TextMeshProUGUI>($"{stats}/Row_Angel/Txt_Value");
            _txtDevil       = Find<TextMeshProUGUI>($"{stats}/Row_Devil/Txt_Value");
            _txtGold        = Find<TextMeshProUGUI>($"{stats}/Row_Gold/Txt_Value");

            _txtScoreValue = Find<TextMeshProUGUI>("Middle/Center/ScorePanel/Txt_ScoreValue");

            _highlightsContainer = Find<Transform>("Middle/Right/Highlights");
            _highlightTemplate   = Find("Middle/Right/Highlights/HighlightTemplate");

            _badgeAlmanac = Find("Top/Badge_Almanac");

            _btnAlmanac   = Find<Button>("Bottom/Btn_Almanac");
            _btnBackMenu  = Find<Button>("Bottom/Btn_BackMenu");
            _btnCookAgain = Find<Button>("Bottom/Btn_CookAgain");
        }

        public override void InitData()
        {
            base.InitData();
            _btnAlmanac?  .onClick.AddListener(() => ApplyFunc("Summary.ViewAlmanac"));
            _btnBackMenu? .onClick.AddListener(() => ApplyFunc("Summary.BackMenu"));
            _btnCookAgain?.onClick.AddListener(() => ApplyFunc("Summary.CookAgain"));
        }

        public override void Open(params object[] args)
        {
            SetVisible(true);
        }

        public void Refresh(SummaryModel model)
        {
            if (model == null) return;

            set(_txtDeckName,    model.DeckName);
            set(_txtRounds,      $"{model.RoundsDone}/{model.RoundsTotal}");
            set(_txtTotalFlavor, model.TotalFlavor.ToString());
            set(_txtMaxSingle,   model.MaxSingleRound.ToString());
            set(_txtResonance,   $"{model.ResonanceCount} 次");
            set(_txtAngel,       $"{model.AngelBlessCount} 次");
            set(_txtDevil,       $"{model.DevilDealCount} 次");
            set(_txtGold,        model.GoldEarned.ToString());

            if (_badgeAlmanac != null)
                _badgeAlmanac.SetActive(model.ShowAlmanacBadge);

            refreshHighlights(model);

            if (_countUpCoroutine != null) StopCoroutine(_countUpCoroutine);
            _countUpCoroutine = StartCoroutine(countUp(model.FinalScore));
        }

        private void refreshHighlights(SummaryModel model)
        {
            if (_highlightsContainer == null || _highlightTemplate == null) return;
            _highlightTemplate.SetActive(false);

            for (int i = _highlightsContainer.childCount - 1; i >= 0; i--)
            {
                var child = _highlightsContainer.GetChild(i).gameObject;
                if (child != _highlightTemplate) Destroy(child);
            }

            foreach (var line in model.Highlights)
            {
                var item = Instantiate(_highlightTemplate, _highlightsContainer) as GameObject;
                if (item == null) continue;
                item.SetActive(true);
                var tmp = item.transform.Find("Txt_Highlight")?.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = line;
            }
        }

        private IEnumerator countUp(int target)
        {
            if (_txtScoreValue == null) yield break;
            _txtScoreValue.text = "1";
            float duration = 2f, elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _txtScoreValue.text = Mathf.RoundToInt(Mathf.Lerp(1, target, t)).ToString();
                yield return null;
            }
            _txtScoreValue.text = target.ToString();
        }

        private static void set(TextMeshProUGUI tmp, string text)
        {
            if (tmp != null) tmp.text = text;
        }
    }
}
