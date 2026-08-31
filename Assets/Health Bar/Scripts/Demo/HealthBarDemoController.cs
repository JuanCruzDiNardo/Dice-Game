using System.Collections;
using UnityEngine;

namespace HandDrawnHealthBar
{
    [DisallowMultipleComponent]
    public sealed class HealthBarDemoController : MonoBehaviour
    {
        [SerializeField]
        private HealthBarController healthBar;

        [SerializeField, Min(0.01f)]
        private float healthStep = 10f;

        [SerializeField, Min(0.01f)]
        private float stepInterval = 0.28f;

        [SerializeField, Min(0f)]
        private float endpointPause = 0.9f;

        private Coroutine demoRoutine;

        private void OnEnable()
        {
            if (healthBar != null)
                demoRoutine = StartCoroutine(RunDemoLoop());
        }

        private void OnDisable()
        {
            if (demoRoutine == null)
                return;

            StopCoroutine(demoRoutine);
            demoRoutine = null;
        }

        public void DamageOneStep()
        {
            healthBar?.TakeDamage(healthStep);
        }

        public void HealOneStep()
        {
            healthBar?.Heal(healthStep);
        }

        public void ResetBar()
        {
            healthBar?.RestoreFullHealth();
        }

        public void Configure(HealthBarController controller)
        {
            healthBar = controller;
        }

        private IEnumerator RunDemoLoop()
        {
            while (true)
            {
                healthBar.RestoreFullHealth();
                yield return WaitUnscaled(endpointPause);

                while (!healthBar.IsEmpty)
                {
                    healthBar.TakeDamage(healthStep);
                    yield return WaitUnscaled(stepInterval);
                }

                yield return WaitUnscaled(endpointPause);

                while (!healthBar.IsFull)
                {
                    healthBar.Heal(healthStep);
                    yield return WaitUnscaled(stepInterval);
                }

                yield return WaitUnscaled(endpointPause);
            }
        }

        private static IEnumerator WaitUnscaled(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
