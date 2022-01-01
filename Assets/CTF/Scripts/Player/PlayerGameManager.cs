using Mirror;
using UnityEngine;

namespace CTF
{
    public class PlayerGameManager : NetworkBehaviour
    {
        [HideInInspector] public Team team = Team.None;

        private void OnTriggerEnter(Collider other)
        {
            if (!isLocalPlayer)
            {
                return;
            }
            if (team == Team.None)
            {
                SetNewTeam(other);
            }
            else if (team == Team.Blue && other.tag == Consts.RED_FLAG)
            {
                CTFServer.Instance.PickFlag(Team.Red);
                Debug.Log("hit flag");
            }
            else if (team == Team.Red && other.tag == Consts.BLUE_FLAG)
            {
                CTFServer.Instance.PickFlag(Team.Blue);
                Debug.Log("hit flag");
            }
        }
        private void SetNewTeam(Collider other)
        {
            if (other.tag == Consts.RED_FLAG)
            {
                team = Team.Red;
                gameObject.GetComponent<PlayerColor>().playerColor = Color.red;
                Debug.Log($"player {netId} is now {team}");
            }
            else if (other.tag == Consts.BLUE_FLAG)
            {
                team = Team.Blue;
                gameObject.GetComponent<PlayerColor>().playerColor = Color.blue;
                Debug.Log($"player {netId} is now {team}");
            }
        }

        private void ReturnFlag()
        {
            CTFServer.Instance.ReturnFlag(team);
        }
    }
}