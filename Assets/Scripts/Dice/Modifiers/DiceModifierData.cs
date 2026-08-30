using UnityEngine;

// =========================================================
// BASE
// =========================================================

public abstract class DiceModifierData : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string modifierName;

    [TextArea]
    [SerializeField] private string description;

    [SerializeField] private Sprite icon;

    public string ModifierName => modifierName;
    public string Description => description;
    public Sprite Icon => icon;

    public abstract void ApplyModifier();
}

// =========================================================
// DAMAGE BASE
// =========================================================

public abstract class DiceDamageModifierData : DiceModifierData
{
    protected abstract IDiceDamageModifier CreateModifier();

    public override void ApplyModifier()
    {
        if (DiceDamageManager.Instance == null)
        {
            Debug.LogWarning("No existe un DiceDamageManager activo.");
            return;
        }

        IDiceDamageModifier modifier = CreateModifier();

        if (modifier == null)
            return;

        DiceDamageManager.Instance.AddModifier(modifier);
    }
}

// =========================================================
// FACE BASE
// =========================================================

public abstract class DiceFaceModifierData : DiceModifierData
{
    protected DiceFaceManager FaceManager => DiceFaceManager.Instance;

    protected bool HasFaceManager()
    {
        if (FaceManager != null)
            return true;

        Debug.LogWarning("No existe un DiceFaceManager activo.");
        return false;
    }
}

// =========================================================
// DAMAGE
// =========================================================
// =========================================================
// FUTURE ROLLS
// =========================================================
// =========================================================
// FACE SET MODIFIERS
// =========================================================
// =========================================================
// FACE TRANSFORMATIONS
// =========================================================
