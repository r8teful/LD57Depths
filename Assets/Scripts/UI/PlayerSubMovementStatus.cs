using TMPro;
using UnityEngine;

public class PlayerSubMovementStatus : MonoBehaviour {

    [SerializeField] private TextMeshProUGUI _playerNameText;
    
    [SerializeField] private GameObject _statusWaiting;
    [SerializeField] private GameObject _statusConfirmed;
    internal void Init(string playerName, bool accepted) {
        _playerNameText.text = playerName;
        _statusConfirmed.SetActive(accepted);
        _statusWaiting.SetActive(!accepted);
    }

}