using System;
using System.Collections.Generic;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.TowerFilters;
using Il2CppAssets.Scripts.Models.Towers.Weapons;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using Il2CppAssets.Scripts.Unity.Bridge;

namespace AbilityChoice.AbilityChoices.Hero.Adora;

public class BloodSacrifice : HeroAbilityChoice
{
    public override string HeroId => TowerType.Adora;

    public override Dictionary<int, string> Descriptions1 => new()
    {
        {
            7,
            "You may pay tribute to Adora, passively spending money in order to give bonus XP to Adora and boost her attack range and rate of fire (and also 3+/X/X Super Monkeys')."
        },
        {
            20,
            "Ball of Light is greatly improved, plus increased rate of fire, range, and cost efficiency for paying tribute."
        }
    };

    public override Dictionary<int, string> Descriptions2 => new()
    {
        {
            7,
            "Adora get more 10% Hero XP for each 3+/X/X Super Monkey (up to 50%). Each one nearby will buff the range and rate of fire of themselves and Adora by 5%, stacking up to 25%."
        },
        {
            20,
            "Ball of Light is greatly improved, plus increased rate of fire, range. Super Monkey buff is now 10% per stack, up to 50%."
        }
    };

    public override void Apply1(TowerModel model)
    {
        var ability = AbilityModel(model);

        var bloodSacrifice = ability.GetBehavior<BloodSacrificeModel>();

        model.AddBehavior(bloodSacrifice);

        model.towerSelectionMenuThemeId = GetId<AdoraTsmTheme>();
    }

    public override bool DoesTick => Mode == 1;

    internal static readonly Dictionary<ObjectId, int> NextSacrificeTimes = new();

    protected override void Tick(int ticks, Simulation sim)
    {
        var bridge = UnityToSimulation.Current;

        var adoras = sim.towerManager.GetTowersByBaseId(TowerType.Adora).ToList()
            .Where(tower => tower.owner == bridge.GetInputId());

        foreach (var adora in adoras)
        {
            var nextSacrifice = NextSacrificeTimes.GetValueOrDefault(adora.Id, 0);

            if (sim.roundTime.elapsed < nextSacrifice || AdoraTsmTheme.SacrificeAmount == 0) continue;

            NextSacrificeTimes[adora.Id] = sim.roundTime.elapsed + 60;

            var bloodSacrifice = adora.entity
                .GetBehavior<Il2CppAssets.Scripts.Simulation.Towers.Behaviors.Abilities.Behaviors.BloodSacrifice>();

            if (bloodSacrifice == null) continue;

            var model = bloodSacrifice.bloodSacrificeModel;

            var amount = (float) Math.Min(AdoraTsmTheme.SacrificeAmount, bridge.GetCash());

            adora.entity.GetBehavior<Il2CppAssets.Scripts.Simulation.Towers.Behaviors.Hero>()
                .AddXp(amount * model.xpMultiplier);

            var factor = model.bonusSacrificeAmount / model.buffDuration;

            var mutator = model.GetMutator((int) Math.Round(amount / factor), "");

            adora.AddMutator(mutator, 66);

            var towersInRange = sim.towerManager.GetTowersInRange(adora.Position, adora.towerModel.range).ToList();

            foreach (var sunGuy in towersInRange.Where(tower =>
                         tower.towerModel.appliedUpgrades.Contains(UpgradeType.SunAvatar)))
            {
                sunGuy.AddMutatorIncludeSubTowers(mutator, 66);
            }

            sim.RemoveCash(amount, Simulation.CashType.Normal, adora.owner, Simulation.CashSource.TowerSold);

            // sim.AddBehavior<ImfLoanCollection>(new ImfLoanCollectionModel("BloodSacrifice", .5f, SacrificeAmount));
        }
    }

    private static TowerFilterModel[] Tier3SuperMonkeys =>
    [
        FilterInBaseTowerIdModel.Create(new() { baseIds = [TowerType.SuperMonkey] }),
        FilterInTowerTiersModel.Create(new()
        {
            path1MinTier = 3, path1MaxTier = 999,
            path2MinTier = 0, path2MaxTier = 999,
            path3MinTier = 0, path3MaxTier = 999,
        })
    ];

    private static TowerFilterModel[] JustAdora =>
    [
        FilterInBaseTowerIdModel.Create(new() { baseIds = [TowerType.Adora] }),
    ];

    private static TowerFilterModel[] AdoraAndSuperMonkeys =>
    [
        FilterInBaseTowerIdModel.Create(new() { baseIds = [TowerType.SuperMonkey, TowerType.Adora] }),
        FilterInTowerTiersModel.Create(new()
        {
            path1MinTier = 3, path1MaxTier = 999,
            path2MinTier = 0, path2MaxTier = 999,
            path3MinTier = 0, path3MaxTier = 999,
        })
    ];

    public override void Apply2(TowerModel model)
    {
        model.AddBehavior(ScaleHeroXpWithTowerCountModel.Create(new()
        {
            name = Name,
            towerIds = TowerType.SuperMonkey,
            tier = 3,
            maxPercent = 50,
            percentPerTower = 5
        }));

        var bloodSacrifice = model.GetDescendant<BloodSacrificeModel>();

        model.AddBehavior(AddBehaviorToTowerSupportModel.Create(new()
        {
            mutationId = Name + "AddBehavior",
            behaviors =
            [
                SupportStackingRangeModel.Create(new()
                {
                    name = Name,
                    mutatorId = Name,
                    rangeMultiplier = model.tier < 20 ? .05f : .1f,

                    buffIconName = bloodSacrifice.buffIconName,
                    buffLocsName = bloodSacrifice.buffLocsName,
                    filters = AdoraAndSuperMonkeys,
                    maxStacks = 5
                })
            ],

            filters = Tier3SuperMonkeys,
        }));
    }

    [HarmonyPatch(typeof(SupportStackingRange.MutatorTower), nameof(SupportStackingRange.MutatorTower.Mutate))]
    internal static class SupportStackingRange_MutatorTower_Mutate
    {
        [HarmonyPostfix]
        internal static void Postfix(SupportStackingRange.MutatorTower __instance, Model model)
        {
            var support = __instance.parent.supportStackingRangeModel;
            if (support.name.Contains(nameof(BloodSacrifice)) && model.Is(out TowerModel towerModel))
            {
                towerModel.GetDescendants<WeaponModel>().ForEach(weapon =>
                {
                    weapon.Rate *= 1 - support.rangeMultiplier;
                });
            }
        }
    }
}
