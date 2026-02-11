using System;
using System.Collections.Generic;
using UnityEngine;
namespace r8teful {
    public class Stat {
        public float BaseValue; 
        protected bool _isDirty = true;
        protected float _value;
    
        private readonly List<StatModifier> _modifiers = new();
    
        public Stat(float baseValue) {
            BaseValue = baseValue;
        }
    
        public float Value {
            get {
                if (_isDirty) {
                    _value = CalculateFinalValue(null);
                    _isDirty = false;
                }
                return _value;
            }
        }
        internal bool HasModifiers() => _modifiers.Count > 0;

        public void AddModifier(StatModifier mod) {
            _isDirty = true;
            _modifiers.Add(mod);
        }
    
        public bool RemoveModifiersFromSource(object source) {
            if (_modifiers.RemoveAll(x => x.Source == source) > 0) {
                _isDirty = true;
                return true;
            }
            return false;
        }
        public float GetTotalFlat(StatModifier extraMod = null) {
            float sum = 0;
            foreach (var mod in _modifiers)
                if (mod.Type == StatModifyType.Add) sum += mod.Value;

            if (extraMod != null && extraMod.Type == StatModifyType.Add) sum += extraMod.Value;
            return sum;
        }
        public float GetTotalPercentAdd(StatModifier extraMod = null) {
            float sum = 0;
            foreach (var mod in _modifiers)
                if (mod.Type == StatModifyType.PercentAdd) sum += mod.Value;

            if (extraMod != null && extraMod.Type == StatModifyType.PercentAdd) sum += extraMod.Value;
            return sum;
        }

        public float GetTotalPercentMult(StatModifier extraMod = null) {
            float product = 1f;
            foreach (var mod in _modifiers)
                if (mod.Type == StatModifyType.PercentMult) product *= mod.Value;

            if (extraMod != null && extraMod.Type == StatModifyType.PercentMult) product *= extraMod.Value;
            return product;
        }
        public float CalculateFinalValue(StatModifier extraMod) {
            float finalFlat = GetTotalFlat(extraMod);
            float finalPercentAdd = GetTotalPercentAdd(extraMod);
            float finalPercentMult = GetTotalPercentMult(extraMod);

            float result = (BaseValue + finalFlat) * (1f + finalPercentAdd) * finalPercentMult;

            return Mathf.Max(0, result);
        }

        // For ui:  base * percent add
        internal float GetTotalIncrease(StatModifier extraMod) {
            float finalFlat = GetTotalFlat(extraMod);
            float finalPercentAdd = GetTotalPercentAdd(extraMod);

            float result = (BaseValue + finalFlat) * (1f + finalPercentAdd);
            float increase = result / BaseValue; 
            return Mathf.Max(0, increase);
        }


        /// <summary>
        /// Use with causion!
        /// </summary>
        internal void RemoveAllModifiers() {
            _modifiers.Clear();
            _isDirty = true;
        }
    }
}