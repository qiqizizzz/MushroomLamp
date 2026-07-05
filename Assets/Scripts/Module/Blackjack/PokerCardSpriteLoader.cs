using System.Collections.Generic;
using Common;
using Common.Defines;
using UnityEngine;

namespace Module.Blackjack
{
    // 扑克牌面 Resources 加载（Art/Poker/*.png）
    public static class PokerCardSpriteLoader
    {
        private static Sprite _back;
        private static readonly Dictionary<string, Sprite> _faces = new();

        public static Sprite Back =>
            _back ??= ArtAssetLoader.LoadSprite(AddressDefines.Art_PokerCardBack);

        public static Sprite GetFace(string spriteKey)
        {
            if (string.IsNullOrWhiteSpace(spriteKey)) return null;

            if (_faces.TryGetValue(spriteKey, out Sprite cached))
                return cached;

            Sprite loaded = ArtAssetLoader.LoadSprite($"{AddressDefines.Art_PokerRoot}/{spriteKey}");
            if (loaded != null)
                _faces[spriteKey] = loaded;
            return loaded;
        }

        // 按 21 点点数随机花色，生成资源名（如 7H、AS）
        public static string RollFaceSpriteKey(int point)
        {
            string rank = resolveRank(point);
            char suit = Suits[Random.Range(0, Suits.Length)];
            return $"{rank}{suit}";
        }

        private static readonly char[] Suits = { 'C', 'D', 'H', 'S' };

        private static string resolveRank(int point)
        {
            return point switch
            {
                1 => "A",
                11 => FaceRanks[Random.Range(0, FaceRanks.Length)],
                _ => point.ToString()
            };
        }

        private static readonly string[] FaceRanks = { "J", "Q", "K" };
    }
}
