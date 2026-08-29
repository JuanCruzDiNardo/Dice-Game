using System;
using System.Collections.Generic;
using UnityEngine;

public class DiceUpgradeManager : MonoBehaviour
{
    public static DiceUpgradeManager Instance { get; private set; }

    [Header("Upgrade Pool")]
    [SerializeField] private List<DiceModifierData> availableModifiers = new List<DiceModifierData>();

    [Header("Selection")]
    [SerializeField, Min(1)] private int optionsPerSelection = 3;

    [Header("Debug")]
    [SerializeField] private bool selectionActive;

    private readonly List<DiceModifierData> currentOptions = new List<DiceModifierData>();
    private readonly List<DiceModifierData> acquiredModifiers = new List<DiceModifierData>();

    public IReadOnlyList<DiceModifierData> AvailableModifiers => availableModifiers;
    public IReadOnlyList<DiceModifierData> CurrentOptions => currentOptions;
    public IReadOnlyList<DiceModifierData> AcquiredModifiers => acquiredModifiers;
    public bool SelectionActive => selectionActive;

    public event Action<IReadOnlyList<DiceModifierData>> OnOptionsGenerated;
    public event Action<DiceModifierData> OnModifierSelected;
    public event Action OnSelectionCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool GenerateOptions()
    {
        currentOptions.Clear();

        if (availableModifiers.Count == 0)
        {
            selectionActive = false;
            return false;
        }

        List<DiceModifierData> temporaryPool = new List<DiceModifierData>(availableModifiers);
        int optionAmount = Mathf.Min(optionsPerSelection, temporaryPool.Count);

        for (int i = 0; i < optionAmount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, temporaryPool.Count);
            DiceModifierData modifier = temporaryPool[randomIndex];

            currentOptions.Add(modifier);
            temporaryPool.RemoveAt(randomIndex);
        }

        selectionActive = true;

        OnOptionsGenerated?.Invoke(currentOptions);

        return true;
    }

    public void SelectModifier(DiceModifierData modifier)
    {
        if (!selectionActive)
            return;

        if (modifier == null)
            return;

        if (!currentOptions.Contains(modifier))
        {
            Debug.LogWarning($"El modificador {modifier.name} no pertenece a las opciones actuales.");
            return;
        }

        if (!availableModifiers.Contains(modifier))
        {
            Debug.LogWarning($"El modificador {modifier.name} ya no pertenece a la pool.");
            return;
        }

        modifier.ApplyModifier();

        availableModifiers.Remove(modifier);
        acquiredModifiers.Add(modifier);

        selectionActive = false;
        currentOptions.Clear();

        OnModifierSelected?.Invoke(modifier);
        OnSelectionCompleted?.Invoke();
    }
}