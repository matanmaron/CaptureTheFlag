using Mirror;
using TMPro;
using UnityEngine;
using System.Collections;
using System;

namespace CTF
{
    public class PlayerGameManager : NetworkBehaviour
    {
        private void Start()
        {
            if (!isLocalPlayer)
            {
                Destroy(PlayerCamera.gameObject);
            }
            startPosition = transform.position;
            MouseLookStart();
        }
        private void Update()
        {
            if (isDead || !isLocalPlayer)
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
        bool isDead = false;
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
                playerColor = Color.red;
                Debug.Log($"player {netId} is now {team}");
            }
            else if (other.tag == Consts.BLUE_FLAG)
            {
                team = Team.Blue;
                playerColor = Color.blue;
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

        #region COLOR
        [SerializeField] Renderer renderer1;
        [SerializeField] Renderer renderer2;

        [SyncVar(hook = nameof(OnColorChanged))]
        public Color playerColor = Color.white;
        Material playerMaterialClone;

        private void OnColorChanged(Color _Old, Color _New)
        {
            playerMaterialClone = new Material(renderer1.material);
            playerMaterialClone.color = _New;
            renderer1.material = playerMaterialClone;
            renderer2.material = playerMaterialClone;
        }
        #endregion

        #region GUN
        [SerializeField] float damage = 10f;
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
            flash.Play();
            RaycastHit hit;
            if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
            {
                Debug.Log(hit.transform.name);
                hit.transform.GetComponent<PlayerGameManager>()?.TakeDamage(damage, AddKill);
            }
            var effect = Instantiate(impact, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(effect, 2);
        }
        private void AddKill()
        {
            kills++;
        }

        #endregion

        #region Target
        [SerializeField] float Health = 50f;
        [SerializeField] GameObject PlayerObject;
        private int kills;
        private int deaths;
        public void TakeDamage(float amount, Action killCallback)
        {
            Health -= amount;
            Debug.Log($"health is {Health}");
            if (Health <= 0)
            {
                killCallback?.Invoke();
                Die();
            }
        }

        private void Die()
        {
            deaths++;
            PlayerObject.SetActive(false);
            isDead = true;
            StartCoroutine(Revive());
        }

        IEnumerator Revive()
        {
            yield return new WaitForSeconds(TIME_TO_REVIVE);
            transform.position = startPosition;
            PlayerObject.SetActive(true);
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
            HealthText.text = $"{kills}-{deaths}";
        }
        #endregion
    }
}