using System.Collections;
using UnityEngine;
public enum SubMachineType {
    ControlPanell,
    Workstation,
    ResearchTable
}
public class SubMachine : MonoBehaviour {
    public SubMachineType type; // Set in inspector!


}