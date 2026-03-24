using DG.Tweening;
using r8teful;
using Sirenix.OdinInspector;
using System;
using System.Collections;
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

    private Dictionary<ushort, SubUpgradeEffect> _subUpgrades = new Dictionary<ushort, SubUpgradeEffect>(); // UPGRADE NODE ID, EFFECT
    
    [SerializeField] private Transform _cutsceneCameraPosUpgradeMachine;
    [SerializeField] private Transform _cutsceneCameraPosControlPanel; 
    [SerializeField] private Transform _cutsceneCameraCables; 
    [SerializeField] private SpriteRenderer _upgradeMachine; 
    [SerializeField] private SpriteRenderer _subControlPanel;

    public bool CanMoveTo(int index) {
        return index <= GetUpgradeStage();
    }

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
        float yBefore = transform.position.y;
        MoveInterior(VisibilityLayerType.Interior); // so confusing but this will simply update the position of the interior so that its where the exterior part of the submarine is
        float yNow = transform.position.y;
        float yDiff = yNow - yBefore;
        var p = PlayerManager.Instance.gameObject.transform.position;
        p.y += yDiff; // this unexpectidly also works with negative numbers 
        PlayerManager.Instance.gameObject.transform.position = p; // this is so bad, it it should teleport the player to the right positon

        StartCoroutine(MoveSubRoutine()); // visual + audio
     }
    internal void SetSubPosIndex(int index) {
        _currentZoneIndex = index;
        OnSubMoved?.Invoke(index);
    }
    private IEnumerator MoveSubRoutine() {
        // todo dissable interaction etc...
        yield return App.Backdrop.Require();
        var s = AudioController.Instance.PlaySound2D("SubMove",0,looping:true);
        s.DOFade(0.2f, 1);
        yield return new WaitForSeconds(3);
        s.DOFade(0, 2).OnComplete(()=>Destroy(s.gameObject));
        yield return App.Backdrop.Release();
    }

    internal void NewSubUpgrade(SubUpgradeEffect effect) {
        bool success = _subUpgrades.TryAdd(effect.upgrade.ID,effect);
        if (!success) {
            Debug.LogError("Sub recipe already purchased! Did you assigned a unique ID?");
            return;
        }
        if (GameManager.Instance.IsBooting) {
            OnSubUpgrade?.Invoke(effect.upgrade.ID); // skip the cutscene
        } else {
            HandleCutscene(effect.upgrade.ID, ResourceSystem.SubUpgradePanel, _cutsceneCameraPosUpgradeMachine);
            HandleCutscene(effect.upgrade.ID, ResourceSystem.SubUpgradeControlPanel, _cutsceneCameraPosControlPanel);
            HandleCutscene(effect.upgrade.ID, ResourceSystem.SubUpgradeCables, _cutsceneCameraCables);
        }
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

    public int GetUpgradeStage() {
        int stage = 0;
        foreach (var kvp in _subUpgrades) {
            if (kvp.Value.unlocksNextZone)
                stage++;
        }
        return stage;
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