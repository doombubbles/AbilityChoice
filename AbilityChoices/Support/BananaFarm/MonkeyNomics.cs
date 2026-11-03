using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;

namespace AbilityChoice.AbilityChoices.Support.BananaFarm;

public class MonkeyNomics : TowerAbilityChoice
{
    public override string UpgradeId => UpgradeType.MonkeyNomics;
    public override string AbilityName => UpgradeId;

    public override string Description1 => "For when you're too big to fail... such that you periodically get $10,000.";
    public override string Description2 =>
        "For when you're too big to fail... such that Bank Capacity becomes ~$100,000";

    public override void Apply1(TowerModel model)
    {
    }

    public override void Apply2(TowerModel model)
    {
        model.GetBehavior<BankModel>().capacity += 87500;
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