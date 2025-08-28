using RimWorld;
using Verse;

namespace WhatsForSale
{
    [StaticConstructorOnStartup]
    internal class DefAlterer
    {
        static DefAlterer()
        {
            DoAlteration();
        }

        public static void DoAlteration()
        {
            var def = DefDatabase<MainButtonDef>.GetNamedSilentFail("WFS_TradeStock");
            if (def != null)
                def.buttonVisible = WhatsForSale_Mod.settings.ActivateGlobalStockTab;
        }
    }
}