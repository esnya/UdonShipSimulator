
using UdonSharp;
using UdonToolkit;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace UdonShipSimulator
{
    [RequireComponent(typeof(AudioSource))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class GunController : UdonSharpBehaviour
    {
        [ListView("Particles")] public ParticleSystem[] particles = { };
        [ListView("Particles")] public int[] emissions = { 1 };

        [Tooltip("fire/min")] public float fireRate = 20.0f;
        [Tooltip("N")] public float reactionaryForce = 0.001f;
        public AudioClip fireSound;
        private GameObject root;
        private int particleCount;
        private new Rigidbody rigidbody;
        private void Start()
        {
            rigidbody = GetComponentInParent<Rigidbody>();
            if (rigidbody) root = rigidbody.gameObject;
            particleCount = Mathf.Min(particles.Length, emissions.Length);
        }

        private bool ready = true;
        public void Ready()
        {
            ready = true;
        }

        public void PlayFireEffect()
        {
            if (fireSound != null) GetComponent<AudioSource>().PlayOneShot(fireSound);
            for (int i = 0; i < particleCount; i++)
            {
                particles[i].Emit(emissions[i]);
            }
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

        private void OnParticleCollision(GameObject other)
        {
            if (other != null) SendHitMessage(other);
        }

        public void Fire()
        {
            if (!ready) return;
            ready = false;

            if (rigidbody != null && Networking.IsOwner(rigidbody.gameObject)) rigidbody.AddForceAtPosition(-transform.forward * reactionaryForce, transform.position, ForceMode.Force);
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayFireEffect));
            SendCustomEventDelayedSeconds(nameof(Ready), GetIntervalSeconds());
        }

        public float GetIntervalSeconds()
        {
            return 60.0f / fireRate;
        }
    }
}
