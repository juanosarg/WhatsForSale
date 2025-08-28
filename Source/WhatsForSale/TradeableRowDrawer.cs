using RimWorld;
using RimWorld.Planet;
using System;
using UnityEngine;
using Verse;

namespace WhatsForSale
{
    public static class TradeableRowDrawer
    {
        private const float CountColumnWidth = 50f;
        private const float PriceColumnWidth = 50f;
        private const float TraderIconWidth = 30f;
        private const float TraderLabelWidth = 270f;
        private const float TraderNameWidth = 120f;
        private const float ExtraInfoWidth = 150f;
        private const float JoinLabelWidth = 140f;
        private const float LabelPadding = 5f;
        private const float LabelSpacing = 4f;
        private const float IconLabelSpacing = 2f;

        public static void Draw(Rect rowRect, Tradeable tradeable, ITrader trader, int index, bool showTraderInfo = true, Func<Settlement, string> getRestockText = null)
        {
            if (tradeable is null) return;
            if (index % 2 == 1) Widgets.DrawLightHighlight(rowRect);

            if (getRestockText is null)
                getRestockText = _ => string.Empty;

            Text.Font = GameFont.Small;
            GUI.BeginGroup(rowRect);
            float remainingWidth = rowRect.width;

            DrawCount(rowRect, tradeable, ref remainingWidth);
            DrawPrice(rowRect, tradeable, ref remainingWidth);

            if (showTraderInfo) DrawTraderInfo(rowRect, trader, ref remainingWidth, getRestockText);

            DrawExtraInfo(rowRect, tradeable, ref remainingWidth);
            DrawMainInfo(rowRect, tradeable, remainingWidth);

            GenUI.ResetLabelAlign();
            GUI.EndGroup();
        }

        private static void DrawCount(Rect rowRect, Tradeable tradeable, ref float remainingWidth)
        {
            Rect countRect = new Rect(remainingWidth - CountColumnWidth, 0f, CountColumnWidth, rowRect.height);
            if (!tradeable.IsThing) return;

            int count = tradeable.CountHeldBy(Transactor.Trader);
            if (count <= 0) return;

            if (Mouse.IsOver(countRect)) Widgets.DrawHighlight(countRect);

            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(countRect.ContractedBy(LabelPadding), count.ToStringCached());
            Text.Anchor = TextAnchor.UpperLeft;

            TooltipHandler.TipRegionByKey(countRect, "TraderCount");
            remainingWidth -= CountColumnWidth;
        }
        private static void DrawPrice(Rect rowRect, Tradeable tradeable, ref float remainingWidth)
        {
            Rect priceRect = new Rect(remainingWidth - PriceColumnWidth, 0f, PriceColumnWidth, rowRect.height);
            if (!tradeable.IsThing) return;

            Rect labelRect = priceRect.ContractedBy(LabelPadding);
            Text.Anchor = TextAnchor.MiddleRight;

            string priceLabel;
            string tooltip = null;

            if (TradeSession.playerNegotiator != null && tradeable.AnyThing != null)
            {
                priceLabel = tradeable.GetPriceFor(TradeAction.PlayerBuys).ToStringMoney();
                tooltip = tradeable.GetPriceTooltip(TradeAction.PlayerBuys);
            }
            else
            {
                priceLabel = tradeable.ThingDef.BaseMarketValue.ToStringMoney();
                tooltip = "WDYS.NoNegotiator".Translate();
            }

            if (Mouse.IsOver(priceRect))
            {
                Widgets.DrawHighlight(priceRect);
                if (tooltip != null)
                    TooltipHandler.TipRegion(priceRect, new TipSignal(() => tooltip, tradeable.GetHashCode() * 297));
            }

            Widgets.Label(labelRect, priceLabel);
            Text.Anchor = TextAnchor.UpperLeft;

            remainingWidth -= PriceColumnWidth;
        }
        private static void DrawTraderInfo(Rect rowRect, ITrader trader, ref float remainingWidth, Func<Settlement, string> getRestockText)
        {
            if (!(trader is Settlement settlement) || !settlement.Spawned) return;

            float infoBlockWidth = TraderIconWidth + TraderLabelWidth;
            Rect traderInfoRect = new Rect(remainingWidth - infoBlockWidth, 0f, infoBlockWidth, rowRect.height);

            if (ButtonHighlight(traderInfoRect))
            {
                CameraJumper.TryJump(settlement.Tile);
                Find.WorldSelector.ClearSelection();
                Find.WorldSelector.Select(settlement);
            }

            Rect iconRect = new Rect(traderInfoRect.x, 0f, TraderIconWidth, rowRect.height);
            GUI.color = trader.Faction?.Color ?? Color.white;
            GUI.DrawTexture(iconRect, trader.Faction?.def.FactionIcon ?? BaseContent.BadTex);
            GUI.color = Color.white;

            Text.Anchor = TextAnchor.MiddleLeft;

            string settlementName = settlement.Name.TrimmedToLength(12);
            string restockLabel = getRestockText(settlement);

            float restockWidth = TraderLabelWidth - TraderNameWidth;

            Rect nameRect = new Rect(iconRect.xMax + IconLabelSpacing, 0f, TraderNameWidth - IconLabelSpacing, rowRect.height);
            Widgets.Label(nameRect, settlementName);

            if (!string.IsNullOrEmpty(restockLabel))
            {
                Rect restockRect = new Rect(nameRect.xMax + LabelSpacing, 0f, restockWidth - LabelSpacing, rowRect.height);
                Widgets.Label(restockRect, $"Restock in {restockLabel}");
            }

            Text.Anchor = TextAnchor.UpperLeft;
            remainingWidth -= infoBlockWidth;
        }
        private static void DrawExtraInfo(Rect rowRect, Tradeable tradeable, ref float remainingWidth)
        {
            Rect extraInfoRect = new Rect(remainingWidth - ExtraInfoWidth, 0f, ExtraInfoWidth, rowRect.height);
            float currentX = extraInfoRect.x;

            TransferableUIUtility.DoExtraIcons(tradeable, extraInfoRect, ref currentX);

            if (ModsConfig.IdeologyActive && tradeable.AnyThing is Pawn pawn && pawn.guest != null)
            {
                TaggedString joinLabel = pawn.guest.joinStatus == JoinStatus.JoinAsColonist
                    ? "JoinsAsColonist".Translate()
                    : "JoinsAsSlave".Translate();

                Rect joinLabelRect = new Rect(currentX, 0f, JoinLabelWidth, rowRect.height);

                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(joinLabelRect, joinLabel);
                Text.Anchor = TextAnchor.UpperLeft;

                if (Mouse.IsOver(joinLabelRect))
                {
                    Widgets.DrawHighlight(joinLabelRect);
                    string tooltip = pawn.guest.joinStatus == JoinStatus.JoinAsColonist
                        ? "JoinsAsColonistDesc".Translate()
                        : "JoinsAsSlaveDesc".Translate();
                    TooltipHandler.TipRegion(joinLabelRect, tooltip);
                }
            }

            remainingWidth -= ExtraInfoWidth;
        }
        private static void DrawMainInfo(Rect rowRect, Tradeable tradeable, float width)
        {
            Rect infoRect = new Rect(0f, 0f, width, rowRect.height);
            TransferableUIUtility.DrawTransferableInfo(tradeable, infoRect, Color.white);
        }
        private static bool ButtonHighlight(Rect rect)
        {
            if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
            return Widgets.ButtonInvisible(rect);
        }
    }
}