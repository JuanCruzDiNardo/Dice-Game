public interface IDiceDamageModifier
{
    void Modify(DamageContext context);
}

public class EvenDoubleDamageModifier : IDiceDamageModifier
{
    public void Modify(DamageContext context)
    {
        if (context.DiceValue % 2 == 0)
        {
            context.Damage *= 2;
        }
    }
}

public class CriticalOnSixModifier : IDiceDamageModifier
{
    private readonly int criticalMultiplier;

    public CriticalOnSixModifier(int criticalMultiplier = 3)
    {
        this.criticalMultiplier = criticalMultiplier;
    }

    public void Modify(DamageContext context)
    {
        if (context.DiceValue == 6)
        {
            context.Damage *= criticalMultiplier;
        }
    }
}
