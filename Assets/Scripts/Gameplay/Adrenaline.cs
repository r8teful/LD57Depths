using UnityEngine;

public class Adrenaline : MonoBehaviour, IInitializableAbility {

    private BuffHandle _activeBuffNormal;
    private BuffHandle _activeBuffPlus;
    private float _oxygenProcentLimit = 0.2f;
    public BuffSO buffAdrenaline;
    public BuffSO buffAdrenalinePlus;
    private PlayerManager _player;
    private bool _isPlus = false;
    public void Init(AbilityInstance instance, PlayerManager player) {
        _player = player;
    }

    internal void ActivePlus() {
        _isPlus = true;
    }
    private void OnEnable() {
        OxygenManager.OnOxygenChanged += OnOxygenChange;
    }
    private void OnDisable() {
        OxygenManager.OnOxygenChanged += OnOxygenChange;
    }

    private void OnOxygenChange(float current, float max) {
        if (Mathf.Approximately(max, 0))return;
        float oxygenProcent = current / max;
        if(_activeBuffNormal == null && oxygenProcent <= _oxygenProcentLimit) {
            ApplyBuff();
        } else if(_activeBuffNormal != null && oxygenProcent > _oxygenProcentLimit ) {
            RemoveBuff();
        }
    }

    private void RemoveBuff() {
        if(_activeBuffNormal == null) {
            Debug.LogWarning("Tried to remove buff but its not active!");
            return;
        }
        _activeBuffNormal.Remove();
        _activeBuffNormal = null;
        if (_activeBuffPlus == null) {
            //Debug.LogWarning("Tried to remove buff but its not active!");
            return;
        }
        _activeBuffPlus.Remove();
        _activeBuffPlus = null;
    }

    private void ApplyBuff() {
        _activeBuffNormal = _player.PlayerStats.TriggerBuff(buffAdrenaline);
        if (_isPlus) {
            _activeBuffPlus = _player.PlayerStats.TriggerBuff(buffAdrenalinePlus);
        }
    }
}