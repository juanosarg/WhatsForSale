using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace WhatsForSale
{
    internal class MainTabWindow_TradeStock : MainTabWindow
    {
        private readonly TradeStockManager _tradeManager = new TradeStockManager();
        private readonly List<List<TradeableWithTrader>> _cachedLists = new List<List<TradeableWithTrader>>(9);
        private readonly CategoryEntry[] _categories;

        private bool _haveConsole;
        private string _search = "";
        private string _lastSearch = "";
        private Vector2 _scrollPosition;
        private int _selectedTabIndex = 0;

        public override Vector2 RequestedTabSize => new Vector2(1010f, 640f);

        public MainTabWindow_TradeStock()
        {
            _categories = new[]
            {
                new CategoryEntry { LabelKey = "WDYS.Foods",        GetList = c => _tradeManager.GetTradeables(ThingCategoryDefOf.Foods, c) },
                new CategoryEntry { LabelKey = "WDYS.ResourcesRaw", GetList = c => _tradeManager.GetTradeables(ThingCategoryDefOf.ResourcesRaw, c) },
                new CategoryEntry { LabelKey = "WDYS.Manufactured", GetList = c => _tradeManager.GetTradeables(ThingCategoryDefOf.Manufactured, c) },
                new CategoryEntry { LabelKey = "WDYS.Apparel",      GetList = c => _tradeManager.GetTradeables(ThingCategoryDefOf.Apparel, c) },
                new CategoryEntry { LabelKey = "WDYS.Weapons",      GetList = c => _tradeManager.GetTradeables(ThingCategoryDefOf.Weapons, c) },
                new CategoryEntry { LabelKey = "WDYS.Items",        GetList = c => _tradeManager.GetTradeables(ThingCategoryDefOf.Items, c) },
                new CategoryEntry { LabelKey = "WDYS.Buildings",    GetList = c => _tradeManager.GetTradeables(ThingCategoryDefOf.Buildings, c) },
                new CategoryEntry { LabelKey = "WDYS.Pawns",        GetList = c => _tradeManager.GetPawns(c) },
                new CategoryEntry { LabelKey = "WDYS.Other",        GetList = c => _tradeManager.GetOthers(c) }
            };
        }

        public override void PreOpen()
        {
            base.PreOpen();
            _haveConsole = Find.CurrentMap.listerBuildings.allBuildingsColonist
                .Any(b => b.def.IsCommsConsole && b.GetComp<CompPowerTrader>()?.PowerOn == true);
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
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;

            float y = 0f;
            DrawSearchPanel(inRect, ref y);
            DrawNegotiatorPanel(ref y);
            DrawTabs(inRect, ref y);
            DrawStockArea(inRect, y);

            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawSearchPanel(Rect inRect, ref float y)
        {
            var labelRect = new Rect(0f, y + 10f, TradeUIConstants.LabelWidth, TradeUIConstants.TextFieldHeight);
            Widgets.Label(labelRect, "WDYS.SearchBar".Translate());

            var fieldRect = new Rect(labelRect.xMax + 10f, labelRect.y, inRect.width - labelRect.width - 116f, TradeUIConstants.TextFieldHeight);
            _search = Widgets.TextField(fieldRect, _search);

            var resetRect = new Rect(fieldRect.xMax + TradeUIConstants.ResetButtonPadding, fieldRect.y, inRect.width - fieldRect.xMax - TradeUIConstants.ResetButtonPadding, TradeUIConstants.TextFieldHeight);
            if (Widgets.ButtonText(resetRect, "WDYS.Reset".Translate()))
            {
                _search = "";
                _lastSearch = "";
                _cachedLists.Clear();
                _tradeManager.RefreshData();
            }
            TooltipHandler.TipRegion(resetRect, "WDYS.ResetToolTip".Translate());
            y += TradeUIConstants.TextFieldHeight + TradeUIConstants.ControlSpacingY;
        }
        private void DrawNegotiatorPanel(ref float y)
        {
            const float iconSize = TradeUIConstants.IconSize;
            const float spacing = TradeUIConstants.ControlSpacingY;
            float labelX = 0f;
            float labelWidth = windowRect.width;
            float labelHeight;
            string labelText;

            var negotiator = _tradeManager.Negotiator;
            if (negotiator is null)
            {
                labelText = "WDYS.PawnNotFound".Translate();
                labelHeight = Text.CalcHeight(labelText, labelWidth);
                var labelRect = new Rect(labelX, y, labelWidth, labelHeight);
                Text.Anchor = TextAnchor.UpperLeft;
                Widgets.Label(labelRect, labelText);
                y += labelHeight + spacing;
            }
            else
            {
                labelText = "WDYS.Pawn".Translate(negotiator.NameFullColored, _tradeManager.NegotiatorStat.ToStringPercent());

                var iconRect = new Rect(0f, y, iconSize, iconSize);
                Widgets.ThingIcon(iconRect, negotiator);

                labelX = iconRect.xMax + 10f;
                labelWidth = windowRect.width - labelX;
                labelHeight = Text.CalcHeight(labelText, labelWidth);

                var labelY = y + (iconSize - labelHeight) / 2f;
                var labelRect = new Rect(labelX, labelY, labelWidth, labelHeight);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, labelText);

                y += iconSize + spacing;
            }
        }
        private void DrawTabs(Rect inRect, ref float y)
        {
            var tabRecords = new List<TabRecord>();
            for (int i = 0; i < _categories.Length; i++)
            {
                var tab = _categories[i].ToTabRecord(
                    getIndex: () => i,
                    setIndex: index => {
                        _selectedTabIndex = index;
                        _scrollPosition = Vector2.zero;
                        _lastSearch = "";
                    },
                    currentIndex: _selectedTabIndex);

                if (tab is TabRecord record)
                    tabRecords.Add(record);
            }

            TabDrawer.DrawTabs(new Rect(0f, y + 30f, inRect.width, 30f), tabRecords);
            y += 35f;
        }
        private void DrawStockArea(Rect inRect, float y)
        {
            var area = new Rect(0f, y, inRect.width, inRect.height - y);
            bool needConsole = WhatsForSale_Mod.settings.NeedTradeConsole;
            if (_tradeManager.Traders.Count > 0 && (!needConsole || _haveConsole))
                FillMainRect(area);
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                var msg = _tradeManager.Traders.Count == 0 ? "WDYS.NoTrader" : "WDYS.NeedTradeConsoleUI";
                Widgets.Label(area, msg.Translate());
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }
        private void FillMainRect(Rect mainRect)
        {
            var crit = new TradeableFilterCriteria(_search);

            if (_cachedLists.Count != _categories.Length || _lastSearch != _search)
            {
                _scrollPosition = Vector2.zero;
                _lastSearch = _search;
                _cachedLists.Clear();

                foreach (var category in _categories)
                    _cachedLists.Add(category.GetList(crit));
            }

            var selectedItems = _cachedLists[_selectedTabIndex];
            float viewHeight = selectedItems.Count * TradeUIConstants.LineHeight;
            var viewRect = new Rect(0f, 0f, mainRect.width - TradeUIConstants.ScrollbarWidth, viewHeight);

            Widgets.BeginScrollView(mainRect, ref _scrollPosition, viewRect);

            var context = new CategoryDrawContext(
                selectedItems,
                viewRect,
                _scrollPosition.y,
                _scrollPosition.y + mainRect.height,
                _tradeManager.GetRestockInfo
            );

            float y = 0f;
            TradeableCategoryDrawer.Draw(ref y, context, showTraderInfo: true);
            Widgets.EndScrollView();
        }
    }
}