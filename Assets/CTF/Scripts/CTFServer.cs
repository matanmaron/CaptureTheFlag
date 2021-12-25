using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CTF
{
    public class CTFServer : NetworkBehaviour
    {
        #region singletone
        private static CTFServer instance = null;
        public static CTFServer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CTFServer();
                }
                return instance;
            }
        }
        #endregion
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