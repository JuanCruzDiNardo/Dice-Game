using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

internal sealed class DiceFaceTextureArrayLease
{
    // Lightweight ownership token used by one optimized die.

    internal DiceFaceTextureArrayLease(DiceFaceTextureArrayCache.Entry entry)
    {
        Entry = entry;
    }

    internal DiceFaceTextureArrayCache.Entry Entry { get; }

    public Texture2DArray TextureArray => Entry.TextureArray;

    public int GetSlice(Texture2D texture)
    {
        return Entry.SliceByTexture.TryGetValue(texture, out int slice)
            ? slice
            : 0;
    }
}

internal static class DiceFaceTextureArrayCache
{
    // Shared cache model ----------------------------------------------------

    internal sealed class Entry
    {
        public int Hash;
        public int Resolution;
        public Texture2D[] Textures;
        public Texture2DArray TextureArray;
        public Dictionary<Texture2D, int> SliceByTexture;
        public int ReferenceCount;
    }

    private static readonly Dictionary<int, List<Entry>> EntriesByHash = new();

    // Cache lifecycle -------------------------------------------------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        foreach (List<Entry> candidates in EntriesByHash.Values)
        {
            foreach (Entry entry in candidates)
                DestroyTexture(entry.TextureArray);
        }

        EntriesByHash.Clear();
    }

    public static DiceFaceTextureArrayLease Acquire(
        IReadOnlyList<Texture2D> faceTextures,
        int resolution)
    {
        Texture2D[] uniqueTextures = GetCanonicalTextureSet(faceTextures);
        int hash = CalculateHash(uniqueTextures, resolution);

        if (EntriesByHash.TryGetValue(hash, out List<Entry> candidates))
        {
            foreach (Entry candidate in candidates)
            {
                if (candidate.Resolution == resolution &&
                    ReferencesMatch(candidate.Textures, uniqueTextures))
                {
                    candidate.ReferenceCount++;
                    return new DiceFaceTextureArrayLease(candidate);
                }
            }
        }
        else
        {
            candidates = new List<Entry>();
            EntriesByHash.Add(hash, candidates);
        }

        Entry entry = CreateEntry(uniqueTextures, resolution, hash);
        candidates.Add(entry);
        return new DiceFaceTextureArrayLease(entry);
    }

    public static void Release(DiceFaceTextureArrayLease lease)
    {
        if (lease?.Entry == null)
            return;

        Entry entry = lease.Entry;
        entry.ReferenceCount--;

        if (entry.ReferenceCount > 0)
            return;

        if (EntriesByHash.TryGetValue(entry.Hash, out List<Entry> candidates))
        {
            candidates.Remove(entry);

            if (candidates.Count == 0)
                EntriesByHash.Remove(entry.Hash);
        }

        DestroyTexture(entry.TextureArray);
    }

    // Entry and GPU resource construction ---------------------------------

    private static Entry CreateEntry(
        Texture2D[] textures,
        int resolution,
        int hash)
    {
        Texture2DArray textureArray = BuildTextureArray(textures, resolution);
        Dictionary<Texture2D, int> slices = new(textures.Length);

        for (int i = 0; i < textures.Length; i++)
            slices[textures[i]] = i;

        return new Entry
        {
            Hash = hash,
            Resolution = resolution,
            Textures = textures,
            TextureArray = textureArray,
            SliceByTexture = slices,
            ReferenceCount = 1
        };
    }

    private static Texture2DArray BuildTextureArray(
        IReadOnlyList<Texture2D> textures,
        int resolution)
    {
        Texture2DArray textureArray = new(
            resolution,
            resolution,
            textures.Count,
            TextureFormat.RGBA32,
            true,
            false)
        {
            name = $"Dice Face Textures {resolution}px ({textures.Count})",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat,
            anisoLevel = 1,
            hideFlags = HideFlags.DontSave
        };

        RenderTexture renderTarget = RenderTexture.GetTemporary(
            resolution,
            resolution,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB);
        Texture2D readableCopy = new(
            resolution,
            resolution,
            TextureFormat.RGBA32,
            false,
            false);
        RenderTexture previousTarget = RenderTexture.active;

        try
        {
            for (int slice = 0; slice < textures.Count; slice++)
            {
                Graphics.Blit(textures[slice], renderTarget);
                RenderTexture.active = renderTarget;
                readableCopy.ReadPixels(
                    new Rect(0, 0, resolution, resolution),
                    0,
                    0,
                    false);
                readableCopy.Apply(false, false);
                // Copy raw RGBA bytes directly. This avoids allocating and
                // converting one managed Color[] for every texture slice.
                textureArray.SetPixelData(
                    readableCopy.GetPixelData<Color32>(0),
                    0,
                    slice);
            }

            textureArray.Apply(true, true);
        }
        finally
        {
            RenderTexture.active = previousTarget;
            RenderTexture.ReleaseTemporary(renderTarget);
            UnityEngine.Object.Destroy(readableCopy);
        }

        return textureArray;
    }

    // Canonical cache keys -------------------------------------------------

    private static Texture2D[] GetCanonicalTextureSet(
        IReadOnlyList<Texture2D> faceTextures)
    {
        HashSet<Texture2D> unique = new();

        for (int i = 0; i < faceTextures.Count; i++)
            unique.Add(faceTextures[i] != null ? faceTextures[i] : Texture2D.whiteTexture);

        Texture2D[] textures = new Texture2D[unique.Count];
        unique.CopyTo(textures);
        Array.Sort(textures, CompareTextures);
        return textures;
    }

    private static int CompareTextures(Texture2D left, Texture2D right)
    {
        return RuntimeHelpers.GetHashCode(left).CompareTo(
            RuntimeHelpers.GetHashCode(right));
    }

    private static int CalculateHash(
        IReadOnlyList<Texture2D> textures,
        int resolution)
    {
        unchecked
        {
            int hash = resolution;

            for (int i = 0; i < textures.Count; i++)
                hash = hash * 397 ^ RuntimeHelpers.GetHashCode(textures[i]);

            return hash;
        }
    }

    private static bool ReferencesMatch(
        IReadOnlyList<Texture2D> left,
        IReadOnlyList<Texture2D> right)
    {
        if (left.Count != right.Count)
            return false;

        for (int i = 0; i < left.Count; i++)
        {
            if (left[i] != right[i])
                return false;
        }

        return true;
    }

    private static void DestroyTexture(Texture texture)
    {
        if (texture == null)
            return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(texture);
        else
            UnityEngine.Object.DestroyImmediate(texture);
    }
}
