using Mirror;
using TMPro;
using UnityEngine;
using System.Collections;
using System;
using System.Linq;

namespace CTF
{
    public class PlayerGameManager : NetworkBehaviour
    {
        [SerializeField] GameObject floatingInfo;
        private SceneScript sceneScript;
        [SerializeField] GameObject canvas;
        [SerializeField] GameObject UIMenu;
        bool menuHidden = true;
        private void Awake()
        {
            sceneScript = GameObject.Find("SceneReference").GetComponent<SceneReference>().sceneScript;
        }

        private void Start()
        {
            if (!isLocalPlayer)
            {
                canvas.SetActive(false);
            }
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            playerName = $"Player {Environment.MachineName}";
            fpsCam = PlayerCamera.gameObject.AddComponent<Camera>();
            gameObject.AddComponent<AudioListener>();
            startPosition = transform.position;
            MouseLookStart();
            //floatingInfo.transform.localPosition = new Vector3(0, -0.3f, 0.6f);
            //floatingInfo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            updateHealthUIText();
            updateKDUIText();
        }

        private void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                if (menuHidden)
                {
                    Cursor.lockState = CursorLockMode.None;
                    UIMenu.SetActive(true);
                    menuHidden = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    UIMenu.SetActive(false);
                    menuHidden = true;
                }
            }
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                isBoost = true;
            }
            else if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                isBoost = false;
            }
            if (isDead)
            {
                return;
            }
            if (!menuHidden)
            {
                return;
            }
            MoveUpdate();
            MouseUpdate();
            GunUpdate();
        }

        #region MANAGER
        [HideInInspector] public Team team = Team.None;
        Vector3 startPosition;
        [SyncVar(hook = nameof(OnDeadChanged))]
        public bool isDead = false;
        public GameObject[] objectsToHide;
        [SyncVar]bool hasFlag = false;
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
                hasFlag = true;
                ServerPickFlag((int)Team.Red);
                CmdSendPlayerMessage($"{playerName} has the RED flag !");
                Debug.Log("hit flag");
            }
            else if (team == Team.Red && other.tag == Consts.BLUE_FLAG)
            {
                hasFlag = true;
                ServerPickFlag((int)Team.Blue);
                CmdSendPlayerMessage($"{playerName} has the BLUE flag !");
                Debug.Log("hit flag");
            }
            else if(hasFlag && team == Team.Blue && other.tag == Consts.BLUE_FLAG)
            {
                CmdSendPlayerMessage($"{playerName} SCORES !");
                hasFlag = false;
                ServerReturnFlag((int)Team.Red);
                CmdUpdateScore((int)Team.Blue);
            }
            else if (hasFlag && team == Team.Red && other.tag == Consts.RED_FLAG)
            {
                CmdSendPlayerMessage($"{playerName} SCORES !");
                hasFlag = false;
                ServerReturnFlag((int)Team.Blue);
                CmdUpdateScore((int)Team.Red);
            }
        }

        [Command]
        private void ServerPickFlag(int flag)
        {
            CTFServer.Instance.RPCPickFlag(flag);
        }
        [Command]
        private void ServerReturnFlag(int flag)
        {
            CTFServer.Instance.RPCReturnFlag(flag);
        }

        void OnDeadChanged(bool _Old, bool _New)
        {
            if (isDead == false) // respawn
            {
                Health = 50;
                this.transform.position = startPosition;
                if (isLocalPlayer)
                {
                    updateHealthUIText();
                    updateKDUIText();
                }
                foreach (var obj in objectsToHide)
                {
                    obj.SetActive(true);
                }
            }
            else if (isDead == true) // death
            {
                if (hasFlag)
                {
                    Team oppTeam = team == Team.Red ? Team.Red : Team.Blue;
                    ServerReturnFlag((int)oppTeam);
                }
                this.transform.position = startPosition;
                deaths++;
                updateKDUIText();
                StartCoroutine(Revive());
                foreach (var obj in objectsToHide)
                {
                    obj.SetActive(false);
                }
            }
        }

        [Command]
        public void CmdPlayerStatus(bool _value)
        {
            // player info sent to server, then server changes sync var which updates, causing hooks to fire
            isDead = _value;
        }
        private void SetNewTeam(Collider other)
        {
            if (other.tag == Consts.RED_FLAG)
            {
                team = Team.Red;
                playerColor = Color.red;
                CmdSetupPlayer(playerName, playerColor);
                CmdSendPlayerMessage($"{playerName} has joind the RED team !");
                Debug.Log($"player {netId} is now {team}");
            }
            else if (other.tag == Consts.BLUE_FLAG)
            {
                team = Team.Blue;
                playerColor = Color.blue;
                CmdSetupPlayer(playerName, playerColor);
                CmdSendPlayerMessage($"{playerName} has joind the BLUE team !");
                Debug.Log($"player {netId} is now {team}");
            }
        }
        #endregion

        #region MOVMENT
        [SerializeField] CharacterController controller;
        [SerializeField] Transform groundCheck;
        [SerializeField] LayerMask groundMask;

        Vector3 velocity;
        const float speed = 12f;
        const float groundDistance = 0.4f;
        const float gravity = -9.81f * 2;
        const float jumpHeight = 2f;
        bool isGrounded;
        bool isBoost = false;
        private void MoveUpdate()
        {
            float boost = 1;
            if (isBoost)
            {
                boost = 1.5f;
            }
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; //force player to the ground, better then 0
            }
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");
            Vector3 move = transform.right * x + transform.forward * z;
            controller.Move(move * speed * boost* Time.deltaTime);
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
            }
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        #endregion

        #region MOUSE_LOOK
        float mouseSensitivity = 300f;
        private float xRotation;
        [SerializeField] Transform PlayerCamera;
        private void MouseLookStart()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void MouseUpdate()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            PlayerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
            transform.Rotate(Vector3.up * mouseX);
        }
        #endregion

        #region COLOR AND NAME
        [SyncVar(hook = nameof(OnNameChanged))] public string playerName;
        [SyncVar(hook = nameof(OnColorChanged))] public Color playerColor = Color.white;
        [SerializeField] Material playerMaterialClone;
        [SerializeField] TextMeshPro playerNameText;
        [SerializeField] GameObject playerRepresent;
        void OnNameChanged(string _Old, string _New)
        {
            playerNameText.text = playerName;
        }

        void OnColorChanged(Color _Old, Color _New)
        {
            playerNameText.color = _New;
            playerMaterialClone = new Material(playerRepresent.GetComponent<Renderer>().material);
            playerMaterialClone.color = _New;
            playerRepresent.GetComponent<Renderer>().material = playerMaterialClone;
        }
        #endregion

        #region GUN
        [SerializeField] Camera fpsCam;
        [SerializeField] ParticleSystem flash;
        [SerializeField] GameObject impact;

        private float nextTimeToFire = 0f;

        const float range = 100f;
        const float fireRate = 10f;
        const int TIME_TO_REVIVE = 3;

        private void GunUpdate()
        {
            if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
            {
                nextTimeToFire = Time.time + 1 / fireRate;
                Shoot();
            }
        }

        private void Shoot()
        {
            if (fpsCam == null)
            {
                return;
            }
            ServerShoot(netId);
            Debug.Log($"{netId}-Shoot");
            flash.Play();
            RaycastHit hit;
            if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
            {
                Debug.Log(hit.transform.name);
                var otherPlayer = hit.transform.gameObject.GetComponent<PlayerGameManager>();
                if(otherPlayer != null)
                {
                    ServerHit(this.netId, otherPlayer.netId);
                }
            }
            CMDGunEffect(hit.point, hit.normal);
        }

        [Command]
        private void ServerShoot(uint shooter)
        {
            RpcClientShoot(shooter);
        }
        
        [Command]
        private void ServerHit(uint shooter, uint netIdToHit)
        {
            RpcClientHit(shooter, netIdToHit);
        }

        [ClientRpc]
        private void RpcClientShoot(uint shooterid)
        {
            var shooter = GameObject.FindObjectsOfType<PlayerGameManager>().FirstOrDefault(a => a.netId == shooterid);
            shooter.PlayGunshot();
        }

        [ClientRpc]
        private void RpcClientHit(uint shooter, uint netIdToHit)
        {
            var theHitedPlayer = GameObject.FindObjectsOfType<PlayerGameManager>().FirstOrDefault(a => a.netId == netIdToHit );
            if (theHitedPlayer.isLocalPlayer)
            {
                theHitedPlayer.TakeDamage(shooter);
            }
        }

        [Command]
        void CMDGunEffect(Vector3 point, Vector3 normal)
        {
            var effect = Instantiate(impact, point, Quaternion.LookRotation(normal));
            NetworkServer.Spawn(effect);
            Destroy(effect, 2);
        }

        [Command]
        public void ServerAddKill(uint shooter)
        {
            RPCAddKill(shooter);

        }

        [ClientRpc]
        private void RPCAddKill(uint shooter)
        {
            var killer = GameObject.FindObjectsOfType<PlayerGameManager>().FirstOrDefault(a => a.netId == shooter);
            if (killer.isLocalPlayer)
            {
                killer.AddKill(); 
            }
        }

        private void AddKill()
        {
            kills++;
            updateKDUIText();
        }
        #endregion

        #region Target
        [SerializeField][SyncVar] float Health = 50f;
        [SerializeField] GameObject PlayerObject;
        private int kills;
        private int deaths;

        public void TakeDamage(uint shooter)
        {
            if (isDead)
            {
                return;
            }
            Debug.Log($"{netId}-TakeDamage");
            Health -= Consts.DAMAGE;
            Debug.Log($"health is {Health}");
            updateHealthUIText();
            if (Health <= 0)
            {
                ServerAddKill(shooter);
                CmdPlayerStatus(true);
            }
            updateKDUIText();
        }

        IEnumerator Revive()
        {
            yield return new WaitForSeconds(1);
            isDead = false;
        }
        #endregion

        #region SOUND
        [SerializeField] GameObject gunSFX;
        private void PlayGunshot()
        {   
            var a = Instantiate(gunSFX, transform);
            Destroy(a, 1);
        }
        #endregion

        #region UI
        [SerializeField] TextMeshProUGUI HealthText;
        [SerializeField] TextMeshProUGUI KDText;

        public void updateHealthUIText()
        {
            HealthText.text = $"HEALTH: {Health}";
        }

        public void updateKDUIText()
        {
            KDText.text = $"{kills}-{deaths}";
        }
        #endregion

        #region SERVER
        [Command]
        public void CmdUpdateScore(int team)
        {
            if (sceneScript)
                sceneScript.UpdateScore(team);
        }

        [Command]
        public void CmdSendPlayerMessage(string msg)
        {
            if (sceneScript)
                sceneScript.statusText = $"{playerName} - {msg}";
        }

        [Command]
        public void CmdSetupPlayer(string _name, Color _col)
        {
            //player info sent to server, then server updates sync vars which handles it on all clients
            playerName = _name;
            playerColor = _col;

            sceneScript.statusText = $"{playerName} joined.";
        }
        #endregion
    }
}