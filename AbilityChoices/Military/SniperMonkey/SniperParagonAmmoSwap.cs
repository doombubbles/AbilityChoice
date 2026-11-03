using System.Collections.Generic;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Weapons.Behaviors;
using Il2CppAssets.Scripts.Simulation.Bloons;
using Il2CppAssets.Scripts.Simulation.Bloons.Behaviors;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors.Abilities;
using Il2CppAssets.Scripts.Simulation.Towers.Projectiles;
using Il2CppAssets.Scripts.Simulation.Towers.Weapons.Behaviors;

namespace AbilityChoice.AbilityChoices.Military.SniperMonkey;

public class SniperParagonAmmoSwap : TowerAbilityChoice
{
    public override string UpgradeId => UpgradeType.SnipermonkeyParagon;

    public override string AbilityName => "SniperParaogonAmmoSwap";

    public override string Description1 =>
        "Simia ex umbris. Automatically calls down ammo caches. Slowly charges up energy for a kill shot on the strongest Bloon (can occasionally negate a Boss Skull).";

    public override string Description2 =>
        "Simia ex umbris. Shrapnel targets First with increased lifespan, pierce, and explosion count. Next attack is 4x faster if the main shot kills the target.";

    internal const int Factor = 3;
    internal const int Factor2 = 180 / 15;
    internal const int Factor3 = 4;

    public override void Apply1(TowerModel model)
    {
        var ability = AbilityModel(model);
        ability.Cooldown /= Factor;
        ability.maxActivationsPerRound = -1;
        ability.RemoveBehavior<CreateSoundOnAbilityModel>();

        var ammoSwap = ability.GetBehavior<SniperAmmoSwapBehaviorModel>();
        ammoSwap.AbilityDuration /= Factor;
        ammoSwap.Initialdelay /= Factor;

        ability.GetDescendant<AgeModel>().Lifespan /= Factor;

        var singleOut = model.GetAbilities().First(a => a.displayName.Contains("SingleOut"));

        var singleOutAttack = singleOut.GetDescendant<AttackModel>();
        model.AddBehavior(singleOutAttack);

        var behavior = singleOut.GetDescendant<SniperParagonAbilityWeaponBehaviorModel>();
        behavior.animationStateWhileCharging = -1;
        behavior.chargeSound = null;
        behavior.baseShootingDamage /= Factor2;
        behavior.maximumShotingDelay *= Factor2;
        behavior.maximumShootingDelayFrames *= Factor2;

        var line = singleOut.GetDescendant<LineEffectModel>();
        line.lineDisplayPath = null;

        singleOut.isHidden = true;
        singleOut.RemoveBehavior<ActivateAttackUntillXAttacksModel>();
        singleOut.RemoveBehavior<CreateSoundOnAbilityModel>();
    }

    public override void Apply2(TowerModel model)
    {
        var attack = model.GetAttackModel();

        var shrapnel = attack.GetDescendant<EmitOnDamageModel>().projectile;

        shrapnel.maxPierce = 0;
        shrapnel.pierce *= Factor3;

        var tracking = shrapnel.GetBehavior<TrackTargetAfterDelayModel>();
        tracking.Lifespan *= Factor3;
        tracking.targetType = TargetType.first;
    }

    protected override void RemoveAbility(TowerModel model)
    {
        if (Mode2)
        {
            base.RemoveAbility(model);
            var singleOut = model.GetAbilities().First(a => a.displayName.Contains("SingleOut"));
            model.RemoveBehavior(singleOut);
        }
        else
        {
            TechBotify(model);
        }
    }
}


/// <summary>
/// Only let the Sniper Paragon Red Ability Choice block a Boss Skull based on the cooldown of the now hidden Single Out ability
/// </summary>
[HarmonyPatch(typeof(HealthPercentTrigger), nameof(HealthPercentTrigger.Trigger))]
internal static class HealthPercentTrigger_Trigger
{
    [HarmonyPrefix]
    internal static void Prefix(HealthPercentTrigger __instance)
    {
        var sim = __instance.Sim;
        if (!sim.blockNextBossSkullOverrideReset ||
            sim.blockNextBossSkull && !__instance.hasIgnoredTrigger ||
            sim.towerBlockingNextBossSkull is not { } tower ||
            !ModContent.GetInstance<SniperParagonAmmoSwap>().Enabled) return;

        var singleOut = tower.entity.GetBehaviorsInDependants<Ability>()
            .FirstOrDefault(a => a.abilityModel.displayName.Contains("SingleOut"));
        if (singleOut == null) return;

        if (singleOut.CooldownRemaining > 0)
        {
            sim.blockNextBossSkullOverrideReset = false;
            sim.towerBlockingNextBossSkull = null;
        }
        else
        {
            singleOut.CooldownRemaining = singleOut.abilityModel.cooldownFrames;
        }
    }
}

/// <summary>
/// For Sniper Paragon, only display when there's actually a valid target
/// </summary>
[HarmonyPatch(typeof(LineEffect), nameof(LineEffect.Process))]
internal static class LineEffect_Process
{
    [HarmonyPrefix]
    internal static bool Prefix(LineEffect __instance)
    {
        if (!__instance.lineEffectModel.pauseEffectOnAttackPaused ||
            __instance.weapon.attack.frameTarget.IsValid) return true;

        __instance.lineEffect?.Destroy();
        __instance.lineEffect = null;
        __instance.effectAtStart?.Destroy();
        __instance.effectAtStart = null;
        __instance.effectAtEnd?.Destroy();
        __instance.effectAtEnd = null;

        return false;
    }
}

/// <summary>
/// Sniper Paragon shoots 4x faster for its next attack after killing a target
/// </summary>
[HarmonyPatch(typeof(BloonManager), nameof(BloonManager.BloonDestroy))]
internal static class BloonManager_BloonDestroy
{
    private static readonly Dictionary<nint, int> BoostedFrom = new();

    [HarmonyPostfix]
    internal static void Postfix(Projectile projectile)
    {
        if (projectile?.projectileModel?.id != "Projectile" ||
            ModContent.GetInstance<SniperParagonAmmoSwap>().Mode != 2 ||
            projectile.EmittedBy is not { towerModel: { isParagon: true, baseId: "SniperMonkey" } } ||
            projectile.Weapon is not { } weapon) return;

        if (BoostedFrom.TryGetValue(weapon.Pointer, out var boosted) && boosted == weapon.lastEmission) return;

        weapon.lastEmission -= (int) (weapon.rateFrames * (1 - 1f / SniperParagonAmmoSwap.Factor3));
        BoostedFrom[weapon.Pointer] = weapon.lastEmission;
    }
}