using UnityEngine;

/// <summary>
/// 마우스 클릭(무기 발사)을 기다리는 튜토리얼 단계입니다.
/// </summary>
public class TutorialStep_WaitForMouseClick : TutorialStep
{
    [Header("References")]
    [SerializeField]
    private WeaponType requiredWeaponType;
    [SerializeField]    // 인스펙터에서 직접 할당
    private WeaponSwitchSystem weaponSwitchSystem;
    private WeaponBase currentWeapon;

    [Header("Flags")]
    [SerializeField]
    private bool waitForFire = false; // true이면 발사를, false이면 무기 발사를 기다립니다.
    
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

        isFinished = false;
        if (weaponSwitchSystem == null)
        {
            Debug.LogError("WeaponSwitchSystem을 찾을 수 없습니다.");
            isFinished = true; // 오류 발생 시 강제 종료
            return;
        }

        // 튜토리얼 단계에 진입할 때 무기 교체를 비활성화합니다.
        weaponSwitchSystem.SetWeaponSwitchingEnabled(false);
    }

    public override void Execute(TutorialController controller)
    {
        // 스킵 상태 확인
        if (isSkipped)
        {
            controller.SetNextTutorial();
            return;
        }

        if (isFinished) return;

        currentWeapon = weaponSwitchSystem.CurrentWeapon;

        if (currentWeapon == null) return;

        // 1. 올바른 무기를 들고 있는지 확인
        if (currentWeapon.WeaponType == requiredWeaponType)
        {
            // 2. 발사를 기다리는 단계인지, 단순 장착을 기다리는 단계인지 확인
            if (waitForFire)
            {
                // 현재 무기가 발사 중인지 확인
                if (currentWeapon.IsAttack)
                {
                    isFinished = true;
                }
            }
            else
            {
                // 무기 장착만 확인되면 즉시 완료
                isFinished = true;
            }
        }
    }

    public override void Exit()
    {
        // 튜토리얼 단계가 끝날 때 무기 교체를 다시 활성화합니다.
        if (weaponSwitchSystem != null)
        {
            weaponSwitchSystem.SetWeaponSwitchingEnabled(true);
        }
    }
}
