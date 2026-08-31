using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HandDrawnHealthBar
{
    [DisallowMultipleComponent]
    public sealed class HealthBarController : MonoBehaviour
    {
        private const float MinimumMaximumHealth = 0.0001f;

        [Header("Health")]
        [SerializeField, Min(MinimumMaximumHealth)]
        private float maximumHealth = 100f;

        [SerializeField, Min(0f)]
        private float currentHealth = 100f;

        [Header("Visual References")]
        [SerializeField]
        [Tooltip("RectTransform whose horizontal anchors represent the current health ratio.")]
        private RectTransform fillRect;

        [SerializeField]
        private Graphic fillGraphic;

        [SerializeField]
        private Text valueLabel;

        [SerializeField]
        private bool showNumericValue = true;

        [Header("Color Thresholds")]
        [SerializeField]
        [Tooltip("The first matching maximum ratio is used. Defaults: red up to 30%, yellow up to 65%, green up to 100%.")]
        private List<HealthBarColorThreshold> colorThresholds = CreateDefaultThresholds();

        public event Action<float, float> HealthChanged;

        public float MaximumHealth => maximumHealth;
        public float CurrentHealth => currentHealth;
        public float NormalizedHealth => maximumHealth > 0f
            ? Mathf.Clamp01(currentHealth / maximumHealth)
            : 0f;
        public bool IsEmpty => currentHealth <= 0f;
        public bool IsFull => Mathf.Approximately(currentHealth, maximumHealth);

        private void OnEnable()
        {
            ValidateState();
            RefreshVisuals();
        }

        private void OnValidate()
        {
            ValidateState();
            RefreshVisuals();
        }

        public void SetCurrentHealth(float value)
        {
            SetHealthInternal(value, maximumHealth);
        }

        public void SetHealth(float value, float newMaximumHealth)
        {
            SetHealthInternal(value, newMaximumHealth);
        }

        public void SetMaximumHealth(float value)
        {
            SetHealthInternal(currentHealth, value);
        }

        public void SetMaximumHealthPreservingRatio(float value)
        {
            float preservedRatio = NormalizedHealth;
            float validatedMaximum = Mathf.Max(MinimumMaximumHealth, value);
            SetHealthInternal(validatedMaximum * preservedRatio, validatedMaximum);
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f)
                return;

            SetCurrentHealth(currentHealth - amount);
        }

        public void Heal(float amount)
        {
            if (amount <= 0f)
                return;

            SetCurrentHealth(currentHealth + amount);
        }

        public void RestoreFullHealth()
        {
            SetCurrentHealth(maximumHealth);
        }

        public void RefreshVisuals()
        {
            float ratio = NormalizedHealth;

            if (fillRect != null)
            {
                Vector2 anchorMax = fillRect.anchorMax;
                anchorMax.x = ratio;
                fillRect.anchorMax = anchorMax;
            }

            if (fillGraphic != null)
                fillGraphic.color = EvaluateColor(ratio);

            if (valueLabel != null)
            {
                valueLabel.gameObject.SetActive(showNumericValue);
                if (showNumericValue)
                    valueLabel.text = $"{currentHealth:0} / {maximumHealth:0}";
            }
        }

        public Color EvaluateColor(float normalizedHealth)
        {
            float ratio = Mathf.Clamp01(normalizedHealth);
            HealthBarColorThreshold selectedThreshold = null;
            float selectedMaximum = float.PositiveInfinity;
            HealthBarColorThreshold fallbackThreshold = null;
            float fallbackMaximum = float.NegativeInfinity;

            if (colorThresholds != null)
            {
                for (int index = 0; index < colorThresholds.Count; index++)
                {
                    HealthBarColorThreshold threshold = colorThresholds[index];
                    if (threshold == null)
                        continue;

                    float maximumRatio = Mathf.Clamp01(threshold.MaximumNormalizedHealth);
                    if (maximumRatio > fallbackMaximum)
                    {
                        fallbackMaximum = maximumRatio;
                        fallbackThreshold = threshold;
                    }

                    if (ratio <= maximumRatio && maximumRatio < selectedMaximum)
                    {
                        selectedMaximum = maximumRatio;
                        selectedThreshold = threshold;
                    }
                }
            }

            HealthBarColorThreshold result = selectedThreshold ?? fallbackThreshold;
            return result != null ? result.Color : Color.green;
        }

        public void ConfigureReferences(
            RectTransform healthFillRect,
            Graphic healthFillGraphic,
            Text healthValueLabel)
        {
            fillRect = healthFillRect;
            fillGraphic = healthFillGraphic;
            valueLabel = healthValueLabel;
            RefreshVisuals();
        }

        private void SetHealthInternal(float value, float newMaximumHealth)
        {
            float validatedMaximum = Mathf.Max(MinimumMaximumHealth, newMaximumHealth);
            float validatedCurrent = Mathf.Clamp(value, 0f, validatedMaximum);
            bool changed =
                !Mathf.Approximately(maximumHealth, validatedMaximum) ||
                !Mathf.Approximately(currentHealth, validatedCurrent);

            maximumHealth = validatedMaximum;
            currentHealth = validatedCurrent;
            RefreshVisuals();

            if (changed)
                HealthChanged?.Invoke(currentHealth, maximumHealth);
        }

        private void ValidateState()
        {
            maximumHealth = Mathf.Max(MinimumMaximumHealth, maximumHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maximumHealth);

            if (colorThresholds == null || colorThresholds.Count == 0)
                colorThresholds = CreateDefaultThresholds();

            for (int index = 0; index < colorThresholds.Count; index++)
                colorThresholds[index]?.Validate();
        }

        private static List<HealthBarColorThreshold> CreateDefaultThresholds()
        {
            return new List<HealthBarColorThreshold>
            {
                new(0.30f, new Color(0.78f, 0.12f, 0.09f, 1f)),
                new(0.65f, new Color(0.95f, 0.67f, 0.08f, 1f)),
                new(1.00f, new Color(0.20f, 0.57f, 0.39f, 1f))
            };
        }
    }
}
