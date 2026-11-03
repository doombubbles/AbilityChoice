using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;
using Il2CppAssets.Scripts.Unity;

namespace AbilityChoice.AbilityChoices.Support.SpikeFactory;

public class Spikeageddon : TowerAbilityChoice
{
    public override string UpgradeId => UpgradeType.SpikefactoryParagon;

    public override string Description1 =>
        "A factory of explosive spike Bloon annihilation. Gets a 10s rate boost once per round, and occasionally creates Spikeageddons across the track.";

    public override string Description2 =>
        "A factory of explosive spike Bloon annihilation. Gets a 10s rate boost once per round, and ejects a continuous stream of Spikeageddon mines along the track.";

    private const float Factor = 5;

    public override void Apply1(TowerModel model)
    {
        var ability = AbilityModel(model);
        var spikeageddon = ability.GetBehavior<SpikeaggedonModel>();
        var proj = spikeageddon.projectileModel;
        ability.Cooldown /= Factor;

        proj.GetDescendants<DamageModel>().ForEach(damageModel => damageModel.damage /= Factor);
        proj.GetDescendants<DamageModifierForTagModel>().ForEach(tagModel => tagModel.damageAddative /= Factor);
    }

    public override void Apply2(TowerModel model)
    {
        var ability = AbilityModel(model);
        var spikeageddon = ability.GetBehavior<SpikeaggedonModel>();
        var createProj = spikeageddon.GetDescendant<CreateProjectilesAlongPathWhenCloseModel>();

        var proj = spikeageddon.projectileModel.GetDescendant<ProjectileModel>();

        var spikeStorm = Game.instance.model.GetTower(TowerType.SpikeFactory, 0, 4, 0)
            .GetAbility()
            .GetDescendant<ActivateAttackModel>()
            .attacks[0]
            .Duplicate("Spikestorm");

        var weapon = spikeStorm.weapons[0]!;

        weapon.projectile = proj;
        weapon.Rate = ability.Cooldown / createProj.amountOfProjectilesPerPath;
        weapon.animation = 3;

        model.AddBehavior(spikeStorm);
    }

    protected override void ApplyBoth(TowerModel model)
    {
        var ability = model.GetAbilities().First(a => a.displayName == "Controlled Burst");

        TechBotify(model, ability);
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