using FishNet.Object;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    [SerializeField] private Vector3 _offset = new(0f, 5f, -7f);
    [SerializeField] private Camera _myCamera; 

    public override void OnStartNetwork()
    {
        if (base.Owner.IsLocalClient)
        {
            if (_myCamera != null)
            {
                _myCamera.gameObject.SetActive(true);
                _myCamera.enabled = true;
                _myCamera.tag = "MainCamera"; 
            }

            Camera sceneCam = GameObject.Find("Main Camera")?.GetComponent<Camera>();
            if (sceneCam != null && sceneCam != _myCamera) sceneCam.gameObject.SetActive(false);
        }
        else
        {
            if (_myCamera != null) _myCamera.gameObject.SetActive(false);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        _myCamera.transform.position = transform.position + _offset;
        _myCamera.transform.LookAt(transform.position + Vector3.up * 1.5f);
    }
}