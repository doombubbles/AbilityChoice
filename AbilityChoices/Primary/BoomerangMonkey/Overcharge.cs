using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;

namespace AbilityChoice.AbilityChoices.Primary.BoomerangMonkey;

public class Overcharge : TowerAbilityChoice
{
    public override string UpgradeId => UpgradeType.BoomerangmonkeyParagon;

    public override string Description1 =>
        "The Bloons will look upon my Glaives, and they will know fear. Periodically overcharges its own attack speed, with the bonus tapering off over 10 seconds.";

    public override string Description2 =>
        "The Bloons will look upon my Glaives, and they will know fear. Buffs the attack speed of all primary towers, including itself and other paragons, by 25% instead of 11%.";

    private const int Factor = 3;

    public override void Apply1(TowerModel model)
    {
        var ability = AbilityModel(model);

        ability.Cooldown /= Factor;
        ability.RemoveBehavior<CreateSoundOnAbilityModel>();

        var overcharge = ability.GetBehavior<OverchargeModel>();

        overcharge.durationPerStack /= Factor;
    }

    public override void Apply2(TowerModel model)
    {
        foreach (var rateSupportModel in model.GetBehaviors<RateSupportModel>())
        {
            rateSupportModel.appliesToOwningTower = true;
            rateSupportModel.multiplier = .8f;
        }
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