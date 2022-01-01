using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CTF
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] CharacterController controller;
        [SerializeField] Transform groundCheck;
        [SerializeField] LayerMask groundMask;

        Vector3 velocity;
        const float speed = 12f;
        const float groundDistance = 0.4f;
        const float gravity = -9.81f * 2;
        const float jumpHeight = 3f;
        bool isGrounded;

        void Update()
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
    }
}