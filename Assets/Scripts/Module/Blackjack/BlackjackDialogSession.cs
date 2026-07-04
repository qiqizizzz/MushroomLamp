using System.Collections.Generic;
using UnityEngine;

namespace Module.Blackjack
{
    // 单次魔盒会话内的台词抽取、显示时长与打断
    public class BlackjackDialogSession
    {
        private readonly Dictionary<string, int> _sessionCounts = new();
        private readonly Dictionary<string, int> _stageCounts = new();
        private readonly Dictionary<string, float> _lastShownTime = new();

        private string _lastDevilLineId;
        private string _lastAngelLineId;
        private string _lastDevilText = string.Empty;
        private string _lastAngelText = string.Empty;
        private string _lastPhase = string.Empty;

        private bool _devilBubbleVisible;
        private bool _angelBubbleVisible;
        private float _devilHideAt = float.PositiveInfinity;
        private float _angelHideAt = float.PositiveInfinity;
        private bool _dialogEnabled;

        public bool DialogEnabled => _dialogEnabled;

        public string DevilText => _devilBubbleVisible ? _lastDevilText : string.Empty;
        public string AngelText => _angelBubbleVisible ? _lastAngelText : string.Empty;

        public void Reset()
        {
            _sessionCounts.Clear();
            _stageCounts.Clear();
            _lastShownTime.Clear();
            _lastDevilLineId = null;
            _lastAngelLineId = null;
            _lastDevilText = string.Empty;
            _lastAngelText = string.Empty;
            _lastPhase = string.Empty;
            _devilBubbleVisible = false;
            _angelBubbleVisible = false;
            _devilHideAt = float.PositiveInfinity;
            _angelHideAt = float.PositiveInfinity;
            _dialogEnabled = false;
        }

        public void SetDialogEnabled(bool enabled)
        {
            _dialogEnabled = enabled;
            if (enabled) return;

            _devilBubbleVisible = false;
            _angelBubbleVisible = false;
            _devilHideAt = float.PositiveInfinity;
            _angelHideAt = float.PositiveInfinity;
        }

        public void Refresh(BlackjackModel model, BlackjackDialogContext context)
        {
            if (!_dialogEnabled) return;
            string phase = BlackjackDialogCatalogLoader.ResolvePhase(model, context);
            bool phaseChanged = phase != _lastPhase;
            _lastPhase = phase;

            pickRole("devil", phase, phaseChanged, ref _lastDevilLineId, ref _lastDevilText,
                ref _devilBubbleVisible, ref _devilHideAt);
            pickRole("angel", phase, phaseChanged, ref _lastAngelLineId, ref _lastAngelText,
                ref _angelBubbleVisible, ref _angelHideAt);
        }

        public void Tick()
        {
            if (_devilBubbleVisible && _devilHideAt < float.PositiveInfinity && Time.unscaledTime >= _devilHideAt)
                _devilBubbleVisible = false;

            if (_angelBubbleVisible && _angelHideAt < float.PositiveInfinity && Time.unscaledTime >= _angelHideAt)
                _angelBubbleVisible = false;
        }

        private void pickRole(
            string role,
            string phase,
            bool phaseChanged,
            ref string lastLineId,
            ref string lastText,
            ref bool bubbleVisible,
            ref float hideAt)
        {
            if (!phaseChanged && !string.IsNullOrEmpty(lastText))
            {
                if (tryHighPriorityInterrupt(role, phase, ref lastLineId, ref lastText, ref bubbleVisible, ref hideAt))
                    return;

                if (bubbleVisible)
                    return;
            }

            if (!BlackjackDialogCatalogLoader.TryPickLine(
                    role, phase, _sessionCounts, _stageCounts, _lastShownTime, lastLineId,
                    out BlackjackDialogLineJsonData line, out string text))
            {
                if (string.IsNullOrEmpty(lastText))
                {
                    bubbleVisible = false;
                    hideAt = float.PositiveInfinity;
                }

                return;
            }

            applyPick(line, text, ref lastLineId, ref lastText, ref bubbleVisible, ref hideAt);
        }

        private bool tryHighPriorityInterrupt(
            string role,
            string phase,
            ref string lastLineId,
            ref string lastText,
            ref bool bubbleVisible,
            ref float hideAt)
        {
            if (string.IsNullOrEmpty(lastLineId) || BlackjackDialogCatalogLoader.IsHighPriorityLine(lastLineId))
                return false;

            if (!BlackjackDialogCatalogLoader.TryPickLine(
                    role, phase, _sessionCounts, _stageCounts, _lastShownTime, lastLineId,
                    out BlackjackDialogLineJsonData line, out string text, highPriorityOnly: true))
                return false;

            applyPick(line, text, ref lastLineId, ref lastText, ref bubbleVisible, ref hideAt);
            return true;
        }

        private void applyPick(
            BlackjackDialogLineJsonData line,
            string text,
            ref string lastLineId,
            ref string lastText,
            ref bool bubbleVisible,
            ref float hideAt)
        {
            lastLineId = line?.id;
            lastText = text ?? string.Empty;
            bubbleVisible = !string.IsNullOrEmpty(lastText);

            if (line != null)
                recordShown(line.id);

            hideAt = bubbleVisible && line != null && line.displaySeconds > 0f
                ? Time.unscaledTime + line.displaySeconds
                : float.PositiveInfinity;
        }

        private void recordShown(string lineId)
        {
            if (string.IsNullOrWhiteSpace(lineId)) return;

            _sessionCounts.TryGetValue(lineId, out int sessionCount);
            _sessionCounts[lineId] = sessionCount + 1;

            _stageCounts.TryGetValue(lineId, out int stageCount);
            _stageCounts[lineId] = stageCount + 1;

            _lastShownTime[lineId] = Time.unscaledTime;
        }
    }
}
