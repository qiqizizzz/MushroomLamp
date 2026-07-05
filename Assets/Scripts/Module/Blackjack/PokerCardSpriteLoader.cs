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

        private static readonly char[] Suits = { 'C', 'D', 'H', 'S' };
        private static readonly string[] Ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

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

        // 生成并洗牌 52 张不重复牌堆（如 7H、AS）
        public static List<string> CreateShuffledDeck()
        {
            var deck = new List<string>(52);
            for (int i = 0; i < Ranks.Length; i++)
            {
                for (int j = 0; j < Suits.Length; j++)
                    deck.Add($"{Ranks[i]}{Suits[j]}");
            }

            for (int i = deck.Count - 1; i > 0; i--)
            {
                int swap = Random.Range(0, i + 1);
                (deck[i], deck[swap]) = (deck[swap], deck[i]);
            }

            return deck;
        }

        public static float ResolvePointFromSpriteKey(string spriteKey)
        {
            string rank = parseRank(spriteKey);
            if (rank == "A")
                return BlackjackModel.AcePoint;
            if (rank == "J" || rank == "Q" || rank == "K")
                return BlackjackModel.FacePoint;

            return rank == "10" ? 10f : float.Parse(rank);
        }

        private static string parseRank(string spriteKey)
        {
            if (string.IsNullOrWhiteSpace(spriteKey) || spriteKey.Length < 2)
                return string.Empty;

            char last = spriteKey[spriteKey.Length - 1];
            if (last is not ('C' or 'D' or 'H' or 'S'))
                return string.Empty;

            return spriteKey.Substring(0, spriteKey.Length - 1);
        }
    }
}
