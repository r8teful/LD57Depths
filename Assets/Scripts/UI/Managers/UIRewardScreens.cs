using UnityEngine;

public class UIRewardScreens : MonoBehaviour {
    [SerializeField] private UIShrineRewardScreen _shrineScreen;
    [SerializeField] private UIChestRewardScreen _chestScreen;

    public bool IsOpen => _shrineScreen.IsOpen || _chestScreen.IsOpen;
}