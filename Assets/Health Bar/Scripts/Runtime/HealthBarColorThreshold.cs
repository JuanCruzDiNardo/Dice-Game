using System;
using UnityEngine;

namespace HandDrawnHealthBar
{
    [Serializable]
    public sealed class HealthBarColorThreshold
    {
        [SerializeField, Range(0f, 1f)]
        [Tooltip("Highest normalized health value that uses this color.")]
        private float maximumNormalizedHealth = 1f;

        [SerializeField]
        private Color color = Color.green;

        public float MaximumNormalizedHealth => maximumNormalizedHealth;
        public Color Color => color;

        public HealthBarColorThreshold(float maximumHealthRatio, Color thresholdColor)
        {
            maximumNormalizedHealth = Mathf.Clamp01(maximumHealthRatio);
            color = thresholdColor;
        }

        public void Validate()
        {
            maximumNormalizedHealth = Mathf.Clamp01(maximumNormalizedHealth);
        }
    }
}
