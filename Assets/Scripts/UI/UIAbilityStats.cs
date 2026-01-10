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
        foreach (var statData in _cachedAbility.Stats) {
            var e = Instantiate(App.ResourceSystem.GetPrefab<UIStatDisplayElement>("UIStatDisplayElement"), _statDisplayElements);
            if(statData.Key == StatType.MiningDamage && _cachedAbility.Data.ID == ResourceSystem.BrimstoneBuffID) {
                Debug.Log("This is mining damage");
            }
            var statRaw = 0; // tdo 
            //var statRaw = _cachedAbility.GetRawValue(statData.Key);
            var statEffective = _cachedAbility.GetEffectiveStat(statData.Key);
            e.Init(statData.Key,statEffective, statRaw);
        }
    }
}