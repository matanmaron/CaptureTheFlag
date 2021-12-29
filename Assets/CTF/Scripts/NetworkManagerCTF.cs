using Mirror;
using UnityEngine;

namespace CTF
{
    public class NetworkManagerCTF : NetworkManager
    {
        [SerializeField] GameObject ctfServer;
        public override void OnStartServer()
        {
            base.OnStartServer();
            if (FindObjectOfType<CTFServer>() == null)
            {
                var s = Instantiate(ctfServer);
                NetworkServer.Spawn(s);
            }
        }
    }
}