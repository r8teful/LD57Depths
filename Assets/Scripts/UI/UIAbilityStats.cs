using TMPro;
using UnityEngine;

public class UIAbilityStats : MonoBehaviour {
    // This should show the stats this ability is using, and when it gets upgrades, it should show what those stats are
    // Basically just implement what the lazer needs to show right now
    [SerializeField] private Transform _statDisplayElements;
    [SerializeField] private TextMeshProUGUI _abilityHeaderText;
    private AbilityInstance _cachedAbility;

    public void Init(AbilityInstance ability) {
        if (ability == null) return;
        _cachedAbility = ability;
        _abilityHeaderText.text = ability.Data.displayName;
        CreateStatDisplays();
        ability.OnModifiersChanged += ModifiersChanged;
    }
    private void ModifiersChanged() {
        CreateStatDisplays();
    }

    private void CreateStatDisplays() {
        foreach (Transform child in _statDisplayElements.transform) {
            Destroy(child.gameObject);
        }
        foreach (var statData in _cachedAbility.Data.statTypes) {
            var e = Instantiate(App.ResourceSystem.GetPrefab<UIStatDisplayElement>("UIStatDisplayElement"), _statDisplayElements);
            if(statData.stat == StatType.MiningDamage && _cachedAbility.Data.ID == ResourceSystem.BrimstoneBuffID) {
                Debug.Log("This is mining damage");
            }
            var statValue = _cachedAbility.GetBuffStatStrength(statData.stat,statData.modType);
            e.Init(statData.stat,statValue);
        }
    }
}