using FishNet.Demo.AdditiveScenes;
using Sirenix.Utilities;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISubPanelMove : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _zoneText;
    [SerializeField] private Transform _zoneResourcesContainer;
    [SerializeField] private Transform _playerStatusContainer;
    [SerializeField] private GameObject _waitingContainer;
    [SerializeField] private Button _buttonMove;
    [SerializeField] private Button _buttonCancelMove;
    [SerializeField] private UISubMap _mapScript;
    private int _currentShownIndex;
    private void Awake() {
        _buttonMove.onClick.AddListener(OnMoveClicked);
        _buttonCancelMove.onClick.AddListener(OnCancelMoveClicked);
        _waitingContainer.SetActive(false);
        _mapScript.Init(this);
    }

    private void Start() {

    }
    public void OnMapButtonClicked(ZoneSO zoneinfo) {
        _currentShownIndex = zoneinfo.ZoneIndex;
        SetMapInfo(zoneinfo);
    }
    private void SetMapInfo(ZoneSO zone) {
        _zoneText.text = zone.ZoneName;

        // Resources

        // Clear all first
        for (int j = 0; j < _zoneResourcesContainer.childCount; j++) {
            Destroy(_zoneResourcesContainer.GetChild(j).gameObject);
        }
        // Now polulate
        foreach (var item in zone.AvailableResources) {
            var g = Instantiate(App.ResourceSystem.GetPrefab("UIZoneResourceElement"), _zoneResourcesContainer); // Will automatically make a nice grid
            if (DiscoveryManager.Instance.IsDiscovered(item)) {
                g.GetComponent<Image>().sprite = item.icon;
            } else {
                g.GetComponent<Image>().sprite = App.ResourceSystem.GetSprite("ItemUnknown");
            }
        }
        // Should move button be visable?
        if (zone.ZoneIndex == SubmarineManager.Instance.CurrentZoneIndex) {
            // hide
            _buttonMove.gameObject.SetActive(false);
        } else if (!_buttonMove.gameObject.activeSelf) {
            _buttonMove.gameObject.SetActive(true);
        }
    }

 

    private void OnMoveClicked() {
        // Server handles most logic, will call OnMoveEnter if succefull 
        SubMovementManager.Instance.RequestMovement(NetworkedPlayer.LocalInstance.LocalConnection, _currentShownIndex);
    }
    private void OnCancelMoveClicked() {
        Debug.Log("Initiated client has canceled move req!");
        SubMovementManager.Instance.CancelRequest(NetworkedPlayer.LocalInstance.LocalConnection);

        OnMoveExit();
    }

    public void OnMoveExit() {
        _buttonMove.gameObject.SetActive(true);
        _waitingContainer.SetActive(false);
        _mapScript.EnableMapInteractions();
    }

    public void OnMoveEnter() {
        _mapScript.DissableMapInteractions();
        // Delete existing status things    
        for (int i = 0; i < _playerStatusContainer.childCount; i++) {
            Destroy(_playerStatusContainer.GetChild(i).gameObject);
        }
        _waitingContainer.SetActive(true);
        _buttonMove.gameObject.SetActive(false);

        // We can safely assume all players status is pending
        NetworkedPlayersManager.Instance.GetAllPlayers().ForEach(p => {
            var status = Instantiate(App.ResourceSystem.GetPrefab<PlayerSubMovementStatus>("UIPlayerSubMovementStatus"), _playerStatusContainer);
            status.Init(p.GetPlayerName(), p.OwnerId == NetworkedPlayer.LocalInstance.OwnerId); // set ourselves as ready
        });
    }

    internal void UpdatePlayerStatus(int[] acceptedIds, int[] pendingIds) {
        // Destroy all old ones first
        for (int i = 0; i < _playerStatusContainer.childCount; i++) {
            Destroy(_playerStatusContainer.GetChild(i).gameObject);
        }
        // Now make new ones
        foreach (int id in acceptedIds) {
            NetworkedPlayersManager.Instance.TryGetPlayer(id, out var player);
            if (player != null) {
                var status = Instantiate(App.ResourceSystem.GetPrefab<PlayerSubMovementStatus>("UIPlayerSubMovementStatus"), _playerStatusContainer);
                status.Init(player.GetPlayerName(),true);
            }
        }
        foreach (int id in pendingIds) {
            NetworkedPlayersManager.Instance.TryGetPlayer(id, out var player);
            if (player != null) {
                var status = Instantiate(App.ResourceSystem.GetPrefab<PlayerSubMovementStatus>("UIPlayerSubMovementStatus"), _playerStatusContainer);
                status.Init(player.GetPlayerName(), false);
            }
        }
    }

    internal void RequestRejected() {
        // Some kind of Error?
        Debug.LogWarning("Failed to enter move request!");
    }
}