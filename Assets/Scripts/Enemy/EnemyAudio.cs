using UnityEngine;

public enum EnemyDeathType
{
    Dice,
    Nexus
}

[RequireComponent(typeof(AudioSource))]
public class EnemyAudio : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private AudioClip[] damageClips;

    [Header("Death - Dice")]
    [SerializeField] private AudioClip[] diceDeathClips;

    [Header("Death - Nexus")]
    [SerializeField] private AudioClip[] nexusDeathClips;

    [Header("Variation")]
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float damageVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float deathVolume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void PlayDamage()
    {
        PlayRandomClip(damageClips, damageVolume);
    }

    public void PlayDeath(EnemyDeathType deathType)
    {
        switch (deathType)
        {
            case EnemyDeathType.Dice:
                PlayRandomClip(diceDeathClips, deathVolume);
                CameraShake.Instance.Shake();
                break;

            case EnemyDeathType.Nexus:
                PlayRandomClip(nexusDeathClips, deathVolume);
                break;
        }
    }

    private void PlayRandomClip(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip, volume);
    }
}