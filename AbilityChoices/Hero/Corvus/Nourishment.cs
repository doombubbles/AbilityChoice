using HarmonyLib;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Simulation.Corvus.Spells;
using Il2CppAssets.Scripts.Simulation.Corvus.TowerManager;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;

namespace AbilityChoice.AbilityChoices.Hero.Corvus;

public class Nourishment : CorvusAbilityChoice
{
    protected override int Order => 1;

    public override string Description1 => "Consumes Mana and converts it into cash.";

    protected override bool HasMode2 => false;

    public override void Apply1(TowerModel model)
    {
        var spell = InstantSpell(model);

        spell.initialManaCost = (int) (800 / spell.cooldown);

        spell.duration = 1;
        spell.cooldown = 0;
    }

    public override void Apply2(TowerModel model)
    {
        var spell = InstantSpell(model);

        spell.initialManaCost = (int) (800 / spell.cooldown);

        spell.duration = 1;
        spell.cooldown = 0;
    }

    [HarmonyPatch(typeof(Il2CppAssets.Scripts.Simulation.Corvus.Spells.Instant.Nourishment),
        nameof(Il2CppAssets.Scripts.Simulation.Corvus.Spells.Instant.Nourishment.Cast), [])]
    internal static class Nourishment_Cast
    {
        [HarmonyPrefix]
        internal static bool Prefix(Il2CppAssets.Scripts.Simulation.Corvus.Spells.Instant.Nourishment __instance)
        {
            if (!EnabledForSpell(CorvusSpellType.Nourishment)) return true;

            var tower = __instance.tower;
            var mana = __instance.SpellModel.initialManaCost;
            
            if (__instance.Sim.model.gameMode != "Clicks" && __instance.Sim.model.gameMode != "Deflation")
            {
                __instance.Sim.AddCash(mana * __instance.spellModel.cashPerMana, Simulation.CashType.Ability,
                    tower.owner, Simulation.CashSource.CorvusNourishment);
            }

            return false;
        }
    }

    /// <summary>
    /// Adjust for Nourishment not draining all mana
    /// </summary>
    [HarmonyPatch(typeof(CorvusManager), nameof(CorvusManager.HasEnoughManaForSpell))]
    internal static class CorvusManager_HasEnoughManaForSpell
    {
        [HarmonyPostfix]
        internal static void Postfix(CorvusManager __instance, CorvusSpell spell, ref bool __result)
        {
            if (spell.SpellType == CorvusSpellType.Nourishment)
            {
                __result &= __instance.AvailableMana >= spell.corvusSpellModel.initialManaCost;
            }
        }
    }

    /// <summary>
    /// Adjust for Nourishment not draining all mana
    /// </summary>
    [HarmonyPatch(typeof(CorvusSpellbookSpellUi), nameof(CorvusSpellbookSpellUi.UpdateUi))]
    internal static class CorvusSpellbookSpellUi_UpdateUi
    {
        [HarmonyPostfix]
        internal static void Postfix(CorvusSpellbookSpellUi __instance)
        {
            if (__instance.corvusSpellModel.spellType == CorvusSpellType.Nourishment)
            {
                __instance.manaCost.SetText(__instance.corvusSpellModel.initialManaCost.ToString());
            }
        }
    }

    /// <summary>
    /// Prevent error
    /// </summary>
    [HarmonyPatch(typeof(Il2CppAssets.Scripts.Simulation.Corvus.Spells.Instant.Nourishment), nameof(Il2CppAssets.Scripts.Simulation.Corvus.Spells.Instant.Nourishment.Expire))]
    internal static class Nourishment_Expire
    {
        [HarmonyPrefix]
        internal static bool Prefix(Il2CppAssets.Scripts.Simulation.Corvus.Spells.Instant.Nourishment __instance)
        {
            return false;
        }
    }
}
