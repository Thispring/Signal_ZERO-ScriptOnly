using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Video;

/// <summary>
/// 게임의 사운드 볼륨을 관리하는 스크립트 입니다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    // 소환되는 Enemy, 버튼 호버 사운드 등, 해당 클래스를 직접 참조가 어려운 경우, Instance를 통해 접근합니다.
    public static SoundManager Instance { get; private set; }

    [Header("Values")]
    // 게임 씬이 변경되어도 변경된 볼륨을 사용하기 위해 static 선언
    public static float SavedBGMVolume = 1f;
    public static float SavedSoundEffectVolume = 1f;

    // 아래 모든 변수들은 Inspector에서 할당
    [Header("BGM")]
    [SerializeField]
    private List<AudioSource> bgmAudios = new List<AudioSource>();

    [Header("SoundEffect")]
    [SerializeField]
    private List<AudioSource> soundEffectAudios = new List<AudioSource>();

    [Header("UI")]
    [SerializeField]
    private Slider bgmSlider;
    [SerializeField]
    private Slider soundEffectSlider;

    [Header("Video Players")]
    [SerializeField]
    private VideoPlayer introVideoPlayer;
    [SerializeField]
    private VideoPlayer[] bossCutSceneVideoPlayers;

    [Header("Button Sound Effects")]
    [SerializeField] 
    private AudioClip buttonHoverClip;
    [SerializeField] 
    private AudioClip buttonClickClip;
    [SerializeField]
    private AudioSource uiAudioSource;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        // 리스너 등록 및 값 동기화
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.AddListener(BGMVolumeChange);
            bgmSlider.value = SavedBGMVolume;
        }
        if (soundEffectSlider != null)
        {
            soundEffectSlider.onValueChanged.AddListener(SoundEffectVolumeChange);
            soundEffectSlider.value = SavedSoundEffectVolume;
        }

        // 실제 오디오 소스 볼륨 적용
        foreach (var audio in bgmAudios)
        {
            if (audio != null)
                audio.volume = SavedBGMVolume;
        }
        foreach (var audio in soundEffectAudios)
        {
            if (audio != null)
                audio.volume = SavedSoundEffectVolume;
        }

        // 비디오 플레이어는 BGM 볼륨으로 설정
        if (introVideoPlayer != null)
            introVideoPlayer.SetDirectAudioVolume(0, SavedBGMVolume);

        if (bossCutSceneVideoPlayers != null)
        {
            foreach (var videoPlayer in bossCutSceneVideoPlayers)
            {
                videoPlayer.SetDirectAudioVolume(0, SavedBGMVolume);
            }
        }
    }

    private void BGMVolumeChange(float value)
    {
        foreach (var audio in bgmAudios)
        {
            if (audio != null)
                audio.volume = value;
        }
        SavedBGMVolume = value; // static 변수에 저장

        if (introVideoPlayer != null)
            introVideoPlayer.SetDirectAudioVolume(0, value);

        if (bossCutSceneVideoPlayers != null)
        {
            foreach (var videoPlayer in bossCutSceneVideoPlayers)
            {
                videoPlayer.SetDirectAudioVolume(0, value);
            }
        }
    }

    private void SoundEffectVolumeChange(float value)
    {
        foreach (var audio in soundEffectAudios)
        {
            if (audio != null)
                audio.volume = value;
        }
        SavedSoundEffectVolume = value;
    }

    // 새로운 Enemy가 생성될 때 호출
    public void RegisterEnemyAudio(AudioSource enemyAudio)
    {
        if (!soundEffectAudios.Contains(enemyAudio))
            soundEffectAudios.Add(enemyAudio);
        enemyAudio.volume = SavedSoundEffectVolume;
    }

    public void RegisterSoundEffect(AudioSource soundEffectAudio, int index = -1)
    {
        // 리스트 크기 보장
        while (soundEffectAudios.Count <= 1)
            soundEffectAudios.Add(null);

        if (index == 0 || index == 1)
        {
            soundEffectAudios[index] = soundEffectAudio;
        }
        else
        {
            if (!soundEffectAudios.Contains(soundEffectAudio))
                soundEffectAudios.Add(soundEffectAudio);
        }
        soundEffectAudio.volume = SavedSoundEffectVolume;
    }

    // 스테이지 클리어 시 모든 사운드 효과 정지
    public void StopAllSoundEffects()
    {
        foreach (var audio in soundEffectAudios)
        {
            if (audio != null)
            {
                audio.Stop();
            }
        }
    }

    public void PlayButtonHover()
    {
        if (buttonHoverClip != null && uiAudioSource != null)
            uiAudioSource.PlayOneShot(buttonHoverClip);
    }

    public void PlayButtonClick()
    {
        if (buttonClickClip != null && uiAudioSource != null)
            uiAudioSource.PlayOneShot(buttonClickClip);
    }
}
