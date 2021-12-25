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
        private const float BulletSpeed = 15.0f;
        private const float Cooldown = 1.0f;
        private float CooldownTime;
        private float BulletLife = 1.0f;
        [SyncVar(hook = nameof(SetColor))] public Color32 color = Color.black;
        Material cachedMaterial;

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
            Camera.main.transform.localPosition = new Vector3(0f, 3f, -3f);
            Camera.main.transform.localEulerAngles = new Vector3(10f, 0f, 0f);

            characterController.enabled = true;
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

        [Header("Movement Settings")]
        public float moveSpeed = 8f;
        public float turnSensitivity = 5f;
        public float maxTurnSpeed = 150f;

        [Header("Diagnostics")]
        public float horizontal;
        public float vertical;
        public float turn;
        public float jumpSpeed;
        //public bool isGrounded = true;
        //public bool isFalling;
        public Vector3 velocity;

        void Update()
        {
            if (!isLocalPlayer || characterController == null || !characterController.enabled)
                return;

            if (Input.GetKey(KeyCode.Space))
            {
                if (Time.time > CooldownTime)
                {
                    CooldownTime = Time.time + Cooldown;
                    CmdShootRay();
                }
            }

            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");

            // left and right cancel each other out, reducing the turn to zero
            if (Input.GetKey(LEFT_TURN))
                turn = Mathf.MoveTowards(turn, -maxTurnSpeed, turnSensitivity);
            if (Input.GetKey(RIGHT_TURN))
                turn = Mathf.MoveTowards(turn, maxTurnSpeed, turnSensitivity);
            if (Input.GetKey(LEFT_TURN) && Input.GetKey(RIGHT_TURN))
                turn = Mathf.MoveTowards(turn, 0, turnSensitivity);
            if (!Input.GetKey(LEFT_TURN) && !Input.GetKey(RIGHT_TURN))
                turn = Mathf.MoveTowards(turn, 0, turnSensitivity);

            //if (isGrounded)
            //    isFalling = false;

            //if ((isGrounded || !isFalling) && jumpSpeed < 1f && Input.GetKey(KeyCode.Space))
            //{
            //    jumpSpeed = Mathf.Lerp(jumpSpeed, 1f, 0.5f);
            //}
            //else if (!isGrounded)
            //{
            //    isFalling = true;
            //    jumpSpeed = 0;
            //}


        }

        void FixedUpdate()
        {
            if (!isLocalPlayer || characterController == null || !characterController.enabled)
                return;

            transform.Rotate(0f, turn * Time.fixedDeltaTime, 0f);

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
            GameObject bullet = Instantiate(Bullet, BulletFirePosition.position, BulletFirePosition.rotation);
            bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * BulletSpeed;
            Destroy(bullet, BulletLife);
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
