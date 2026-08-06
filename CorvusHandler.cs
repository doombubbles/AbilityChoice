using System;
using System.Collections.Generic;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.CorvusSpells;
using Il2CppAssets.Scripts.Simulation.Corvus.Spells;
using Il2CppAssets.Scripts.Simulation.Corvus.Spells.Continuous;
using Il2CppAssets.Scripts.Simulation.Corvus.Spells.Instant;
using Il2CppAssets.Scripts.Simulation.Corvus.TowerManager;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;

namespace AbilityChoice;

public static class CorvusHandler
{
    public static readonly Dictionary<int, HashSet<CorvusSpellType>> SpellsToReactivate = new();

    /// <summary>
    /// Add continuous spells to the reactive list when you run out of mana
    /// </summary>
    [HarmonyPatch(typeof(CorvusManager), nameof(CorvusManager.ConsumeContinuousSpellMana))]
    internal static class CorvusManager_ConsumeContinuousSpellMana
    {
        [HarmonyPrefix]
        internal static void Prefix(CorvusManager __instance, ref List<CorvusContinuousSpell> __state)
        {
            __state = __instance.activeContinuousSpells.Keys().ToList();
        }

        [HarmonyPostfix]
        internal static void Postfix(CorvusManager __instance, CorvusContinuousSpell spell,
            ref List<CorvusContinuousSpell> __state)
        {
            if (__instance.IsSpellActive(spell)) return;

            SpellsToReactivate.TryAdd(__instance.owner, []);
            var reactivate = SpellsToReactivate[__instance.owner];

            foreach (var continuousSpell in __state.Where(s => CorvusAbilityChoice.EnabledForSpell(s.SpellType)))
            {
                reactivate.Add(continuousSpell.SpellType);
            }
        }
    }

    [HarmonyPatch(typeof(CorvusManager), nameof(CorvusManager.Process))]
    internal static class CorvusManager_Process
    {
        [HarmonyPostfix]
        internal static void Postfix(CorvusManager __instance)
        {
            SpellsToReactivate.TryAdd(__instance.owner, []);

            // Let other spells be cast alongside modified Overload
            foreach (var spellType in new[] {CorvusSpellType.Overload, CorvusSpellType.Trample})
            {
                if (CorvusAbilityChoice.EnabledForSpell(spellType) &&
                    __instance.spellRestrictions.TryGetValue(spellType, out var restrictions))
                {
                    restrictions.Clear();
                }
            }

            // Drain mana to maintain modified instant spells
            foreach (var spell in __instance.activeInstantSpells.Keys().ToList())
            {
                var timing = __instance.activeInstantSpells[spell];
                if (!CorvusAbilityChoice.EnabledForSpell(spell.SpellType) || timing.FramesRemainingActive > 1 ||
                    spell.SpellType == CorvusSpellType.SoulBarrier) continue;

                if (__instance.TryConsumeMana(spell.SpellModel.initialManaCost))
                {
                    timing.totalFramesActive += (int) (spell.SpellModel.duration * 60);
                    if (spell.SpellType is CorvusSpellType.Nourishment or CorvusSpellType.Overload
                        or CorvusSpellType.Trample)
                    {
                        spell.Cast();
                    }
                }
                else
                {
                    if (spell.SpellType is not (CorvusSpellType.Nourishment or CorvusSpellType.Overload))
                    {
                        __instance.CancelInstantSpell(spell.SpellType);
                    }

                    SpellsToReactivate[__instance.owner].Add(spell.SpellType);
                }
            }


            // Reactive spells if has the mana
            foreach (var spell in SpellsToReactivate[__instance.owner].ToList())
            {
                if (!__instance.CanSpellBeCast(spell) ||
                    !__instance.HasEnoughManaForSpell(__instance.LookupSpellByType(spell))) continue;

                SpellsToReactivate[__instance.owner].Remove(spell);
                
                if (!__instance.IsSpellActive(spell))
                {
                    __instance.CastSpell(spell);
                }
            }
        }
    }

    /// <summary>
    /// Never show cooldown or active duration for AbilitChoice spells
    /// </summary>
    [HarmonyPatch(typeof(CorvusManager), nameof(CorvusManager.GetSpellStatus))]
    internal static class CorvusManager_GetSpellStatus
    {
        [HarmonyPostfix]
        internal static void Postfix(CorvusSpellType spellType, ref CorvusSpellStatus __result)
        {
            if (CorvusAbilityChoice.EnabledForSpell(spellType) && spellType != CorvusSpellType.SoulBarrier)
            {
                __result = new CorvusSpellStatus(spellType, __result.isActive, __result.canSpellBeCast, 0, 0);
            }
        }
    }

    /// <summary>
    /// Still show mana as draining due to modified instant spells
    /// </summary>
    [HarmonyPatch(typeof(CorvusManager), nameof(CorvusManager.IsManaDraining))]
    internal static class CorvusManager_IsManaDraining
    {
        [HarmonyPostfix]
        internal static void Postfix(CorvusManager __instance, ref bool __result)
        {
            __result |= __instance.activeInstantSpells.Keys()
                .Any(spell => CorvusAbilityChoice.EnabledForSpell(spell.SpellType));
        }
    }

    /// <summary>
    /// Show the deactivate button for the modified instant spells
    /// </summary>
    [HarmonyPatch(typeof(CorvusSpellbookUi), nameof(CorvusSpellbookUi.UpdateDeactivateButton))]
    internal static class CorvusSpellbookUi_UpdateDeactivateButton
    {
        [HarmonyPostfix]
        internal static bool Prefix(CorvusSpellbookUi __instance)
        {
            if (!TowerSelectionMenu.instance.IsSecondarySelectionMenuVisible() ||
                !__instance.TowerBasedShopItemModel.Is(out CorvusInstantSpellModel spell) ||
                !CorvusAbilityChoice.EnabledForSpell(spell.spellType)) return true;

            var status = InGame.Bridge.GetCorvusSpellStatus(spell.spellType, InGame.Bridge.GetInputId());
            __instance.deactivateButton.gameObject.SetActive(status.isActive);
            return false;
        }
    }

    /// <summary>
    /// Deactivate button correctly cancels modified instant spells
    /// </summary>
    [HarmonyPatch(typeof(CorvusSpellbookUi), nameof(CorvusSpellbookUi.DeactivateSelectedSpell))]
    internal static class CorvusSpellbookUi_DeactivateSelectedSpell
    {
        [HarmonyPrefix]
        internal static bool Prefix(CorvusSpellbookUi __instance)
        {
            if (!__instance.TowerBasedShopItemModel.Is(out CorvusInstantSpellModel spell) ||
                !CorvusAbilityChoice.EnabledForSpell(spell.spellType)) return true;

            InGame.Bridge.Simulation.GetCorvusManager(InGame.Bridge.GetInputId()).CancelInstantSpell(spell.spellType);
            return false;
        }
    }

    /// <summary>
    /// Fixup any weird interactions
    /// </summary>
    [HarmonyPatch(typeof(CorvusManager), nameof(CorvusManager.TriggerCorvusToReleaseSpirit))]
    internal static class CorvusManager_TriggerCorvusToReleaseSpirit
    {
        public static bool releasing;

        [HarmonyPrefix]
        internal static void Prefix(CorvusManager __instance)
        {
            releasing = true;
        }

        [HarmonyPostfix]
        internal static void Postfix(CorvusManager __instance)
        {
            releasing = false;
            if (CorvusAbilityChoice.EnabledForSpell(CorvusSpellType.Echo) &&
                __instance.IsSpellActive(CorvusSpellType.Echo) && __instance.duplicateSpiritSubTower == null)
            {
                __instance.LookupSpellByType(CorvusSpellType.Echo).Cast<Echo>().CreateDuplicateSpirit();
            }
        }
    }


    /// <summary>
    /// Save info for spells to reactivate
    /// </summary>
    [HarmonyPatch(typeof(CorvusManager), nameof(CorvusManager.GetSaveMetaData))]
    internal static class CorvusManager_GetSaveMetaData
    {
        [HarmonyPostfix]
        internal static void Postfix(CorvusManager __instance,
            Il2CppSystem.Collections.Generic.Dictionary<string, string> metaData)
        {
            if (!SpellsToReactivate.TryGetValue(__instance.owner, out var reactivate)) return;

            metaData.Add("CorvusMetadata_SpellsToReactivate" + __instance.owner,
                reactivate.Select(type => type.ToString()).Join(delimiter: ","));
        }
    }

    /// <summary>
    /// Load saved info for spells to reactivate
    /// </summary>
    [HarmonyPatch(typeof(CorvusManager), nameof(CorvusManager.SetSaveMetaData))]
    internal static class CorvusManager_SetSaveMetaData
    {
        [HarmonyPostfix]
        internal static void Postfix(CorvusManager __instance,
            Il2CppSystem.Collections.Generic.Dictionary<string, string> metaData)
        {
            if (!metaData.TryGetValue("CorvusMetadata_SpellsToReactivate" + __instance.owner, out var s)) return;

            SpellsToReactivate[__instance.owner] = s.Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(Enum.Parse<CorvusSpellType>).ToHashSet();
        }
    }
}
