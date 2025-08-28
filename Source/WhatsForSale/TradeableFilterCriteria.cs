using RimWorld;
using System;

namespace WhatsForSale
{
    public class TradeableFilterCriteria
    {
        public string Search { get; }
        public float? MinPrice { get; }
        public float? MaxPrice { get; }
        public Func<Tradeable, bool> CustomPredicate { get; }

        public TradeableFilterCriteria(string search = "", float? minPrice = null, float? maxPrice = null, Func<Tradeable, bool> customPredicate = null)
        {
            Search = search;
            MinPrice = minPrice;
            MaxPrice = maxPrice;
            CustomPredicate = customPredicate ?? (_ => true);
        }
    }
}