using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WhatsForSale
{
    public struct CategoryEntry
    {
        public string LabelKey;
        public Func<TradeableFilterCriteria, List<TradeableWithTrader>> GetList;

        public TabRecord ToTabRecord(Func<int> getIndex, Action<int> setIndex, int currentIndex)
        {
            if (!GetList(new TradeableFilterCriteria(""))?.Any() ?? true)
                return null;

            int index = getIndex();
            return new TabRecord(
                LabelKey.Translate(),
                () => setIndex(index),
                currentIndex == index
            );
        }
    }
}