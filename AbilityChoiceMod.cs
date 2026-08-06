using System;
using System.Collections.Generic;
using AbilityChoice.AbilityChoices.Hero.Adora;
using AbilityChoice.AbilityChoices.Primary.IceMonkey;
using AbilityChoice.Patches;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Artifacts;
using Il2CppAssets.Scripts.Models.Artifacts.Behaviors;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Simulation.Bloons;
using Il2CppAssets.Scripts.Simulation.Corvus.TowerManager;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.Legends;
using Il2CppAssets.Scripts.Unity.UI_New.Popups;
using Il2CppSystem.IO;
using UnityEngine;
using UnityEngine.EventSystems;

[assembly: MelonInfo(typeof(AbilityChoiceMod), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6-Epic")]

namespace AbilityChoice;

public class AbilityChoiceMod : BloonsTD6Mod
{
    private static readonly ModSettingBool ApplyAfterChallengeRules = new(true)
    {
        icon = VanillaSprites.ChallengeRulesBtn,
        description =
            "Makes Ability Choices calculate and apply their tower modification AFTER Challenge Rules change the cooldowns of towers rather than before"
    };

    private static readonly ModSettingButton SetAllToOff = new()
    {
        action = () => ModContent.GetContent<TowerAbilityChoice>().ForEach(choice => choice.Mode = 0),
        icon = TowerAbilityChoice.IconForMode(0),
        description = "Sets all abilities back to their default vanilla affects.",
        buttonSprite = VanillaSprites.YellowBtnLong,
        buttonText = "Set All"
    };

    private static readonly ModSettingButton SetAllToMode1 = new()
    {
        action = () => ModContent.GetContent<TowerAbilityChoice>().ForEach(choice => choice.Mode = 1),
        icon = TowerAbilityChoice.IconForMode(1),
        description = "For most towers, this is a permanent but weaker version of the ability.",
        buttonSprite = VanillaSprites.RedBtnLong,
        buttonText = "Set All"
    };

    private static readonly ModSettingButton SetAllToMode2 = new()
    {
        action = () => ModContent.GetContent<TowerAbilityChoice>().ForEach(choice => choice.Mode = 2),
        icon = TowerAbilityChoice.IconForMode(2),
        description = "For the towers that have it, a different alternate affect to the ability. Towers that don't" +
                      "have a second mode will be set to Mode 1.",
        buttonSprite = VanillaSprites.BlueBtnLong,
        buttonText = "Set All"
    };

    public static readonly ModSettingBool MoreBalanced = new(true)
    {
        description = "Nerfs a few effects that were unbalanced in their initial implementation"
    };

    public static readonly ModSettingInt MagusPerfectusSwitchThreshold = new(0)
    {
        description = "Controls the threshold where the Magus Perfectus will switch to its mana generating attack " +
                      "when it drops below this amount of mana.",
        min = 0,
        max = 100000,
        icon = VanillaSprites.MagusPerfectusUpgradeIcon
    };

    // public static readonly string DontShowAbilityKeyword = " DONT SHOW";

    public override bool UsesArtifactDependants => true;

    public static MelonPreferences_Category AbilityChoiceSettings { get; private set; }

    public override void OnMainMenu()
    {
        AbilityChoiceSettings.SaveToFile(false);
    }

    public override void OnTitleScreen()
    {
        foreach (var abilityChoice in ModContent.GetContent<AbilityChoice>())
        {
            abilityChoice.CacheAffectedIds();
        }
    }

    public override void OnUpdate()
    {
        if (UnityToSimulation.Current != null && ModContent.GetInstance<EclipseOfFimbulwinter>().Mode == 1 &&
            UnityToSimulation.Current.GetAllTowers()
                .Any(tower => tower?.Def != null && tower.Def.baseId == TowerType.IceMonkey && tower.Def.isParagon) &&
            UnityToSimulation.Current.GetBossBloon() != null &&
            UnityToSimulation.Current.GetCurrentBossSkull() == 0)
        {
            Simulation.Current.blockNextBossSkull = true;
        }

        if (Input.GetMouseButtonUp((int) PointerEventData.InputButton.Right))
        {
            var raycastResults = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
            var data = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            EventSystem.current.RaycastAll(data, raycastResults);

            foreach (var raycastResult in raycastResults)
            {
                if (raycastResult.gameObject.HasComponent(out TowerAbilityChoiceInfo towerInfo))
                {
                    towerInfo.abilityChoice.Toggle();
                    towerInfo.UpdateIcon();
                    towerInfo.upgradeDetails.OnPointerExit(data);
                    towerInfo.upgradeDetails.OnPointerEnter(data);
                }
                else if (raycastResult.gameObject.HasComponent(out HeroAbilityChoiceInfo heroInfo))
                {
                    heroInfo.abilityChoice.Toggle();
                    heroInfo.UpdateIcon();
                    heroInfo.UpdateDescriptions();
                }
                else continue;

                if (InGame.instance != null && !PopupScreen.instance.IsPopupActive())
                {
                    PopupScreen.instance.ShowOkPopup(
                        "In order for this to take effect, you'll need to exit to the main menu and come back to the game.");
                }
                else
                {
                    MenuManager.instance.buttonClickSound.Play("ClickSounds");
                }
            }
        }
    }

    public override void OnApplicationStart()
    {
        AbilityChoiceSettings = MelonPreferences.CreateCategory("AbilityChoiceSettings");
    }

    public static void ApplyAbilityChoices(GameModel gameModel)
    {
        GetRelevantArtifactEffects(out var boosts, out var towerSetChanges, out var abilityStackings);

        if (InGameData.CurrentGame?.rogueData != null && RogueLegendsManager.instance?.RogueSaveData != null)
        {
            ProcessBoosts(gameModel, boosts, towerSetChanges, abilityStackings, false);
        }

        foreach (var abilityChoice in ModContent.GetContent<AbilityChoice>().Where(choice => choice.Enabled))
        {
            try
            {
                abilityChoice.Apply(gameModel);
            }
            catch (Exception e)
            {
                ModHelper.Error<AbilityChoiceMod>(e);
            }
        }

        if (InGameData.CurrentGame?.rogueData != null && RogueLegendsManager.instance?.RogueSaveData != null)
        {
            ProcessBoosts(gameModel, boosts, towerSetChanges, abilityStackings, true);
        }
    }

    private static bool UseLateOnNewGameModel => ApplyAfterChallengeRules && !InGameData.CurrentGame.IsLegend();

    public override void OnNewGameModel(GameModel gameModel)
    {
        if (!UseLateOnNewGameModel)
        {
            ApplyAbilityChoices(gameModel);
        }
    }

    // TODO switch to LateOnNewGameModel after Mod Helper update
    [HarmonyPatch(typeof(CosmeticHelper), nameof(CosmeticHelper.ApplyCosmeticsToGameModel))]
    internal static class CosmeticHelper_ApplyCosmeticsToGameModel
    {
        [HarmonyPostfix]
        internal static void Postfix()
        {
            var gameModel = CosmeticHelper.rootGameModel;
            if (UseLateOnNewGameModel)
            {
                ApplyAbilityChoices(gameModel);
            }
        }
    }

    private static void GetRelevantArtifactEffects(out List<BoostArtifactModel> boosts,
        out List<CountAllCategoriesBehaviorModel> towerSetChanges,
        out List<AbilityStackingBehaviorModel> abilityStackings)
    {
        boosts = [];
        towerSetChanges = [];
        abilityStackings = [];

        if (InGameData.CurrentGame?.rogueData == null || RogueLegendsManager.instance?.RogueSaveData == null) return;

        foreach (var artifact in RogueLegendsManager.instance.RogueSaveData.artifactsInventory)
        {
            var artifactModel = GameData.Instance.artifactsData
                .GetArtifactData(artifact.artifactName)
                .ArtifactModel();
            if (artifactModel.Is(out BoostArtifactModel boostArtifactModel))
            {
                boosts.Add(boostArtifactModel);
            }
            boosts.AddRange(artifactModel.GetDescendants<BoostArtifactModel>().ToArray());
            towerSetChanges.AddRange(artifactModel.GetDescendants<CountAllCategoriesBehaviorModel>().ToArray());
            abilityStackings.AddRange(artifactModel.GetDescendants<AbilityStackingBehaviorModel>().ToArray());
        }
    }

    public static void ProcessBoosts(GameModel gameModel, List<BoostArtifactModel> boosts,
        List<CountAllCategoriesBehaviorModel> towerSetChanges, List<AbilityStackingBehaviorModel> abilityStackings,
        bool unapply)
    {
        foreach (var boost in boosts)
        {
            var cooldownBehaviors = boost.GetBehaviors<CooldownBoostBehaviorModel>();
            if (!cooldownBehaviors.Any()) continue;

            foreach (var tower in gameModel.towers)
            {
                if (boost.towerTypes != null &&
                    boost.towerTypes.Any() &&
                    boost.towerTypes.Contains(tower.baseId) == boost.inverseSets) continue;
                if (!tower.CheckTiers(boost.tiers, boost.tiersMustBeEqual, boost.inverseTiers)) continue;

                var towerSet = tower.towerSet;
                foreach (var towerSetChange in towerSetChanges.Where(towerSetChange =>
                             towerSetChange.towerSetList.ContainsFlags(tower.towerSet)))
                {
                    foreach (var set in towerSetChange.alsoCountsAsList)
                    {
                        tower.towerSet |= set;
                    }
                }
                var result = tower.CheckSet(boost.towerSet, boost.inverseSets);
                tower.towerSet = towerSet;

                if (!result) continue;

                tower.GetDescendants<AbilityModel>().ForEach(ability =>
                {
                    foreach (var cooldownBehavior in cooldownBehaviors)
                    {
                        switch (cooldownBehavior.multiplier < 1, unapply)
                        {
                            case (false, false):
                                ability.Cooldown /= cooldownBehavior.multiplier;
                                return;
                            case (false, true):
                                ability.Cooldown *= cooldownBehavior.multiplier;
                                return;
                            case (true, false):
                                ability.Cooldown *= 2 - cooldownBehavior.multiplier;
                                return;
                            case (true, true):
                                ability.Cooldown /= 2 - cooldownBehavior.multiplier;
                                return;
                        }
                    }
                });
            }
        }

        foreach (var abilityStacking in abilityStackings)
        {
            foreach (var tower in gameModel.towers)
            {
                if (abilityStacking.towerTypes != null &&
                    abilityStacking.towerTypes.Any() &&
                    abilityStacking.towerTypes.Contains(tower.baseId) == abilityStacking.inverseSets) continue;

                var towerSet = tower.towerSet;
                foreach (var towerSetChange in towerSetChanges.Where(towerSetChange =>
                             towerSetChange.towerSetList.ContainsFlags(tower.towerSet)))
                {
                    foreach (var set in towerSetChange.alsoCountsAsList)
                    {
                        tower.towerSet |= set;
                    }
                }
                var result = tower.CheckSet(abilityStacking.towerSet, abilityStacking.inverseSets);
                tower.towerSet = towerSet;

                if (!result) continue;

                tower.GetDescendants<AbilityModel>().ForEach(ability =>
                {
                    var amount = 1 + abilityStacking.stackCount * .15f;
                    if (unapply)
                    {
                        ability.Cooldown *= amount;
                    }
                    else
                    {
                        ability.Cooldown /= amount;
                    }
                });

            }

        }
    }

    public override void OnRoundStart()
    {
        Syphon_OnBloonCreate.counter = 0;
    }

    public override void OnGameObjectsReset()
    {
        OverclockHandler.Dots.Clear();
        BloodSacrifice.NextSacrificeTimes.Clear();
        CorvusHandler.SpellsToReactivate.Clear();
    }

    public override bool PreBloonLeaked(Bloon bloon)
    {
        var me = InGame.Bridge?.GetInputId() ?? -1;
        if (bloon.Sim.GetCorvusManagerExists(me))
        {
            var corvus = bloon.Sim.GetCorvusManager(me);

            if (corvus.CanSpellBeCast(CorvusSpellType.SoulBarrier) &&
                CorvusAbilityChoice.EnabledForSpell(CorvusSpellType.SoulBarrier))
            {
                corvus.CastSpell(CorvusSpellType.SoulBarrier);
            }
        }

        return true;
    }

    public override void OnTowerSaved(Tower tower, TowerSaveDataModel saveData)
    {
        if (BloodSacrifice.NextSacrificeTimes.TryGetValue(tower.Id, out var time))
        {
            saveData.metaData["AbilityChoice-NextSacrificeTime"] = time.ToString();
        }

        if (tower.GetMutatorById("Overclock").Is(out var mutator) &&
            mutator.mutator.Is(out OverclockModel.OverclockMutator overclockMutator))
        {
            var model = overclockMutator.overclockModel;

            if (model.OverclockAbilityChoice())
            {
                saveData.metaData["OverclockAbilityChoice-rateModifier"] = model.rateModifier.ToString();
                saveData.metaData["OverclockAbilityChoice-villageRangeModifier"] =
                    model.villageRangeModifier.ToString();
            }
        }

        if (tower.GetMutatorById("TakeAim").Is(out mutator) &&
            mutator.mutator.Is(out TakeAimModel.TakeAimMutator takeAimMutator))
        {
            var model = takeAimMutator.takeAimModel;

            if (model.OverclockAbilityChoice())
            {
                saveData.metaData["TakeAimAbilityChoice-rangeModifier"] = model.rangeModifier.ToString();
                saveData.metaData["TakeAimAbilityChoice-spreadModifier"] =
                    model.spreadModifier.ToString();
            }
        }
    }

    public override void OnTowerLoaded(Tower tower, TowerSaveDataModel saveData)
    {
        if (saveData.metaData.TryGetValue("AbilityChoice-NextSacrificeTime", out var time))
        {
            BloodSacrifice.NextSacrificeTimes[tower.Id] = int.Parse(time);
        }

        if (tower.GetMutatorById("Overclock").Is(out var mutator))
        {
            var overclockMutator = mutator.mutator.Cast<OverclockModel.OverclockMutator>();
            var model = overclockMutator.overclockModel;

            if (model.OverclockAbilityChoice())
            {
                mutator.totalDuration = -1;
                mutator.removeAt = -1;

                if (saveData.metaData.TryGetValue("OverclockAbilityChoice-rateModifier", out var rm) &&
                    float.TryParse(rm, out var rateModifier))
                {
                    model.rateModifier = rateModifier;
                }

                if (saveData.metaData.TryGetValue("OverclockAbilityChoice-villageRangeModifier", out var vrm) &&
                    float.TryParse(vrm, out var villageRangeModifier))
                {
                    model.villageRangeModifier = villageRangeModifier;
                }

                overclockMutator.resultCache.Clear();
                tower.ApplyMutation();
            }
        }

        if (tower.GetMutatorById("TakeAim").Is(out mutator))
        {
            var takeAimMutator = mutator.mutator.Cast<TakeAimModel.TakeAimMutator>();
            var model = takeAimMutator.takeAimModel;

            if (model.OverclockAbilityChoice())
            {
                mutator.totalDuration = -1;
                mutator.removeAt = -1;

                if (saveData.metaData.TryGetValue("TakeAimAbilityChoice-rangeModifier", out var rm) &&
                    float.TryParse(rm, out var rangeModifier))
                {
                    model.rangeModifier = rangeModifier;
                }

                if (saveData.metaData.TryGetValue("TakeAimAbilityChoice-spreadModifier", out var sm) &&
                    float.TryParse(sm, out var spreadModifier))
                {
                    model.spreadModifier = spreadModifier;
                }

                takeAimMutator.resultCache.Clear();
                tower.ApplyMutation();
            }
        }
    }
}
