using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Shows stats, items etc.
public class UIManagerStats : MonoBehaviour {
    private PlayerStatsManager _playerStats;
    private Dictionary<ushort, UIHudIconStatus> _hudIconsByID = new();
    [SerializeField] Transform _iconContainer;
    internal void Init(PlayerStatsManager playerStats) {
        _playerStats = playerStats;
        _playerStats.OnBuffListChanged += RebuildList;
        _playerStats.OnBuffsUpdated += RefreshTimes;
        RebuildList();
    }
    void OnDestroy() {
        if (_playerStats != null) {
            _playerStats.OnBuffListChanged -= RebuildList;
            _playerStats.OnBuffsUpdated -= RefreshTimes;
        }
    }
    private void RefreshTimes() {
        var snapshots = _playerStats.GetBuffSnapshots();
        foreach (var snapshot in snapshots) { 
            if(!_hudIconsByID.TryGetValue(snapshot.abilityId, out var icon)) {
                Debug.LogWarning("trying to update and icon before it has been createad, try syncing before refreshing");
            }
            icon.SetTime(snapshot.remainingSeconds);
        }
    }

    void RebuildList() {
        //  Add new entries & remove the ones that aren't there
        var snapshots = _playerStats.GetBuffSnapshots();
        HashSet<ushort> currentIds = new();
        foreach (var snapshot in snapshots) {
            currentIds.Add(snapshot.abilityId);
        }
        // Remove first
        var toRemove = new List<ushort>();
        foreach (var kvp in _hudIconsByID) {
            if(!currentIds.Contains(kvp.Key))
                toRemove.Add(kvp.Key);
        }
        foreach (var id in toRemove) {
            Destroy(_hudIconsByID[id].gameObject);
            _hudIconsByID.Remove(id);
        }
        // Now add 
        foreach (var snapshot in snapshots) {
            if (!_hudIconsByID.ContainsKey(snapshot.abilityId)) {
                var hudIcon = CreateHudIcon(snapshot);
                _hudIconsByID.Add(snapshot.abilityId, hudIcon);
            }
        }
    }
    private UIHudIconStatus CreateHudIcon(BuffSnapshot snapshot) {
        var uiIcon = Instantiate(App.ResourceSystem.GetPrefab<UIHudIconStatus>("UIHudIconStatus"), _iconContainer);
        uiIcon.Init(snapshot.icon,snapshot.displayName);
        return uiIcon; 
    }
}