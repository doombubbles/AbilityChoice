using System.Collections;
using BTD_Mod_Helper.Api.Testing;
using UnityEngine;

namespace AbilityChoice;

public class AbilityChoiceTest : ModTest
{
    public override IEnumerator Test()
    {
        for (var mode = 1; mode <= 2; mode++)
        {
            yield return EnsureOnMainMenuWithNoPopUps();

            AbilityChoice.overrideMode = mode;

            yield return LoadIntoGame();

            foreach (var towerModel in GetContent<AbilityChoice>()
                         .Where(choice => choice.Mode == mode)
                         .SelectMany(abilityChoice => abilityChoice.GetAffectedCached(Bridge.Model))
                         .Distinct())
            {
                var tower = AssertNotNull(CreateTowerAt(Bridge, new Vector2(0, 0), towerModel, costOverride: 0),
                    towerModel.name);
                yield return null;
                yield return null;
                yield return null;
                Bridge.SellTower(Bridge.GetInputId(), tower.Id);
            }
        }

        AbilityChoice.overrideMode = null;
    }
}
