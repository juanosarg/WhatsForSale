using HarmonyLib;
using UnityEngine;
using Verse;

namespace WhatsForSale
{
    internal class WhatsForSale_Mod : Mod
    {
        public static WhatsForSale_Settings settings;

        public WhatsForSale_Mod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("kikohi.whatsforale");
            harmony.PatchAll();
            Log.Message("[WhatsForSale] Initialized");

            settings = GetSettings<WhatsForSale_Settings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(inRect);
            listing_Standard.CheckboxLabeled("WDYS.GlobalStockTab".Translate() + ": ", ref settings.ActivateGlobalStockTab);
            listing_Standard.GapLine();
            listing_Standard.CheckboxLabeled("WDYS.NeedTradeConsole".Translate() + ": ", ref settings.NeedTradeConsole);
            listing_Standard.Gap();
            listing_Standard.CheckboxLabeled("WDYS.OnlyIndustrial".Translate() + ": ", ref settings.OnlyIndustrialAndHigher);
            listing_Standard.Gap();
            listing_Standard.CheckboxLabeled("WDYS.FilterByRoyalTitleEligibility".Translate() + ": ", ref settings.FilterByRoyalTitleEligibility);
            listing_Standard.GapLine();
            listing_Standard.CheckboxLabeled("WDYS.EnableMaxTile".Translate() + ": ", ref settings.EnableMaxTile);
            if (settings.EnableMaxTile)
            {
                listing_Standard.Label("WDYS.MaxDays".Translate(settings.MaxTiles));
                settings.MaxTiles = listing_Standard.Slider(settings.MaxTiles, 1f, 100f);
            }
            listing_Standard.GapLine();
            listing_Standard.CheckboxLabeled("WDYS.OnlyShowReachable".Translate() + ": ", ref settings.OnlyShowReachable);
            listing_Standard.End();
            settings.Write();
        }
        public override void WriteSettings()
        {
            base.WriteSettings();
            DefAlterer.DoAlteration();
        }
        public override string SettingsCategory() => "WDYS".Translate();
    }
}