using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;

namespace AbilityChoice.AbilityChoices.Support.BananaFarm;

public class IMFLoan : TowerAbilityChoice
{
    public override string UpgradeId => UpgradeType.IMFLoan;
    public override string AbilityName => UpgradeId;

    public override string Description1 => "Periodically gives you $10,000 that has to be paid back over time.";
    public override string Description2 => "Bank capacity is increased to ~$20,000";

    public override void Apply1(TowerModel model)
    {
    }

    public override void Apply2(TowerModel model)
    {
        model.GetBehavior<BankModel>().capacity += 7500;
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