using UnityEngine;

[CreateAssetMenu(fileName = "SafeDice", menuName = "Dice/Modifiers/Faces/Dado Seguro")]
public class SafeDiceData : DiceFaceModifierData
{
    [SerializeField] private int valueToReplace = 1;
    [SerializeField] private int replacementValue = 3;

    public override void ApplyModifier()
    {
        if (!HasFaceManager())
            return;

        FaceManager.ReplaceAllValues(valueToReplace, replacementValue);
    }
}
