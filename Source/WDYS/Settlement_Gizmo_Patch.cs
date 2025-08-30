using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace WDYS
{
    [HarmonyPatch(typeof(Settlement), "GetGizmos")]
    public static class Settlement_Gizmo_Patch
    {
        public static void Postfix(ref IEnumerable<Gizmo> __result, Settlement __instance)
        {
            if (__instance.CanTradeNow && !__instance.Faction.HostileTo(Faction.OfPlayer))
            {
                if ((WDYS_Mod.settings.needTradeConsole && WDYS_Mod.settings.onlyIndustrialAndHigher && __instance.Faction.def.techLevel < TechLevel.Industrial) ||
                    (WDYS_Mod.settings.needTradeConsole &&
                     Find.Maps.Find(map => map.ParentFaction == Faction.OfPlayer &&
                        map.listerBuildings.allBuildingsColonist.Exists(b => b is Building_CommsConsole && b != null && b.GetComp<CompPowerTrader>().PowerOn)) == null))
                {
                    return;
                }

                List<Gizmo> gizmoList = __result.ToList();
                gizmoList.Add(new Command_Action
                {
                    defaultLabel = "WDYS.CommandShowSettlementGoods".Translate(),
                    defaultDesc = "WDYS.CommandShowSettlementGoodsDesc".Translate(),
                    icon = Settlement.ShowSellableItemsCommand,
                    action = delegate ()
                    {
                        Pawn negotiator = null;
                        float stat = 0f;
                        foreach (Map map in Find.Maps)
                        {
                            if (map.ParentFaction == Faction.OfPlayer)
                            {
                                foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                                {
                                    if (pawn.IsColonist && !pawn.WorkTagIsDisabled(WorkTags.Social))
                                    {
                                        float tempStat = pawn.GetStatValue(StatDefOf.TradePriceImprovement);
                                        if (tempStat > stat)
                                        {
                                            stat = tempStat;
                                            negotiator = pawn;
                                        }
                                    }
                                }
                            }
                        }
                        foreach (Caravan caravan in Find.WorldObjects.Caravans)
                        {
                            if (caravan.Faction == Faction.OfPlayer)
                            {
                                foreach (Pawn pawn in caravan.PawnsListForReading)
                                {
                                    if (pawn.IsColonist && !pawn.WorkTagIsDisabled(WorkTags.Social))
                                    {
                                        float tempStat = pawn.GetStatValue(StatDefOf.TradePriceImprovement);
                                        if (tempStat > stat)
                                        {
                                            stat = tempStat;
                                            negotiator = pawn;
                                        }
                                    }
                                }
                            }
                        }

                        Find.WindowStack.Add(new Dialog_ShowBuyable(__instance, negotiator, __instance));
                        RoyalTitleDef titleRequiredToTrade = __instance.TraderKind.TitleRequiredToTrade;
                        if (titleRequiredToTrade != null)
                        {
                            TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.TradingRequiresPermit, new string[]
                            {
                                titleRequiredToTrade.GetLabelCapForBothGenders()
                            });
                        }
                    }
                });
                __result = gizmoList;
            }
        }
    }
}