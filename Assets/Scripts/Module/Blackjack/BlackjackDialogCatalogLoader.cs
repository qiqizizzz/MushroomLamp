using System;
using System.Collections.Generic;
using Common;
using Common.Defines;
using UnityEngine;

namespace Module.Blackjack
{
    public static class BlackjackDialogCatalogLoader
    {
        public const string PhaseEnter = "magic_box_enter";
        public const string PhaseLow = "blackjack_low";
        public const string PhaseMid = "blackjack_mid";
        public const string PhaseHigh = "blackjack_high";
        public const string PhaseNearTarget = "near_target";
        public const string PhaseTargetReached = "target_reached";

        private static BlackjackDialogJsonConfig _config;

        public static void EnsureLoaded()
        {
            if (_config != null) return;
            _config = JsonConfigLoader.LoadFromConfig<BlackjackDialogJsonConfig>(AddressDefines.Config_BlackjackDialogCatalog);
        }

        public static bool IsHighPriority(BlackjackDialogLineJsonData line)
        {
            return isHighPriorityValue(line?.priority);
        }

        public static bool IsHighPriorityLine(string lineId)
        {
            return IsHighPriority(GetLineById(lineId));
        }

        public static BlackjackDialogLineJsonData GetLineById(string lineId)
        {
            EnsureLoaded();
            if (_config?.lines == null || string.IsNullOrWhiteSpace(lineId)) return null;

            foreach (BlackjackDialogLineJsonData line in _config.lines)
            {
                if (line != null && line.id == lineId)
                    return line;
            }

            return null;
        }

        public static string ResolvePhase(BlackjackModel model, BlackjackDialogContext context)
        {
            EnsureLoaded();
            if (model == null) return PhaseEnter;

            if (model.RevealedCount <= 0 && model.TotalPoint <= 0)
                return PhaseEnter;

            BlackjackDialogRulesJsonData rules = _config?.rules ?? new BlackjackDialogRulesJsonData();

            if (context.HasCookTarget)
            {
                if (context.CookCurrentScore >= context.CookTargetMin)
                    return PhaseTargetReached;

                float nearThreshold = context.CookTargetMin * Mathf.Clamp01(rules.nearTargetRatio);
                if (context.CookCurrentScore >= nearThreshold)
                    return PhaseNearTarget;
            }

            if (model.IsBusted || model.TotalPoint >= rules.blackjackHighMinPoint)
                return PhaseHigh;

            if (model.TotalPoint > rules.blackjackLowMaxPoint)
                return PhaseMid;

            return PhaseLow;
        }

        public static bool TryPickLine(
            string role,
            string phase,
            IReadOnlyDictionary<string, int> sessionCounts,
            IReadOnlyDictionary<string, int> stageCounts,
            IReadOnlyDictionary<string, float> lastShownTime,
            string currentLineId,
            out BlackjackDialogLineJsonData pickedLine,
            out string text,
            bool highPriorityOnly = false)
        {
            pickedLine = null;
            text = string.Empty;
            EnsureLoaded();

            if (_config?.lines == null || string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(phase))
                return false;

            var candidates = new List<BlackjackDialogLineJsonData>();
            int totalWeight = 0;

            foreach (BlackjackDialogLineJsonData line in _config.lines)
            {
                if (line == null || !line.enabled) continue;
                if (!string.Equals(line.role, role, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(line.phase, phase, StringComparison.OrdinalIgnoreCase)) continue;
                if (highPriorityOnly && !IsHighPriority(line)) continue;
                if (string.Equals(line.id, currentLineId, StringComparison.Ordinal)) continue;
                if (!passesLimits(line, sessionCounts, stageCounts, lastShownTime, currentLineId)) continue;

                candidates.Add(line);
                totalWeight += Mathf.Max(1, line.weight);
            }

            if (candidates.Count == 0)
            {
                if (highPriorityOnly) return false;

                text = getFallbackText(role, phase);
                return !string.IsNullOrEmpty(text);
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            foreach (BlackjackDialogLineJsonData line in candidates)
            {
                roll -= Mathf.Max(1, line.weight);
                if (roll >= 0) continue;

                pickedLine = line;
                text = line.text ?? string.Empty;
                return true;
            }

            pickedLine = candidates[0];
            text = pickedLine.text ?? string.Empty;
            return true;
        }

        private static bool isHighPriorityValue(string priority)
        {
            if (string.IsNullOrWhiteSpace(priority)) return false;
            return string.Equals(priority, "high", StringComparison.OrdinalIgnoreCase)
                   || priority.Contains("高", StringComparison.Ordinal);
        }

        private static bool passesLimits(
            BlackjackDialogLineJsonData line,
            IReadOnlyDictionary<string, int> sessionCounts,
            IReadOnlyDictionary<string, int> stageCounts,
            IReadOnlyDictionary<string, float> lastShownTime,
            string currentLineId)
        {
            if (line.maxPerSession > 0
                && sessionCounts != null
                && sessionCounts.TryGetValue(line.id, out int sessionCount)
                && sessionCount >= line.maxPerSession
                && !string.Equals(line.id, currentLineId, StringComparison.Ordinal))
                return false;

            if (line.maxPerStage > 0
                && stageCounts != null
                && stageCounts.TryGetValue(line.id, out int stageCount)
                && stageCount >= line.maxPerStage
                && !string.Equals(line.id, currentLineId, StringComparison.Ordinal))
                return false;

            if (line.cooldownSeconds > 0f
                && lastShownTime != null
                && lastShownTime.TryGetValue(line.id, out float lastTime)
                && Time.unscaledTime - lastTime < line.cooldownSeconds
                && !string.Equals(line.id, currentLineId, StringComparison.Ordinal))
                return false;

            return true;
        }

        private static string getFallbackText(string role, string phase)
        {
            if (_config?.lines == null) return string.Empty;

            foreach (BlackjackDialogLineJsonData line in _config.lines)
            {
                if (line == null || !line.enabled) continue;
                if (!string.Equals(line.role, role, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(line.phase, phase, StringComparison.OrdinalIgnoreCase)) continue;
                return line.text ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
