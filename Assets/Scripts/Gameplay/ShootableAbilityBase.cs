using System.Collections;
using UnityEngine;

// Inheritance is nice when it works 
public abstract class ShootableAbilityBase : MonoBehaviour, IInitializableAbility {
    protected NetworkedPlayer _player;
    protected AbilityInstance _abilityInstance;
    private Coroutine _loop;

    public virtual void Init(AbilityInstance instance, NetworkedPlayer player) {
        _player = player;
        _abilityInstance = instance;
        _loop = StartCoroutine(FireLoop());
    }

    private IEnumerator FireLoop() {
        while (true) {
            if (_abilityInstance == null) yield break;
            float wait = _abilityInstance.GetEffectiveStat(StatType.Cooldown); // this clamps 
            Shoot();
            yield return new WaitForSeconds(wait);
        }
    }
    public abstract void Shoot();

    void OnDestroy() {
        if (_loop != null) StopCoroutine(_loop);
    }
}