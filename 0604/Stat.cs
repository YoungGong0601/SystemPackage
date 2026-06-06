using System;
using System.Collections.Generic;
using UnityEngine;

// ── Enum ──────────────

public enum ModifierType { Flat = 0, PercentAdd = 100, Multiplier = 200, ANYTHING = 999 }
public enum StatLayerType { Default = 0, Buff = 100, Debuff = 200, FinalCalculation = 900 }

// ═══════════════════════════════════════
//  STAT
// ═══════════════════════════════════════
// Observer , Dirty Flag Pattern
//
// 계산 공식 : ((value + Flat) + (value * PercentAdd)) * Multiplier
//

[System.Serializable]
public class Stat
{
    // ── Base Value ──────────────
    [SerializeField]
    protected float _baseValue;

    // ── Data ──────────────
    private SortedDictionary<int, StatLayer> _layers;

    public Action OnValueChanged;

    // ── Property ──────────────
    protected SortedDictionary<int, StatLayer> Layers => _layers ??= new();

    public float BaseValue
    {
        get => _baseValue;
        set
        {
            _baseValue = value;
            OnValueChanged?.Invoke();
        }
    }

    public float Value
    {
        get
        {
            float calculated = BaseValue;

            foreach (var layer in Layers)
                calculated = layer.Value.CalculateLayer(calculated);

            return calculated;
        }
    }

    public float ValueWithOutFinalCalculation
    {
        get
        {
            float calculated = BaseValue;
            foreach (var layer in Layers)
            {
                if (layer.Key > 899) break; // 900 이상은 최종 계산 직전에 적용되는 레이어라고 가정 (예: 최종 데미지 가중치)
                calculated = layer.Value.CalculateLayer(calculated);
            }
            return calculated;
        }
    }

    // ── Value Excluding ──────────────
    // 지정한 SourceKey 모디파이어들을 제외하고 계산한 값.
    // 특정 모디파이어의 기여분을 분리할 때 사용 (예: 데미지 텍스트를 기본/가중분으로 분할 표시).
    // Dirty 캐시를 쓰지 않고 매 호출 재계산하므로, 레이어에 Flat/PercentAdd가 섞여도 정확하다.
    public float ValueExcluding(params string[] excludeKeys)
    {
        if (excludeKeys == null || excludeKeys.Length == 0) return Value;

        var set = new HashSet<string>(excludeKeys);
        float calculated = BaseValue;
        foreach (var layer in Layers)
            calculated = layer.Value.CalculateLayer(calculated, set);
        return calculated;
    }

    //━━━━━━━━━━ Settings ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    // 생성자
    public Stat(float baseValue = 0)
    {
        BaseValue = baseValue;
    }

    // float 연산자
    public static implicit operator float(Stat s) => s.Value;

    //━━━━━━━━━━ Methods ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    // ───────────────────────────────
    //  Modifier
    // ───────────────────────────────
    // 레이어와 모디파이어 관리 함수
    // - AddModifier: 특정 레이어에 모디파이어 추가 (레이어가 없으면 생성)
    // - UpdateModifier: 특정 레이어의 모디파이어 업데이트 (없으면 추가)
    // - RemoveModifier: 특정 레이어에서 모디파이어 제거

    // ── Add ──────────────
    public void AddModifier(StatModifier modifier, int layer = 0)
    {
        if (!Layers.ContainsKey(layer))
            Layers[layer] = new StatLayer(() => OnValueChanged?.Invoke());

        Layers[layer].AddModifier(modifier);
    }

    // ── Update ──────────────
    public void UpdateModifier(StatModifier modifier, int layer = 0)
    {
        if (Layers.ContainsKey(layer))
        {
            Layers[layer].UpdateModifier(modifier);
        }
        else
        {
            Debug.LogWarning($"[Stat] 레이어 {layer}에서 Modifier를 업데이트하려 했으나 해당 레이어가 존재하지 않습니다. SourceKey: {modifier.SourceKey}");
            AddModifier(modifier, layer);
        }
    }

    // ── Remove ──────────────
    public void RemoveModifier(string sourceKey, int layer = 0)
    {
        if (Layers.ContainsKey(layer))
        {
            Layers[layer].RemoveModifier(sourceKey);
        }
        else
        {
            Debug.LogWarning($"[Stat] 레이어 {layer}에서 Modifier를 제거하려 했으나 해당 레이어가 존재하지 않습니다. SourceKey: {sourceKey}");
        }
    }

    // ── Get ──────────────
    public StatModifier GetModifier(string sourceKey)
    {
        foreach (var layer in Layers)
        {
            var mod = layer.Value.GetModifier(sourceKey);
            if (mod != null) return mod;
        }
        return null;
    }

    // ── TryGet ──────────────
    public bool TryGetModifier(string sourceKey, out StatModifier modifier)
    {
        foreach (var layer in Layers)
        {
            var mod = layer.Value.GetModifier(sourceKey);
            if (mod != null)
            {
                modifier = mod;
                return true;
            }
        }
        modifier = null;
        return false;
    }

    //━━━━━━━━━━ Debug ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public void DebugModifierList(string debugName = "")
    {
        Debug.Log($"[🛠️{debugName}] =========== Modifier ============");
        float FlatSum = 0, PercentAddSum = 0, MultiplierProduct = 1;
        foreach (var layer in Layers)
        {
            layer.Value.DebugModifierList(BaseValue, $"Layer {layer.Key}", debugName);
            FlatSum += layer.Value.Flat;
            PercentAddSum += layer.Value.PercentAdd;
            MultiplierProduct *= layer.Value.Multiplier;
        }
        Debug.Log($"[📌{debugName}] Flat: {FlatSum}, PercentAdd: {PercentAddSum}, Multiplier: {MultiplierProduct}");
        Debug.Log($"[✅{debugName}] LayerResult: {Value}");
        Debug.Log($"[🛠️{debugName}] ========== End of Modifier ==========");
    }
}



// ═══════════════════════════════════════
//  Layer
// ═══════════════════════════════════════

public class StatLayer
{
    //              합적용 , 비율적용     , 곱적용
    protected float m_flat, m_percentAdd, m_multiplier;

    Action _parentOnValueChanged;

    bool _isDirty = true;

    // ── Layer Data ──────────────
    Dictionary<ModifierType, List<StatModifier>> Modifiers = new()
    {
        { ModifierType.Flat, new() },
        { ModifierType.PercentAdd, new() },
        { ModifierType.Multiplier, new() }
    };

    Dictionary<StatModifier, Action> _dynamicActions = new();

    // ── Property ──────────────
    public float Flat
    {
        get
        {
            if (_isDirty) Refresh();
            return m_flat;
        }
    }

    public float PercentAdd
    {
        get
        {
            if (_isDirty) Refresh();
            return m_percentAdd;
        }
    }

    public float Multiplier
    {
        get
        {
            if (_isDirty) Refresh();
            return m_multiplier;
        }
    }

    //━━━━━━━━━━ Settings ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    // 생성자
    public StatLayer(Action ValueChangedCallback)
    {
        _parentOnValueChanged = ValueChangedCallback;
    }

    //━━━━━━━━━━ Methods ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    // ── Calculate ──────────────
    public float CalculateLayer(float value)
    {
        if (_isDirty) Refresh();
        return ((value + Flat) + (value * PercentAdd)) * Multiplier;
    }

    // excludeKeys에 포함된 SourceKey 모디파이어를 빼고 즉석 계산 (캐시 미사용).
    public float CalculateLayer(float value, HashSet<string> excludeKeys)
    {
        if (excludeKeys == null || excludeKeys.Count == 0) return CalculateLayer(value);

        float flat = 0, percentAdd = 0, multiplier = 1;

        foreach (var mod in Modifiers[ModifierType.Flat])
            if (!excludeKeys.Contains(mod.SourceKey)) flat += mod.GetValue();
        foreach (var mod in Modifiers[ModifierType.PercentAdd])
            if (!excludeKeys.Contains(mod.SourceKey)) percentAdd += mod.GetValue();
        foreach (var mod in Modifiers[ModifierType.Multiplier])
            if (!excludeKeys.Contains(mod.SourceKey)) multiplier *= mod.GetValue();

        return ((value + flat) + (value * percentAdd)) * multiplier;
    }

    public float CalculateModValues(ModifierType type)
    {
        float value = 0;
        switch (type)
        {
            case ModifierType.Flat:
            case ModifierType.PercentAdd:
                foreach (var mod in Modifiers[type])
                    value += mod.GetValue();
                break;

            case ModifierType.Multiplier:
                value = 1;
                foreach (var mod in Modifiers[type])
                {
                    value *= mod.GetValue();
                }
                break;
        }
        return value;
    }

    // ── Modifier Management ──────────────

    public void AddModifier(StatModifier modifier)
    {
        foreach (var m in Modifiers[modifier.Type])
        {
            if (m.SourceKey == modifier.SourceKey)
            {
                UpdateModifier(modifier);
                return;
            }
        }

        Modifiers[modifier.Type].Add(modifier);
        Dirty(modifier.Type);

        if (modifier is DynamicModifier dynamicModifier)
        {
            Action dirtyAction = () => Dirty(modifier.Type);
            dynamicModifier.Bind(dirtyAction);
            _dynamicActions[modifier] = dirtyAction;
        }
    }

    public void UpdateModifier(StatModifier modifier)
    {
        RemoveModifier(modifier.SourceKey, modifier.Type);
        AddModifier(modifier);
    }

    public void RemoveModifier(string sourceKey)
    {
        foreach (var pair in Modifiers)
        {
            var type = pair.Key;
            var modList = pair.Value;
            bool isRemoved = false;

            for (int i = modList.Count - 1; i >= 0; i--)
            {
                var m = modList[i];
                if (m.SourceKey == sourceKey)
                {
                    if (_dynamicActions.TryGetValue(m, out Action action))
                    {
                        ((DynamicModifier)m).Unbind(action);
                        _dynamicActions.Remove(m);
                    }
                    modList.RemoveAt(i);
                    isRemoved = true;
                }
            }

            // 지워진 Mod의 타입 Dirty 처리
            if (isRemoved) Dirty(type);
        }
    }

    public void RemoveModifier(string sourceKey, ModifierType type)
    {
        if (Modifiers[type].RemoveAll(m => m.SourceKey == sourceKey) > 0)
            Dirty(type);
    }

    public StatModifier GetModifier(string sourceKey)
    {
        foreach (var weightLayer in Modifiers)
            foreach (var mod in weightLayer.Value)
                if (mod.SourceKey == sourceKey)
                    return mod;

        return null;
    }

    //━━━━━━━━━━ Dirty Flag ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    Dictionary<ModifierType, bool> _dirtyFlags = new()
    {
        { ModifierType.Flat, true },
        { ModifierType.PercentAdd, true },
        { ModifierType.Multiplier, true }
    };

    void Dirty(ModifierType type)
    {
        if (type == ModifierType.ANYTHING)
        {
            foreach (ModifierType key in _dirtyFlags.Keys)
                _dirtyFlags[key] = true;
        }
        else
            _dirtyFlags[type] = true;

        _isDirty = true;
        _parentOnValueChanged?.Invoke();
    }

    void Refresh()
    {
        if (_dirtyFlags[ModifierType.Flat])
        {
            m_flat = CalculateModValues(ModifierType.Flat);
            _dirtyFlags[ModifierType.Flat] = false;
        }
        if (_dirtyFlags[ModifierType.PercentAdd])
        {
            m_percentAdd = CalculateModValues(ModifierType.PercentAdd);
            _dirtyFlags[ModifierType.PercentAdd] = false;
        }
        if (_dirtyFlags[ModifierType.Multiplier])
        {
            m_multiplier = CalculateModValues(ModifierType.Multiplier);
            _dirtyFlags[ModifierType.Multiplier] = false;
        }
        _isDirty = false;
    }

    //━━━━━━━━━━ Debug ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public void DebugModifierList(float value, string layerName, string debugName = "")
    {
        Debug.Log($"[📌{debugName}📄] -- {layerName} --");
        foreach (var modifier in Modifiers)
        {
            foreach (var mod in modifier.Value)
            {
                Debug.Log($"[📌{debugName}📄] {mod.SourceKey} => {mod.GetValue()} ({mod.Type})");
            }
        }
        Debug.Log($"[📌{debugName}📄] Flat: {Flat}, PercentAdd: {PercentAdd}, Multiplier: {Multiplier}");
        Debug.Log($"[📌{debugName}📄✨] LayerResult: {CalculateLayer(value)}");
    }
}



// ═══════════════════════════════════════
//  Modifier
// ═══════════════════════════════════════

// ───────────────────────────────
//  기본 Modifier
// ───────────────────────────────

public class StatModifier
{
    readonly float BaseValue;

    public readonly string SourceKey;
    public readonly ModifierType Type;

    public StatModifier(float value, ModifierType type, string sourceKey)
    {
        this.SourceKey = sourceKey;
        Type = type;
        BaseValue = value;
    }

    public virtual float GetValue()
    {
        return BaseValue;
    }
}

// ───────────────────────────────
//  동적 참조 Modifier
// ───────────────────────────────

public class DynamicModifier : StatModifier
{
    private readonly Stat _sourceStat;
    private readonly float _ratio; // 반영 비율

    public DynamicModifier(Stat sourceStat, float ratio, ModifierType type, string sourceKey) : base(0, type, sourceKey)
    {
        _sourceStat = sourceStat;
        _ratio = ratio;
    }

    public void Bind(Action dirtyAction)
    {
        _sourceStat.OnValueChanged += dirtyAction;
    }
    public void Unbind(Action dirtyAction)
    {
        _sourceStat.OnValueChanged -= dirtyAction;
    }

    public override float GetValue()
    {
        return _sourceStat.Value * _ratio;
    }
}