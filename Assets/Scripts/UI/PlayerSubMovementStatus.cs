using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSubMovementStatus : MonoBehaviour {
    private int _cachedOwnerID; // When a client confirms/denies the req, they'll just send their ownerID, which
                                // We'll compare with this variable

    [SerializeField] private TextMeshProUGUI _playerNameText;
    
    [SerializeField] private GameObject _statusWaiting;
    [SerializeField] private GameObject _statusConfirmed;
    internal void Init(string playerName, int ownerId) {
        _cachedOwnerID = ownerId;
        _playerNameText.text = playerName;
        _statusWaiting.SetActive(true);
        _statusConfirmed.SetActive(false);
    }

}