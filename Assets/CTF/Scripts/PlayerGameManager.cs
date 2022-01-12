using Mirror;
using TMPro;
using UnityEngine;
using System.Collections;
using System;

namespace CTF
{
    public class PlayerGameManager : NetworkBehaviour
    {
        [SerializeField] GameObject floatingInfo;
        private SceneScript sceneScript;
        [SerializeField] GameObject canvas;
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
            startPosition = transform.position;
            MouseLookStart();
            floatingInfo.transform.localPosition = new Vector3(0, -0.3f, 0.6f);
            floatingInfo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            updateHealthUIText();
            updateKDUIText();
        }

        private void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }
            if (Input.GetKey(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }
            if (isDead)
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
                CmdSendPlayerMessage($"{playerName} has the RED flag !");
                Debug.Log("hit flag");
            }
            else if (team == Team.Red && other.tag == Consts.BLUE_FLAG)
            {
                CTFServer.Instance.PickFlag(Team.Blue);
                CmdSendPlayerMessage($"{playerName} has the BLUE flag !");
                Debug.Log("hit flag");
            }
        }

        void OnDeadChanged(bool _Old, bool _New)
        {
            if (isDead == false) // respawn
            {
                foreach (var obj in objectsToHide)
                {
                    obj.SetActive(true);
                }

                if (isLocalPlayer)
                {
                    // Uses NetworkStartPosition feature, optional.
                    this.transform.position = startPosition;
                    Health = 50;
                    updateHealthUIText();
                    updateKDUIText();
                }
            }
            else if (isDead == true) // death
            {
                // have meshes hidden, disable movement and show respawn button
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

        private void ReturnFlag()
        {
            CTFServer.Instance.ReturnFlag(team);
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
        const float jumpHeight = 3f;
        bool isGrounded;

        private void MoveUpdate()
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; //force player to the ground, better then 0
            }
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");
            Vector3 move = transform.right * x + transform.forward * z;
            controller.Move(move * speed * Time.deltaTime);
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
            }
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        #endregion

        #region MOUSE_LOOK
        public float mouseSensitivity = 100f;
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
        [SerializeField] TextMesh playerNameText;
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
                Shoot(gameObject);
            }
        }

        [Command]
        private void Shoot(GameObject source)
        {
            Debug.Log($"{netId}-Shoot");
            flash.Play();
            RaycastHit hit;
            if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
            {
                Debug.Log(hit.transform.name);
                hit.transform.gameObject.GetComponent<PlayerGameManager>()?.TakeDamage(source);
            }
            var effect = Instantiate(impact, hit.point, Quaternion.LookRotation(hit.normal));
            NetworkServer.Spawn(effect);
            Destroy(effect, 2);
        }

        public void AddKill()
        {
            kills++;
        }

        #endregion

        #region Target
        [SerializeField] float Health = 50f;
        [SerializeField] GameObject PlayerObject;
        private int kills;
        private int deaths;

        [ClientRpc]
        public void TakeDamage(GameObject source)
        {
            Debug.Log($"{netId}-TakeDamage");
            Health -= Consts.DAMAGE;
            Debug.Log($"health is {Health}");
            updateHealthUIText();
            if (Health <= 0)
            {
                source.GetComponent<PlayerGameManager>()?.AddKill();
                Die();
                updateKDUIText();
            }
        }

        private void Die()
        {
            if (!isLocalPlayer)
            {
                Debug.Log("not you");
                return;
            }
            Debug.Log("you dead");
            deaths++;
            CmdPlayerStatus(true);
        }

        [ClientRpc]
        public void RPCRevive()
        {
            transform.position = startPosition;
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