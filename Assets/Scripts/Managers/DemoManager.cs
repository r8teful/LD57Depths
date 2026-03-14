using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Only exists if we are playing the demo
public class DemoManager : StaticInstance<DemoManager> {
    [SerializeField] private List<UpgradeNodeSO> _lockedNodes = new List<UpgradeNodeSO>(); // asigned in the inspector
    private HashSet<ushort> _lockedNodeIDs;

    public HashSet<ushort> LockedNodes => _lockedNodeIDs;

    protected override void Awake() {
        base.Awake();
        if (!App.isDemo) Destroy(gameObject);
        _lockedNodeIDs = _lockedNodes
            .Select(n => n.ID)
            .ToHashSet();
    }
}