using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace WhatsForSale
{
    internal static class TraderScanner
    {
        public static List<Settlement> FindValidTraders(int playerTile)
        {
            List<Settlement> result = new List<Settlement>();
            var worldGrid = Find.WorldGrid;
            var colonists = Find.CurrentMap?.mapPawns?.FreeColonists ?? new List<Pawn>();

            foreach (var settlement in Find.World.worldObjects.Settlements)
            {
                int dist = worldGrid.TraversalDistanceBetween(settlement.Tile, playerTile, false);

                if (
                    (!WhatsForSale_Mod.settings.OnlyShowReachable || dist != int.MaxValue) &&
                    settlement.Faction != Faction.OfPlayer &&
                    settlement.CanTradeNow &&
                    !settlement.Faction.HostileTo(Faction.OfPlayer) &&
                    (!WhatsForSale_Mod.settings.EnableMaxTile || dist <= WhatsForSale_Mod.settings.MaxTiles) &&
                    (!WhatsForSale_Mod.settings.OnlyIndustrialAndHigher || settlement.Faction.def.techLevel >= TechLevel.Industrial) &&
                    (!WhatsForSale_Mod.settings.FilterByRoyalTitleEligibility || CanSatisfyRoyalRequirement(settlement, colonists))
                )
                {
                    result.Add(settlement);
                }
            }

            return result;
        }

        private static bool CanSatisfyRoyalRequirement(Settlement settlement, IEnumerable<Pawn> colonists)
        {
            var requiredTitle = settlement.TraderKind?.TitleRequiredToTrade;
            if (requiredTitle == null)
                return true;

            foreach (var pawn in colonists)
            {
                if (pawn.royalty == null) continue;

                var currentTitle = pawn.royalty.GetCurrentTitle(settlement.Faction);
                if (currentTitle != null && currentTitle.seniority >= requiredTitle.seniority)
                    return true;
            }

            return false;
        }
    }
}