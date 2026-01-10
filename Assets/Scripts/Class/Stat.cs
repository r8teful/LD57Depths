using System.Collections.Generic;
using System.Linq;
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
    
        public float GetTotalFlatModifier(StatModifier extraMod) {
            if (extraMod != null) {
                var mod = new List<StatModifier>();
                mod.AddRange(_modifiers);
                mod.Add(extraMod);
                return mod.Where(m => m.Type == StatModifyType.Add).Sum(m => m.Value);
            }
            return _modifiers.Where(m => m.Type == StatModifyType.Add).Sum(m => m.Value);
        }
        public float GetTotalPercentModifier(StatModifier extraMod) {
            if (extraMod != null) {
                var mod = new List<StatModifier>();
                mod.AddRange(_modifiers);
                mod.Add(extraMod);
                return mod.Where(m => m.Type == StatModifyType.Multiply).Sum(m => m.Value);
            }
            return _modifiers.Where(m => m.Type == StatModifyType.Multiply).Sum(m => m.Value);
            //return _instanceMods.Where(m => m.Stat == stat && m.Type == IncreaseType.Multiply).Aggregate(1f, (a, m) => a * m.Value);
        }
    
        public float CalculateFinalValue(StatModifier extraMod) {
            float baseValue = BaseValue;

            var flat = GetTotalFlatModifier(extraMod);
            var percentAdd = GetTotalPercentModifier(extraMod);
            var finalValue = (baseValue + flat) * (1+percentAdd);
            //var finalValue = (baseValue + flat) * Mathf.Max(1,percentAdd);
            /*
             * Maybe this later?
            // Apply Multiplicative Percentages 
            for (int i = 0; i < _modifiers.Count; i++) {
                if (_modifiers[i].Type == StatModType.PercentMult)
                    finalValue *= _modifiers[i].Value;
            }
                */
    
            return finalValue; // Math.Max(0, finalValue) if you want no negatives
        }
    
    }
}