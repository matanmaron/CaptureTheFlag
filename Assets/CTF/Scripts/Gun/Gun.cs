using Mirror;
using UnityEngine;

namespace CTF
{
    public class Gun : MonoBehaviour
    {
        [SerializeField] float damage = 10f;
        public Camera fpsCam;
        public ParticleSystem flash;
        public GameObject impact;

        const float range = 100f;
        const float fireRate = 10f;

        private float nextTimeToFire = 0f;

        private void Update()
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
                hit.transform.GetComponent<Target>()?.TakeDamage(damage);
            }
            var effect = Instantiate(impact, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(effect, 2);
        }
    }
}