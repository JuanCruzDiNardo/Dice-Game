using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DiceAudioManager : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioClip[] collisionSounds;

    [Header("Pitch Variation")]
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    [Header("Collision")]
    [SerializeField] private float minimumImpactSpeed = 0.5f;
    [SerializeField] private float maxImpactSpeed = 8f;

    [Header("Volume")]
    [SerializeField] private float minVolume = 0.15f;
    [SerializeField] private float maxVolume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Tray")
        {
            float impactSpeed = collision.relativeVelocity.magnitude;

            // Ignore tiny contacts and vibrations.
            if (impactSpeed < minimumImpactSpeed)
                return;

            PlayCollisionSound(impactSpeed);
        }        
    }

    private void PlayCollisionSound(float impactSpeed)
    {
        if (collisionSounds == null || collisionSounds.Length == 0)
            return;

        // Choose a random sound.
        AudioClip clip = collisionSounds[
            Random.Range(0, collisionSounds.Length)
        ];

        // Slightly randomize pitch.
        audioSource.pitch = Random.Range(minPitch, maxPitch);

        // Stronger impacts = louder sound.
        float normalizedImpact = Mathf.InverseLerp(
            minimumImpactSpeed,
            maxImpactSpeed,
            impactSpeed
        );

        float volume = Mathf.Lerp(
            minVolume,
            maxVolume,
            normalizedImpact
        );

        audioSource.PlayOneShot(clip, volume);
    }
}