using UnityEngine;

/// <summary>
/// Player의 사격 관련 스크립트 입니다.
/// 고정시점뷰를 고려하여 마우스 입력을 통한 main Camera Ray를 쏘고, 해당 방향으로 UI, GameObject를 이동시킵니다.
/// </summary>
public class PlayerShotController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]    // Inspector에서 할당
    private PlayerStatusManager playerStatusManager;  // Player의 상태를 관리하는 스크립트
    [SerializeField]    // Inspector에서 할당
    private PlayerHUD playerHUD; // 스나이퍼 줌 HUD 제어용 참조
    private WeaponBase weapon;  // 모든 무기가 상속받는 기반 클래스

    // 모든 UI Elements 요소 Inspector에서 할당
    [Header("UI Elements")]
    [SerializeField]
    private RectTransform aimUI;
    [SerializeField]
    private RectTransform scopeUI;
    [SerializeField]
    private Canvas canvas;

    [Header("Ray Settings")]
    private float maxRayDistance = 100f;

    [Header("Transform")]
    [SerializeField]    // Inspector에서 할당
    private Transform bulletSpawnPoint;

    [Header("Ray")]
    private Ray ray;
    private RaycastHit hitInfo;

    [Header("Camera Settings")]
    [SerializeField]    // Inspector에서 할당
    private Camera sniperCamera;
    private Vector3 cameraOriginPos;
    private bool isCameraMoved = false;
    private float cameraMoveSpeed = 5f; // 이동 속도

    [Header("Flags")]
    // 재장전 상태 전이 감지용 캐시
    private bool prevIsReload = false;

    [Header("Enemy Outline Tracking")]
    // Inspector에서 확인용
    [SerializeField]
    private EnemyEffectController lastOutlinedEffect = null;
    [SerializeField]
    private BossDroneManager lastOutlinedBossDrone = null;

    [Header("Tutorial")]
    public bool isTutorialAction = false; // 튜토리얼 모드 여부
    public bool isTutorialBurstAction = false; // 튜토리얼에서 버스트 사용 여부

    void Awake()
    {
        // 튜토리얼 flag 초기화
        isTutorialAction = false;
        isTutorialBurstAction = false;
    }

    void Start()
    {
        cameraOriginPos = Camera.main.transform.position;
    }

    void Update()
    {
        // isTutorialAction가 false일 때 무기 액션 중지
        if (Time.timeScale != 0 && isTutorialAction)    // 게임이 일시정지 되지 않았을 때만 실행
            UpdateWeaponAction();   // 무기 액션 업데이트

        // 1920x1080 해상도 기준으로 마우스 위치 클램핑 및 카메라 이동 처리
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.x = Mathf.Clamp(mouseScreenPos.x, 0, 1920);
        mouseScreenPos.y = Mathf.Clamp(mouseScreenPos.y, 0, 1080);

        // 화면 끝에 닿았는지 체크 후, 해당 방향으로 이동하는 애니메이션 코드
        Vector3 cameraTargetOffset = Vector3.zero;
        if (mouseScreenPos.x <= 0)
            cameraTargetOffset.x = -1f;
        else if (mouseScreenPos.x >= 854)
            cameraTargetOffset.x = 1f;

        if (mouseScreenPos.y <= 0)
            cameraTargetOffset.y = -1f;
        else if (mouseScreenPos.y >= 480)
            cameraTargetOffset.y = 1f;

        if (cameraTargetOffset != Vector3.zero)
        {
            // 카메라를 부드럽게 이동
            Camera.main.transform.position = Vector3.Lerp(
                Camera.main.transform.position,
                cameraOriginPos + cameraTargetOffset,
                Time.deltaTime * cameraMoveSpeed
            );
            isCameraMoved = true;
        }
        else if (isCameraMoved)
        {
            // 마우스가 다시 해상도 안으로 들어오면 원래 위치로 복귀
            Camera.main.transform.position = Vector3.Lerp(
                Camera.main.transform.position,
                cameraOriginPos,
                Time.deltaTime * cameraMoveSpeed
            );
            // 복귀가 거의 완료되면 플래그 해제
            if (Vector3.Distance(Camera.main.transform.position, cameraOriginPos) < 0.01f)
                isCameraMoved = false;
        }

        // SniperRifle 망원 효과를 위한 로직
        if (weapon is WeaponSniperRifle && weapon.IsBurst == false)
        {
            ray = Camera.main.ScreenPointToRay(mouseScreenPos);
            UpdateSniperCamera(mouseScreenPos);

            // 마우스 입력에 따라 줌 HUD 애니메이션/리셋
            if (Input.GetMouseButtonDown(0))
            {
                if (playerHUD != null && weapon.IsReload == false)
                {
                    playerHUD.AnimateSniperZoomHUD();
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (playerHUD != null && weapon.IsReload == false)
                {
                    playerHUD.ResetSniperZoomHUD(0f);
                    // 버스트 중에는 블러 유지
                    if (!PlayerStatusManager.Instance.isBurstActive)
                        playerHUD.ResetScopeBlur(); // 블러 즉시 0
                }
            }

            // 마우스를 누른 상태에서 재장전 상태 전이 감지 시 즉시 HUD 갱신
            if (playerHUD != null && Input.GetMouseButton(0))
            {
                bool isReloadNow = weapon.IsReload;
                // true -> false (재장전 종료): 바로 확대 HUD 적용
                if (prevIsReload && !isReloadNow)
                {
                    playerHUD.AnimateSniperZoomHUD();
                }
                // false -> true (재장전 시작): 바로 축소 HUD 및 블러 초기화
                else if (!prevIsReload && isReloadNow)
                {
                    playerHUD.ResetSniperZoomHUD(0f);
                    if (!PlayerStatusManager.Instance.isBurstActive)
                        playerHUD.ResetScopeBlur();
                }
                prevIsReload = isReloadNow;
            }
        }
        else if (weapon is WeaponSniperRifle && weapon.IsBurst)
        {
            ray = Camera.main.ScreenPointToRay(mouseScreenPos);
            UpdateSniperCamera(mouseScreenPos);

            if (playerHUD != null)
            {
                playerHUD.SetScopeBlurMax();
                playerHUD.AnimateSniperZoomHUD();
            }
        }
        else
        {
            ray = Camera.main.ScreenPointToRay(mouseScreenPos);
            // 스나이퍼가 아닌 무기 전환 시 HUD를 기본 크기로 복구(선택)
            if (playerHUD != null)
            {
                playerHUD.ResetSniperZoomHUD(0f);
                playerHUD.ResetScopeBlur();
            }
            // 스나이퍼가 아닐 땐 캐시 리셋
            prevIsReload = false;
        }

        Vector3 targetPosition = ray.origin + ray.direction * 5f;
        Vector3 adjustedTargetPosition = new Vector3(targetPosition.x, targetPosition.y, 5f);

        // 오브젝트와 충돌 여부 체크
        if (Physics.Raycast(ray, out hitInfo, maxRayDistance))
        {
            targetPosition = hitInfo.point;
            UpdateAimUI(hitInfo.point);
            UpdateBulletSpawnPoint(hitInfo.point); // 총알 스폰위치 업데이트
        }
        else
        {
            targetPosition = ray.origin + ray.direction * maxRayDistance;
            Vector3 rayEndPoint = ray.origin + ray.direction * maxRayDistance;
            UpdateAimUI(rayEndPoint);
            UpdateBulletSpawnPoint(hitInfo.point); // 총알 스폰위치 업데이트
        }
        // 타겟위치의 Z값을 5로 고정
        targetPosition.z = 5f;

        // Enemy 아웃라인 보는 코드
        // Enemy 머테리얼을 Toon 셰이더로 설정 후 _Outline_Width 속성 조절
        if (Physics.Raycast(ray, out hitInfo, maxRayDistance))
        {
            if (hitInfo.collider.CompareTag("EnemySmall") || hitInfo.collider.CompareTag("EnemyMedium")
            || hitInfo.collider.CompareTag("EnemyBig"))
            {
                GameObject enemyObj = hitInfo.collider.gameObject;
                EnemyEffectController effect = enemyObj.GetComponentInParent<EnemyEffectController>();

                if (effect != null)
                {
                    // 이전 Enemy와 다르면 이전 Enemy의 Outline을 0으로
                    if (lastOutlinedEffect != null && lastOutlinedEffect != effect)
                    {
                        Debug.Log("Resetting outline of previous enemy");
                        SetOutlineWidth(lastOutlinedEffect, 0f);
                    }
                    SetOutlineWidth(effect, 0.2f);
                    lastOutlinedEffect = effect;
                }
            }
        }
        else
        {
            if (lastOutlinedEffect != null)
            {
                SetOutlineWidth(lastOutlinedEffect, 0f);
                lastOutlinedEffect = null;
            }
        }

        if (Physics.Raycast(ray, out hitInfo, maxRayDistance))
        {
            if (hitInfo.collider.CompareTag("BossDrone"))
            {
                GameObject enemyObj = hitInfo.collider.gameObject;
                BossDroneManager bossDrone = enemyObj.GetComponentInParent<BossDroneManager>();

                if (bossDrone != null)
                {
                    // 이전 BossDrone과 다르면 이전 BossDrone의 Outline을 0으로
                    if (lastOutlinedBossDrone != null && lastOutlinedBossDrone != bossDrone)
                    {
                        SetOutlineWidthBossDrone(lastOutlinedBossDrone, 0f);
                    }
                    SetOutlineWidthBossDrone(bossDrone, 0.2f);
                    lastOutlinedBossDrone = bossDrone;
                }
            }
        }
        else
        {
            if (lastOutlinedBossDrone != null)
            {
                SetOutlineWidthBossDrone(lastOutlinedBossDrone, 0f);
                lastOutlinedBossDrone = null;
            }
        }
    }

    private void SetOutlineWidth(EnemyEffectController effect, float width)
    {
        // 부모(EnemyEffectController) 아래 모든 자식 Renderer를 순회
        SkinnedMeshRenderer[] renderers = effect.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer rend in renderers)
        {
            foreach (var mat in rend.materials)
            {
                if (mat != null && mat.HasProperty("_Outline_Width"))
                {
                    mat.SetFloat("_Outline_Width", width);
                }
            }
        }
    }

    private void SetOutlineWidthBossDrone(BossDroneManager effect, float width)
    {
        // 부모(EnemyEffectController) 아래 모든 자식 Renderer를 순회
        MeshRenderer[] renderers = effect.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer rend in renderers)
        {
            foreach (var mat in rend.materials)
            {
                if (mat != null && mat.HasProperty("_Outline_Width"))
                {
                    mat.SetFloat("_Outline_Width", width);
                }
            }
        }
    }

    private void UpdateSniperCamera(Vector3 mouseScreenPos)
    {
        // 마우스 포지션을 월드 좌표로 변환
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);
        Vector3 sniperPosition = ray.origin + ray.direction * 10f; // z축 기준으로 적절한 거리 설정
        sniperPosition.z = 5f; // z축 고정

        // sniperCamera 위치 업데이트
        sniperCamera.transform.position = sniperPosition;

        // sniperCamera가 마우스 방향을 바라보도록 설정
        sniperCamera.transform.LookAt(ray.origin + ray.direction * maxRayDistance);
    }

    private void UpdateBulletSpawnPoint(Vector3 worldPosition)
    {
        // worldPosition의 z값을 3으로 고정하고 localPosition으로 변환
        Vector3 adjustedPosition = new Vector3(worldPosition.x, worldPosition.y, 3f);

        // 총알 스폰 위치 업데이트
        bulletSpawnPoint.position = adjustedPosition;
    }

    private void UpdateAimUI(Vector3 worldPosition)
    {
        // world position을 screen position으로 변환
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPosition);

        // screen position을 canvas의 local position으로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPoint,
            canvas.worldCamera,
            out Vector2 localPoint);

        // 사격 관련 UI 위치 업데이트
        aimUI.anchoredPosition = localPoint;
        scopeUI.anchoredPosition = localPoint;
    }

    private void UpdateWeaponAction()
    {
        // Single, Rapid 사격 처리
        if ((weapon is WeaponSingleRifle || weapon is WeaponRapidRifle) && Input.GetMouseButtonDown(0))
        {
            playerStatusManager.isHiding = false;
            weapon.StartWeaponAction();
        }
        else if (Input.GetMouseButtonUp(0)) // 마우스 왼쪽 버튼을 뗐을 때
        {
            weapon.StopWeaponAction();
            playerStatusManager.isHiding = true;
        }

        // Sniper 사격 처리
        if (weapon.IsReload == false && weapon is WeaponSniperRifle && Input.GetMouseButton(0))
        {
            // isShot 상태조절을 통해, 재장전 중 사격 중복을 방지
            weapon.StartWeaponAction();
        }
        else if (weapon is WeaponSniperRifle && Input.GetMouseButtonUp(0))  // 왼쪽 버튼을 뗐을 때
        {
            weapon.StopWeaponAction();
            playerStatusManager.isHiding = true;
        }

        // 재장전
        // 재장전 시 엄폐 상태 유지
        if (Input.GetKeyDown(KeyCode.R)) // 'R' 키 눌렀을 시 재장전
        {
            weapon.StartReload();
        }

        if (isTutorialBurstAction && playerStatusManager.isBurstReady && Input.GetKeyDown(KeyCode.F))
        {
            // 버스트 함수 실행
            playerStatusManager.isBurstActive = true; // 버스트 활성화
            playerStatusManager.burstCurrentGauge = 0; // 버스트 게이지 초기화
            weapon.WeaponBurst();
        }
    }

    // 튜토리얼에서 버스트 액션을 위한 외부 함수
    public void TutorialBurstActionStart()
    {
        // 버스트 함수 실행
        playerStatusManager.isBurstActive = true; // 버스트 활성화
        playerStatusManager.burstCurrentGauge = 0; // 버스트 게이지 초기화
        weapon.WeaponBurst();
    }

    public void SwitchingWeapon(WeaponBase newWeapon)
    {
        if (weapon != null)
        {
            weapon.StopWeaponAction(); // 기존 무기 액션 종료
        }

        weapon = newWeapon;
        weapon.ResetWeaponState(); // 새 무기의 상태 초기화
    }

    // 디버그용 Gizmos 
    void OnDrawGizmos()
    {
        if (ray.direction != Vector3.zero)
        {
            Gizmos.color = Color.red;

            if (Physics.Raycast(ray, out hitInfo, maxRayDistance))
            {
                Gizmos.DrawLine(ray.origin, hitInfo.point);

                Gizmos.DrawSphere(hitInfo.point, 0.1f);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(bulletSpawnPoint.position, hitInfo.point);
            }
            else
            {
                Vector3 rayEndPoint = ray.origin + ray.direction * maxRayDistance;
                Gizmos.DrawLine(ray.origin, rayEndPoint);

                Gizmos.DrawSphere(rayEndPoint, 0.1f);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(bulletSpawnPoint.position, rayEndPoint);
            }
        }
    }
}
