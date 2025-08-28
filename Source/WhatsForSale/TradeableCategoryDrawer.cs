using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhatsForSale
{
    internal static class TradeableCategoryDrawer
    {
        public static void Draw(ref float y, CategoryDrawContext context, bool showTraderInfo)
        {
            int rowIndex = 0;
            foreach (var entry in context.Entries)
            {
                if (context.IsRowAbove(y))
                {
                    y += context.LineHeight;
                    rowIndex++;
                    continue;
                }

                if (context.IsRowBelow(y)) return;

                var rowRect = context.RowRect(y);
                TradeableRowDrawer.Draw(
                    rowRect,
                    entry.Tradeable,
                    entry.Trader,
                    rowIndex++,
                    showTraderInfo,
                    context.GetRestockText);

                y += context.LineHeight;
            }
        }
    }

    public class CategoryDrawContext
    {
        public IReadOnlyList<TradeableWithTrader> Entries { get; }
        public Rect ViewRect { get; }
        public float ScrollTop { get; }
        public float ScrollBottom { get; }
        public Func<Settlement, string> GetRestockText { get; }
        public float LineHeight => TradeUIConstants.LineHeight;

        public CategoryDrawContext(
            IReadOnlyList<TradeableWithTrader> entries,
            Rect viewRect,
            float scrollTop,
            float scrollBottom,
            Func<Settlement, string> getRestockText)
        {
            Entries = entries;
            ViewRect = viewRect;
            ScrollTop = scrollTop;
            ScrollBottom = scrollBottom;
            GetRestockText = getRestockText ?? (_ => string.Empty);
        }

        public bool IsHeaderBelow(float y) => y > ScrollBottom;
        public bool IsRowAbove(float y) => y + LineHeight < ScrollTop;
        public bool IsRowBelow(float y) => y > ScrollBottom;
        public Rect RowRect(float y) => new Rect(0f, y, ViewRect.width, LineHeight);
    }

    public readonly struct TradeableWithTrader
    {
        public readonly Tradeable Tradeable;
        public readonly ITrader Trader;
        public TradeableWithTrader(Tradeable tradeable, ITrader trader)
        {
            Tradeable = tradeable;
            Trader = trader;
        }
    }
}