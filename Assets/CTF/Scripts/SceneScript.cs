using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CTF
{
    public class SceneScript : NetworkBehaviour
    {
        public Text canvasStatusText;
        [SyncVar(hook = nameof(OnStatusTextChanged))]
        public string statusText;

        public TextMeshProUGUI canvasScoreText;
        [SyncVar(hook = nameof(UpdateScoreText))]
        public int BlueScore = 0;
        [SyncVar(hook = nameof(UpdateScoreText))]
        public int RedScore = 0;

        private void Start()
        {
            canvasScoreText.text = $"<color=\"red\">{RedScore}</color>-<color=\"blue\">{BlueScore}</color>";
        }

        void OnStatusTextChanged(string _Old, string _New)
        {
            //called from sync var hook, to update info on screen for all players
            canvasStatusText.text = statusText;
        }

        public void UpdateScore(int flagColor)
        {
            if ((Team)flagColor == Team.Red)
            {
                RedScore++;
            }
            else if ((Team)flagColor == Team.Blue)
            {
                BlueScore++;
            }
        }

        void UpdateScoreText(int _Old, int _New)
        {
            canvasScoreText.text = $"<color=\"red\">{RedScore}</color>-<color=\"blue\">{BlueScore}</color>";
        }
    }
}