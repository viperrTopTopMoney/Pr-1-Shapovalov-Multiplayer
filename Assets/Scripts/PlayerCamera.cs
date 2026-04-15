using Unity.Netcode;
using Unity.Netcode;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    [SerializeField] private Vector3 _offset = new(0f, 5f, -7f);
    
    // Сюда в инспекторе префаба перетащи камеру, которую мы создали внутри игрока
    [SerializeField] private Camera _myCamera; 

    public override void OnNetworkSpawn()
    {
        // Проверяем: этот объект принадлежит МНЕ (локальному игроку)?
        if (IsOwner)
        {
            // 1. Включаем только СВОЮ камеру
            if (_myCamera != null)
            {
                _myCamera.gameObject.SetActive(true);
                _myCamera.enabled = true;
                
                // Делаем её главной, чтобы стандартные скрипты Unity её видели
                _myCamera.tag = "MainCamera"; 
            }

            // 2. Ищем на сцене любую "стартовую" камеру (которая была там до спавна игрока) и выключаем её
            Camera sceneCam = GameObject.Find("Main Camera")?.GetComponent<Camera>();
            if (sceneCam != null && sceneCam != _myCamera) 
            {
                sceneCam.gameObject.SetActive(false);
            }
        }
        else
        {
            // Если это чужой игрок — выключаем его камеру полностью, чтобы она не мешала
            if (_myCamera != null)
            {
                _myCamera.gameObject.SetActive(false);
            }
            
            // Выключаем сам этот скрипт, чтобы LateUpdate не тратил ресурсы на чужих игроков
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        // Благодаря 'enabled = false' выше, этот код будет работать ТОЛЬКО у владельца
        if (_myCamera == null) return;

        _myCamera.transform.position = transform.position + _offset;
        _myCamera.transform.LookAt(transform.position + Vector3.up * 1.5f);
    }
}