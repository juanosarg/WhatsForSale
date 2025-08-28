using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WhatsForSale
{
    public abstract class TradeManager
    {
        private readonly TradeableCacheService _cacheService = new TradeableCacheService();
        private readonly Dictionary<ITrader, TradeableCache> _cache = new Dictionary<ITrader, TradeableCache>();
        private readonly Dictionary<ITrader, ITradeableFilterService> _filters = new Dictionary<ITrader, ITradeableFilterService>();
        private readonly Dictionary<ITrader, string> _restockInfo = new Dictionary<ITrader, string>();

        protected readonly List<ITrader> _traders = new List<ITrader>();

        private Pawn _negotiator;
        private float _negotiatorStat;

        public Pawn Negotiator => _negotiator;
        public float NegotiatorStat => _negotiatorStat;
        public IReadOnlyList<ITrader> Traders => _traders;
        
        public void RefreshData()
        {
            ClearData();
            LoadData();
        }
        public void ClearData()
        {
            _traders.Clear();
            _cache.Clear();
            _filters.Clear();
            _restockInfo.Clear();
        }
        public void LoadData()
        {
            SetupTraders();

            _negotiator = GetBestNegotiator(Find.CurrentMap);

            if (_negotiator != null)
                _negotiatorStat = _negotiator.GetStatValue(StatDefOf.TradePriceImprovement);
            else
                _negotiatorStat = 0f;

            foreach (var trader in _traders)
            {
                TradeSession.playerNegotiator = _negotiator;
                TradeSession.trader = trader;
                TradeSession.giftMode = false;

                var result = _cacheService.BuildCache(trader);
                _cache[trader] = result;
                _filters[trader] = new TradeableFilterService(result);
                _restockInfo[trader] = FormatRestockTime(trader);
            }
        }
        public List<TradeableWithTrader> GetTradeables(ThingCategoryDef category, TradeableFilterCriteria criteria) =>
            _filters
            .SelectMany(kvp => kvp.Value
                .FilterAll(category, criteria)
                .Select(t => new TradeableWithTrader(t, kvp.Key))
            )
            .ToList();
        public List<TradeableWithTrader> GetPawns(TradeableFilterCriteria criteria) =>
            _filters.SelectMany(kvp => kvp.Value.FilterPawns(criteria)
                                    .Select(t => new TradeableWithTrader(t, kvp.Key)))
                    .ToList();
        public List<TradeableWithTrader> GetOthers(TradeableFilterCriteria criteria) =>
            _filters.SelectMany(kvp => kvp.Value.FilterOthers(criteria)
                                    .Select(t => new TradeableWithTrader(t, kvp.Key)))
                    .ToList();
        public int CountTradeables(ThingCategoryDef category, TradeableFilterCriteria criteria) =>
            _filters.Values.Sum(fs => fs.FilterAll(category, criteria).Count);
        public int CountPawns(TradeableFilterCriteria criteria) =>
            _filters.Values.Sum(fs => fs.FilterPawns(criteria).Count);
        public int CountOthers(TradeableFilterCriteria criteria) =>
            _filters.Values.Sum(fs => fs.FilterOthers(criteria).Count);
        public string GetRestockInfo(ITrader trader) =>
            _restockInfo.TryGetValue(trader, out var info) ? info : string.Empty;
        public Tradeable GetCurrency(ITrader trader)
        {
            if (_cache.TryGetValue(trader, out var result)) return result.Currency;

            return null;
        }

        protected abstract void SetupTraders();

        protected virtual Pawn GetBestNegotiator(Map map) =>
            map.mapPawns.FreeColonistsSpawned
                .Where(p => p.IsColonist && !p.WorkTagIsDisabled(WorkTags.Social))
                .OrderByDescending(p => p.GetStatValue(StatDefOf.TradePriceImprovement))
                .FirstOrDefault();

        private static string FormatRestockTime(ITrader trader)
        {
            if (trader is Settlement s)
                return (s.NextRestockTick - Find.TickManager.TicksGame).ToStringTicksToDays();
            return string.Empty;
        }
    }

    public class TradeStockManager : TradeManager
    {
        public TradeStockManager()
        {
            LoadData();
        }

        protected override void SetupTraders()
        {
            int playerTile = Find.CurrentMap.Tile;
            var settlements = TraderScanner.FindValidTraders(playerTile);
            _traders.AddRange(settlements);
        }
    }

    public class BuyableDialogManager : TradeManager
    {
        private readonly ITrader _trader;

        public BuyableDialogManager(ITrader trader)
        {
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            LoadData();
        }

        protected override void SetupTraders()
        {
            _traders.Add(_trader);
        }
        protected override Pawn GetBestNegotiator(Map map)
        {
            RoyalTitleDef titleRequiredToTrade = _trader.TraderKind.TitleRequiredToTrade;
            if (titleRequiredToTrade != null)
            {
                return map.mapPawns.FreeColonistsSpawned
                    .Where(p => p.IsColonist && !p.WorkTagIsDisabled(WorkTags.Social) && p.royalty.HasTitle(titleRequiredToTrade))
                    .OrderByDescending(p => p.GetStatValue(StatDefOf.TradePriceImprovement))
                    .FirstOrDefault();
            }

            return base.GetBestNegotiator(map);
        }
    }
}