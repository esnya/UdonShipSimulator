
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace UdonShipSimulator
{
    [RequireComponent(typeof(ParticleSystem)), RequireComponent(typeof(AudioSource))]
    public class GunController : UdonSharpBehaviour
    {
        [Tooltip("fire/min")] public float fireRate = 20.0f;
        [Tooltip("N")] public float reactionaryForce = 0.001f;
        public AudioClip fireSound;
        private GameObject root;
        private void Start()
        {
            var rigidbody = GetComponentInParent<Rigidbody>();
            if (rigidbody) root = rigidbody.gameObject;

        }

        private bool ready = true;
        public void Ready()
        {
            ready = true;
        }

        public void PlayFireEffect()
        {
            GetComponent<ParticleSystem>().Emit(1);
            if (fireSound != null) GetComponent<AudioSource>().PlayOneShot(fireSound);
        }

        private void SendHitMessage(GameObject obj)
        {
            if (obj == null || obj == root) return;
            Debug.Log($"Hit: {gameObject.name} -> {obj.name}");
            var rigidbody = obj.GetComponentInParent<Rigidbody>();
            if (rigidbody == null) return;
            var udon = (UdonBehaviour)rigidbody.GetComponent(typeof(UdonBehaviour));
            if (udon == null) return;
            udon.SendCustomNetworkEvent(NetworkEventTarget.Owner, "BulletHit");
        }

        private void OnParticleCollision(GameObject other) {
            if (other != null) SendHitMessage(other);
        }

        public void Fire()
        {
            if (!ready) return;
            ready = false;
            GetComponentInParent<Rigidbody>().AddForceAtPosition(-transform.forward * reactionaryForce, transform.position, ForceMode.Force);
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayFireEffect));
            SendCustomEventDelayedSeconds(nameof(Ready), 60.0f / fireRate);
        }
    }
}
