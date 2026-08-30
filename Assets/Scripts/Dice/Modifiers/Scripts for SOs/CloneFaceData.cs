using UnityEngine;

[CreateAssetMenu(fileName = "CloneFace", menuName = "Dice/Modifiers/Faces/Clonacion")]
public class CloneFaceData : DiceFaceModifierData
{
    public override void ApplyModifier()
    {
        if (!HasFaceManager())
            return;

        FaceManager.CloneRandomFace();
    }
}