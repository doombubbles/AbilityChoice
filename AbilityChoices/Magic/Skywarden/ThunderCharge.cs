using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;

namespace AbilityChoice.AbilityChoices.Magic.Skywarden;

public class ThunderCharge : TowerAbilityChoice
{
    public override string AbilityName => Name;
    public override string UpgradeId => UpgradeType.Thundersdecree;
    public override string Description1 =>
        "Combo attacks can stun ZOMGs. Occasionally electrically charges nearby Monkeys, granting them faster, ricocheting projectiles that can pop Frozen Bloons.";

    public override string Description2 =>
        "Combo attacks can stun ZOMGs. Permanently electrically charges itself, granting faster, ricocheting projectiles.";

    internal const int Factor = 6;

    public override void Apply1(TowerModel model)
    {
        var ability = AbilityModel(model);

        var projSpeed = ability.GetDescendant<ActivateProjectileSpeedSupportZoneModel>();
        projSpeed.lifespan /= Factor;
        projSpeed.lifespanFrames /= Factor;

        var ricochet = ability.GetDescendant<ActivateRicochetSupportZoneModel>();
        ricochet.lifespan /= Factor;
        ricochet.lifespanFrames /= Factor;

        var damageBypass = ability.GetDescendant<ActivateDamageBypassSupportZoneModel>();
        damageBypass.lifespan /= Factor;
        damageBypass.lifespanFrames /= Factor;

        ability.Cooldown /= Factor;

        ability.RemoveBehavior<CreateSoundOnAbilityModel>();
    }

    public override void Apply2(TowerModel model)
    {
        var ability = AbilityModel(model);
        ability.isHidden = true;

        var projSpeed = ability.GetDescendant<ActivateProjectileSpeedSupportZoneModel>();
        var ricochet = ability.GetDescendant<ActivateRicochetSupportZoneModel>();

        ability.RemoveBehavior<ActivateProjectileSpeedSupportZoneModel>();
        ability.RemoveBehavior<ActivateRicochetSupportZoneModel>();
        ability.RemoveBehavior<ActivateDamageBypassSupportZoneModel>();
        ability.RemoveBehavior<CreateSoundOnAbilityModel>();
        ability.RemoveBehavior<CreateEffectOnAbilityModel>();

        ability.animation = -1;
        ability.startOffCooldown = true;

        var attack = model.GetAttackModel();
        var projectile = attack.GetDescendant<ProjectileModel>();


        var arriveAtTargetModel = projectile.GetBehavior<ArriveAtTargetModel>();
        arriveAtTargetModel.altSpeed += projSpeed.speedIncrease;

        projectile.maxPierce = 3;
        projectile.RemoveBehavior<ArriveAtTargetModel>();
        projectile.AddBehavior(TrackTargetModel.Create(new()
        {
            distance = ricochet.distance,
            maxSeekAngle = 999,
            ignoreSeekAngle = true,
            overrideRotation = true,
            useLifetimeAsDistance = true,
            turnRate = 360,
            constantlyAquireNewTarget = true
        }));
        projectile.AddBehavior(TravelStraitModel.Create(new()
        {
            speed = arriveAtTargetModel.altSpeed,
            lifespan = .16f
        }));
        projectile.AddBehavior(RetargetOnContactModel.Create(new()
        {
            targetType = ricochet.targetType,
            delay = .01f,
            distance = ricochet.distance,
            minDistance = ricochet.minDistance,
            maxBounces = ricochet.maxBounces,
            expireIfNoTargetFound = ricochet.expireIfNoTargetFound
        }));

        var createProj = projectile.GetBehavior<CreateDistanceProjectileOnExhaustFractionModel>();
        var createEffect = projectile.GetBehavior<CreateEffectOnExhaustFractionModel>();

        createEffect.effectModel.useTransformPosition = false;

        projectile.AddBehavior(CreateProjectileOnContactModel.Create(new()
        {
            projectile = createProj.projectile,
            emission = createProj.emission,
        }));
        projectile.RemoveBehavior(createProj);

        projectile.AddBehavior(CreateEffectOnContactModel.Create(new()
        {
            effectModel = createEffect.effectModel,
        }));
        projectile.RemoveBehavior(createEffect);
    }

    protected override void RemoveAbility(TowerModel model)
    {
        TechBotify(model);
    }

}