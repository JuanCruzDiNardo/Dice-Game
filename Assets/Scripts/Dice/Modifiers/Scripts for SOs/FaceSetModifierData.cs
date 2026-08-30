using UnityEngine;

[CreateAssetMenu(fileName = "FaceSetModifier", menuName = "Dice/Modifiers/Faces/Aplicar Face Set")]
public class FaceSetModifierData : DiceFaceModifierData
{
    [Header("Face Set")]
    [SerializeField] private DiceFaceSetData faceSet;

    public override void ApplyModifier()
    {
        if (!HasFaceManager())
            return;

        if (faceSet == null)
        {
            Debug.LogWarning($"El modificador {name} no tiene un Face Set asignado.");
            return;
        }

        FaceManager.ApplyFaceSet(faceSet);
    }
}
