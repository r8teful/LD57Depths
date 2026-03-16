using UnityEngine;

public class UISubControlPanel : MonoBehaviour {
    public bool IsOpen { get; private set; }
    [SerializeField] private GameObject _panelMain;
    [SerializeField] private UISubPanelMove _panelMoveScript;
    public Transform PanelMain => _panelMain.transform;
    private void Awake() {
        _panelMain.SetActive(false);
    }
    public void ControlPanelShow() {
        _panelMain.SetActive(true);
    }
    public void ControlPanelHide() {
        _panelMain.SetActive(false);
    }
    internal void ControlPanelToggle() {
        _panelMain.SetActive(!_panelMain.activeSelf);
    }
}