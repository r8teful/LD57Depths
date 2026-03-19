using System.Collections;
using UnityEngine;
public class CompassArrow : MonoBehaviour {
    private Transform _player;
    private Transform _target;

    public Transform Target => _target;

    public void Init(Transform player, Transform target) {
        _player = player;
        _target = target;
    }

    public void UpdateArrow() {
        if (_player == null || _target == null) return;

        Vector3 direction = _target.position - _player.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) return;

        float worldAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float relativeAngle = worldAngle - _player.eulerAngles.y;

        transform.localEulerAngles = new Vector3(0f, 0f, -relativeAngle);
    }

}