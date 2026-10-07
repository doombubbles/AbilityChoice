using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;
namespace AbilityChoice.AbilityChoices.Military.SniperMonkey;

public class EliteSupplyDrop : SupplyDrop
{
    public override string UpgradeId => UpgradeType.EliteSniper;

    public override string AbilityName => "Supply Drop 5";

    public override string Description1 =>
        "Occasionally drops more cash. Grants Elite targeting prio and faster reload to all snipers.";

    public override string Description2 =>
        "Bullets bounce even more, and can bounce back to the same target. Grants Elite targeting prio and faster reload to all snipers.";

    public override void Apply2(TowerModel model)
    {
        base.Apply2(model);
        var projectileModel = model.GetWeapon().projectile;
        if (!AbilityChoiceMod.MoreBalanced)
        {
            projectileModel.pierce *= 2;
        }

        var delay = projectileModel.GetBehavior<RetargetOnContactModel>().delay;
        projectileModel.AddBehavior(ClearHitBloonsModel.Create(new()
        {
            name = "ClearHitBloonsModel_", interval = delay
        }));
    }
}