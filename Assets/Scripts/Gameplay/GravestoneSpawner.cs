using UnityEngine;

public class GravestoneSpawner : MonoBehaviour, IValueModifiable, IInitializableAbility {
    [SerializeField] GraveStone _gravestonePrefab;
    private float _passOutLoseProcent;
    private float _passOutLoseProcentBase = 0.8f;

    public void Init(AbilityInstance instance, PlayerManager player) {
    
    }
    private void Awake() {
        OxygenManager.OnPassOut += OnPlayerPassout;
        Register();
        _passOutLoseProcent = _passOutLoseProcentBase;
    }
    private void OnDestroy() {
        OxygenManager.OnPassOut -= OnPlayerPassout;
    }
    private void OnPlayerPassout() {
        if (PlayerManager.Instance == null) return;
        var itemsToPutInGrave = PlayerManager.Instance.GetInventory().GetItemSnapshot(1 - _passOutLoseProcent); // we keep what we don't lose 
        if (itemsToPutInGrave == null || itemsToPutInGrave.Count == 0) return; 
        var grave = Instantiate(_gravestonePrefab, PlayerManager.Instance.GetWorldPosition, Quaternion.identity);
        
        grave.Init(itemsToPutInGrave);
    }
    public float GetValueBase(ValueKey key) {
        if(key == ValueKey.PassOutLoseProcent) {
            return _passOutLoseProcentBase;
        }
        return 1;
    }

    public float GetValueNow(ValueKey key) {
        if (key == ValueKey.PassOutLoseProcent) {
            return _passOutLoseProcent;
        }
        return 1;
    }

    public void ModifyValue(ValueModifier modifier) {
        if (modifier.Key == ValueKey.PassOutLoseProcent) {
            _passOutLoseProcent = UpgradeCalculator.CalculateNewUpgradeValue(_passOutLoseProcent, modifier);
        }
    }

    public void Register() {
        PlayerManager.Instance.UpgradeManager.RegisterValueModifierScript(ValueKey.PassOutLoseProcent, this);
    }

    public void ReturnValuesToBase() {
        _passOutLoseProcent = _passOutLoseProcentBase;
    }

}