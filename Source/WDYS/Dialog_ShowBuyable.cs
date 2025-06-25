using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace WDYS
{
    public class Dialog_ShowBuyable : Window
    {
        private readonly List<Tradeable> cachedNoMainCat = new List<Tradeable>();
        private readonly List<Tradeable_Pawn> cachedTradeablePawns = new List<Tradeable_Pawn>();
        private readonly Pawn negotiator;
        private readonly float negotiatorStat;
        private readonly Settlement settlement;
        private readonly TransferableSorterDef sorter = TransferableSorterDefOf.Category;
        private readonly ITrader trader;
        private Tradeable cachedCurrencyTradeable;
        private List<Tradeable> cachedTradeables = new List<Tradeable>();
        private Vector2 scrollPosition = Vector2.zero;
        private string search = "";

        private bool showApparel = true;
        private bool showBuildings = true;
        private bool showFood = true;
        private bool showItem = true;
        private bool showManufactured = true;
        private bool showOthers = true;
        private bool showPawns = true;
        private bool showRaw = true;
        private bool showWeapons = true;

        public Dialog_ShowBuyable(ITrader trader, Pawn negotiator, Settlement settlement)
        {
            forcePause = true;
            absorbInputAroundWindow = true;
            this.trader = trader;
            this.negotiator = negotiator;
            negotiatorStat = this.negotiator != null ? this.negotiator.GetStatValue(StatDefOf.TradePriceImprovement) : 0f;
            this.settlement = settlement;

            TradeSession.playerNegotiator = this.negotiator;
            TradeSession.trader = this.trader;
            TradeSession.giftMode = false;
            CacheTradeables();
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1024f, Mathf.Min(UI.screenHeight, 1000));
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            float num = 0f;

            // Window title
            Rect titleRect = new Rect(0f, 0f, inRect.width, 60f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect, "WDYS.WindowTitle".Translate(settlement.Name, (settlement.trader.NextRestockTick - Find.TickManager.TicksGame).ToStringTicksToDays()));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            num += 60f;
            Text.Anchor = TextAnchor.MiddleLeft;

            // Search bar
            Rect labelRect = new Rect(0f, 10f + num, 150f, 30f);
            Widgets.Label(labelRect, "WDYS.SearchBar".Translate());
            Rect searchRect = new Rect(labelRect.width + 10f, 10f + num, inRect.width - labelRect.width - 30f, 30f);
            search = Widgets.TextField(searchRect, search);
            num += 50f;

            // Draw pawn
            Rect pawnIconRow = new Rect(0f, 10f + num, 40f, 40f);
            Widgets.ThingIcon(pawnIconRow, negotiator);
            Rect pawnTextRow = new Rect(60f, 10f + num, windowRect.width - 60f, 40f);
            Widgets.Label(pawnTextRow, "WDYS.Pawn".Translate(negotiator?.NameFullColored ?? string.Empty, negotiatorStat.ToStringPercent()));
            num += 50f;

            // Draw settlement money
            Rect settlementMoneyRect = new Rect(0f, num, inRect.width - 16f, 30f);
            DrawTradeableRow(settlementMoneyRect, cachedCurrencyTradeable, 0);
            num += 30f;
            Widgets.DrawLineHorizontal(0f, num, windowRect.width);
            num += 5f;

            // Draw goods
            Rect mainRect = new Rect(0f, 10f + num, inRect.width, inRect.height - num - 100f);
            FillMainRect(mainRect, search);

            Rect rect4 = new Rect((mainRect.width / 2f) - 80f, mainRect.y + mainRect.height + 40f, 160f, 40f);
            if (Widgets.ButtonText(rect4, "WDYS.Quit".Translate(), true, true, true))
            {
                Close(true);
            }
        }

        public void DrawTradeableRow(Rect rect, Tradeable trad, int index)
        {
            if (trad != null && rect != null)
            {
                if (index % 2 == 1) Widgets.DrawLightHighlight(rect);
                Text.Font = GameFont.Small;
                GUI.BeginGroup(rect);
                float num = rect.width;

                Rect tradRect = new Rect(num - 75f, 0f, 75f, rect.height);
                if (trad.CountHeldBy(Transactor.Trader) is int tradCount && tradCount != 0 && trad.IsThing)
                {
                    if (Mouse.IsOver(tradRect))
                    {
                        Widgets.DrawHighlight(tradRect);
                    }
                    Text.Anchor = TextAnchor.MiddleRight;
                    Rect countRect = tradRect;
                    countRect.xMin += 5f;
                    countRect.xMax -= 5f;
                    Widgets.Label(countRect, tradCount.ToStringCached());
                    TooltipHandler.TipRegionByKey(tradRect, "TraderCount");
                    Rect priceRect = new Rect(tradRect.x - 100f, 0f, 100f, rect.height);
                    Text.Anchor = TextAnchor.MiddleRight;
                    DrawPrice(priceRect, trad);
                }
                num -= 590f;
                TransferableUIUtility.DoExtraIcons(trad, rect, ref num);
                if (ModsConfig.IdeologyActive)
                    TransferableUIUtility.DrawCaptiveTradeInfo(trad, TradeSession.trader, rect, ref num);
                Rect idRect = new Rect(0f, 0f, num, rect.height);
                TransferableUIUtility.DrawTransferableInfo(trad, idRect, Color.white);
                GenUI.ResetLabelAlign();
                GUI.EndGroup();
            }
        }

        private void CacheTradeables()
        {
            foreach (Thing t in trader.Goods)
            {
                Tradeable tradeable = TradeableMatching(t, cachedTradeables);
                if (tradeable == null)
                {
                    if (t is Pawn)
                    {
                        tradeable = new Tradeable_Pawn();
                    }
                    else
                    {
                        tradeable = new Tradeable();
                    }
                    cachedTradeables.Add(tradeable);
                }
                tradeable.AddThing(t, Transactor.Trader);
            }

            cachedCurrencyTradeable = cachedTradeables.FirstOrDefault((Tradeable x) => x.IsCurrency && !x.IsFavor);
            cachedTradeables =
                (from tr in cachedTradeables
                 where !tr.IsCurrency && (trader.TraderKind.WillTrade(tr.ThingDef) || !TradeSession.trader.TraderKind.hideThingsNotWillingToTrade)
                 select tr).OrderBy((Tradeable tr) => tr, sorter.Comparer).ThenBy((Tradeable tr) => tr.ThingDef.label).ThenBy(delegate (Tradeable tr)
                 {
                     if (tr.AnyThing.TryGetQuality(out QualityCategory result))
                     {
                         return (int)result;
                     }
                     return -1;
                 }).ToList();

            foreach (Tradeable tr in cachedTradeables)
            {
                if (tr is Tradeable_Pawn trp) cachedTradeablePawns.Add(trp);
                else if (!InMainCat(tr) && tr.ThingDef.race == null) cachedNoMainCat.Add(tr);
            }
        }

        private void DrawPrice(Rect rect, Tradeable trad)
        {
            rect = rect.Rounded();
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
                TooltipHandler.TipRegion(rect, new TipSignal(() => trad.GetPriceTooltip(TradeAction.PlayerBuys), trad.GetHashCode() * 297));
            }
            string label = trad.GetPriceFor(TradeAction.PlayerBuys).ToStringMoney();
            Rect rect2 = new Rect(rect);
            rect2.xMax -= 5f;
            rect2.xMin += 5f;
            if (Text.Anchor == TextAnchor.MiddleLeft)
            {
                rect2.xMax += 300f;
            }
            if (Text.Anchor == TextAnchor.MiddleRight)
            {
                rect2.xMin -= 300f;
            }

            switch (trader.TraderKind.PriceTypeFor(trad.ThingDef, TradeAction.PlayerBuys))
            {
                case PriceType.VeryCheap:
                    GUI.color = new Color(0f, 1f, 0f);
                    break;

                case PriceType.Cheap:
                    GUI.color = new Color(0.5f, 1f, 0.5f);
                    break;

                case PriceType.Normal:
                    GUI.color = Color.white;
                    break;

                case PriceType.Expensive:
                    GUI.color = new Color(1f, 0.5f, 0.5f);
                    break;

                case PriceType.Exorbitant:
                    GUI.color = new Color(1f, 0f, 0f);
                    break;
            }

            Widgets.Label(rect2, label);
            GUI.color = Color.white;
        }

        private void FillMainRect(Rect mainRect, string search)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            int line = 9;
            if (showFood) line += cachedTradeables.FindAll(tr => Is(tr, ThingCategoryDefOf.Foods) && tr.ThingDef.label.ToLower().Contains(search.ToLower())).Count;
            if (showRaw) line += cachedTradeables.FindAll(tr => Is(tr, ThingCategoryDefOf.ResourcesRaw) && tr.ThingDef.label.ToLower().Contains(search.ToLower())).Count;
            if (showManufactured) line += cachedTradeables.FindAll(tr => Is(tr, ThingCategoryDefOf.Manufactured) && tr.ThingDef.label.ToLower().Contains(search.ToLower())).Count;
            if (showApparel) line += cachedTradeables.FindAll(tr => Is(tr, ThingCategoryDefOf.Apparel) && tr.ThingDef.label.ToLower().Contains(search.ToLower())).Count;
            if (showWeapons) line += cachedTradeables.FindAll(tr => Is(tr, ThingCategoryDefOf.Weapons) && tr.ThingDef.label.ToLower().Contains(search.ToLower())).Count;
            if (showItem) line += cachedTradeables.FindAll(tr => Is(tr, ThingCategoryDefOf.Items) && tr.ThingDef.label.ToLower().Contains(search.ToLower())).Count;
            if (showBuildings) line += cachedTradeables.FindAll(tr => Is(tr, ThingCategoryDefOf.Buildings) && tr.ThingDef.label.ToLower().Contains(search.ToLower())).Count;
            if (showPawns) line += cachedTradeablePawns.FindAll(tr => tr.ThingDef.label.ToLower().Contains(search.ToLower())).Count;
            if (showOthers) line += cachedNoMainCat.FindAll(tr => tr.ThingDef.label.ToLower().Contains(search.ToLower())).Count;

            float height = line * 30f;
            Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, height);
            Widgets.BeginScrollView(mainRect, ref scrollPosition, viewRect, true);

            float num = 0f;

            Show(ThingCategoryDefOf.Foods, viewRect, ref num, ref showFood);
            Show(ThingCategoryDefOf.ResourcesRaw, viewRect, ref num, ref showRaw);
            Show(ThingCategoryDefOf.Manufactured, viewRect, ref num, ref showManufactured);
            Show(ThingCategoryDefOf.Apparel, viewRect, ref num, ref showApparel);
            Show(ThingCategoryDefOf.Weapons, viewRect, ref num, ref showWeapons);
            Show(ThingCategoryDefOf.Items, viewRect, ref num, ref showItem);
            Show(ThingCategoryDefOf.Buildings, viewRect, ref num, ref showBuildings);

            if (cachedTradeablePawns.Any())
            {
                int index = 0, n = 0;
                Rect lRect = new Rect(0f, num, viewRect.width, 30f);
                Widgets.CheckboxLabeled(lRect, "WDYS.Pawns".Translate(), ref showPawns);
                num += 30f;
                index++;
                if (showPawns)
                {
                    foreach (var item in cachedTradeablePawns.FindAll(tr => tr.ThingDef.label.ToLower().Contains(search.ToLower())))
                    {
                        Rect rect = new Rect(0f, num, viewRect.width, 30f);
                        DrawTradeableRow(rect, item, index);
                        num += 30f;
                        index++;
                        n++;
                    }
                }
                if (n > 0)
                {
                    Widgets.DrawHighlight(lRect);
                }
            }

            if (cachedNoMainCat.Any())
            {
                int index = 0, n = 0;
                Rect lRect = new Rect(0f, num, viewRect.width, 30f);
                Widgets.CheckboxLabeled(lRect, "WDYS.Other".Translate(), ref showOthers);
                num += 30f;
                index++;
                if (showPawns)
                {
                    foreach (var item in cachedNoMainCat.FindAll(tr => tr.ThingDef.label.ToLower().Contains(search.ToLower())))
                    {
                        Rect rect = new Rect(0f, num, viewRect.width, 30f);
                        DrawTradeableRow(rect, item, index);
                        num += 30f;
                        index++;
                        n++;
                    }
                }
                if (n > 0)
                {
                    Widgets.DrawHighlight(lRect);
                }
            }

            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private bool InMainCat(Tradeable tr) => Is(tr, ThingCategoryDefOf.Foods) || Is(tr, ThingCategoryDefOf.ResourcesRaw) || Is(tr, ThingCategoryDefOf.Manufactured) || Is(tr, ThingCategoryDefOf.Apparel) ||
                                                Is(tr, ThingCategoryDefOf.Weapons) || Is(tr, ThingCategoryDefOf.Items) || Is(tr, ThingCategoryDefOf.Buildings);

        private bool Is(Tradeable tr, ThingCategoryDef def) => tr.ThingDef.thingCategories != null && (tr.ThingDef.thingCategories.Contains(def) || tr.ThingDef.thingCategories.FindAll(c => c.Parents.Contains(def)).Any());

        private float Show(ThingCategoryDef cat, Rect viewRect, ref float num, ref bool show)
        {
            int index = 0, n = 0;
            Rect lRect = new Rect(0f, num, viewRect.width, 30f);
            Widgets.CheckboxLabeled(lRect, cat.LabelCap, ref show);
            num += 30f;
            index++;
            if (show)
            {
                foreach (var item in cachedTradeables.FindAll(tr => Is(tr, cat) && tr.ThingDef.label.ToLower().Contains(search.ToLower())))
                {
                    Rect rect = new Rect(0f, num, viewRect.width, 30f);
                    DrawTradeableRow(rect, item, index);
                    num += 30f;
                    index++;
                    n++;
                }
            }

            if (n > 0)
            {
                Widgets.DrawHighlight(lRect);
            }

            return num;
        }

        private Tradeable TradeableMatching(Thing thing, List<Tradeable> tradeables)
        {
            if (thing == null || tradeables == null)
            {
                return null;
            }
            for (int i = 0; i < tradeables.Count; i++)
            {
                Tradeable tradeable = tradeables[i];
                if (tradeable.HasAnyThing)
                {
                    TransferAsOneMode mode = trader.TraderKind.WillTrade(tradeable.ThingDef) ? TransferAsOneMode.Normal : TransferAsOneMode.InactiveTradeable;
                    if (TransferableUtility.TransferAsOne(thing, tradeable.AnyThing, mode))
                    {
                        return tradeable;
                    }
                }
            }
            return null;
        }
    }
}