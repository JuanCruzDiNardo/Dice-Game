using UnityEngine;

[CreateAssetMenu(fileName = "Sacrifice", menuName = "Dice/Modifiers/Faces/Sacrificio")]
public class SacrificeDiceData : DiceFaceModifierData
{
    public override void ApplyModifier()
    {
        if (!HasFaceManager())
            return;

        FaceManager.SacrificeRandomFaces();
    }
}