using UnityEngine;

[CreateAssetMenu(fileName = "LoadedDice", menuName = "Dice/Modifiers/Faces/Dado Cargado")]
public class LoadedDiceData : DiceFaceModifierData
{
    [SerializeField] private int loadedValue = 6;

    public override void ApplyModifier()
    {
        if (!HasFaceManager())
            return;

        FaceManager.SetRandomFaceToValue(loadedValue);
    }
}

