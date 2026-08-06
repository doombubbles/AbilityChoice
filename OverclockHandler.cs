using System;
using System.Reflection;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors.Abilities;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors.Abilities.Behaviors;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppAssets.Scripts.Utils;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.IO;

namespace AbilityChoice;

internal static class OverclockHandler
{
    public const string Enabled = "AbilityChoiceEnabled";

    public static BehaviorMutator GetMutator(TapTowerAbilityBehavior behavior, int tier)
    {
        var ability = behavior.ability;
        var cooldownFrames = ability.abilityModel.cooldownFrames * (1 - ability.abilityModel.CooldownSpeedScale);

        if (behavior.Is(out Overclock o))
        {
            var overclock = o.overclockModel.Duplicate();
            tier = Math.Clamp(tier, 0, overclock.tierBasedDurationMultiplier.Count - 1);

            var uptime = overclock.tierBasedDurationMultiplier[tier] * overclock.lifespanFrames / cooldownFrames;

            overclock.rateModifier = 1 / AbilityChoice.CalcAvgBonus(uptime, 1 / overclock.rateModifier);
            overclock.villageRangeModifier = AbilityChoice.CalcAvgBonus(uptime, overclock.villageRangeModifier);

            return new OverclockModel.OverclockMutator(overclock);
        }

        if (behavior.Is(out TakeAim t))
        {
            var takeAim = t.takeAimModel.Duplicate();
            var uptime = takeAim.lifespanFrames / cooldownFrames;

            takeAim.rangeModifier = AbilityChoice.CalcAvgBonus(uptime, takeAim.rangeModifier);
            takeAim.spreadModifier = 1 / AbilityChoice.CalcAvgBonus(uptime, 1 / takeAim.spreadModifier);

            return new TakeAimModel.TakeAimMutator(takeAim);
        }

        return null;
    }

    public static void ApplyOverclock(Tower from, Tower to, TapTowerAbilityBehavior behavior)
    {

        if (from.Sim.factory.Get<TapTowerAbilityBehavior>().ToArray().Count(overclock => overclock.selectedTower?.Id == to.Id) <= 1)
        {
            to.RemoveMutatorsById(behavior.MutatorId());
        }

        var tier = to.towerModel.tier;
        if (to.towerModel.IsHero())
        {
            tier = (tier - 1) / 4;
        }

        to.AddMutator(GetMutator(behavior, tier), isParagonMutator: from.IsParagonBased(), cantBeAbsorbed: true);
    }

    internal static bool OverclockAbilityChoice(this TapTowerAbilityBehavior overclock) =>
        overclock.model.OverclockAbilityChoice();

    internal static bool OverclockAbilityChoice(this AbilityModel abilityModel) =>
        abilityModel.HasDescendant(out OverclockModel overclockModel) && overclockModel.OverclockAbilityChoice() ||
        abilityModel.HasDescendant(out TakeAimModel takeAimModel) && takeAimModel.OverclockAbilityChoice();

    internal static bool OverclockAbilityChoice(this Model overclockModel) =>
        overclockModel.name.Contains(Enabled);

    private static string MutatorId(this TapTowerAbilityBehavior instance) => instance.GetIl2CppType().Name;

    internal static readonly Dictionary<ObjectId, List<Entity>> Dots = new();

    private static TechBotLink GetFakeTechBotLink(Ability __instance)
    {
        var oc = __instance.entity.GetBehaviorInDependants<TapTowerAbilityBehavior>();

        if (!Dots.TryGetValue(__instance.tower.Id, out var dots))
        {
            dots = Dots[__instance.tower.Id] = new List<Entity>();
        }

        var model = Game.instance.model.GetPowerWithId(TowerType.TechBot).tower.GetDescendant<TechBotLinkModel>();

        return new TechBotLink
        {
            ability = __instance,
            entity = __instance.entity,
            Sim = __instance.Sim,
            selectedTowerId = oc.selectedTower?.Id ?? new ObjectId(),
            linkedTowers = new[] { oc.selectedTower }.ToIl2CppList(),
            model = model,
            techBotLinkModel = model,
            lineDotDisplays = dots
        };
    }

    /// <summary>
    /// Override Overclock application
    /// </summary>
    [HarmonyPatch]
    internal static class TapTowerAbilityBehavior_Activate
    {
        internal static System.Collections.Generic.IEnumerable<MethodInfo> TargetMethods() =>
        [
            AccessTools.Method(typeof(Overclock), nameof(Overclock.Activate)),
            AccessTools.Method(typeof(TakeAim), nameof(TakeAim.Activate)),
        ];

        [HarmonyPostfix]
        internal static void Postfix(TapTowerAbilityBehavior __instance)
        {
            if (!__instance.OverclockAbilityChoice() ||
                __instance.selectedTower == null ||
                __instance.IsBanned(__instance.selectedTower)) return;

            ApplyOverclock(__instance.ability.tower, __instance.selectedTower, __instance);
        }
    }

    /// <summary>
    /// Ultraboost auto stacking, link display
    /// </summary>
    [HarmonyPatch(typeof(Ability), nameof(Ability.Process))]
    internal static class Ability_Process
    {
        [HarmonyPostfix]
        internal static void Postfix(Ability __instance)
        {
            if (__instance.CooldownRemaining == 0 &&
                __instance.abilityModel.OverclockAbilityChoice() &&
                __instance.abilityModel.GetBehavior<OverclockPermanentModel>().Is(out var overclockPermanentModel) &&
                __instance.entity.GetBehaviorInDependants<Overclock>().Is(out var overclock) &&
                overclock.selectedTower != null &&
                !(overclock.selectedTower.GetMutatorById(OverclockPermanentModel.MutatorId).Is(out var timedMutator) &&
                  timedMutator.mutator.Is(out OverclockPermanentModel.OverclockPermanentMutator ultraBoost) &&
                  ultraBoost.stacks >= overclockPermanentModel.maxStacks))
            {
                __instance.Activate();
                __instance.CooldownRemaining = __instance.abilityModel.cooldownFrames;
            }

            if (__instance.abilityModel.OverclockAbilityChoice() &&
                __instance.entity.GetBehaviorInDependants<TapTowerAbilityBehavior>().Is(out var behavior))
            {
                var fakeTechBotLink = GetFakeTechBotLink(__instance);

                if (behavior.selectedTower != null &&
                    TowerSelectionMenu.instance.selectedTower?.Id == __instance.tower.Id)
                {
                    fakeTechBotLink.PlotPointsToLinkedTower(behavior.selectedTower);
                }
                else
                {
                    fakeTechBotLink.RemoveDots();
                }
            }
        }
    }

    /// <summary>
    /// Ensure link display removed
    /// </summary>
    [HarmonyPatch(typeof(Ability), nameof(Ability.OnDestroy))]
    internal static class Ability_OnDestroy
    {
        [HarmonyPrefix]
        internal static void Prefix(Ability __instance)
        {
            if (!__instance.abilityModel.OverclockAbilityChoice()) return;

            var fakeTechBotLink = GetFakeTechBotLink(__instance);
            fakeTechBotLink.RemoveDots();
        }
    }

    /// <summary>
    /// Ensure don't Ultraboost too fast
    /// </summary>
    [HarmonyPatch(typeof(OverclockPermanent), nameof(OverclockPermanent.ApplyToTower))]
    internal static class OverclockPermanent_ApplyToTower
    {
        [HarmonyPrefix]
        internal static bool Prefix(OverclockPermanent __instance)
        {
            if (!__instance.ability.abilityModel.OverclockAbilityChoice()) return true;

            return __instance.ability.CooldownRemaining == 0;
        }
    }

    /// <summary>
    /// For clarity, don't allow re-overclocking the same tower
    /// </summary>
    [HarmonyPatch(typeof(TapTowerAbilityBehavior), nameof(TapTowerAbilityBehavior.GetCustomInputData))]
    internal static class TapTowerAbilityBehavior_GetCustomInputData
    {
        [HarmonyPostfix]
        internal static void Postfix(TapTowerAbilityBehavior __instance, ref Il2CppSystem.Object __result)
        {
            if (!__instance.OverclockAbilityChoice() || !__result.Is(out OverclockCIData data)) return;

            data.validTowerIds.RemoveAll(new Func<ObjectId, bool>(id => id == __instance.selectedTower?.Id));
        }
    }

    /// <summary>
    /// Reapply when a tower gets upgraded because the tier may change the mutator
    /// </summary>
    [HarmonyPatch(typeof(TapTowerAbilityBehavior), nameof(TapTowerAbilityBehavior.OnTowerUpgraded))]
    internal static class TapTowerAbilityBehavior_OnTowerUpgraded
    {
        [HarmonyPostfix]
        internal static void Postfix(TapTowerAbilityBehavior __instance, Tower tower)
        {
            if (!__instance.OverclockAbilityChoice()) return;

            if (tower.Id == __instance.selectedTower?.Id && !__instance.IsBanned(tower))
            {
                ApplyOverclock(__instance.ability.tower, tower, __instance);
            }

            if (tower.Id == __instance.ability.tower.Id &&
                __instance.selectedTower != null &&
                __instance.ability.entity.GetBehavior<OverclockPermanent>() != null &&
                __instance.selectedTower.GetMutatorById(OverclockPermanentModel.MutatorId) == null)
            {
                __instance.ability.ClearCooldown();
                __instance.ability.Activate();
            }
        }
    }

    /// <summary>
    /// Remove mutators from selected tower when destroyed
    /// </summary>
    [HarmonyPatch(typeof(TapTowerAbilityBehavior), nameof(TapTowerAbilityBehavior.OnDestroy))]
    internal static class TapTowerAbilityBehavior_OnDestroy
    {
        [HarmonyPrefix]
        internal static void Prefix(TapTowerAbilityBehavior __instance)
        {
            if (!__instance.OverclockAbilityChoice() || __instance.selectedTower == null) return;

            __instance.selectedTower.RemoveMutatorsById(__instance.MutatorId());
        }
    }

    /// <summary>
    /// Remove from original tower when a new tower is boosted
    /// </summary>
    [HarmonyPatch]
    internal static class TapTowerAbilityBehavior_ApplyCustomInputData
    {
        internal static System.Collections.Generic.IEnumerable<MethodInfo> TargetMethods() =>
        [
            AccessTools.Method(typeof(Overclock), nameof(Overclock.ApplyCustomInputData)),
            AccessTools.Method(typeof(TakeAim), nameof(TakeAim.ApplyCustomInputData)),
        ];

        [HarmonyPrefix]
        internal static void Prefix(TapTowerAbilityBehavior __instance,
            Il2CppAssets.Scripts.Simulation.Towers.Behaviors.Abilities.CustomInputData data)
        {
            if (__instance.OverclockAbilityChoice() &&
                __instance.selectedTower != null &&
                __instance.selectedTower.Id != data.objectIdValue)
            {
                __instance.selectedTower.RemoveMutatorsById(__instance.MutatorId());
            }
        }
    }

    /// <summary>
    /// Can switch Overclocks without cooldown
    /// </summary>
    [HarmonyPatch(typeof(Ability), nameof(Ability.IsReady))]
    internal static class Ability_IsReady
    {
        [HarmonyPrefix]
        internal static void Prefix(Ability __instance, ref bool ignoreCooldown)
        {
            if (!__instance.abilityModel.OverclockAbilityChoice()) return;

            ignoreCooldown = true;
        }
    }

    /// <summary>
    /// Can switch Overclocks without cooldown
    /// </summary>
    [HarmonyPatch(typeof(Ability), nameof(Ability.Activate))]
    internal static class Ability_Activate
    {
        [HarmonyPrefix]
        internal static void Prefix(Ability __instance, ref bool ignoreCooldown)
        {
            if (!__instance.abilityModel.OverclockAbilityChoice()) return;

            ignoreCooldown = true;
        }
    }

    /// <summary>
    /// Hide abilities from bar
    /// </summary>
    [HarmonyPatch(typeof(UnityToSimulation), nameof(UnityToSimulation.GetAllAbilities))]
    internal static class UnityToSimulation_GetAllAbilities
    {
        [HarmonyPostfix]
        internal static void Postfix(ref Il2CppSystem.Collections.Generic.List<AbilityToSimulation> __result)
        {
            __result = __result?.Where(a => a?.model.OverclockAbilityChoice() != true);
        }
    }

    /// <summary>
    /// Late saved tower data after mutators
    /// </summary>
    [HarmonyPatch(typeof(MapSaveLoader), nameof(MapSaveLoader.LoadMapSaveData))]
    internal static class MapSaveLoader_LoadMapSaveData
    {
        [HarmonyPostfix]
        internal static void Postfix(Simulation sim, MapSaveDataModel mapData)
        {
            if (mapData == null) return;
            foreach (var saveData in mapData.placedTowers)
            {
                if (saveData == null) continue;
                var tower = sim.towerManager.GetTowerById(saveData.IdLastSave);

                ModHelper.GetMod<AbilityChoiceMod>().OnTowerLoaded(tower, saveData);
            }
        }
    }

    /// <summary>
    /// Turn the AOE Overclock from paragon into a support zone
    /// </summary>
    [HarmonyPatch(typeof(OverclockModel.OverclockMutator), nameof(OverclockModel.OverclockMutator.Mutate))]
    internal static class OverclockMutator_Mutate
    {
        [HarmonyPostfix]
        internal static void Postfix(OverclockModel.OverclockMutator __instance, Model model)
        {
            if (!model.Is(out TowerModel towerModel)) return;

            if (__instance.overclockModel.OverclockAbilityChoice() && __instance.overclockModel.isParagonMode)
            {
                var overclock = __instance.overclockModel;
                var uptime = overclock.paragonZoneLifespanFrames / (float) overclock.lifespanFrames;

                var rate = 1 / AbilityChoice.CalcAvgBonus(uptime, 1 / overclock.rateModifier);
                var range = AbilityChoice.CalcAvgBonus(uptime, overclock.villageRangeModifier);
                towerModel.AddBehavior(RangeSupportModel.Create(new()
                {
                    name = "Overclock",
                    isUnique = true,
                    multiplier = rate,
                    additive = range,
                    mutatorId = overclock.mutatorId,
                    buffLocsName = overclock.buffLocsName,
                    buffIconName = overclock.buffIconName,
                    showBuffIcon = true,
                    isCustomRadius = true,
                    customRadius = overclock.paragonZoneRange
                }));
            }
        }
    }

    /// <summary>
    /// Make this support zone work as an Overclock if it has that ID
    /// </summary>
    [HarmonyPatch(typeof(RangeSupport.MutatorTower), nameof(RangeSupport.MutatorTower.Mutate))]
    internal static class RangeSupport_Mutate
    {
        [HarmonyPrefix]
        internal static bool Prefix(RangeSupport.MutatorTower __instance, Model baseModel, Model model,
            ref bool __result)
        {
            if (__instance.id != "Overclock") return true;

            var overclock = Game.instance.model.GetTower(TowerType.EngineerMonkey, 0, 4, 0)
                .GetDescendant<OverclockModel>()
                .Duplicate();

            overclock.rateModifier = __instance.multiplier;
            overclock.villageRangeModifier = __instance.additive;
            overclock.buffIconName = __instance.buffIndicator.iconName;
            overclock.buffLocsName = __instance.buffIndicator.buffName;

            var fakeMutator = new OverclockModel.OverclockMutator(overclock)
            {
                resultCache = __instance.resultCache,
                limiters = __instance.limiters,
                mutated = __instance.mutated
            };

            __result = fakeMutator.Mutate(baseModel, model);
            return false;
        }
    }
}
