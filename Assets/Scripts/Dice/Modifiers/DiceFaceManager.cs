using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(DiceVisualController))]
public class DiceFaceManager : MonoBehaviour
{
    public static DiceFaceManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private DiceVisualController visualController;

    [Header("Initial Configuration")]
    [SerializeField] private DiceFaceSetData initialFaceSet;

    [Header("Debug")]
    [SerializeField] private List<int> currentValues = new List<int>();

    private List<DiceFaceData> faces;

    public IReadOnlyList<DiceFaceData> Faces => faces;
    public IReadOnlyList<int> CurrentValues => currentValues;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (visualController == null)
            visualController = GetComponent<DiceVisualController>();

        faces = visualController.Faces;

        if (initialFaceSet != null)
            ApplyFaceSet(initialFaceSet);
        else
            RefreshCurrentValues();
    }

    public int GetTopFaceValue()
    {
        if (faces == null || faces.Count == 0)
            return 0;

        DiceFaceData topFace = faces.OrderByDescending(face => face.Anchor.transform.position.y).FirstOrDefault();

        return topFace != null ? topFace.Value : 0;
    }

    public void ApplyFaceSet(DiceFaceSetData faceSet)
    {
        if (faceSet == null)
            return;

        if (!faceSet.IsValid())
        {
            Debug.LogWarning($"El FaceSet {faceSet.name} debe contener exactamente 6 valores.");
            return;
        }

        if (faces == null || faces.Count != 6)
        {
            Debug.LogWarning("El dado debe tener exactamente 6 caras configuradas.");
            return;
        }

        for (int i = 0; i < faces.Count; i++)
            faces[i].SetValue(faceSet.Values[i]);

        RefreshVisuals();
    }

    public void ReplaceAllValues(int oldValue, int newValue)
    {
        if (faces == null)
            return;

        foreach (DiceFaceData face in faces)
        {
            if (face.Value == oldValue)
                face.SetValue(newValue);
        }

        RefreshVisuals();
    }

    public void SetFaceValue(int index, int value)
    {
        if (!IsValidFaceIndex(index))
            return;

        faces[index].SetValue(value);

        RefreshVisuals();
    }

    public int GetFaceValue(int index)
    {
        if (!IsValidFaceIndex(index))
            return 0;

        return faces[index].Value;
    }

    public int GetRandomFaceIndex()
    {
        if (faces == null || faces.Count == 0)
            return -1;

        return Random.Range(0, faces.Count);
    }

    public int GetRandomFaceIndexExcept(int excludedIndex)
    {
        if (faces == null || faces.Count <= 1)
            return -1;

        List<int> availableIndexes = new List<int>();

        for (int i = 0; i < faces.Count; i++)
        {
            if (i != excludedIndex)
                availableIndexes.Add(i);
        }

        if (availableIndexes.Count == 0)
            return -1;

        return availableIndexes[Random.Range(0, availableIndexes.Count)];
    }

    public void SetRandomFaceToValue(int value)
    {
        int index = GetRandomFaceIndex();

        if (index < 0)
            return;

        faces[index].SetValue(value);

        RefreshVisuals();
    }

    public void SacrificeRandomFaces()
    {
        int sacrificeIndex = GetRandomFaceIndex();

        if (sacrificeIndex < 0)
            return;

        int rewardIndex = GetRandomFaceIndexExcept(sacrificeIndex);

        if (rewardIndex < 0)
            return;

        faces[sacrificeIndex].SetValue(1);
        faces[rewardIndex].SetValue(6);

        RefreshVisuals();
    }

    public void CloneRandomFace()
    {
        int sourceIndex = GetRandomFaceIndex();

        if (sourceIndex < 0)
            return;

        int targetIndex = GetRandomFaceIndexExcept(sourceIndex);

        if (targetIndex < 0)
            return;

        int clonedValue = faces[sourceIndex].Value;

        faces[targetIndex].SetValue(clonedValue);

        RefreshVisuals();
    }

    private bool IsValidFaceIndex(int index)
    {
        return faces != null && index >= 0 && index < faces.Count;
    }

    private void RefreshVisuals()
    {
        RefreshCurrentValues();

        if (visualController != null)
            visualController.ApplyLabelChanges();
    }

    private void RefreshCurrentValues()
    {
        currentValues.Clear();

        if (faces == null)
            return;

        foreach (DiceFaceData face in faces)
            currentValues.Add(face.Value);
    }
}