using System.Collections.Generic;
using BTD_Mod_Helper.Api.Helpers;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Weapons;

namespace AbilityChoice.AbilityChoices.Hero.Sauda;

public class SwordCharge : HeroAbilityChoice
{
    public override string HeroId => TowerType.Sauda;

    public override Dictionary<int, string> Descriptions1 => new()
    {
        { 10, "Sauda sends mirages of herself along the track, devastating Bloons as she goes." },
        { 14, "Sauda attacks even faster, and Sword Charge mirages are sent twice as often!" },
        { 20, "Sword Charge and Leaping Sword power greatly increased." }
    };

    public override Dictionary<int, string> Descriptions2 => new()
    {
        { 10, "Sauda sends mirages of herself forward towards Bloons." },
        { 14, "Sauda attacks even faster, and Sword Charge mirages are sent twice as often!" },
        { 20, "Sword Charge and Leaping Sword power greatly increased." }
    };

    public const float Factor = 5;
    public override void Apply1(TowerModel model)
    {
        var ability = AbilityModel(model);
        var swordCharge = ability.GetBehavior<SwordChargeModel>();

        ability.RemoveBehavior<CreateSoundOnAbilityModel>();
        ability.RemoveBehavior<CreateEffectOnAbilityModel>();
        swordCharge.landingSound = null;
        swordCharge.spawnSound = null;
        swordCharge.effectAtEndModel = null;

        ability.Cooldown /= Factor;

        var factor = swordCharge.iterations / Factor;

        swordCharge.GetDescendants<DamageModel>().ForEach(damage => damage.damage *= factor);
        swordCharge.GetDescendants<DamageModifierForTagModel>().ForEach(modifier => modifier.damageAddative *= factor);
        swordCharge.GetDescendants<SaudaAfflictionDamageModifierModel>().ForEach(modifier =>
        {
            modifier.lv7NonMoabBonus *= factor;
            modifier.lv7MoabBonus *= factor;
            modifier.lv11NonMoabBonus *= factor;
            modifier.lv11MoabBonus *= factor;
            modifier.lv19NonMoabBonus *= factor;
            modifier.lv19MoabBonus *= factor;
        });

        swordCharge.iterations = 1;
    }
    public override void Apply2(TowerModel model)
    {
        var ability = AbilityModel(model);
        var swordCharge = ability.GetBehavior<SwordChargeModel>();

        var proj = swordCharge.projectileModel;

        proj.AddBehavior(TravelStraitModel.Create(new()
        {
            lifespan = 1,
            speed = proj.GetBehavior<TravelAlongPathModel>().speedFrames * 60
        }));
        proj.AddBehavior(TrackTargetModel.Create(new()
        {
            distance = 999,
            trackNewTargets = true,
            maxSeekAngle = 360,
            turnRate = 1000,
            useLifetimeAsDistance = true
        }));

        proj.RemoveBehavior<AgeModel>();
        proj.RemoveBehavior<TravelAlongPathModel>();

        swordCharge.GetDescendants<DamageModel>().ForEach(damage => damage.damage /= Factor);
        swordCharge.GetDescendants<DamageModifierForTagModel>().ForEach(modifier => modifier.damageAddative /= Factor);
        swordCharge.GetDescendants<SaudaAfflictionDamageModifierModel>().ForEach(modifier =>
        {
            modifier.lv7NonMoabBonus /= Factor;
            modifier.lv7MoabBonus /= Factor;
            modifier.lv11NonMoabBonus /= Factor;
            modifier.lv11MoabBonus /= Factor;
            modifier.lv19NonMoabBonus /= Factor;
            modifier.lv19MoabBonus /= Factor;
        });

        var rate = ability.Cooldown / Factor / swordCharge.iterations;

        model.AddBehavior(AttackModel.Create(new()
        {
            name = Name,
            range = 9999,
            addsToSharedGrid = false,
            CanSeeCamo = true,
            behaviors =
            [
                RotateToTargetModel.Create(new()
                {
                    onlyRotateDuringThrow = true,
                    rotateTower = true
                })
            ],
            weapon = WeaponModel.Create(new()
            {
                animation = -1,
                rate = rate,
                projectile = proj
            })
        }));
    }
    protected override void RemoveAbility(TowerModel model)
    {
        if (Mode2)
        {
            base.RemoveAbility(model);
        }
        else
        {
            TechBotify(model);
        }
    }
}

[HarmonyPatch(typeof(Il2CppAssets.Scripts.Simulation.Towers.Behaviors.Abilities.Behaviors.SwordCharge),
    nameof(Il2CppAssets.Scripts.Simulation.Towers.Behaviors.Abilities.Behaviors.SwordCharge.HideTower))]
internal static class SwordCharge_HideTower
{
    [HarmonyPrefix]
    private static bool Prefix(
        Il2CppAssets.Scripts.Simulation.Towers.Behaviors.Abilities.Behaviors.SwordCharge __instance)
    {
        return __instance.swordChargeModel.effectAtEndModel != null;
    }
}