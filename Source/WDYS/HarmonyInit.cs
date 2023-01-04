using HarmonyLib;
using Verse;

namespace WDYS
{
    [StaticConstructorOnStartup]
    internal static class HarmonyInit
    {
        static HarmonyInit()
        {
            new Harmony("kikohi.WhatsForSale").PatchAll();
        }
    }
}