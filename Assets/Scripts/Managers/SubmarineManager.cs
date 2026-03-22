using r8teful;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

// This is on the interior instance
public class SubmarineManager : StaticInstance<SubmarineManager>, ISaveable {
     private int _currentZoneIndex;
    public int CurrentZoneIndex => _currentZoneIndex;

    public event Action<int> OnSubMoved; // Used by map 
    public static event Action<ushort> OnSubUpgrade; 
    public GameObject submarineExterior;
    public Transform InteriorSpawnPoint;
    [ShowInInspector]
    private InventoryManager subInventory;
    public InventoryManager SubInventory => subInventory;
    [SerializeField] private SubItemGainVisualSpawner itemGainSpawner;

    private HashSet<ushort> _subUpgrades = new HashSet<ushort>(); // UPGRADE NODE ID 
    
    [SerializeField] private Transform _cutsceneCameraPosUpgradeMachine;
    [SerializeField] private Transform _cutsceneCameraPosControlPanel; 
    [SerializeField] private Transform _cutsceneCameraCables; 
    [SerializeField] private SpriteRenderer _upgradeMachine; 
    [SerializeField] private SpriteRenderer _subControlPanel; 
    
    protected override void Awake() {
        base.Awake();
        subInventory = new InventoryManager(); // will get overwritten in OnLoad if we have save data
        GameManager.OnSetupComplete += MyStart;
    }
    private void OnDestroy() {
        GameManager.OnSetupComplete -= MyStart;
    }

    private void MyStart() {
        itemGainSpawner.Init(subInventory); // We init it here because inventory will be already properly loaded
        var y = GameManager.Instance.WorldGenSettings.MaxDepth;
        submarineExterior.transform.position = new Vector3(0, y);
    }

    public void MoveSub(int index) {
        submarineExterior.transform.position = new(0, GameManager.Instance.WorldGenSettings.GetWorldLayerYPos(index));
        SetSubPosIndex(index);
    }
    internal void SetSubPosIndex(int index) {
        _currentZoneIndex = index;
        OnSubMoved?.Invoke(index);
    }
    

    internal void NewSubUpgrade(SubUpgradeEffect effect) {
        if (effect.isMajor) {
            bool success = _subUpgrades.Add(effect.upgrade.ID);
            if (!success) {
                Debug.LogError("Sub recipe already purchased! Did you assigned a unique ID?");
                return;
            }
            if (GameManager.Instance.IsBooting) {
                OnSubUpgrade?.Invoke(effect.upgrade.ID); // skip the cutscene
            } else {
                HandleCutscene(effect.upgrade.ID, ResourceSystem.SubUpgradePanel, _cutsceneCameraPosUpgradeMachine);
                HandleCutscene(effect.upgrade.ID,ResourceSystem.SubUpgradeControlPanel, _cutsceneCameraPosControlPanel);
                HandleCutscene(effect.upgrade.ID,ResourceSystem.SubUpgradeCables, _cutsceneCameraCables);
            }
        }
        // TODO update visual of the sub based on effect sprites

    }

    private void HandleCutscene(ushort newUpgrade, ushort ID, Transform cutscenePosition) {
        if (newUpgrade == ID) {
            GameSequenceManager.Instance.AddEvent(shouldPause: false,
                onStart: () => {
                    GameCutsceneManager.Instance.StartSubUpgradeCutscene(
                        cutscenePosition,
                        () => OnSubUpgrade?.Invoke(ID));
                },
            onFinish: () => {

            }
           );
        }
    }
    private void ChangeUpgradeSprite() {

    }

    internal void MoveInterior(VisibilityLayerType currentLayer) {
        if (currentLayer == VisibilityLayerType.Interior) {
            // Move in
            transform.position = submarineExterior.transform.position;
        } else {
            // Move out lol
            Vector3 YEET = new(6000, 0);
            transform.position = submarineExterior.transform.position + YEET;
        }
    }

    public int GetUpgradeStage() => _subUpgrades.Count; // lol?
    
    public bool CanMoveTo(int index) {
        return index <= GetUpgradeStage();
    }

    // FOR DEBUGING PUPROSES!!
    internal void RemoveAllUpgrades() {
        _subUpgrades.Clear();
    }

    internal void FixControlPanel() {

    }

    public void OnSave(SaveData data) {
        Dictionary<ushort, int> inventorySaveData = new Dictionary<ushort, int>();
        foreach (var slot in subInventory.Slots) {
            inventorySaveData.Add(slot.Key, slot.Value.quantity);
        }
        data.bobData.inventorySaveData = inventorySaveData; 
    }

    public void OnLoad(SaveData data) {
        if (data == null) return;
        if (data.bobData == null) return;
        subInventory ??= new(); // if this would be the case ui would be broken becuase they subscribe when 
        subInventory.LoadFromSave(data.bobData.inventorySaveData);
    }
}