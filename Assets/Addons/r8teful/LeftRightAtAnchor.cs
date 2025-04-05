using UnityEngine;

public class LeftRightAtAnchor : MonoBehaviour {
    [Range(0,2)]
    [SerializeField] private float _rockingSpeed = 1f;
    [SerializeField] private float _angle = 0.01f;
    [SerializeField] private GameObject _anchorPoint;
    [SerializeField] private Vector3Int _axis;
    private float _timeCounter;
   


    private void Update() {
        _timeCounter += Time.deltaTime * _rockingSpeed;
        float currentAngle =  Mathf.Sin(_timeCounter) * _angle;
        if (_anchorPoint == null) {
            transform.RotateAround(transform.position,Vector3.forward,currentAngle);
        } else {
            transform.RotateAround(_anchorPoint.transform.position, _axis, currentAngle);
        }
        //Quaternion currentRotation = Quaternion.Euler(0, 0, currentAngle) * initialRotation;
        //transform.rotation = currentRotation;
    }
}
