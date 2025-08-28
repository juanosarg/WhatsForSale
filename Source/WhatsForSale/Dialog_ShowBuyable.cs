using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace WhatsForSale
{
    internal class Dialog_ShowBuyable : Window
    {
        private readonly BuyableDialogManager _tradeManager;
        private readonly Settlement _settlement;
        private readonly ITrader _trader;
        private readonly CategoryEntry[] _categories;

        private string _search = "";
        private Vector2 _scrollPosition = Vector2.zero;
        private int _selectedTabIndex = 0;

        public override Vector2 InitialSize => new Vector2(1024f, Mathf.Min(UI.screenHeight, 1000f));

        public Dialog_ShowBuyable(Settlement settlement)
        {
            forcePause = true;
            absorbInputAroundWindow = true;

            _settlement = settlement;
            _trader = _settlement;

            _tradeManager = new BuyableDialogManager(_trader);

            _categories = new[]
            {
                new CategoryEntry { LabelKey = "WDYS.Foods",         GetList = c => _tradeManager.GetTradeables(ThingCategoryDefOf.Foods, c) },
                new CategoryEntry { LabelKey = "WDYS.ResourcesRaw",  GetList = c => _tradeManager.GetTradeables(ThingCategoryDefOf.ResourcesRaw, c) },
                new CategoryEntry { LabelKey = "WDYS.Manufactured",  GetList = c => _tradeManager.GetTradeables(ThingCategoryDefOf.Manufactured, c) },
                new CategoryEntry { LabelKey = "WDYS.Apparel",       GetList = c => _tradeManager.GetTradeables(ThingCategoryDefOf.Apparel, c) },
                new CategoryEntry { LabelKey = "WDYS.Items",         GetList = c => _tradeManager.GetTradeables(ThingCategoryDefOf.Items, c) },
                new CategoryEntry { LabelKey = "WDYS.Buildings",     GetList = c => _tradeManager.GetTradeables(ThingCategoryDefOf.Buildings, c) },
                new CategoryEntry { LabelKey = "WDYS.Pawns",         GetList = c => _tradeManager.GetPawns(c) },
                new CategoryEntry { LabelKey = "WDYS.Other",         GetList = c => _tradeManager.GetOthers(c) }
            };
        }

        public override void PostClose()
        {
            base.PostClose();

            TradeSession.playerNegotiator = null;
            TradeSession.trader = null;
            TradeSession.giftMode = false;
        }
        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;
            DrawHeader(inRect, ref y);
            DrawSearchBar(inRect, ref y);
            DrawNegotiatorInfo(ref y);
            DrawCurrencyRow(inRect, ref y);
            DrawTabs(inRect, ref y);
            DrawSelectedCategory(inRect, ref y);
            DrawCloseButton(inRect, y);
        }

        private void DrawHeader(Rect inRect, ref float y)
        {
            var rect = new Rect(0f, y, inRect.width, 60f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            var restock = (_settlement.trader.NextRestockTick - Find.TickManager.TicksGame).ToStringTicksToDays();
            Widgets.Label(rect, "WDYS.WindowTitle".Translate(_settlement.Name, restock));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            y += 60f;
        }
        private void DrawSearchBar(Rect inRect, ref float y)
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            var labelRect = new Rect(0f, y + 10f, 150f, 30f);
            Widgets.Label(labelRect, "WDYS.SearchBar".Translate());
            var fieldRect = new Rect(labelRect.xMax + 10f, labelRect.y, inRect.width - labelRect.width - 30f, 30f);
            _search = Widgets.TextField(fieldRect, _search);
            Text.Anchor = TextAnchor.UpperLeft;
            y += 50f;
        }
        private void DrawNegotiatorInfo(ref float y)
        {
            var negotiator = _tradeManager.Negotiator;
            if (negotiator is null) return;

            var iconRect = new Rect(0f, y, TradeUIConstants.IconSize, TradeUIConstants.IconSize);
            Widgets.ThingIcon(iconRect, negotiator);

            var labelWidth = windowRect.width - iconRect.width - 10f;
            var labelHeight = Text.CalcHeight(negotiator.NameFullColored, labelWidth);
            var labelX = iconRect.xMax + 10f;
            var labelY = iconRect.y + (iconRect.height - labelHeight) / 2f;
            var labelRect = new Rect(labelX, labelY, labelWidth, labelHeight);
            Widgets.Label(labelRect, "WDYS.Pawn".Translate(negotiator.NameFullColored, _tradeManager.NegotiatorStat.ToStringPercent()));

            y += iconRect.height + 10f;
        }
        private void DrawCurrencyRow(Rect inRect, ref float y)
        {
            var rowRect = new Rect(0f, y, inRect.width - 16f, 30f);
            var currency = _tradeManager.GetCurrency(_trader);
            TradeableRowDrawer.Draw(rowRect, currency, _trader, 0, showTraderInfo: false);
            y += 30f;

            Widgets.DrawLineHorizontal(0f, y, windowRect.width, Color.gray);
            y += TradeUIConstants.ControlSpacingY;
        }
        private void DrawTabs(Rect inRect, ref float y)
        {
            var tabRecords = new List<TabRecord>();

            for (int i = 0; i < _categories.Length; i++)
            {
                var tab = _categories[i].ToTabRecord(
                    getIndex: () => i,
                    setIndex: newIndex => {
                        _selectedTabIndex = newIndex;
                        _scrollPosition = Vector2.zero;
                    },
                    currentIndex: _selectedTabIndex);

                if (tab is TabRecord record)
                    tabRecords.Add(record);
            }

            TabDrawer.DrawTabs(new Rect(0f, y + 30f, inRect.width, 30f), tabRecords);
            y += 35f;
        }
        private void DrawSelectedCategory(Rect inRect, ref float y)
        {
            var mainRect = new Rect(0f, y, inRect.width, inRect.height - y - 100f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            var criteria = new TradeableFilterCriteria(_search);
            var category = _categories[_selectedTabIndex];
            var items = category.GetList(criteria).ToList();

            float viewHeight = items.Count * TradeUIConstants.LineHeight;
            var viewRect = new Rect(0f, 0f, mainRect.width - TradeUIConstants.ScrollbarWidth, viewHeight);

            Widgets.BeginScrollView(mainRect, ref _scrollPosition, viewRect);

            float localY = 0f;
            float scrollTop = _scrollPosition.y;
            float scrollBottom = scrollTop + mainRect.height;

            var context = new CategoryDrawContext(
                items,
                viewRect,
                scrollTop,
                scrollBottom,
                getRestockText: null);

            TradeableCategoryDrawer.Draw(ref localY, context, showTraderInfo: false);

            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;
            y += mainRect.height;
        }
        private void DrawCloseButton(Rect inRect, float y)
        {
            var buttonRect = new Rect(inRect.width / 2f - 80f, y + 40f, 160f, 40f);
            if (Widgets.ButtonText(buttonRect, "WDYS.Quit".Translate()))
                Close();
        }
    }
}