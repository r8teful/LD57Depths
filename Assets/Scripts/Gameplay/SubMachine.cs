using UnityEngine;
public enum SubMachineType {
    ControlPanell,
    Workstation,
    ResearchTable
}
// Currently just using this so I can know which machine is being fixed, so I can enable the fixing of the ladder. 
public class SubMachine : MonoBehaviour {
    public SubMachineType type; // Set in inspector!
}