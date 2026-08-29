using System.Collections.Generic;
using UnityEngine;

public class DiceUpgradeSelectionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform optionsContainer;
    [SerializeField] private UpgradeOptionUI optionPrefab;
    [SerializeField] private DiceUpgradeManager upgradeManager;

    private void Start()
    {
        if (upgradeManager == null)
            upgradeManager = DiceUpgradeManager.Instance;

        if (upgradeManager == null)
        {
            Debug.LogWarning("DiceUpgradeSelectionUI no encontró un DiceUpgradeManager.");
            return;
        }

        upgradeManager.OnOptionsGenerated += ShowOptions;

        panel.SetActive(false);

        if (upgradeManager.SelectionActive && upgradeManager.CurrentOptions.Count > 0)
            ShowOptions(upgradeManager.CurrentOptions);
    }

    private void OnDestroy()
    {
        if (upgradeManager != null)
            upgradeManager.OnOptionsGenerated -= ShowOptions;
    }

    private void ShowOptions(IReadOnlyList<DiceModifierData> options)
    {
        ClearOptions();

        panel.SetActive(true);

        foreach (DiceModifierData modifier in options)
        {
            UpgradeOptionUI optionUI = Instantiate(optionPrefab, optionsContainer);
            optionUI.Setup(modifier, SelectModifier);
        }
    }

    private void SelectModifier(DiceModifierData modifier)
    {
        if (upgradeManager == null)
            return;

        upgradeManager.SelectModifier(modifier);

        panel.SetActive(false);
        ClearOptions();
    }

    private void ClearOptions()
    {
        for (int i = optionsContainer.childCount - 1; i >= 0; i--)
            Destroy(optionsContainer.GetChild(i).gameObject);
    }
}