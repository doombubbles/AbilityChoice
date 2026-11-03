using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.Towers;
namespace AbilityChoice.AbilityChoices.Military.SniperMonkey;

public class SupplyDrop : TowerAbilityChoice
{
    public override string UpgradeId => UpgradeType.SupplyDrop;

    public override string BackUpAbilityName => "Supply Drop 5";

    public override string Description1 =>
        "Supply Drop: toggles either a crate full of cash, or a special ammo crate that grants improved shrapnel that can pop all Bloon types for the next few shots.";

    public override string Description2 =>
        "Bullets can bounce twice as much. Increases Shrapnel popping power.";


    public override void Apply1(TowerModel model)
    {
    }

    public override void Apply2(TowerModel model)
    {
        model.GetWeapon().projectile.pierce *= 2;
    }

    protected override void RemoveAbility(TowerModel model)
    {
        var ammoSwap = model.GetAbilities().First(a => a.displayName.Contains("AmmoSwap"));
        if (Mode2)
        {
            base.RemoveAbility(model);
            model.RemoveBehavior(ammoSwap);
        }
        else
        {
            TechBotify(model);
            TechBotify(model, ammoSwap);
        }
    }
}