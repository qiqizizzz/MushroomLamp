using System.Collections.Generic;
using MVC.Model;
using UnityEngine;

namespace Module.Summary
{
    public class SummaryModel : BaseModel
    {
        public string DeckName        { get; private set; }
        public int    RoundsDone      { get; private set; }
        public int    RoundsTotal     { get; private set; }
        public int    TotalFlavor     { get; private set; }
        public int    MaxSingleRound  { get; private set; }
        public int    ResonanceCount  { get; private set; }
        public int    AngelBlessCount { get; private set; }
        public int    DevilDealCount  { get; private set; }
        public int    GoldEarned      { get; private set; }
        public int    FinalScore      { get; private set; }
        public bool   ShowAlmanacBadge { get; private set; }
        public IReadOnlyList<string> Highlights => _highlights;

        private readonly List<string> _highlights = new();

        private static readonly string[] _deckNames =
            { "草本调和箱", "香料爆发箱", "根茎稳固箱", "花系轻灵箱" };

        private static readonly string[] _highlightPool =
        {
            "下轮目标达成: {0}/{0}",
            "锅盖稳定度: 良好",
            "已解锁饰底宝物: {1} 件",
            "本局关键词: 草本 稳定 祝福",
            "连续共鸣达到 {2} 次",
            "本局从未跳过回合",
            "恶魔交易全部兑现",
            "天使祝福触发率超预期",
        };

        public void Randomize()
        {
            RoundsTotal     = 9;
            RoundsDone      = 9;
            DeckName        = _deckNames[Random.Range(0, _deckNames.Length)];
            TotalFlavor     = Random.Range(80, 160);
            MaxSingleRound  = Random.Range(20, 55);
            ResonanceCount  = Random.Range(8, 22);
            AngelBlessCount = Random.Range(2, 8);
            DevilDealCount  = Random.Range(1, 6);
            GoldEarned      = Random.Range(40, 120);
            FinalScore      = TotalFlavor + Random.Range(0, 30);
            ShowAlmanacBadge = Random.value > 0.4f;

            _highlights.Clear();
            int count = Random.Range(3, 6);
            var pool = new List<string>(_highlightPool);
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = Random.Range(0, pool.Count);
                string raw = pool[idx];
                pool.RemoveAt(idx);
                _highlights.Add(string.Format(raw,
                    TotalFlavor,
                    Random.Range(1, 4),
                    Random.Range(3, 8)));
            }
        }
    }
}
