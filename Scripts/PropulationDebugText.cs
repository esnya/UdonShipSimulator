namespace USS2
{
    using TMPro;
    using UdonSharp;
    using UnityEngine;

    [RequireComponent(typeof(TextMeshPro))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(100)] // After ScrewPropeller
    public class PropulationDebugText : UdonSharpBehaviour
    {
        public int updateInterval = 10;
        public ScrewPropeller propeller;

        private Transform propellerTransform;

        private Vector3 prevPosition;
        private float prevTime;
        private TextMeshPro textMesh;
        private int updateOffset;

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

            var t = propeller.n;
            var n = propeller.nr;
            var maxRPM = propeller.maxRPM;
            var rpmResponse = propeller.rpmResponse;
            var qa = propeller.GetAvailableTorque(vs, t);
            var qr = propeller.GetPropellerTorque(vs, n);
            var j = propeller.GetJ(vs, n);
            var eta0 = propeller.GetPropellerEfficiency(j);


            textMesh.text = string.Join("\n", new[] {
                $"Throttle:\t{t * 100.0f:F2}%",
                $"N:\t{n * 60.0f:F2}rpm",
                $"\t{n / maxRPM * 60.0f * 100.0f:F2}%",
                $"ΔN:\t{(qa - qr) * rpmResponse * 100:F2}rpm/s",
                $"Qa:\t{qa / 1000.0f:F2}kNm",
                $"Qr:\t{qr / 1000.0f:F2}kNm",
                $"\t{(qa - qr) / qa * 100:F2}%",
                $"T:\t{propeller.GetPropellerThrust(vs, n) / 1000.0f:F2}kN",
                "",
                $"Va:\t{vs:F2}m/s",
                $"J:\t{j:F2}",
                $"KT:\t{propeller.GetKT(j):F2}",
                $"KQ:\t{propeller.GetKQ(j):F2}",
                $"η0:\t{eta0:F2}",
                $"η:\t{propeller.GetEfficiency(vs) * eta0:F2}",
            });
        }
    }

}