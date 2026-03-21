using UnityEngine;

public class GravestoneSpawner : MonoBehaviour, IInitializableAbility {
    [SerializeField] GraveStone _gravestonePrefab;
    [SerializeField] private ValueModifiableComponent _values;

    public void Init(AbilityInstance instance, PlayerManager player) {
        _values.Register();
    }
    private void Awake() {
        OxygenManager.OnPassOut += OnPlayerPassout;
    }
    private void OnDestroy() {
        OxygenManager.OnPassOut -= OnPlayerPassout;
    }
    private void OnPlayerPassout() {
        if (PlayerManager.Instance == null) return;
        var passOutLoseProcent = _values.GetValueNow(ValueKey.PassOutLoseProcent);
        var itemsToPutInGrave = PlayerManager.Instance.GetInventory().GetItemSnapshot(1 - passOutLoseProcent); // we keep what we don't lose 
        if (itemsToPutInGrave == null || itemsToPutInGrave.Count == 0) return; 
        var grave = Instantiate(_gravestonePrefab, PlayerManager.Instance.GetWorldPosition, Quaternion.identity);
        
        grave.Init(itemsToPutInGrave);
    }

}