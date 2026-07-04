using System;
using System.Collections.Generic;
using UnityEngine;

namespace Module.Blackjack
{
    // 小牌在容器内的居中横向布局（Model 层计算，View 只负责应用坐标）
    public static class BlackjackCardLayout
    {
        public static IReadOnlyList<Vector2> ComputeCenteredAnchoredPositions(
            int cardCount,
            float containerWidth,
            float cardWidth,
            float spacing)
        {
            if (cardCount <= 0)
                return Array.Empty<Vector2>();

            spacing = Mathf.Max(0f, spacing);
            cardWidth = Mathf.Max(1f, cardWidth);

            if (cardCount == 1)
                return new[] { Vector2.zero };

            float totalWidth = cardCount * cardWidth + (cardCount - 1) * spacing;
            float startX = -totalWidth * 0.5f + cardWidth * 0.5f;

            var positions = new Vector2[cardCount];
            for (int i = 0; i < cardCount; i++)
                positions[i] = new Vector2(startX + i * (cardWidth + spacing), 0f);

            return positions;
        }
    }
}
