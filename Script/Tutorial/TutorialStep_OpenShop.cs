using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 튜토리얼 단계에서 상점을 열고, 상점 UI와 대사 시퀀스를 관리하는 스크립트 입니다.
/// </summary>
[RequireComponent(typeof(DialogSystem))]
public class TutorialStep_OpenShop : TutorialStep
{
    // 캐릭터들의 대사를 진행하는 DialogSystem
    private DialogSystem dialogSystem;

    [Header("Shop Settings")]
    [SerializeField] 
    private ShopManager shopManager;
    [SerializeField] 
    private StageManager stageManager;
    [SerializeField] 
    private Image[] pointerImages; // 버튼을 가리키는 이미지 배열
    [SerializeField] 
    private GameObject[] shopUIElements; // 상점 UI 요소들

    private bool isFinished = false;
    public override bool IsFinished => isFinished;

    private Coroutine dialogSequenceCoroutine; // 코루틴 참조

    public override void Enter()
    {
        // 스킵 상태 확인
        if (isSkipped)
        {
            isFinished = true;
            return;
        }

        dialogSystem = GetComponent<DialogSystem>();

        isFinished = false;
        if (shopManager == null)
        {
            Debug.LogError("ShopManager가 할당되지 않았습니다.");
            isFinished = true;
            return;
        }

        // 상점단계 진입하고 대사 추가
        stageManager.PauseGame();
        // 대사 재생 전 상호작용 차단
        shopManager.SetInteractable(false);

        // 포인터 초기 상태 -> 모두 비활성화
        if (pointerImages != null)
        {
            for (int i = 0; i < pointerImages.Length; i++)
                if (pointerImages[i] != null) pointerImages[i].gameObject.SetActive(false);
        }

        // 전체 시퀀스 재생 시작 (코루틴 참조 저장)
        dialogSequenceCoroutine = StartCoroutine(RunDialogSequence());
    }

    public override void Execute(TutorialController controller)
    {
        // 스킵 상태 확인
        if (isSkipped)
        {
            controller.SetNextTutorial();
            return;
        }

        // stageManager의 ResumeGame 호출이 되어 isStart가 true가 될 때
        if (stageManager.isStart)
        {
            controller.SetNextTutorial();
        }      
    }

    public override void Skip()
    {
        base.Skip();
        // 상점 튜토리얼 강제 중단 처리
        ForceStopShopTutorial();
    }

    public override void Exit()
    {
        ForceStopShopTutorial();
    }

    // 상점 튜토리얼 강제 중단 처리
    private void ForceStopShopTutorial()
    {
        try
        {
            // 1. 코루틴 중단
            if (dialogSequenceCoroutine != null)
            {
                StopCoroutine(dialogSequenceCoroutine);
                dialogSequenceCoroutine = null;
            }

            // 2. 대화 시스템 정리
            if (dialogSystem != null)
            {
                dialogSystem.OffDialog(0);
            }

            // 3. UI 정리
            CleanupUI();

            // 4. 상점 정리
            CleanupShop();

            // 5. 게임 상태 복구
            RestoreGameState();

            isFinished = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"상점 튜토리얼 스킵 중 오류: {e.Message}");
            isFinished = true;
        }
    }

    private void CleanupUI()
    {
        // 포인터 이미지 정리
        if (pointerImages != null)
        {
            for (int i = 0; i < pointerImages.Length; i++)
            {
                if (pointerImages[i] != null)
                    pointerImages[i].gameObject.SetActive(false);
            }
        }

        // 상점 UI 요소 정리 (첫 번째만 활성화)
        if (shopUIElements != null)
        {
            for (int i = 0; i < shopUIElements.Length; i++)
            {
                if (shopUIElements[i] != null)
                    shopUIElements[i].SetActive(i == 0);
            }
        }
    }

    private void CleanupShop()
    {
        if (shopManager != null)
        {
            shopManager.SetInteractable(true);
        }
    }

    private void RestoreGameState()
    {
        if (stageManager != null && !stageManager.isStart)
        {
            stageManager.ResumeGame();
        }
    }

    // Segment 구조체: 대사 시퀀스의 각 구간을 정의
    private struct Segment
    {
        public int start;
        public int count;
        public int enableIdx;
        public int disableIdx;
        // pre-actions
        public bool callMoveAll;
        public bool callResetAll;
        public int shopEnableIdx; // -1 means none
        public int shopDisableIdx; // -1 means none
    }

    // 코루틴을 통해 대사 시퀀스와 UI 조작을 순차적으로 실행
    private IEnumerator RunDialogSequence()
    {
        // define segments according to user's scenario
        Segment[] segments = new Segment[]
        {
            // 1: shopUI[0] 활성, pointer[0] 비활성
            new Segment{ start = 0,  count = 1, enableIdx = -1, disableIdx = 0, callMoveAll = false, callResetAll = false, shopEnableIdx = 0, shopDisableIdx = -1 },
            // 2: pointer[0] 활성
            // 추가: dialog.MoveAllToTarget 호출
            new Segment{ start = 1,  count = 2, enableIdx = 0,  disableIdx = -1, callMoveAll = true, callResetAll = false, shopEnableIdx = -1, shopDisableIdx = -1 },
            // 3: pointer[1] 활성, pointer[0] 비활성
            // 추가: shopUIElements[3] 비활성, shopUIElements[0] 활성
            // 추가: dialog.ResetAllToStart 호출
            new Segment{ start = 3,  count = 1, enableIdx = 1,  disableIdx = 0, callMoveAll = false, callResetAll = true, shopEnableIdx = 0, shopDisableIdx = 3 },
            // 4: MoveAll, pointer[2] 활성, pointer[1] 비활성
            new Segment{ start = 4,  count = 1, enableIdx = 2,  disableIdx = 1, callMoveAll = true,  callResetAll = false, shopEnableIdx = -1, shopDisableIdx = -1 },
            // 5: ResetAll, pointer[3] 활성, pointer[2] 비활성
            new Segment{ start = 5,  count = 1, enableIdx = 3,  disableIdx = 2, callMoveAll = false, callResetAll = true, shopEnableIdx = -1, shopDisableIdx = -1 },
            // 6: shopUI[0] off, shopUI[1] on, pointer[4] 활성, pointer[3] 비활성
            new Segment{ start = 6,  count = 1, enableIdx = 4,  disableIdx = 3, callMoveAll = false, callResetAll = false, shopEnableIdx = 1, shopDisableIdx = 0 },
            // 7: MoveAll, shopUI[1] off, shopUI[2] on, pointer[5] 활성, pointer[4] 비활성 (3 dialogs)
            new Segment{ start = 7,  count = 1, enableIdx = 5,  disableIdx = 4, callMoveAll = true,  callResetAll = false, shopEnableIdx = 2, shopDisableIdx = 1 },
            // 8: pointer[6] 활성, pointer[5] 비활성
            new Segment{ start = 8, count = 1, enableIdx = 6,  disableIdx = 5, callMoveAll = false, callResetAll = false, shopEnableIdx = -1, shopDisableIdx = -1 },
            // 9: pointer[7] 활성, pointer[6] 비활성
            new Segment{ start = 9, count = 1, enableIdx = 7,  disableIdx = 6, callMoveAll = false, callResetAll = false, shopEnableIdx = -1, shopDisableIdx = -1 },
            // 10: pointer[8] 활성, pointer[7] 비활성
            new Segment{ start = 10, count = 1, enableIdx = 8,  disableIdx = 7, callMoveAll = false, callResetAll = false, shopEnableIdx = -1, shopDisableIdx = -1 },
            // 11: pointer[9] 활성, pointer[8] 비활성
            new Segment{ start = 11, count = 1, enableIdx = 9,  disableIdx = 8, callMoveAll = false, callResetAll = false, shopEnableIdx = -1, shopDisableIdx = -1 },
            // 12: pointer[9] 비활성
            new Segment{ start = 12, count = 3, enableIdx = -1, disableIdx = 9, callMoveAll = false, callResetAll = false, shopEnableIdx = -1, shopDisableIdx = -1 },
            // 13: pointer[10] 활성, shopUI[2] 비활성, shopUI[3] 활성
            new Segment{ start = 15, count = 1, enableIdx = 10, disableIdx = -1, callMoveAll = false, callResetAll = false, shopEnableIdx = 3, shopDisableIdx = 2 },
            // 14: Dialog만 진행
            new Segment{ start = 16, count = 1, enableIdx = -1, disableIdx = -1, callMoveAll = false, callResetAll = false, shopEnableIdx = -1, shopDisableIdx = -1 },
            // 15: pointer[11] 활성, pointer[10] 비활성, shopUI[3] 비활성, shopUI[0] 활성
            new Segment{ start = 17, count = 1, enableIdx = 11, disableIdx = 10, callMoveAll = false, callResetAll = false, shopEnableIdx = 0, shopDisableIdx = 3 },
        };

        // run each segment sequentially
        foreach (var seg in segments)
        {
            // 스킵 확인
            if (!ShouldContinue()) yield break;

            // pre-actions: dialog movement and shop UI toggles (executed before pointer changes and dialog)
            if (seg.callMoveAll && dialogSystem != null)
                dialogSystem.MoveAllToTarget();

            if (seg.callResetAll && dialogSystem != null)
                dialogSystem.ResetAllToStart();

            if (seg.shopDisableIdx >= 0 && shopUIElements != null && seg.shopDisableIdx < shopUIElements.Length && shopUIElements[seg.shopDisableIdx] != null)
                shopUIElements[seg.shopDisableIdx].SetActive(false);

            if (seg.shopEnableIdx >= 0 && shopUIElements != null && seg.shopEnableIdx < shopUIElements.Length && shopUIElements[seg.shopEnableIdx] != null)
                shopUIElements[seg.shopEnableIdx].SetActive(true);

            // toggle pointers
            if (seg.disableIdx >= 0 && pointerImages != null && seg.disableIdx < pointerImages.Length && pointerImages[seg.disableIdx] != null)
                pointerImages[seg.disableIdx].gameObject.SetActive(false);

            if (seg.enableIdx >= 0 && pointerImages != null && seg.enableIdx < pointerImages.Length && pointerImages[seg.enableIdx] != null)
                pointerImages[seg.enableIdx].gameObject.SetActive(true);

            bool done = false;
            dialogSystem.PlayDialogRange(seg.start, seg.count, () => { done = true; });
            // wait until the segment finished (스킵 확인 포함)
            yield return new WaitUntil(() => done || !ShouldContinue());

            if (!ShouldContinue()) yield break;

            // after each segment wait 0.5s (as requested)
            float elapsed = 0f;
            while (ShouldContinue() && elapsed < 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!ShouldContinue()) yield break;
        }

        // sequence finished -> enable shop interaction
        if (pointerImages != null && pointerImages.Length > 11 && pointerImages[11] != null)
        {
            pointerImages[11].gameObject.SetActive(false);
        }
        dialogSystem.ResetAllToStart();
        shopManager.SetInteractable(true);
        dialogSystem.OffDialog(0);  
    }
}
