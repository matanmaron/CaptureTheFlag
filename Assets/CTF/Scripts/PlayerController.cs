using Mirror;
using UnityEngine;

namespace CTF
{
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : NetworkBehaviour
    {
        public CharacterController characterController;
        public GameObject Bullet;
        public Transform BulletFirePosition;
        public Team team = Team.None;
        private const KeyCode LEFT_TURN = KeyCode.A;
        private const KeyCode RIGHT_TURN = KeyCode.D;
        private const float BULLET_SPEED = 100.0f;
        private const float COOLDOWN = 0.3f;
        private float CooldownTime;
        [SyncVar(hook = nameof(SetColor))] public Color32 color = Color.black;
        Material cachedMaterial;


        [Header("Movement Settings")]
        public float moveSpeed = 8f;
        public float turnSensitivity = 5f;
        public float maxTurnSpeed = 150f;

        [Header("Diagnostics")]
        public float horizontal;
        public float vertical;
        public float jumpSpeed;
        public Vector3 velocity;

        float lookSpeed = 3;
        Vector2 rotation = Vector2.zero;
        bool menu = false;

        void OnValidate()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            characterController.enabled = false;
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<NetworkTransform>().clientAuthority = true;
        }

        public override void OnStartLocalPlayer()
        {
            Camera.main.orthographic = false;
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0f, 3f, 0);

            characterController.enabled = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Look()
        {
            rotation.y += Input.GetAxis("Mouse X");
            rotation.x += -Input.GetAxis("Mouse Y");
            rotation.x = Mathf.Clamp(rotation.x, -15f, 15f);
            transform.eulerAngles = new Vector2(0, rotation.y) * lookSpeed;
            Camera.main.transform.localRotation = Quaternion.Euler(rotation.x * lookSpeed, 0, 0);
        }

        void OnDisable()
        {
            if (isLocalPlayer && Camera.main != null)
            {
                Camera.main.orthographic = true;
                Camera.main.transform.SetParent(null);
                Camera.main.transform.localPosition = new Vector3(0f, 70f, 0f);
                Camera.main.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            }
        }

        void Update()
        {
            if (!isLocalPlayer || characterController == null || !characterController.enabled)
                return;

            if (Input.GetButton("Cancel"))
            {
                menu = !menu;
                if (menu)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }

            if (Input.GetButton("Fire1"))
            {
                if (Time.time > CooldownTime)
                {
                    CooldownTime = Time.time + COOLDOWN;
                    CmdShootRay();
                }
            }

            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");

            Look();
        }

        void FixedUpdate()
        {
            if (!isLocalPlayer || characterController == null || !characterController.enabled)
                return;

            Vector3 direction = new Vector3(horizontal, jumpSpeed, vertical);
            direction = Vector3.ClampMagnitude(direction, 1f);
            direction = transform.TransformDirection(direction);
            direction *= moveSpeed;

            if (jumpSpeed > 0)
                characterController.Move(direction * Time.fixedDeltaTime);
            else
                characterController.SimpleMove(direction);

            //isGrounded = characterController.isGrounded;
            velocity = characterController.velocity;
        }

        [Command]
        void CmdShootRay()
        {
            RpcFireWeapon();
        }

        [ClientRpc]
        void RpcFireWeapon()
        {
            //bulletAudio.Play(); muzzleflash  etc
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
            RaycastHit hit;
            Vector3 targetPoint;
            if (Physics.Raycast(ray, out hit))
                targetPoint = hit.point;
            else
                targetPoint = ray.GetPoint(1000); // You may need to change this value according to your needs
            GameObject bullet = Instantiate(Bullet, BulletFirePosition.position, BulletFirePosition.rotation);
            bullet.GetComponent<Rigidbody>().velocity = (targetPoint - bullet.transform.position).normalized * BULLET_SPEED;
        }

        [ServerCallback]
        private void OnTriggerEnter(Collider other)
        {
            if (team == Team.None)
            {
                SetNewTeam(other);
            }
            else if (team == Team.Blue && other.tag == Consts.RED_FLAG)
            {
                Debug.Log("hit flag");
            }
            else if (team == Team.Red && other.tag == Consts.BLUE_FLAG)
            {
                Debug.Log("hit flag");
            }
        }

        private void SetNewTeam(Collider other)
        {
            if (other.tag == Consts.RED_FLAG)
            {
                team = Team.Red;
                color = Color.red;
                Debug.Log($"player {netId} is now {team}");
            }
            else if (other.tag == Consts.BLUE_FLAG)
            {
                team = Team.Blue;
                color = Color.blue;
                Debug.Log($"player {netId} is now {team}");
            }
        }

        void SetColor(Color32 _, Color32 newColor)
        {
            if (cachedMaterial == null) cachedMaterial = GetComponentInChildren<Renderer>().material;
            cachedMaterial.color = newColor;
        }

        void OnDestroy()
        {
            Destroy(cachedMaterial);
        }
    }
}
