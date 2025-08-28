using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WhatsForSale
{
    public class TradeableFilterService : ITradeableFilterService
    {
        private readonly TradeableCache _cache;
        private readonly Dictionary<ThingCategoryDef, List<Tradeable>> _categoryMap;

        public TradeableFilterService(TradeableCache cache)
        {
            _cache = cache;

            _categoryMap = new Dictionary<ThingCategoryDef, List<Tradeable>>
            {
                { ThingCategoryDefOf.Foods,         _cache.Food },
                { ThingCategoryDefOf.ResourcesRaw,  _cache.Raw },
                { ThingCategoryDefOf.Manufactured,  _cache.Manufactured },
                { ThingCategoryDefOf.Apparel,       _cache.Apparel },
                { ThingCategoryDefOf.Weapons,       _cache.Weapons },
                { ThingCategoryDefOf.Items,         _cache.Items },
                { ThingCategoryDefOf.Buildings,     _cache.Buildings }
            };
        }

        public List<Tradeable> FilterAll(ThingCategoryDef def, TradeableFilterCriteria criteria)
        {
            if (!_categoryMap.TryGetValue(def, out var list))
                return new List<Tradeable>();

            return ApplyCriteria(list, criteria);
        }
        public List<Tradeable> FilterOthers(TradeableFilterCriteria criteria) =>
            ApplyCriteria(_cache.Others, criteria);
        public List<Tradeable_Pawn> FilterPawns(TradeableFilterCriteria criteria)
        {
            var source = _cache.Pawns.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(criteria.Search))
                source = source.Where(p => ContainsIgnoreCase(p.ThingDef.label, criteria.Search));

            if (criteria.CustomPredicate != null)
                source = source.Where(p => criteria.CustomPredicate(p));

            return source.ToList();
        }

        private List<Tradeable> ApplyCriteria(IEnumerable<Tradeable> source, TradeableFilterCriteria criteria)
        {
            var query = source;

            if (!string.IsNullOrWhiteSpace(criteria.Search))
                query = query.Where(t => ContainsIgnoreCase(t.ThingDef.label, criteria.Search));

            if (criteria.MinPrice.HasValue)
                query = query.Where(t => t.BaseMarketValue >= criteria.MinPrice.Value);

            if (criteria.MaxPrice.HasValue)
                query = query.Where(t => t.BaseMarketValue <= criteria.MaxPrice.Value);

            if (criteria.CustomPredicate != null)
                query = query.Where(t => criteria.CustomPredicate(t));

            return query.ToList();
        }

        private static bool ContainsIgnoreCase(string text, string search) =>
            !string.IsNullOrEmpty(text) &&
            text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}