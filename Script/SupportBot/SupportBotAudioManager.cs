using UnityEngine;

/// <summary>
/// 보조 로봇의 사운드를 관리하는 스크립트입니다. 
/// 모든 보조 로봇에 관련한 스크립트는 SupportBot을 앞에 붙여줍니다.
/// </summary>
public class SupportBotAudioManager : MonoBehaviour
{
    // 모든 AudioSource Inspector 할당
    [Header("Audio Source")]
    [SerializeField]
    private AudioSource FSMAudioSource;
    [SerializeField]
    private AudioSource statusAudioSource;

    // 모든 AudioClip Inspector 할당
    [Header("Audio Clips")]
    [SerializeField]
    private AudioClip skillActiveClip;
    [SerializeField]
    private AudioClip attackAudioClip;
    [SerializeField]
    private AudioClip deathAudioClip;

    void Awake()
    {
        // AudioSource를 동적으로 생성하고 변수에 할당합니다.
        FSMAudioSource = gameObject.AddComponent<AudioSource>();
        statusAudioSource = gameObject.AddComponent<AudioSource>();

        // 생성된 AudioSource의 playOnAwake를 false로 설정합니다.
        FSMAudioSource.playOnAwake = false;
        statusAudioSource.playOnAwake = false;

        SoundManager soundManager = SoundManager.Instance;
        if (soundManager != null && FSMAudioSource != null)
            soundManager.RegisterEnemyAudio(FSMAudioSource);
        if (soundManager != null && statusAudioSource != null)
            soundManager.RegisterEnemyAudio(statusAudioSource);
    }

    public void PlaySkillActiveSound()
    {
        if (FSMAudioSource != null && skillActiveClip != null)
        {
            FSMAudioSource.PlayOneShot(skillActiveClip);
        }
    }

    public void PlayAttackSound()
    {
        if (FSMAudioSource != null && attackAudioClip != null)
        {
            FSMAudioSource.PlayOneShot(attackAudioClip);
        }
    }

    public void PlayDeathSound()
    {
        if (statusAudioSource != null && deathAudioClip != null)
        {
            statusAudioSource.PlayOneShot(deathAudioClip);
        }
    }
}
