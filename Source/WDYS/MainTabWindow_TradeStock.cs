using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace WDYS
{
    internal class MainTabWindow_TradeStock : MainTabWindow
    {
        private readonly TransferableSorterDef sorter = TransferableSorterDefOf.Category;

        private int line;
        private float count;
        private Pawn negotiator;
        private bool haveConsole;
        private string search = "";
        private string lastsearch = ".";
        private float negotiatorStat;
        private Vector2 scrollPosition;

        private readonly List<Settlement> traders = new List<Settlement>();
        private readonly Dictionary<Settlement, string> traderRestockIn = new Dictionary<Settlement, string>();
        private readonly Dictionary<ITrader, int> traderTileDistance = new Dictionary<ITrader, int>();
        private readonly Dictionary<ITrader, int> tradersCurrency = new Dictionary<ITrader, int>();

        private readonly Dictionary<ITrader, List<Tradeable>> cachedTradeableManufactured = new Dictionary<ITrader, List<Tradeable>>();
        private readonly Dictionary<ITrader, List<Tradeable>> cachedTradeableResourcesRaw = new Dictionary<ITrader, List<Tradeable>>();
        private readonly Dictionary<ITrader, List<Tradeable_Pawn>> cachedTradeablePawns = new Dictionary<ITrader, List<Tradeable_Pawn>>();
        private readonly Dictionary<ITrader, List<Tradeable>> cachedTradeableBuildings = new Dictionary<ITrader, List<Tradeable>>();
        private readonly Dictionary<ITrader, List<Tradeable>> cachedTradeableApparel = new Dictionary<ITrader, List<Tradeable>>();
        private readonly Dictionary<ITrader, List<Tradeable>> cachedTradeableWeapons = new Dictionary<ITrader, List<Tradeable>>();
        private readonly Dictionary<ITrader, List<Tradeable>> cachedTradeableItem = new Dictionary<ITrader, List<Tradeable>>();
        private readonly Dictionary<ITrader, List<Tradeable>> cachedTradeableFood = new Dictionary<ITrader, List<Tradeable>>();
        private readonly Dictionary<ITrader, List<Tradeable>> cachedNoMainCat = new Dictionary<ITrader, List<Tradeable>>();

        private readonly Dictionary<ITrader, List<Tradeable>> cachedTradeableManufacturedS = new Dictionary<ITrader, List<Tradeable>>();
        private readonly Dictionary<ITrader, List<Tradeable>> cachedTradeableResourcesRawS = new Dictionary<ITrader, List<Tradeable>>();
        private readonly Dictionary<ITrader, List<Tradeable_Pawn>> cachedTradeablePawnsS = new Dictionary<ITrader, List<Tradeable_Pawn>>();
        private readonly Dictionary<ITrader, List<Tradeable>> cachedTradeableBuildingsS = new Dictionary<ITrader, List<Tradeable>>();
        private readonly Dictionary<ITrader, List<Tradeable>> cachedTradeableApparelS = new Dictionary<ITrader, List<Tradeable>>();
        private readonly Dictionary<ITrader, List<Tradeable>> cachedTradeableWeaponsS = new Dictionary<ITrader, List<Tradeable>>();
        private readonly Dictionary<ITrader, List<Tradeable>> cachedTradeableItemS = new Dictionary<ITrader, List<Tradeable>>();
        private readonly Dictionary<ITrader, List<Tradeable>> cachedTradeableFoodS = new Dictionary<ITrader, List<Tradeable>>();
        private readonly Dictionary<ITrader, List<Tradeable>> cachedNoMainCatS = new Dictionary<ITrader, List<Tradeable>>();

        private readonly Dictionary<Tradeable, string[]> priceStrCache = new Dictionary<Tradeable, string[]>();

        private bool showManufactured = true;
        private bool showBuildings = true;
        private bool showApparel = true;
        private bool showWeapons = true;
        private bool showOthers = true;
        private bool showPawns = true;
        private bool showFood = true;
        private bool showItem = true;
        private bool showRaw = true;

        private int lineManufactured;
        private int lineBuildings;
        private int lineApparel;
        private int lineWeapons;
        private int lineOthers;
        private int linePawns;
        private int lineFood;
        private int lineItem;
        private int lineRaw;

        public override Vector2 RequestedTabSize => new Vector2(1010f, 640f);

        public override void PreOpen()
        {
            base.PreOpen();
            if (negotiator == null || negotiator.Dead || negotiator.Downed || negotiator.Faction != Faction.OfPlayer)
            {
                ResetNegotiator();
                ResetStocks();
            }
            if (tradersCurrency.Count == 0 || traders.Any(t => t.NextRestockTick - Find.TickManager.TicksGame <= 0)) ResetStocks();

            foreach (Settlement t in traders.Cast<Settlement>())
            {
                traderRestockIn.SetOrAdd(t, (t.NextRestockTick - Find.TickManager.TicksGame).ToStringTicksToDays());
            }

            haveConsole = Find.CurrentMap.listerBuildings.allBuildingsColonist.Any(b => b.def.IsCommsConsole && b.GetComp<CompPowerTrader>() is CompPowerTrader ct && ct.PowerOn);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            float num = 0f;

            // Search bar
            Rect labelRect = new Rect(0f, 10f + num, 150f, 30f);
            Widgets.Label(labelRect, "WDYS.SearchBar".Translate());
            Rect searchRect = new Rect(labelRect.width + 10f, 10f + num, inRect.width - labelRect.width - 116f, 30f);
            search = Widgets.TextField(searchRect, search);
            // Reset button
            Rect resetRect = new Rect(labelRect.width + searchRect.width + 20f, 10f + num, inRect.width - labelRect.width - searchRect.width - 36f, 30f);
            if (Widgets.ButtonText(resetRect, "WDYS.Reset".Translate()))
            {
                ResetStocks();
                ResetNegotiator();
                foreach (Settlement t in traders.Cast<Settlement>())
                {
                    traderRestockIn.SetOrAdd(t, (t.NextRestockTick - Find.TickManager.TicksGame).ToStringTicksToDays());
                }
            }
            TooltipHandler.TipRegion(resetRect, "WDYS.ResetToolTip".Translate());
            num += 50f;

            // Draw pawn
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect pawnIconRow = new Rect(10f, 10f + num, 40f, 40f);
            Widgets.ThingIcon(pawnIconRow, negotiator);
            Rect pawnTextRow = new Rect(70f, 10f + num, windowRect.width - 70f, 40f);
            Widgets.Label(pawnTextRow, "WDYS.Pawn".Translate(negotiator.NameFullColored, negotiatorStat.ToStringPercent()));
            num += 50f;

            Rect mainRect = new Rect(0f, 10f + num, inRect.width, inRect.height - num - 10f);
            if (traders.Count > 0 && ((WDYS_Mod.settings.needTradeConsole && haveConsole) || !WDYS_Mod.settings.needTradeConsole))
            {
                // Draw goods
                FillMainRect(mainRect);
            }
            else if (traders.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(mainRect, "WDYS.NoTrader".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(mainRect, "WDYS.NeedTradeConsoleUI".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
            }

            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void ResetStocks()
        {
            traders.Clear();
            tradersCurrency.Clear();
            traderTileDistance.Clear();

            cachedTradeableManufactured.Clear();
            cachedTradeableResourcesRaw.Clear();
            cachedTradeableBuildings.Clear();
            cachedTradeableApparel.Clear();
            cachedTradeableWeapons.Clear();
            cachedTradeablePawns.Clear();
            cachedTradeableFood.Clear();
            cachedTradeableItem.Clear();
            cachedNoMainCat.Clear();

            priceStrCache.Clear();

            search = "";
            lastsearch = ".";

            cachedTradeableManufacturedS.Clear();
            cachedTradeableResourcesRawS.Clear();
            cachedTradeableBuildingsS.Clear();
            cachedTradeableApparelS.Clear();
            cachedTradeableWeaponsS.Clear();
            cachedTradeablePawnsS.Clear();
            cachedTradeableFoodS.Clear();
            cachedTradeableItemS.Clear();
            cachedNoMainCatS.Clear();

            FindValidSettlements();

            if (traders.Count > 0)
            {
                foreach (ITrader trader in traders)
                {
                    CacheTradeables(trader);
                }
                Sort(cachedTradeableManufactured);
                Sort(cachedTradeableResourcesRaw);
                Sort(cachedTradeableBuildings);
                Sort(cachedTradeableApparel);
                Sort(cachedTradeableWeapons);
                Sort(cachedTradeablePawns);
                Sort(cachedTradeableFood);
                Sort(cachedTradeableItem);
                Sort(cachedNoMainCat);
            }
        }

        private void FindValidSettlements()
        {
            var nTile = Find.CurrentMap.Tile;
            var worldGrid = Find.WorldGrid;
            var settlements = Find.World.worldObjects.Settlements;

            for (int i = 0; i < settlements.Count; i++)
            {
                var s = settlements[i];
                int dist = worldGrid.TraversalDistanceBetween(s.Tile, nTile, false);
                // Skip if cannot reach
                if (WDYS_Mod.settings.onlyShowReachable && dist == int.MaxValue)
                    continue;

                var fac = s.Faction;
                if (fac != Faction.OfPlayer &&
                    s.CanTradeNow &&
                    !fac.HostileTo(Faction.OfPlayer) &&
                    (!WDYS_Mod.settings.activateMaxTile || (WDYS_Mod.settings.activateMaxTile && dist <= WDYS_Mod.settings.maxTiles) || (!WDYS_Mod.settings.onlyShowReachable && dist == int.MaxValue)) &&
                    (!WDYS_Mod.settings.onlyIndustrialAndHigher || (WDYS_Mod.settings.onlyIndustrialAndHigher && s.Faction.def.techLevel >= TechLevel.Industrial)))
                {
                    traders.Add(s);
                    traderTileDistance.Add(s, dist);
                }
            }
        }

        private void CacheTradeables(ITrader trader)
        {
            TradeSession.playerNegotiator = negotiator;
            TradeSession.trader = trader;
            TradeSession.giftMode = false;

            foreach (Thing t in trader.Goods)
            {
                // Create tradeable
                var tradeable = t is Pawn ? new Tradeable_Pawn() : new Tradeable();
                tradeable.AddThing(t, Transactor.Trader);

                if (tradeable.IsCurrency && !tradeable.IsFavor && !tradersCurrency.ContainsKey(trader))
                {
                    tradersCurrency.Add(trader, tradeable.CountHeldBy(Transactor.Trader));
                }
                else if (!tradeable.IsFavor && tradeable.TraderWillTrade)
                {
                    priceStrCache.Add(tradeable, new string[] { tradeable.GetPriceFor(TradeAction.PlayerBuys).ToStringMoney(), tradeable.GetPriceTooltip(TradeAction.PlayerBuys) });

                    if (t is Pawn) AddTo(trader, cachedTradeablePawns, tradeable as Tradeable_Pawn);
                    else if (t.def.IsWithinCategory(ThingCategoryDefOf.ResourcesRaw)) AddTo(trader, cachedTradeableResourcesRaw, tradeable);
                    else if (t.def.IsWithinCategory(ThingCategoryDefOf.Manufactured)) AddTo(trader, cachedTradeableManufactured, tradeable);
                    else if (t.def.IsWithinCategory(ThingCategoryDefOf.Buildings) || t.GetInnerIfMinified() is Thing tM && tM.def.IsWithinCategory(ThingCategoryDefOf.Buildings)) AddTo(trader, cachedTradeableBuildings, tradeable);
                    else if (t.def.IsWithinCategory(ThingCategoryDefOf.Apparel)) AddTo(trader, cachedTradeableApparel, tradeable);
                    else if (t.def.IsWithinCategory(ThingCategoryDefOf.Weapons)) AddTo(trader, cachedTradeableWeapons, tradeable);
                    else if (t.def.IsWithinCategory(ThingCategoryDefOf.Foods)) AddTo(trader, cachedTradeableFood, tradeable);
                    else if (t.def.IsWithinCategory(ThingCategoryDefOf.Items)) AddTo(trader, cachedTradeableItem, tradeable);
                    else AddTo(trader, cachedNoMainCat, tradeable);
                }
            }
        }

        private void AddTo(ITrader trader, Dictionary<ITrader, List<Tradeable>> dic, Tradeable toAdd)
        {
            if (dic.ContainsKey(trader)) dic[trader].Add(toAdd);
            else dic.Add(trader, new List<Tradeable> { toAdd });
        }

        private void AddTo(ITrader trader, Dictionary<ITrader, List<Tradeable_Pawn>> dic, Tradeable_Pawn toAdd)
        {
            if (dic.ContainsKey(trader)) dic[trader].Add(toAdd);
            else dic.Add(trader, new List<Tradeable_Pawn> { toAdd });
        }

        private void Sort(Dictionary<ITrader, List<Tradeable>> dic)
        {
            for (int i = 0; i < dic.Keys.Count; i++)
            {
                dic[dic.ElementAt(i).Key] = dic.ElementAt(i).Value.OrderBy((Tradeable tr) => tr, sorter.Comparer).ThenBy((Tradeable tr) => tr.ThingDef.label).ThenBy(delegate (Tradeable tr)
                {
                    if (tr.AnyThing.TryGetQuality(out QualityCategory result))
                    {
                        return (int)result;
                    }
                    return -1;
                }).ToList();
            }
        }

        private void Sort(Dictionary<ITrader, List<Tradeable_Pawn>> dic)
        {
            for (int i = 0; i < dic.Keys.Count; i++)
            {
                dic[dic.ElementAt(i).Key] = dic.ElementAt(i).Value.OrderBy((Tradeable_Pawn tr) => tr, sorter.Comparer).ThenBy((Tradeable_Pawn tr) => tr.ThingDef.label).ToList();
            }
        }

        private void ResetNegotiator()
        {
            negotiator = null;
            negotiatorStat = -1f;

            foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonistsSpawned)
            {
                if (pawn.IsColonist && !pawn.WorkTagIsDisabled(WorkTags.Social))
                {
                    float tempStat = pawn.GetStatValue(StatDefOf.TradePriceImprovement);
                    if (tempStat > negotiatorStat)
                    {
                        negotiatorStat = tempStat;
                        negotiator = pawn;
                    }
                }
            }
        }

        private void FillMainRect(Rect mainRect)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            if (lastsearch != search)
            {
                CacheSearchResult();
                lastsearch = search;
            }

            Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, line * 30f);
            Widgets.BeginScrollView(mainRect, ref scrollPosition, viewRect);

            count = 0f;
            if (cachedTradeableFood.Count > 0 && lineFood > 0) Show("WDYS.Foods".Translate(), viewRect, mainRect, cachedTradeableFoodS, ref showFood);
            if (cachedTradeableResourcesRaw.Count > 0 && lineRaw > 0) Show("WDYS.ResourcesRaw".Translate(), viewRect, mainRect, cachedTradeableResourcesRawS, ref showRaw);
            if (cachedTradeableManufactured.Count > 0 && lineManufactured > 0) Show("WDYS.Manufactured".Translate(), viewRect, mainRect, cachedTradeableManufacturedS, ref showManufactured);
            if (cachedTradeableApparel.Count > 0 && lineApparel > 0) Show("WDYS.Apparel".Translate(), viewRect, mainRect, cachedTradeableApparelS, ref showApparel);
            if (cachedTradeableWeapons.Count > 0 && lineWeapons > 0) Show("WDYS.Weapons".Translate(), viewRect, mainRect, cachedTradeableWeaponsS, ref showWeapons);
            if (cachedTradeableItem.Count > 0 && lineItem > 0) Show("WDYS.Items".Translate(), viewRect, mainRect, cachedTradeableItemS, ref showItem);
            if (cachedTradeableBuildings.Count > 0 && lineBuildings > 0) Show("WDYS.Buildings".Translate(), viewRect, mainRect, cachedTradeableBuildingsS, ref showBuildings);
            if (cachedTradeablePawns.Count > 0 && linePawns > 0) Show("WDYS.Pawns".Translate(), viewRect, mainRect, cachedTradeablePawnsS, ref showPawns);
            if (cachedNoMainCat.Count > 0 && lineOthers > 0) Show("WDYS.Other".Translate(), viewRect, mainRect, cachedNoMainCatS, ref showOthers);

            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public void CacheSearchResult()
        {
            line = 0;

            CacheSearchInto(cachedTradeableManufactured, cachedTradeableManufacturedS);
            CacheSearchInto(cachedTradeableResourcesRaw, cachedTradeableResourcesRawS);
            CacheSearchInto(cachedTradeableBuildings, cachedTradeableBuildingsS);
            CacheSearchInto(cachedTradeableApparel, cachedTradeableApparelS);
            CacheSearchInto(cachedTradeableWeapons, cachedTradeableWeaponsS);
            CacheSearchInto(cachedTradeablePawns, cachedTradeablePawnsS);
            CacheSearchInto(cachedTradeableFood, cachedTradeableFoodS);
            CacheSearchInto(cachedTradeableItem, cachedTradeableItemS);
            CacheSearchInto(cachedNoMainCat, cachedNoMainCatS);

            lineManufactured = CountListInDic(cachedTradeableManufacturedS);
            lineBuildings = CountListInDic(cachedTradeableBuildingsS);
            lineApparel = CountListInDic(cachedTradeableApparelS);
            lineWeapons = CountListInDic(cachedTradeableWeaponsS);
            lineOthers = CountListInDic(cachedNoMainCatS);
            linePawns = CountListInDic(cachedTradeablePawnsS);
            lineFood = CountListInDic(cachedTradeableFoodS);
            lineItem = CountListInDic(cachedTradeableItemS);
            lineRaw = CountListInDic(cachedTradeableResourcesRawS);

            if (cachedTradeableManufactured.Count > 0 && lineManufactured > 0 && showManufactured) line += lineManufactured + 1;
            else if (cachedTradeableManufactured.Count > 0 && lineManufactured > 0) line++;
            if (cachedTradeableBuildings.Count > 0 && lineBuildings > 0 && showBuildings) line += lineBuildings + 1;
            else if (cachedTradeableBuildings.Count > 0 && lineBuildings > 0) line++;
            if (cachedTradeableApparel.Count > 0 && lineApparel > 0 && showApparel) line += lineApparel + 1;
            else if (cachedTradeableApparel.Count > 0 && lineApparel > 0) line++;
            if (cachedTradeableWeapons.Count > 0 && lineWeapons > 0 && showWeapons) line += lineWeapons + 1;
            else if (cachedTradeableWeapons.Count > 0 && lineWeapons > 0) line++;
            if (cachedTradeableResourcesRaw.Count > 0 && lineRaw > 0 && showRaw) line += lineRaw + 1;
            else if (cachedTradeableResourcesRaw.Count > 0 && lineRaw > 0) line++;
            if (cachedTradeablePawns.Count > 0 && linePawns > 0 && showPawns) line += linePawns + 1;
            else if (cachedTradeablePawns.Count > 0 && linePawns > 0) line++;
            if (cachedTradeableFood.Count > 0 && lineFood > 0 && showFood) line += lineFood + 1;
            else if (cachedTradeableFood.Count > 0 && lineFood > 0) line++;
            if (cachedTradeableItem.Count > 0 && lineItem > 0 && showItem) line += lineItem + 1;
            else if (cachedTradeableItem.Count > 0 && lineItem > 0) line++;
            if (cachedNoMainCat.Count > 0 && lineOthers > 0 && showOthers) line += lineOthers + 1;
            else if (cachedNoMainCat.Count > 0 && lineOthers > 0) line++;
        }

        private int CountListInDic<T>(Dictionary<ITrader, List<T>> dic)
        {
            int count = 0;
            foreach (var pair in dic)
            {
                count += pair.Value.Count;
            }
            return count;
        }

        private void CacheSearchInto(Dictionary<ITrader, List<Tradeable>> from, Dictionary<ITrader, List<Tradeable>> into)
        {
            foreach (var pair in from)
            {
                into.SetOrAdd(pair.Key, new List<Tradeable>());
                if (search != "")
                {
                    foreach (Tradeable tr in pair.Value.ToList().FindAll(tr => tr.Label.ToLower().Contains(search.ToLower())))
                    {
                        into[pair.Key].Add(tr);
                    }
                }
                else
                {
                    into[pair.Key].AddRange(from[pair.Key]);
                }
            }
        }

        private void CacheSearchInto(Dictionary<ITrader, List<Tradeable_Pawn>> from, Dictionary<ITrader, List<Tradeable_Pawn>> into)
        {
            foreach (var pair in from)
            {
                into.SetOrAdd(pair.Key, new List<Tradeable_Pawn>());
                if (search != "")
                {
                    foreach (Tradeable_Pawn tr in pair.Value.ToList().FindAll(tr => tr.Label.ToLower().Contains(search.ToLower())))
                    {
                        into[pair.Key].Add(tr);
                    }
                }
                else
                {
                    into[pair.Key].AddRange(from[pair.Key]);
                }
            }
        }

        private void Show<T>(string cat, Rect viewRect, Rect mainRect, Dictionary<ITrader, List<T>> dic, ref bool show)
        {
            bool showS = show;
            int index = 0;
            Rect lRect = new Rect(0f, count, viewRect.width, 30f);
            Widgets.CheckboxLabeled(lRect, cat, ref show);
            index++;
            count += 30f;

            if (show != showS) CacheSearchResult();
            if (show)
            {
                Widgets.DrawLineHorizontal(lRect.x, lRect.y + 28f, lRect.width);
                Widgets.DrawLineHorizontal(lRect.x, lRect.y, lRect.width);
                foreach (var pair in dic)
                {
                    foreach (var item in pair.Value.Cast<Tradeable>())
                    {
                        if (count > scrollPosition.y - 5f && count < scrollPosition.y + mainRect.height)
                        {
                            Rect rect = new Rect(0f, count, viewRect.width, 30f);
                            DrawTradeableRow(rect, item, index, pair.Key);
                            index++;
                        }
                        count += 30f;
                    }
                }
            }
        }

        public void DrawTradeableRow(Rect rect, Tradeable trad, int index, ITrader trader)
        {
            if (index % 2 == 1) Widgets.DrawLightHighlight(rect);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.BeginGroup(rect);
            float num = rect.width;

            Rect countRect = new Rect(num - 55f, 0f, 55f, rect.height);
            DrawCount(countRect, trad);

            Rect priceRect = new Rect(num - 55f - 55f, 0f, 55f, rect.height);
            DrawPrice(priceRect, trad);

            num -= countRect.width + priceRect.width;

            DrawTraderInfo(rect, trader, ref num);

            DrawThingInfo(rect, trad, num);

            GenUI.ResetLabelAlign();
            GUI.EndGroup();
        }

        private void DrawPrice(Rect rect, Tradeable trad)
        {
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
                TooltipHandler.TipRegion(rect, priceStrCache[trad][1]);
            }

            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rect, priceStrCache[trad][0]);
        }

        private void DrawCount(Rect rect, Tradeable trad)
        {
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
                TooltipHandler.TipRegion(rect, "TraderCount".Translate());
            }

            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rect, trad.CountHeldBy(Transactor.Trader).ToStringCached());
        }

        private void DrawTraderInfo(Rect rect, ITrader trader, ref float num)
        {
            Settlement settlement = trader as Settlement;
            if (settlement.Spawned)
            {
                // Restock info
                Rect settlementRestockRect = new Rect(num - 175f, 0f, 175f, rect.height);
                Widgets.Label(settlementRestockRect, "WDYS.BeforeRestock".Translate(traderRestockIn[settlement]));
                num -= 175f;
                // Settlement name
                Text.Anchor = TextAnchor.MiddleLeft;
                Rect settlementNameRect = new Rect(num - 120f, 0f, 120f, rect.height);
                Widgets.Label(settlementNameRect, settlement.Name.TrimmedToLength(16));
                num -= 120f;
                // Draw icon
                GUI.color = trader.Faction.Color;
                Rect factionIconRect = new Rect(num - 35f, 0f, 30f, rect.height);
                GUI.DrawTexture(factionIconRect, trader.Faction.def.FactionIcon);
                GUI.color = Color.white;
                num -= 35f;
                // Invisible jump rect
                Rect jumpRect = new Rect(num, 0f, 150f, rect.height);
                if (Mouse.IsOver(jumpRect))
                {
                    Widgets.DrawHighlight(jumpRect);
                    var dist = traderTileDistance[settlement] != -1 && traderTileDistance[settlement] != int.MaxValue ? "WDYS.TilesAway".Translate(settlement.Name, traderTileDistance[settlement]) : "WDYS.NoPath".Translate();
                    var silver = "WDYS.HaveXSilver".Translate(settlement.Name, tradersCurrency[trader]);
                    TooltipHandler.TipRegion(jumpRect, dist + "\n" + silver + ".");
                }
                if (Widgets.ButtonInvisible(jumpRect))
                {
                    CameraJumper.TryJump(settlement.Tile);
                    Find.WorldSelector.ClearSelection();
                    Find.WorldSelector.Select(settlement);
                    Close();
                }
            }
        }

        private void DrawThingInfo(Rect rect, Tradeable trad, float num)
        {
            TransferableUIUtility.DoExtraIcons(trad, rect, ref num);

            if (ModsConfig.IdeologyActive && trad.AnyThing is Pawn pawn && pawn.guest != null)
            {
                var label = (pawn.guest.joinStatus == JoinStatus.JoinAsColonist ? "JoinsAsColonist" : "JoinsAsSlave").Translate();
                var joinRect = new Rect(num - 140f, 0.0f, 140f, rect.height);
                num -= 140f;

                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(joinRect, label);
                Text.Anchor = TextAnchor.UpperLeft;

                if (Mouse.IsOver(joinRect))
                {
                    Widgets.DrawHighlight(joinRect);
                    TooltipHandler.TipRegion(joinRect, (pawn.guest.joinStatus == JoinStatus.JoinAsColonist ? "JoinsAsColonistDesc" : "JoinsAsSlaveDesc").Translate());
                }
            }

            Rect idRect = new Rect(0f, 0f, num, rect.height);
            TransferableUIUtility.DrawTransferableInfo(trad, idRect, Color.white);
        }
    }
}