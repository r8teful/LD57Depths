using System.Collections.Generic;
using UnityEngine;

public class UIRepairs : MonoBehaviour {
    public RepairSO repairData;
    public GameObject RepairButton;
    public bool IsComplete { get; set; }
    public List<GameObject> _instantiatedShipCosts = new List<GameObject>();
    public void SetRepairDataCost() {
        foreach (var item in _instantiatedShipCosts) {
            Destroy(item);
        }
        _instantiatedShipCosts.Clear();
        foreach (var cost in repairData.costData) {
            var i = Instantiate(Resources.Load<UIResourceElement>("UI/UIRepairResource"), transform);
            i.Init(cost.resourceType,Mathf.FloorToInt(cost.baseCost));
            _instantiatedShipCosts.Add(i.gameObject);
        }
        RepairButton.transform.SetAsLastSibling();
    }
    public void RepairPressed() {
        ShipManager.Instance.RepairPressed(repairData.RepairType,repairData.costData);
    }
}
