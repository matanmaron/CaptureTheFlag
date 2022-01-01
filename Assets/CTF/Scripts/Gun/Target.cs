using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CTF
{
    public class Target : MonoBehaviour
    {
        [SerializeField] float Health = 50f;

        public void TakeDamage(float amount)
        {
            Health -= amount;
            Debug.Log($"health is {Health}");
            if (Health <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Destroy(gameObject);
        }
    }
}