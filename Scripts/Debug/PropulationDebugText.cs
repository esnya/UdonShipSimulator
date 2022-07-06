namespace USS2
{
    using TMPro;
    using UdonSharp;
    using UnityEngine;
    using VRC.SDKBase;

    [RequireComponent(typeof(TextMeshPro))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    [DefaultExecutionOrder(100)] // After ScrewPropeller
    public class PropulationDebugText : UdonSharpBehaviour
    {
        public int updateInterval = 45;
        public ScrewPropeller propeller;
        public SteamTurbine turbine;

        private Transform propellerTransform;

        private Vector3 prevPosition;
        private float prevTime;
        private TextMeshPro textMesh;
        private int updateOffset;
        [UdonSynced] private float t;
        [UdonSynced] private float n;

        private void Start()
        {
            textMesh = GetComponent<TextMeshPro>();

            updateOffset = UnityEngine.Random.Range(0, updateInterval);
            propellerTransform = propeller.transform;
        }

        private void Update()
        {
            if ((Time.renderedFrameCount + updateOffset) % updateInterval != 0) return;

            var time = Time.time;
            var position = propellerTransform.position;
            var vs = Vector3.Dot(position - prevPosition, propellerTransform.forward) / (time - prevTime);
            prevTime = time;
            prevPosition = position;

            var shaft = turbine.shaft;

            if (Networking.IsOwner(propeller.gameObject))
            {
                t = turbine.input;
                n = shaft.n;
            }
            var maxRPM = turbine.rpm;
            var momentOfInertia = shaft.momentOfInertia;
            var qa = turbine.GetAvailableTorque(t);
            var qr = propeller.GetPropellerTorque(vs, n) / propeller.GetEfficiency(vs);
            var j = propeller.GetJ(vs, n);
            var eta0 = propeller.GetPropellerEfficiency(j);

            textMesh.text = string.Join("\n", new[] {
                $"Throttle:\t{t * 100.0f:F0}%",
                $"N:\t{n * 60.0f:F2}rpm",
                $"\t{n / maxRPM * 60.0f * 100.0f:F2}%",
                $"ΔN:\t{(qa - qr) / momentOfInertia * 100:F2}rpm/s",
                $"Qa:\t{qa / 1000.0f:F2}kNm",
                $"Qr:\t{qr / 1000.0f:F2}kNm",
                $"\t{(qa - qr) / qa * 100:F2}%",
                $"T:\t{propeller.GetPropellerThrust(vs, n) / 1000.0f:F2}kN",
                "",
                $"Va:\t{vs:F2}m/s",
                $"J:\t{j:F4}",
                $"KT:\t{propeller.GetKT(j):F4}",
                $"KQ:\t{propeller.GetKQ(j):F4}",
                $"η0:\t{eta0:F4}",
                $"η:\t{propeller.GetEfficiency(vs) * eta0:F4}",
            });
        }
    }
}