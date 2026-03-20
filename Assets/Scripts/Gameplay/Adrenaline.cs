using System;
using System.Collections;
using UnityEngine;

public class Adrenaline : MonoBehaviour {

    private bool _buffApplied;
    private float _oxygenProcentLimit = 0.2f;
    private void OnEnable() {
        OxygenManager.OnOxygenChanged += OnOxygenChange;
    }
    private void OnDisable() {
        OxygenManager.OnOxygenChanged += OnOxygenChange;
    }

    private void OnOxygenChange(float current, float max) {
        if (Mathf.Approximately(max, 0))return;
        float oxygenProcent = current / max;
        if(!_buffApplied && oxygenProcent <= _oxygenProcentLimit) {
            ApplyBuff();
        } else if(oxygenProcent > _oxygenProcentLimit) {
            RemoveBuff();
        }
    }

    private void RemoveBuff() {
        _buffApplied = false;
    }

    private void ApplyBuff() {
        _buffApplied = true;
    }
}