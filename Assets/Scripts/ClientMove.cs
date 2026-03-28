using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
public class ClientMove : NetworkTransform
{
    // Этот метод говорит Unity: "Если это владелец (Owner), разреши ему менять позицию"
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}