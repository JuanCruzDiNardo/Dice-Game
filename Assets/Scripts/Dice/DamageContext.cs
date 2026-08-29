public class DamageContext
{
    /// <summary>
    /// Valor de la tirada anterior que determina el daño actual.
    /// </summary>
    public int DiceValue;

    /// <summary>
    /// Daño antes de aplicar modificadores.
    /// </summary>
    public int BaseDamage;

    /// <summary>
    /// Daño final después de aplicar modificadores.
    /// </summary>
    public int Damage;

    /// <summary>
    /// Tiempo que el dado estuvo desplazándose durante esta tirada.
    /// </summary>
    public float ThrowDuration;
}