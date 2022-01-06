using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CTF
{
    public class CTFServer : NetworkBehaviour
    {
        #region Singleton
        public static CTFServer Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
            }
            DontDestroyOnLoad(this.gameObject);
        }
        #endregion

        GameObject redFlag;
        GameObject blueFlag;

        private void Start()
        {
            redFlag = GameObject.FindGameObjectWithTag(Consts.RED_FLAG);
            blueFlag = GameObject.FindGameObjectWithTag(Consts.BLUE_FLAG);
        }

        public void PickFlag(Team flagColor)
        {
            SetFlag(flagColor, false);
        }

        public void ReturnFlag(Team flagColor)
        {
            SetFlag(flagColor, true);
        }

        private void SetFlag(Team flagColor, bool state)
        {
            if (flagColor == Team.Blue)
            {
                blueFlag.SetActive(state);
            }
            else if (flagColor == Team.Red)
            {
                redFlag.SetActive(state);
            }
        }
    }


    public enum Team
    {
        None,
        Blue,
        Red
    }

    public static class Consts
    {
        public const string RED_FLAG = "RedFlag";
        public const string BLUE_FLAG = "BlueFlag";
    }
}
