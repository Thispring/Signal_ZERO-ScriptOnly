using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy의 Sound를 관리하는 스크립트 입니다.
/// 모든 Enemy에 관련한 스크립트는 Enemy을 앞에 붙여줍니다.
/// </summary>
public class EnemyAudioController : MonoBehaviour
{
    [Header("References")]
    // 타입에 알맞은 Audio Clip을 초기화하기 위해서 statusManager의 setting 참조
    private EnemyStatusManager statusManager;

    [Header("Coroutines")]
    private Coroutine idleSoundCoroutine; // Idle 사운드 코루틴 핸들

    [Header("Audio Sources")]
    [SerializeField]
    private AudioSource attackAudioSource;
    [SerializeField]
    private AudioSource moveAudioSource;
    [SerializeField]
    private AudioSource spawnAudioSource;
    [SerializeField]
    private AudioSource teleportAudioSource;
    [SerializeField]
    private AudioSource statusAudioSource;

    [Header("Audio Clips")]
    // Use attackAudioSource
    [SerializeField]
    private AudioClip attackAudioClip;
    // Use moveAudioSource
    [SerializeField]
    private AudioClip[] moveAudioClip;
    // Use spawnAudioSource
    [SerializeField]
    private AudioClip spawnAudioClip;
    // Use teleportAudioClip
    [SerializeField]
    private AudioClip teleportAudioClip;
    // Use statusAudioSource
    [SerializeField]
    private AudioClip idleAudioClip;
    [SerializeField]
    private AudioClip deathAudioClip;

    void Awake()
    {
        statusManager = GetComponent<EnemyStatusManager>(); 
        
        // AudioSource 컴포넌트 자동 추가
        if (attackAudioSource == null)
            attackAudioSource = gameObject.AddComponent<AudioSource>();
        if (moveAudioSource == null)
            moveAudioSource = gameObject.AddComponent<AudioSource>();
        if (spawnAudioSource == null)
            spawnAudioSource = gameObject.AddComponent<AudioSource>();
        if (teleportAudioSource == null)
            teleportAudioSource = gameObject.AddComponent<AudioSource>();
        if (statusAudioSource == null)
            statusAudioSource = gameObject.AddComponent<AudioSource>();

        // 타입에 알맞은 오디오 초기화
        LoadAudioClipsByEnemyType();
    }

    void Start()
    {
        // SoundEffect 조절을 위한 audioSource 등록
        SoundManager soundManager = SoundManager.Instance;
        if (soundManager != null && attackAudioSource != null)
            soundManager.RegisterEnemyAudio(attackAudioSource);
        if (soundManager != null && moveAudioSource != null)
            soundManager.RegisterEnemyAudio(moveAudioSource);
        if (soundManager != null && spawnAudioSource != null)
            soundManager.RegisterEnemyAudio(spawnAudioSource);
        if (soundManager != null && teleportAudioSource != null)
            soundManager.RegisterEnemyAudio(teleportAudioSource);
        if (soundManager != null && statusAudioSource != null)
            soundManager.RegisterEnemyAudio(statusAudioSource);

        // 소환 사운드는 Start에서 한 번만 재생
        spawnAudioSource.PlayOneShot(spawnAudioClip);
    }

    void OnDisable()
    {
        // 오브젝트가 비활성화될 때 Idle 사운드 중지
        StopIdleSound();
    }

    private void LoadAudioClipsByEnemyType()
    {
        string basePath = "Sound/Enemy/"; // Resources 폴더 내부 경로

        switch (statusManager.setting.enemySize)
        {
            case EnemySize.Small:
                attackAudioClip = Resources.Load<AudioClip>(basePath + "Small_Attack");
                teleportAudioClip = Resources.Load<AudioClip>(basePath + "Teleport");
                idleAudioClip = Resources.Load<AudioClip>(basePath + "Small_Idle");
                deathAudioClip = Resources.Load<AudioClip>(basePath + "Die");

                break;

            case EnemySize.Medium:
                attackAudioClip = Resources.Load<AudioClip>(basePath + "Medium_Attack");
                spawnAudioClip = Resources.Load<AudioClip>(basePath + "Medium_Spawn");
                teleportAudioClip = Resources.Load<AudioClip>(basePath + "Teleport");
                deathAudioClip = Resources.Load<AudioClip>(basePath + "Die");

                moveAudioClip = new AudioClip[]
                {
                Resources.Load<AudioClip>(basePath + "Medium_Move1"),
                Resources.Load<AudioClip>(basePath + "Medium_Move2")
                };
                break;

            case EnemySize.Big:
                attackAudioClip = Resources.Load<AudioClip>(basePath + "Big_Attack");
                spawnAudioClip = Resources.Load<AudioClip>(basePath + "Big_Spawn");
                teleportAudioClip = Resources.Load<AudioClip>(basePath + "Teleport");
                deathAudioClip = Resources.Load<AudioClip>(basePath + "Big_Die");

                moveAudioClip = new AudioClip[]
                {
                Resources.Load<AudioClip>(basePath + "Big_Move1"),
                Resources.Load<AudioClip>(basePath + "Big_Move2"),
                };
                break;

            default:
                Debug.LogWarning("알 수 없는 Enemy 입니다.");
                break;
        }

        switch (statusManager.setting.enemyType)
        {
            // 미사일 타입만 별도의 공격 사운드 로드
            case EnemyType.Missile:
                attackAudioClip = Resources.Load<AudioClip>(basePath + "Missile_Attack");
                break;
        }
    }

    // 공격 사운드
    public void PlayAttackSound()
    {
        attackAudioSource.PlayOneShot(attackAudioClip);
    }
    // 순간이동 사운드
    public void PlayTeleportSound()
    {
        teleportAudioSource.PlayOneShot(teleportAudioClip);
    }
    // 대기 상태 사운드 - 반복 재생 시작
    public void PlayIdleSound()
    {
        // idle의 경우 Small만 해당
        if (idleAudioClip != null)
        {
            // 이미 재생 중이면 중지하고 새로 시작
            if (idleSoundCoroutine != null)
            {
                StopCoroutine(idleSoundCoroutine);
            }
            idleSoundCoroutine = StartCoroutine(IdleSoundLoop());
        }
    }
    // Idle 사운드 중지
    public void StopIdleSound()
    {
        if (idleSoundCoroutine != null)
        {
            StopCoroutine(idleSoundCoroutine);
            idleSoundCoroutine = null;
        }

        // AudioSource도 중지
        if (statusAudioSource != null && statusAudioSource.isPlaying)
        {
            statusAudioSource.Stop();
        }
    }
    // Idle 사운드를 반복 재생하는 코루틴
    private IEnumerator IdleSoundLoop()
    {
        while (true) // 오브젝트가 비활성화될 때까지 무한 반복
        {
            if (idleAudioClip != null)
            {
                statusAudioSource.PlayOneShot(idleAudioClip);

                // AudioClip의 길이만큼 대기 (사운드가 끝날 때까지)
                yield return new WaitForSeconds(idleAudioClip.length);
            }
            else
            {
                yield break; // AudioClip이 없으면 코루틴 종료
            }
        }
    }
    // 소환 사운드
    public void PlaySpawnSound()
    {
        spawnAudioSource.PlayOneShot(spawnAudioClip);
    }
    // 파괴되었을 때 사운드
    public void PlayDeathSound()
    {
        statusAudioSource.PlayOneShot(deathAudioClip);
    }

    // 이동 사운드의 경우, 모델링의 생김세에 따라 필요한 Clip량이 다를 수 있기에
    // moveAudioClip의 길이만큼 재생합니다.
    public void PlayMoveSound()
    {
        StartCoroutine(EnemyMoveSound());
    }
    private IEnumerator EnemyMoveSound(float delay = 1f)
    {
        for (int i = 0; i < moveAudioClip.Length; i++)
        {
            moveAudioSource.PlayOneShot(moveAudioClip[i]);
            yield return new WaitForSeconds(delay);    // 사운드 싱크를 맞추고 싶다면 delay 값을 조절
        }
    }
}
