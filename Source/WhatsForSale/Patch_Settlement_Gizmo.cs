using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WhatsForSale
{
    [HarmonyPatch(typeof(Settlement), "GetGizmos")]
    public static class Patch_Settlement_Gizmo
    {
        public static void Postfix(ref IEnumerable<Gizmo> __result, Settlement __instance)
        {
            if (!IsValidSettlement(__instance))
                return;

            List<Gizmo> gizmos = __result.ToList();
            gizmos.Add(CreateCommand_ShowSettlementGoods(__instance));
            __result = gizmos;
        }

        private static bool IsValidSettlement(Settlement settlement)
        {
            if (!settlement.CanTradeNow)
                return false;

            if (settlement.Faction.HostileTo(Faction.OfPlayer))
                return false;

            if (WhatsForSale_Mod.settings.NeedTradeConsole)
            {
                if (WhatsForSale_Mod.settings.OnlyIndustrialAndHigher &&
                    (int)settlement.Faction.def.techLevel < 4)
                    return false;

                if (!HasPoweredCommsConsole())
                    return false;
            }

            return true;
        }
        private static bool HasPoweredCommsConsole()
        {
            return Find.Maps.Any(map =>
                map.ParentFaction == Faction.OfPlayer &&
                map.listerBuildings.allBuildingsColonist.Any(b =>
                    b is Building_CommsConsole &&
                    b.GetComp<CompPowerTrader>()?.PowerOn == true));
        }
        private static Command_Action CreateCommand_ShowSettlementGoods(Settlement settlement)
        {
            return new Command_Action
            {
                defaultLabel = "WDYS.CommandShowSettlementGoods".Translate(),
                defaultDesc = "WDYS.CommandShowSettlementGoodsDesc".Translate(),
                icon = Settlement.ShowSellableItemsCommand,
                action = () =>
                {
                    Find.WindowStack.Add(new Dialog_ShowBuyable(settlement));

                    RoyalTitleDef requiredTitle = settlement.TraderKind.TitleRequiredToTrade;
                    if (requiredTitle != null)
                    {
                        TutorUtility.DoModalDialogIfNotKnown(
                            ConceptDefOf.TradingRequiresPermit,
                            requiredTitle.GetLabelCapForBothGenders());
                    }
                }
            };
        }
    }
}