using System.Collections.Generic;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.GeraldoItems;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;
using Il2CppAssets.Scripts.Simulation.GeraldoItems;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Simulation.SMath;
using Il2CppAssets.Scripts.Simulation.Towers;

namespace AbilityChoice.AbilityChoices.Hero.Geraldo;

public class BottleHotSauce : GeraldoAbilityChioce
{
    public override Dictionary<int, string> Descriptions1 => new()
    {
        {
            8,
            "New Item! Bottle of 'Gerry's Fire' - 2.8 million Bloonville units of pure hot sauce fire! Gives a Monkey, Hero, or minion a fiery attack permanently."
        },
        {
            16,
            "Stack of Old Nails becomes a Nail Mine, Jar of Pickles grants additional bonus damage vs. Fortified Bloons, and Gerry's Fire gains bonus burny damage."
        }
    };

    public override Dictionary<int, string> Descriptions2 => new()
    {
        {
            0,
            "2.8 million Bloonville units. Whoever tastes this special sauce will be spewing fire permanently!"
        },
        {
            16,
            "2.8 million Bloonville units. Whoever tastes this special sauce will be spewing fire permanently! Now with extra burny-ness."
        }
    };

    protected override int CostMult => 5;

    protected override void Apply(GeraldoItemModel geraldoItem)
    {
    }

    public override void Apply(GameModel gameModel)
    {
        base.Apply(gameModel);

        foreach (var towerModel in gameModel.GetTowersWithBaseId(TowerType.HotSauceCreature).AsIEnumerable())
        {
            towerModel.GetDescendant<TowerExpireModel>().rounds = -1;
        }
    }
}

[HarmonyPatch(typeof(BottleHotSauceBehavior), nameof(BottleHotSauceBehavior.Activate))]
internal static class BottleHotSauceBehavior_Activate
{
    [HarmonyPrefix]
    private static bool Prefix(BottleHotSauceBehavior __instance, Vector2 location, int inputId)
    {
        var selectableObject = __instance.sim.Map.GetSelection(location, 20);

        if (selectableObject.Is(out Tower tower) && tower.IsMutatedBy(nameof(BottleHotSauce)))
        {
            tower.RemoveMutatorsById(nameof(BottleHotSauce));

            __instance.sim.towerManager.GetChildTowers(tower).ForEach(t =>
            {
                if (t.towerModel.baseId == TowerType.HotSauceCreature)
                {
                    __instance.sim.towerManager.DestroyTower(t, inputId);
                }
            });
        }

        return true;
    }
}

[HarmonyPatch(typeof(BottleHotSauceBehavior), nameof(BottleHotSauceBehavior.IsPositionValid))]
internal static class BottleHotSauceBehavior_IsPositionValid
{
    public static bool CurrentlyChecking { get; private set; }

    [HarmonyPrefix]
    private static void Prefix(BottleHotSauceBehavior __instance)
    {
        CurrentlyChecking = true;
    }

    [HarmonyPostfix]
    private static void Postfix()
    {
        CurrentlyChecking = false;
    }
}

[HarmonyPatch(typeof(Mutable), nameof(Mutable.IsMutatedBy), typeof(string))]
internal static class Mutable_IsMutatedBy
{
    [HarmonyPrefix]
    private static bool Prefix(string id, ref bool __result)
    {
        if (BottleHotSauceBehavior_IsPositionValid.CurrentlyChecking && id == nameof(BottleHotSauce))
        {
            __result = false;
            return false;
        }

        return true;
    }
}