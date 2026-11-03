using System.Collections.Generic;
using AbilityChoice.Displays;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Effects;
using Il2CppAssets.Scripts.Models.Map;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Filters;
using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Weapons;
using Il2CppAssets.Scripts.Models.Towers.Weapons.Behaviors;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors.Attack.Behaviors;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using UnityEngine;

namespace AbilityChoice.AbilityChoices.Hero.AdmiralBrickell;

public class MegaMine : HeroAbilityChoice
{
    public override string HeroId => TowerType.AdmiralBrickell;

    public override Dictionary<int, string> Descriptions1 => new()
    {
        {
            10,
            "Deploys Mega Mines to a targeted water location that trigger on MOABs and stun nearby Bloons. Mega Mines last 3 rounds."
        },
        { 13, "Mega Mines are created more frequently." },
        { 18, "Mega Mines are created even more frequently." },
        { 20, "Mega Mines do massively increased damage and stun longer." }
    };

    public override Dictionary<int, string> Descriptions2 => new()
    {
        { 10, "Sea Mine explosions are larger, deal bonus damage to Moabs, and stun Bloons." },
        { 13, "Sea Mine pierce and Moab damage increased." },
        { 18, "Sea Mine stun duration and Moab Damage increased." },
        { 20, "Sea Mines have more pierce, stun for longer, and their Moab damage is massively increased." }
    };

    public override void Apply1(TowerModel model)
    {
        var ability = AbilityModel(model);

        var placeProjectile = ability.GetBehavior<PlaceProjectileAtModel>();
        var megaMine = placeProjectile.projectileModel.Duplicate();
        megaMine.AddBehavior(ArriveAtTargetModel.Create(new()
        {
            timeToTake = 1,
            curveSamples = [0, 1],
            filterCollisionWhileMoving = true,
            stopOnTargetReached = true,
            positionAboveMoabTypes = true
        }));

        var newAttack = Game.instance.model.GetTower(TowerType.WizardMonkey, 1, 2, 0).GetAttackModel("Wall")
            .Duplicate();
        newAttack.name = "AttackModel_MegaMine";

        newAttack.RemoveBehavior<TargetTrackOrDefaultModel>();

        var targetPoint = newAttack.GetBehavior<TargetSelectedPointModel>();
        newAttack.targetProvider = targetPoint;

        targetPoint.displayInvalid = CreatePrefabReference<MegaMineInvalid>();

        targetPoint.lockToInsideTowerRange = false;
        targetPoint.startWithClosestTrackPoint = false;
        targetPoint.projectileToExpireOnTargetChangeModel = null;

        var filters = newAttack.GetBehavior<AttackFilterModel>();
        var offTrack = filters.GetDescendant<FilterOfftrackModel>();
        filters.RemoveChildDependant(offTrack);
        filters.filters = filters.filters.RemoveItem(offTrack);

        var weapon = newAttack.GetDescendant<WeaponModel>();

        weapon.animation = 3;
        weapon.RemoveBehavior<CreateSoundOnProjectileCreatedModel>();

        weapon.RemoveChildDependant(weapon.projectile);
        weapon.projectile = megaMine;
        weapon.AddChildDependant(megaMine);

        weapon.Rate = ability.Cooldown;
        weapon.startInCooldown = false;
        weapon.customStartCooldown = 5;
        weapon.ejectZ = 9999;


        var effectAtTower = placeProjectile.effectAtTowerModel;
        weapon.AddBehavior(EjectEffectModel.Create(new()
        {
            effectModel = effectAtTower, lifespan = effectAtTower.lifespan
        }));


        model.AddBehavior(newAttack);

        model.towerSelectionMenuThemeId = "SelectPointInput";
        model.UpdateTargetProviders();
    }

    public override void Apply2(TowerModel model)
    {
        var ability = AbilityModel(model);

        var megaMine = ability.GetDescendants<ProjectileModel>().ToList()
            .First(projectileModel => projectileModel.id == "MegaMineExplosion");

        var seaMines = model.GetAttackModel("SeaMine");

        var proj = seaMines.GetDescendants<ProjectileModel>().ToList()
            .First(projectileModel => projectileModel.id == "Explosion");

        var damageFactor = ability.Cooldown;
        var pierceFactor = ability.Cooldown / 2;
        var stunFactor = ability.Cooldown / 5;
        var radiusFactor = 10;

        proj.AddBehavior(DamageModifierForTagModel.Create(new()
        {
            tag = BloonTag.Moabs,
            damageAddative = megaMine.GetDamageModel().damage / damageFactor
        }));

        proj.pierce += megaMine.pierce / pierceFactor;

        var slow = megaMine.GetBehavior<SlowModel>().Duplicate();
        slow.Lifespan /= stunFactor;

        proj.radius += megaMine.radius / radiusFactor;

        proj.UpdateCollisionPassList();
        proj.AddBehavior(slow);

        proj.hasDamageModifiers = true;
    }
}

[HarmonyPatch(typeof(SelectPointInput), nameof(SelectPointInput.IsPositionValid))]
internal static class SelectPointInput_IsPositionValid
{
    [HarmonyPostfix]
    private static void Postfix(SelectPointInput __instance, Vector2 cursorPosWorld, ref bool __result)
    {
        var selectTargetCiData = __instance.cii;

        if (selectTargetCiData?.targetInvalidImageId?.AssetGUID == ModContent.GetDisplayGUID<MegaMineInvalid>())
        {
            var point = new Il2CppAssets.Scripts.Simulation.SMath.Vector2(cursorPosWorld);
            __result &= InGame.instance.bridge.Simulation.Map.GetAreaAtPoint(point)?.areaModel?.type == AreaType.water;
        }
    }
}

[HarmonyPatch(typeof(TargetSelectedPoint), nameof(TargetSelectedPoint.Initialise))]
internal static class TargetSelectedPoint_Initialise
{
    [HarmonyPostfix]
    private static void Postfix(TargetSelectedPoint __instance, Entity target)
    {
        if (__instance.targetSelectedPointModel.displayInvalid?.AssetGUID != ModContent.GetDisplayGUID<MegaMineInvalid>())
            return;

        var tower = InGame.instance.bridge.Simulation.towerManager.GetTowers().ToList()
            .First(tower => tower.entity.Id == target.Parent.Id);

        __instance.SetTargetPosition(tower.Position, true);
    }
}