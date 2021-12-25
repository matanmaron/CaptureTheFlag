using Mirror;
using UnityEngine;

namespace CTF
{ 
    public class Projectile : NetworkBehaviour
    {
        public float destroyAfter = 5;

        public override void OnStartServer()
        {
            Invoke(nameof(DestroySelf), destroyAfter);
        }

        // destroy for everyone on the server
        [Server]
        void DestroySelf()
        {
            NetworkServer.Destroy(gameObject);
        }

        // ServerCallback because we don't want a warning if OnTriggerEnter is
        // called on the client
        [ServerCallback]
        void OnTriggerEnter(Collider co)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}
