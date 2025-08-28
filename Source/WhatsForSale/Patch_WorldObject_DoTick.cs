using HarmonyLib;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace WhatsForSale
{
    [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.DoTick))]
    public static class Patch_WorldObject_DoTick
    {
        private static readonly MethodInfo _tickMethod = typeof(WorldObject).GetMethod("Tick", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo _tickIntervalMethod = typeof(WorldObject).GetMethod("TickInterval", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _tickDeltaField = typeof(WorldObject).GetField("tickDelta", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _destroyedField = typeof(WorldObject).GetField("destroyed", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly PropertyInfo _updateRateTicksProp = typeof(WorldObject).GetProperty("UpdateRateTicks", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly PropertyInfo _updateRateTickOffsetProp = typeof(WorldObject).GetProperty("UpdateRateTickOffset", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo _cachedField = typeof(WorldObject).GetField("cached", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _cachedIsHolderField = typeof(WorldObject).GetField("cachedIsHolder", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _cachedHolderField = typeof(WorldObject).GetField("cachedHolder", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _cachedTickableField = typeof(WorldObject).GetField("cachedTickable", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _tmpHoldersField = typeof(WorldObject).GetField("tmpHolders", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool Prefix(WorldObject __instance)
        {
            _tickMethod.Invoke(__instance, null);

            int tickDelta = (int)_tickDeltaField.GetValue(__instance);
            tickDelta++;
            int updateRate = (int)_updateRateTicksProp.GetValue(__instance);
            int updateOffset = (int)_updateRateTickOffsetProp.GetValue(__instance);

            if (tickDelta > updateRate || GenTicks.IsTickInterval(updateOffset, updateRate))
            {
                _tickIntervalMethod.Invoke(__instance, new object[] { tickDelta });
                tickDelta = 0;
            }

            _tickDeltaField.SetValue(__instance, tickDelta);

            if ((bool)_destroyedField.GetValue(__instance))
                return false;

            if (!(bool)_cachedField.GetValue(__instance))
            {
                _cachedField.SetValue(__instance, true);
                IThingHolder holder = __instance as IThingHolder;
                IThingHolderTickable tickable = __instance as IThingHolderTickable;
                _cachedHolderField.SetValue(__instance, holder);
                _cachedTickableField.SetValue(__instance, tickable);
                _cachedIsHolderField.SetValue(__instance, holder != null);
            }

            bool isHolder = (bool)_cachedIsHolderField.GetValue(__instance);
            var tickableCache = (IThingHolderTickable)_cachedTickableField.GetValue(__instance);

            if (!isHolder || (tickableCache != null && !tickableCache.ShouldTickContents))
                return false;

            if (__instance is Settlement)
                return false;

            var holderRoot = (IThingHolder)_cachedHolderField.GetValue(__instance);
            if (holderRoot is null)
                return false;

            var tmpHolders = (List<IThingHolder>)_tmpHoldersField.GetValue(__instance);
            if (tmpHolders is null)
            {
                tmpHolders = new List<IThingHolder>(8);
                _tmpHoldersField.SetValue(__instance, tmpHolders);
            }

            tmpHolders.Add(holderRoot);
            holderRoot.GetChildHolders(tmpHolders);

            foreach (var holder in tmpHolders)
            {
                ThingOwner directlyHeld = holder.GetDirectlyHeldThings();
                if (directlyHeld is null)
                    continue;

                var owner = directlyHeld.Owner;
                if (owner is Map || owner is Caravan)
                    continue;

                directlyHeld.DoTick();

                if ((bool)_destroyedField.GetValue(__instance))
                    break;
            }

            tmpHolders.Clear();
            return false;
        }
    }
}