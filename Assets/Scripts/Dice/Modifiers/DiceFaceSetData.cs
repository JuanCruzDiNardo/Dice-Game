using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DiceFaceSet", menuName = "Dice/Face Sets/New Face Set")]
public class DiceFaceSetData : ScriptableObject
{
    [Header("Face Values")]
    [SerializeField] private List<int> values = new List<int> { 1, 2, 3, 4, 5, 6 };

    public IReadOnlyList<int> Values => values;

    public bool IsValid()
    {
        return values != null && values.Count == 6;
    }

    private void OnValidate()
    {
        if (values == null)
            values = new List<int>();

        while (values.Count < 6)
            values.Add(1);

        while (values.Count > 6)
            values.RemoveAt(values.Count - 1);

        for (int i = 0; i < values.Count; i++)
            values[i] = Mathf.Clamp(values[i], 1, 6);
    }
}