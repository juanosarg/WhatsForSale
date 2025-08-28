using Verse;

namespace WhatsForSale
{
    internal class WhatsForSale_Settings : ModSettings
    {
        public bool ActivateGlobalStockTab = true;
        public bool OnlyIndustrialAndHigher = true;
        public bool NeedTradeConsole = true;
        public bool EnableMaxTile = true;
        public bool OnlyShowReachable = true;
        public bool FilterByRoyalTitleEligibility = false;
        public float MaxTiles = 40f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ActivateGlobalStockTab, "ActivateGlobalStockTab", defaultValue: true);
            Scribe_Values.Look(ref OnlyIndustrialAndHigher, "OnlyIndustrialAndHigher", defaultValue: true);
            Scribe_Values.Look(ref NeedTradeConsole, "NeedTradeConsole", defaultValue: true);
            Scribe_Values.Look(ref EnableMaxTile, "EnableMaxTile", defaultValue: true);
            Scribe_Values.Look(ref OnlyShowReachable, "OnlyShowReachable", defaultValue: true);
            Scribe_Values.Look(ref FilterByRoyalTitleEligibility, "FilterByRoyalTitleEligibility", defaultValue: false);
            Scribe_Values.Look(ref MaxTiles, "MaxTiles", 40f);
        }
    }
}