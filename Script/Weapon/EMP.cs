using UnityEngine;
using System;

/// <summary>
/// EMP는 사용 시 Enemy의 행동을 즉시 중지시키고, 일정시간 동안 기절 시킵니다.
/// </summary>  
public class EMP : MonoBehaviour
{
    /// EMP 발동 이벤트
    /// Projectile은 발동 즉시 멈춰야하기 때문에 매개변수 없이 선언
    public static event Action<int> OnEmpActivated;
    public static event Action OnEmpProjectileActivated;

    [Header("Audio")]
    [SerializeField]
    private AudioClip audioEMP; 
    private AudioSource audioSource; 

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void EMPAction(int stunTime)
    {
        audioSource.PlayOneShot(audioEMP);

        // Enemy의 기절시간을 조정하려면 stunTime 매개변수를 변경
        OnEmpActivated?.Invoke(stunTime);
        OnEmpProjectileActivated?.Invoke();
    }
}
