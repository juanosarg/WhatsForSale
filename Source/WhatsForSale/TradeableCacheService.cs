using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WhatsForSale
{
    public class TradeableCacheService
    {
        public TradeableCache BuildCache(ITrader trader)
        {
            var result = new TradeableCache();
            var rawList = CollectTradeables(trader);

            var filtered = FilterAndSort(rawList, trader);
            DistributeToCategories(filtered, result);

            return result;
        }

        private List<Tradeable> CollectTradeables(ITrader trader)
        {
            var list = new List<Tradeable>();

            foreach (Thing good in trader.Goods)
            {
                Tradeable tradeable = TradeableMatching(good, list);
                if (tradeable is null)
                {
                    tradeable = good is Pawn ? new Tradeable_Pawn() : new Tradeable();
                    list.Add(tradeable);
                }
                tradeable.AddThing(good, Transactor.Trader);
            }

            return list;
        }
        private List<Tradeable> FilterAndSort(List<Tradeable> list, ITrader trader) => list
            .Where(tr => tr.IsCurrency || trader.TraderKind.WillTrade(tr.ThingDef) || !TradeSession.trader.TraderKind.hideThingsNotWillingToTrade)
            .OrderBy(tr => tr, TransferableSorterDefOf.Category.Comparer)
            .ThenBy(tr => tr.ThingDef.label)
            .ThenBy(tr => tr.AnyThing.TryGetQuality(out var qc) ? (int)qc : -1)
            .ToList();
        private void DistributeToCategories(List<Tradeable> list, TradeableCache cache)
        {
            foreach (var tradeable in list)
            {
                if (tradeable.IsCurrency && !tradeable.IsFavor && cache.Currency is null)
                {
                    cache.Currency = tradeable;
                    continue;
                }

                var def = tradeable.ThingDef;

                if (tradeable is Tradeable_Pawn pawn)
                    cache.Pawns.Add(pawn);
                else if (def.IsWithinCategory(ThingCategoryDefOf.Foods))
                    cache.Food.Add(tradeable);
                else if (def.IsWithinCategory(ThingCategoryDefOf.ResourcesRaw))
                    cache.Raw.Add(tradeable);
                else if (def.IsWithinCategory(ThingCategoryDefOf.Manufactured))
                    cache.Manufactured.Add(tradeable);
                else if (def.IsWithinCategory(ThingCategoryDefOf.Apparel))
                    cache.Apparel.Add(tradeable);
                else if (def.IsWithinCategory(ThingCategoryDefOf.Weapons))
                    cache.Weapons.Add(tradeable);
                else if (def.IsWithinCategory(ThingCategoryDefOf.Items))
                    cache.Items.Add(tradeable);
                else if (def.IsWithinCategory(ThingCategoryDefOf.Buildings))
                    cache.Buildings.Add(tradeable);
                else
                    cache.Others.Add(tradeable);
            }
        }
        private Tradeable TradeableMatching(Thing thing, List<Tradeable> tradeables)
        {
            if (thing is null || tradeables is null)
                return null;

            foreach (var t in tradeables)
            {
                if (t.HasAnyThing)
                {
                    var mode = (!TradeSession.trader.TraderKind.WillTrade(t.ThingDef)
                        ? TransferAsOneMode.InactiveTradeable
                        : TransferAsOneMode.Normal);

                    if (TransferableUtility.TransferAsOne(thing, t.AnyThing, mode))
                        return t;
                }
            }

            return null;
        }
    }

    public class TradeableCache
    {
        public Tradeable Currency;
        public List<Tradeable_Pawn> Pawns = new List<Tradeable_Pawn>();
        public List<Tradeable> Food = new List<Tradeable>();
        public List<Tradeable> Raw = new List<Tradeable>();
        public List<Tradeable> Manufactured = new List<Tradeable>();
        public List<Tradeable> Apparel = new List<Tradeable>();
        public List<Tradeable> Weapons = new List<Tradeable>();
        public List<Tradeable> Buildings = new List<Tradeable>();
        public List<Tradeable> Items = new List<Tradeable>();
        public List<Tradeable> Others = new List<Tradeable>();
    }
}