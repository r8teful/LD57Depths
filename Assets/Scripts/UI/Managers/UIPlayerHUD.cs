using System;
using System.Collections.Generic;
using UnityEngine;

// Shows stats, items etc.
public class UIPlayerHUD : MonoBehaviour {
    private PlayerStatsManager _playerStats;

    // Maybe we could have a generic "uihudiconbase" dictionary but eh...
    private Dictionary<ushort, UIHudIconStatus> _buffIconsByID = new();
    private Dictionary<ushort, UIHudIconAbilityActive> _activeIconsByID = new();
    [SerializeField] Transform _iconContainerPassive;
    [SerializeField] Transform _iconContainerBuffs;
    [SerializeField] Transform _iconContainerActive;
    [SerializeField] Transform _statDisplayElements;
    [SerializeField] Transform _abilityDisplayElements;
    [SerializeField] UIBiomeText _biomeTextPrefab;
    public Compass Compass;
    private PlayerManager _player;

    internal void Init(PlayerManager player) {
        _playerStats = player.PlayerStats;
        _playerStats.OnBuffListChanged += BuffListChange;
        _playerStats.OnBuffsUpdated += RefreshTimes;
        _playerStats.OnStatChanged += StatChange;
        player.PlayerAbilities.OnAbilityAdd += OnAddAbility;
        player.PlayerAbilities.OnabilityRemove += OnRemoveAbility;
        if(BiomeManager.Instance != null) {
            BiomeManager.Instance.OnNewPlayerBiome += OnNewBiome;
        } else {
            Debug.LogWarning("BiomeManager instance is null!");
        }
        _player = player;
        RebuildBuffList();
        CreateStatDisplays();
    }

    private void OnNewBiome(BiomeType from, BiomeType to) {
        if    (to == BiomeType.None || to == BiomeType.Trench 
            || to == BiomeType.Trench1 || to == BiomeType.Trench2 
            || to == BiomeType.Trench3 || to == BiomeType.Surface)
            return;
        var b = Instantiate(_biomeTextPrefab, transform); // we could just reuse the component but eh
        b.StartAnim(to);// Text prefab handles destruction, animation, etc
    }

    private void BuffListChange() {
        Debug.Log("BUFF LIST CHANGE!!");
        RebuildBuffList();
        CreateStatDisplays();// Also refresh stat displays
    }

    private void StatChange() {
        // Just refresh all statDisplays 
        CreateStatDisplays();

    }

    private void OnAddAbility(AbilityInstance ability) {
        // If the ability is an active ability, add it to the bottom of the screen
        if(ability.Data.type == AbilityType.Active) {
            var icon = Instantiate(App.ResourceSystem.GetPrefab<UIHudIconAbilityActive>("UIHudIconAbilityActive"), _iconContainerActive);
            icon.Init(ability);
            _activeIconsByID.Add(ability.Data.ID, icon);
        }
        CreateAbilityUIMenu(ability);
    }
    private void OnRemoveAbility(AbilityInstance ability) {
        if (ability.Data.type == AbilityType.Active) {
            if(_activeIconsByID.TryGetValue(ability.Data.ID,out var icon)) {
                Destroy(icon.gameObject); // Just destroy it? Idk what else we would need to do
            } else {
                Debug.LogWarning("Tried to remove ability that isn't registered in dictionary. Did we forget to add it?");
            }
        }
    }


    void OnDestroy() {
        if (_playerStats != null) {
            _playerStats.OnBuffListChanged -= BuffListChange;
            _playerStats.OnBuffsUpdated -= RefreshTimes;
            _playerStats.OnBuffsUpdated -= RefreshTimes;
            _playerStats.OnStatChanged -= StatChange;
        }
        _player.PlayerAbilities.OnabilityRemove -= OnRemoveAbility;
        _player.PlayerAbilities.OnAbilityAdd -= OnAddAbility;
        
        if (BiomeManager.Instance != null) {
            BiomeManager.Instance.OnNewPlayerBiome -= OnNewBiome;
        }
    }
    private void RefreshTimes() {
        var snapshots = _playerStats.GetBuffSnapshots();
        foreach (var snapshot in snapshots) { 
            if(!_buffIconsByID.TryGetValue(snapshot.buffID, out var icon)) {
                Debug.LogWarning("trying to update and icon before it has been createad, try syncing before refreshing");
            }
            icon.SetTime(snapshot.remainingSeconds);
        }
    }

    void RebuildBuffList() {
        //  Add new entries & remove the ones that aren't there
        var snapshots = _playerStats.GetBuffSnapshots();
        HashSet<ushort> currentIds = new();
        foreach (var snapshot in snapshots) {
            currentIds.Add(snapshot.buffID);
        }
        // Remove first
        var toRemove = new List<ushort>();
        foreach (var kvp in _buffIconsByID) {
            if(!currentIds.Contains(kvp.Key))
                toRemove.Add(kvp.Key);
        }
        foreach (var id in toRemove) {
            Destroy(_buffIconsByID[id].gameObject);
            _buffIconsByID.Remove(id);
        }
        // Now add 
        foreach (var snapshot in snapshots) {
            if (!_buffIconsByID.ContainsKey(snapshot.buffID)) {
                var hudIcon = CreateHudIcon(snapshot);
                _buffIconsByID.Add(snapshot.buffID, hudIcon);
            }
        }
    }


    private void CreateStatDisplays() {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        foreach (Transform child in _statDisplayElements.transform) {
            Destroy(child.gameObject);
        }
        foreach (var stat in (StatType[])Enum.GetValues(typeof(StatType))) {
            var statValue = _playerStats.GetStat(stat);
            var e = Instantiate(App.ResourceSystem.GetPrefab<UIStatDisplayElement>("UIStatDisplayElement"), _statDisplayElements);
            e.Init(stat, statValue);
        }
#endif
    }

    private void CreateAbilityUIMenu(AbilityInstance ability) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var abilityStats = Instantiate(App.ResourceSystem.GetPrefab<UIAbilityStats>("UIAbilityStats"), _abilityDisplayElements);
        abilityStats.Init(ability);
#endif
    }
    private UIHudIconStatus CreateHudIcon(BuffSnapshot snapshot) {
        var uiIcon = Instantiate(App.ResourceSystem.GetPrefab<UIHudIconStatus>("UIHudIconStatus"), _iconContainerBuffs);
        uiIcon.Init(snapshot.icon,snapshot.displayName);
        return uiIcon; 
    }
}