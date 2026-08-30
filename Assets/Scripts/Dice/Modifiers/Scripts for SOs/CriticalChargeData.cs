using UnityEngine;


[CreateAssetMenu(fileName = "CriticalCharge", menuName = "Dice/Modifiers/Future/Carga Critica")]
public class CriticalChargeData : DiceDamageModifierData
{
    [SerializeField] private int multiplierIncrease = 1;

    protected override IDiceDamageModifier CreateModifier()
    {
        return new CriticalChargeModifier(multiplierIncrease);
    }
}
