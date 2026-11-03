using System.Collections.Generic;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;

namespace AbilityChoice.AbilityChoices.Hero.Adora;

public class BallOfLight : HeroAbilityChoice
{
    public override string AbilityName => "Ball of Light";

    public override string HeroId => TowerType.Adora;

    public override Dictionary<int, string> Descriptions1 => new()
    {
        { 10, "Brings forth a powerful ball of energy to strike down the Bloons." }
    };

    public override void Apply1(TowerModel model)
    {
        var ability = AbilityModel(model);

        var createTower = ability.GetBehavior<AbilityCreateTowerModel>();
        var tower = createTower.towerModel;

        var uptime = tower.GetBehavior<TowerExpireModel>().Lifespan / ability.Cooldown;

        tower.RemoveBehavior<TowerExpireModel>();

        foreach (var weaponModel in tower.GetWeapons())
        {
            weaponModel.Rate /= uptime;
        }

        model.AddBehavior(TowerCreateTowerModel.Create(new()
        {
            name = Name + model.tier, towerModel = tower, isAirBasedTower = true
        }));

        // keep around for AdoraSunGodTransformation
        model.AddBehavior(createTower);
    }
}

[HarmonyPatch(typeof(AdoraSunGodTransformationModel.Mutator), nameof(AdoraSunGodTransformationModel.Mutator.Mutate))]
internal static class AdoraSunGodTransformationModel_Mutator_Mutate
{
    [HarmonyPostfix]
    private static void Postfix(Model model)
    {
        var newBall = model.GetDescendant<AbilityCreateTowerModel>().towerModel;
        model.GetDescendants<TowerCreateTowerModel>().ForEach(createTower =>
        {
            createTower.towerModel = newBall;
            createTower.name += "SunGod";
        });
    }
}

[HarmonyPatch(typeof(AdoraSunGodTransformationModel.DarkMutator),
    nameof(AdoraSunGodTransformationModel.DarkMutator.Mutate))]
internal static class AdoraSunGodTransformationModel_DarkMutator_Mutate
{
    [HarmonyPostfix]
    private static void Postfix(Model model)
    {
        var newBall = model.GetDescendant<AbilityCreateTowerModel>().towerModel;
        model.GetDescendants<TowerCreateTowerModel>().ForEach(createTower =>
        {
            createTower.towerModel = newBall;
            createTower.name += "Dark";
        });
    }
}