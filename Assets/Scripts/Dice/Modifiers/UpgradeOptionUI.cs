using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeOptionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button selectButton;

    private DiceModifierData modifier;
    private Action<DiceModifierData> onSelected;

    public void Setup(DiceModifierData newModifier, Action<DiceModifierData> selectionCallback)
    {
        modifier = newModifier;
        onSelected = selectionCallback;

        nameText.text = modifier.ModifierName;
        descriptionText.text = modifier.Description;

        if (iconImage != null)
        {
            iconImage.sprite = modifier.Icon;
            iconImage.enabled = modifier.Icon != null;
        }

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(Select);
    }

    private void Select()
    {
        onSelected?.Invoke(modifier);
    }
}