using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Compass : MonoBehaviour {
    [Header("References")]
    private Transform _player;
    [SerializeField] private Image _compasImage;
    [SerializeField] private CompassArrow arrowHandPrefab;

    // Keyed by target transform for easy lookup and removal
    private readonly Dictionary<Transform, CompassArrow> _arrows = new();

    private void Awake() {
        _compasImage.gameObject.SetActive(false);
    }
    public void Activate(PlayerManager player) {
        _player = player.transform;
        _compasImage.gameObject.SetActive(true);
        if (SubmarineManager.Instance != null)
            AddArrowHand(SubmarineManager.Instance.submarineExterior.transform);
    }
    private void LateUpdate() {
        foreach (var hand in _arrows.Values)
            hand.UpdateArrow();
    }

    public CompassArrow AddArrowHand(Transform target) {
        if (_arrows.ContainsKey(target)) {
            Debug.LogWarning($"[Compass] Arrow hand for target '{target.name}' already exists.");
            return _arrows[target];
        }

        CompassArrow hand = Instantiate(arrowHandPrefab, transform);
        hand.Init(_player, target);
        _arrows[target] = hand;
        return hand;
    }

    public void RemoveArrowHand(Transform target) {
        if (!_arrows.TryGetValue(target, out CompassArrow hand)) return;

        Destroy(hand.gameObject);
        _arrows.Remove(target);
    }

    public void ClearAllArrowHands() {
        foreach (var hand in _arrows.Values)
            Destroy(hand.gameObject);

        _arrows.Clear();
    }

}