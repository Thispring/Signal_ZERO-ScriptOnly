using UnityEngine;
using System.Collections;

/// <summary>
/// 버스트 게이지가 준비되고, 버스트를 사용한 후, 버스트가 끝날 때까지 대기하는 튜토리얼 단계입니다.
/// </summary>
[RequireComponent(typeof(DialogSystem))]
public class TutorialStep_WaitForBurst : TutorialStep
{
    [Header("References")]
    [SerializeField]
    private PlayerStatusManager playerStatusManager; // 플레이어 상태 관리자
    [SerializeField]
    private PlayerShotController playerShotController; // 플레이어 샷 컨트롤러
    [SerializeField]
    private WeaponSwitchSystem weaponSwitchSystem;   // 현재 무기 참조용
    [SerializeField]
    private EnemyMemoryPool enemyMemoryPool;         // 적 처치 점수 확인용
    private DialogSystem dialogSystem;           // 대사 시스템 참조용
    private TutorialController controllerRef;

    [Header("Flags")]
    private bool isDialog = false;
    private bool isDialogFinished = false;
    private bool burstHasBeenActivated = false; // 버스트가 한 번이라도 활성화되었는지 추적
    
    private bool isFinished = false;
    public override bool IsFinished => isFinished;

    public override void Enter()
    {
        // 스킵 상태 확인
        if (isSkipped)
        {
            isFinished = true;
            return;
        }

        isDialog = false;
        isDialogFinished = false;
        dialogSystem = GetComponent<DialogSystem>();

        isFinished = false;
        burstHasBeenActivated = false;

        if (playerStatusManager == null)
        {
            Debug.LogError("PlayerStatusManager가 할당되지 않았습니다.");
            isFinished = true;
            return;
        }
        if (weaponSwitchSystem == null)
        {
            Debug.LogError("WeaponSwitchSystem이 할당되지 않았습니다.");
            isFinished = true;
            return;
        }

        playerStatusManager.isTutorialBurstAction = false;
    }

    public override void Execute(TutorialController controller)
    {
        // 스킵 상태 확인
        if (isSkipped)
        {
            controller.SetNextTutorial();
            return;
        }

        if (controller == null)
        {
            Debug.LogWarning("TutorialStep_WaitForBurst: controller is null");
            isFinished = true;
            return;
        }

        // controller 참조 저장(코루틴에서 사용)
        controllerRef = controller;

        if (isFinished) return;

        WeaponBase currentWeapon = weaponSwitchSystem.CurrentWeapon;
        if (currentWeapon == null) return;

        // 1. 버스트가 준비되었는지 확인 (아직 버스트를 사용하지 않았다면)
        if (!isDialog && playerStatusManager.isBurstReady)
        {
            // isBurstReady 상태는 UI나 준비 상태를 나타낼 뿐, 실제 버스트 사용과는 다름
            // 실제 버스트 사용을 감지해야 함
            // 대사 출력 대기 코루틴 시작
            isDialog = true;
            StartCoroutine(BurstActiveWithDialog());
        }

        // 2. 플레이어가 버스트를 사용했는지 감지
        if (!burstHasBeenActivated && currentWeapon.IsBurst)
        {
            burstHasBeenActivated = true;
        }

        // 중복 호출 방지로 !burstHasBeenActivated 조건 추가
        // 대사가 모두 출력되어야지 버스트 사용 가능
        if (!burstHasBeenActivated && isDialogFinished && Input.GetKeyDown(KeyCode.F))
        {
            PauseManager.RemovePause();
            playerShotController.TutorialBurstActionStart();
            dialogSystem.OffDialog(0);
        }

        // 3. 버스트가 활성화된 적이 있고, 현재는 버스트 상태가 아닐 때 (버스트가 끝났을 때)
        if (burstHasBeenActivated && !currentWeapon.IsBurst)
        {
            StartCoroutine(AfterBurstCoroutine());
            burstHasBeenActivated = false; // 추가 감지를 방지
        }
    }

    public override void Skip()
    {
        base.Skip();
        // 코루틴 중단
        StopAllCoroutines();
        // 대화 시스템 정리
        if (dialogSystem != null)
        {
            dialogSystem.OffDialog(0);
        }
    }

    public override void Exit()
    {
        if (dialogSystem != null)
        {
            dialogSystem.OffDialog(0);
        }
    }

    private IEnumerator AfterBurstCoroutine()
    {
        // 스킵 확인하면서 대기
        while (ShouldContinue() && (enemyMemoryPool == null || enemyMemoryPool.killScore < 3))
        {
            yield return null;
        }

        if (!ShouldContinue()) yield break;

        // 조건 만족 시 처리
        enemyMemoryPool.killScore = 0; // 점수 초기화
        if (controllerRef != null)
        {
            controllerRef.lastSpawnedEnemies.Clear();
            controllerRef.SetNextTutorial();
        }
    }

    // isTutorialBurstAction를 true로 바꾸어, 버스트 준비상태로 변경하고
    // 버스트 시스템 설명을 Dialog로 출력
    private IEnumerator BurstActiveWithDialog()
    {
        float elapsed = 0f;
        while (ShouldContinue() && elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!ShouldContinue()) yield break;

        PauseManager.AddPause();

        playerShotController.isTutorialBurstAction = true; // 대사 출력 후 튜토리얼 버스트 액션 활성화
        dialogSystem.PlayDialogRange(0, 7, () =>
        {
            // 대사가 모두 출력된 후 isDialogFinished를 true로 설정
            if (ShouldContinue())
            {
                isDialogFinished = true;
            }
        });
    }
}
