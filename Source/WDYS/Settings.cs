using RimWorld;
using UnityEngine;
using Verse;

namespace WDYS
{
    internal class WDYS_Settings : ModSettings
    {
        public bool onlyIndustrialAndHigher = true;
        public bool needTradeConsole = true;
        public bool activateGlobalStockTab = true;
        public bool activateMaxTile = true;
        public float maxTiles = 40f;
        public bool onlyShowReachable = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref activateGlobalStockTab, "activateGlobalStockTab", true);
            Scribe_Values.Look(ref onlyIndustrialAndHigher, "onlyIndustrialAndHigher", true);
            Scribe_Values.Look(ref needTradeConsole, "needTradeConsole", true);
            Scribe_Values.Look(ref activateMaxTile, "activateMaxTile", true);
            Scribe_Values.Look(ref maxTiles, "maxTiles", 40f);
            Scribe_Values.Look(ref onlyShowReachable, "onlyShowReachable", true);
        }
    }

    [StaticConstructorOnStartup]
    internal class WDYS_Mod : Mod
    {
        public static WDYS_Settings settings;

        public WDYS_Mod(ModContentPack content) : base(content)
        {
            settings = GetSettings<WDYS_Settings>();
        }

        public override string SettingsCategory() => "WDYS".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard lst = new Listing_Standard();
            lst.Begin(inRect);
            lst.CheckboxLabeled("WDYS.GlobalStockTab".Translate() + ": ", ref settings.activateGlobalStockTab);
            lst.GapLine();
            lst.CheckboxLabeled("WDYS.NeedTradeConsole".Translate() + ": ", ref settings.needTradeConsole);
            lst.Gap();
            lst.CheckboxLabeled("WDYS.OnlyIndustrial".Translate() + ": ", ref settings.onlyIndustrialAndHigher);
            lst.GapLine();
            lst.CheckboxLabeled("WDYS.EnableMaxTile".Translate() + ": ", ref settings.activateMaxTile);
            if (settings.activateMaxTile)
            {
                lst.Label("WDYS.MaxDays".Translate(settings.maxTiles));
                settings.maxTiles = lst.Slider(settings.maxTiles, 1f, 100f);
            }
            lst.GapLine();
            lst.CheckboxLabeled("WDYS.OnlyShowReachable".Translate() + ": ", ref settings.onlyShowReachable);
            lst.End();
            settings.Write();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            DefAlterer.DoAlteration();
        }
    }
}