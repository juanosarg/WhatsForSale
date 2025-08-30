using RimWorld;
using Verse;

namespace WDYS
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
            if (WDYS_Mod.settings.activateGlobalStockTab)
                DefDatabase<MainButtonDef>.GetNamed("WFS_TradeStock").buttonVisible = true;
            else
                DefDatabase<MainButtonDef>.GetNamed("WFS_TradeStock").buttonVisible = false;
        }
    }
}