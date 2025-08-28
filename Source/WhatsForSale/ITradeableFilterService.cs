using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WhatsForSale
{
    public interface ITradeableFilterService
    {
        List<Tradeable> FilterAll(ThingCategoryDef def, TradeableFilterCriteria criteria);
        List<Tradeable> FilterOthers(TradeableFilterCriteria criteria);
        List<Tradeable_Pawn> FilterPawns(TradeableFilterCriteria criteria);
    }
}