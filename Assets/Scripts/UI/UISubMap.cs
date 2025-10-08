using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using UnityEngine;

public class UISubMap : MonoBehaviour {

    [SerializeField] private Transform trenchZonesContainer;
    [SerializeField] private Transform youAreHereContainer;
    private List<UITrenchZone> _trenchZones = new();
    private UISubPanelMove _parent;

    private void Awake() {
        SubmarineManager.Instance.OnSubMoved += SubMoved;
        if (trenchZonesContainer != null) {
            var zones = trenchZonesContainer.GetComponentsInChildren<UITrenchZone>();
            foreach (var zone in zones) {
                _trenchZones.Add(zone);
                zone.Init(this);
            }
        }
    }
    private void OnEnable() {
        RefreshUI();
    }
    internal void Init(UISubPanelMove uISubPanelMove) {
       _parent = uISubPanelMove;
    }
    public void OnMapButtonClicked(ZoneSO zoneData) {
        // Notify parent so it can display the information properly
        _parent.OnMapButtonClicked(zoneData);
        SetMapButtonVisual(zoneData.ZoneIndex);
    }

    private void OnDestroy() {
        SubmarineManager.Instance.OnSubMoved -= SubMoved;
    }
    private void Start() {
        var zoneI = SubmarineManager.Instance.CurrentZoneIndex;
        if(_parent!=null)
            SetMapButtonVisual(zoneI); // start with the visuals in your current zone
        RefreshUI();
    }

    private void SubMoved() {
        RefreshUI();
    }

    private void RefreshUI() {
        var zoneI = SubmarineManager.Instance.CurrentZoneIndex;
        SetYouAreHereVisual(zoneI);
        UpdateButtonVisualColors(SubmarineManager.Instance.GetUpgradeStage());
    }

    private void UpdateButtonVisualColors(int stage) {
        // Simply darken the color by lowering the alpha if its index is higher than the current stage
        for (int i = 0; i < _trenchZones.Count; i++) {
            if (_trenchZones[i] != null) {
                if (i > stage) {
                    _trenchZones[i].SetAlpha(0.2f);
                } else {
                    _trenchZones[i].SetAlpha(1);
                }
            }
        }
    }

    private void SetYouAreHereVisual(int index) {
        // Disabble all
        for (int i = 0; i < youAreHereContainer.childCount; i++) {
            youAreHereContainer.GetChild(i).gameObject.SetActive(false);
        }
        // Enable the specified index
        youAreHereContainer.GetChild(index).gameObject.SetActive(true);
    }
    private void SetMapButtonVisual(int i) {
        var index = _trenchZones.FindIndex(x => x.ZoneData.ZoneIndex == i);
        if (index == -1) {
            Debug.LogWarning("Could  not find valid trenchzone index!");
            return;
        }
        for (int j = 0; j < _trenchZones.Count; j++) {
            if (_trenchZones[j] != null) {
                SetMapButtonVisual(j, j == i);
            } else {
                Debug.LogWarning($"Tab at index {j} is null.");
            }
        }
    }
    private void SetMapButtonVisual(int i, bool setActive) {
        if (setActive) {
            _trenchZones[i].SetColor(Color.white);
            _trenchZones[i].transform.SetAsLastSibling();
        } else {
            _trenchZones[i].SetColor(Color.red);
            if (ColorUtility.TryParseHtmlString("#5BE5C0", out var color)) {
                _trenchZones[i].SetColor(color);
            }
        }
    }
    public void DissableMapInteractions() {
        for (int i = 0; i < _trenchZones.Count; i++) {
            _trenchZones[i].SetInteractable(false);
        }
    }
    public void EnableMapInteractions() {
        for (int i = 0; i < _trenchZones.Count; i++) {
            _trenchZones[i].SetInteractable(true);
        }
    }

    internal void SetBoxHighlight(ZoneSO zone) {
        var i = zone.ZoneIndex;
        SetMapButtonVisual(i);
    }

}