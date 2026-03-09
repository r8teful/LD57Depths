using System.Collections.Generic;
using UnityEngine;

public class ParticleColourSetter : MonoBehaviour {
    private ParticleSystem _particleSystem;
    private RandomSpriteSetter _spriteSetter;
    public List<Gradient> _spriteGradientList;

    private void Awake() {
        _particleSystem = GetComponent<ParticleSystem>();
        _spriteSetter = transform.parent.GetComponentInChildren<RandomSpriteSetter>();
        if (_particleSystem == null || _spriteSetter == null) {
            Debug.LogError("Can't find component!");
            return;
        }
    }
    private void Start() {
        var i = _spriteSetter.RandomIndex;
        if(i == -1) {
            Debug.LogError("Index invalid!");
            return;
        }
        if(i > _spriteGradientList.Count) {
            Debug.LogError("Index higher than gradienrs!");
            return;
        }
        var g = _spriteGradientList[i];
        var m = _particleSystem.main;
        m.startColor = g;
    }
}
