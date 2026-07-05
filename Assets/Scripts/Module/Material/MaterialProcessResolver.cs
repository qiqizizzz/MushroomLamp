/*
* ┌──────────────────────────────────┐
* │  描    述: 材料加工结果解析（原始材料 → 加工产物）
* │  类    名: MaterialProcessResolver.cs
* └──────────────────────────────────┘
*/

using System.Collections.Generic;

namespace Module.Material
{
    // 根据材料配置与加工方式，解析应变成的目标材料 ID
    public static class MaterialProcessResolver
    {
        private readonly struct ProcessRule
        {
            public readonly string Method;
            public readonly string ResultId;

            public ProcessRule(string method, string resultId)
            {
                Method = method;
                ResultId = resultId;
            }
        }

        private static readonly Dictionary<string, ProcessRule[]> Rules = new Dictionary<string, ProcessRule[]>()
        {
            ["VEG_001"] = new[] { new ProcessRule("研磨", "VEG_201"), new ProcessRule("完美加工", "VEG_204") },
            ["VEG_002"] = new[] { new ProcessRule("切碎", "VEG_202"), new ProcessRule("完美加工", "VEG_205") },
            ["VEG_008"] = new[] { new ProcessRule("切碎", "VEG_203") },
        };

        // 研磨器区域：优先匹配「研磨」，否则匹配「切碎」等其它已配置方式
        public static bool TryResolveForGrinder(MaterialJsonData source, out MaterialJsonData result, out string methodUsed)
        {
            result = null;
            methodUsed = null;
            if (source == null || string.IsNullOrEmpty(source.id)) return false;
            if (!Rules.TryGetValue(source.id, out ProcessRule[] rules) || rules == null) return false;

            string methods = source.processMethods ?? string.Empty;
            for (int i = 0; i < rules.Length; i++)
            {
                ProcessRule rule = rules[i];
                if (string.IsNullOrEmpty(rule.Method) || !methods.Contains(rule.Method)) continue;

                MaterialJsonData cfg = MaterialCatalogLoader.GetById(rule.ResultId);
                if (cfg == null) continue;

                result = cfg;
                methodUsed = rule.Method;
                return true;
            }

            return false;
        }
    }
}
