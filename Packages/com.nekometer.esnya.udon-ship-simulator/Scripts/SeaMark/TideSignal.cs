using JetBrains.Annotations;
using TMPro;
using UdonSharp;
using UnityEngine;

namespace USS2
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(100)] // Afet Flow
    public class TideSignal : UdonSharpBehaviour
    {
        /// <summary>
        /// Flow to indicate. Get from parents if null.
        /// </summary>
        [CanBeNull] public Flow flow;

        /// <summary>
        /// Signal objects.
        /// </summary>
        [NotNull][ItemNotNull] public GameObject[] speedDigits = { };

        /// <summary>
        /// Signal objects.
        /// </summary>
        public GameObject speed, directionForward, directionReversed, trendIncreasing, trendDecreasing, trendTransition;

        public TextMeshPro debugText;

        private Ocean ocean;
        private int direction;
        private int speedTransition;
        private int prevSpeed;
        private int prevState;
        private bool initialized;

        private void Start()
        {
            if (!flow) flow = GetComponentInParent<Flow>();
            ocean = flow.GetComponentInParent<Ocean>();
        }

        private void Update()
        {
            var speed = flow.speed * ocean.tideFlow;
            UpdateSpeed(Mathf.Clamp(Mathf.RoundToInt(speed * 1.944f), -9, 9));
            UpdateState(Mathf.FloorToInt(Time.time / 2.0f) % 6);
        }

        private void UpdateSpeed(int speed)
        {
            if (speed == prevSpeed && initialized) return;
            initialized = true;

            for (var i = 0; i <= 9; i++)
            {
                var digit = speedDigits[i];
                if (!digit) continue;
                digit.SetActive(i == speed);
            }

            direction = Mathf.RoundToInt(Mathf.Sign(speed == 0 ? prevSpeed : speed));
            speedTransition = speed == 0 ? 0 : Mathf.Abs(speed) - Mathf.Abs(prevSpeed);
            if (debugText) debugText.text = $"{speed}m/s\n{direction} {speedTransition}";

            prevSpeed = speed;
        }

        private void UpdateState(int state)
        {
            if (state == prevState) return;

            var showDirection = state == 0;
            var showSpeed = state == 2;
            var showTransition = state == 4;

            directionForward.SetActive(showDirection && direction == 1);
            directionReversed.SetActive(showDirection && direction == -1);

            speed.SetActive(showSpeed);

            trendIncreasing.SetActive(showTransition && speedTransition > 0);
            trendDecreasing.SetActive(showTransition && speedTransition < 0);
            trendTransition.SetActive(showTransition && speedTransition == 0);

            prevState = state;
        }
    }
}
