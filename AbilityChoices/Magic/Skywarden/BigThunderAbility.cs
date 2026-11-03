using BTD_Mod_Helper.Api.Enums;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;

namespace AbilityChoice.AbilityChoices.Magic.Skywarden;

public class BigThunderAbility : ThunderCharge
{
    public override string UpgradeId => UpgradeType.Stormwratharchon;

    public override string Description1 =>
        "Spent arrows serve as volatile conduits, lashing out in crackling fury when nearby Bloons are struck by Energy or Plasma. Occasionally weaves a lethal web of lightning between all conduits, obliterating anything caught in the crossfire.";

    public override string Description2 =>
        "Spent arrows serve as volatile conduits, lashing out in crackling fury when nearby Bloons are struck by Energy or Plasma. Weaves a web of lightning between all conduits, damaging anything caught in the crossfire.";

    public override void Apply2(TowerModel model)
    {
        base.Apply2(model);

        var ability = AbilityModel(model);

        var lightningRod = ability.GetDescendant<ActivateLightningRodZapModel>();

        lightningRod.rate *= ability.Cooldown / lightningRod.duration;

        ability.Cooldown /= Factor;

        lightningRod.duration = ability.Cooldown;
    }

    protected override void ApplyBoth(TowerModel model)
    {
        base.ApplyBoth(model);

        var ability = AbilityModel(model);

        ability.GetDescendant<ActivateLightningRodZapModel>().duration /= Factor;
    }
}