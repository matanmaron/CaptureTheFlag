using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CTF
{
    public class PlayerColor : NetworkBehaviour
    {
        [SerializeField] Renderer renderer1;
        [SerializeField] Renderer renderer2;

        [SyncVar(hook = nameof(SetColor))] public Color32 playerColor = Color.black;
        Material cachedMaterial1;
        Material cachedMaterial2;

        private void SetColor(Color32 _, Color32 newColor)
        {
            if (cachedMaterial1 == null) cachedMaterial1 = renderer1.material;
            cachedMaterial1.color = newColor;
            if (cachedMaterial2 == null) cachedMaterial2 = renderer2.material;
            cachedMaterial2.color = newColor;
        }

        void OnDestroy()
        {
            Destroy(cachedMaterial1);
            Destroy(cachedMaterial2);
        }
    }
}