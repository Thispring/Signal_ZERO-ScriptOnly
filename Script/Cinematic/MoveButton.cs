using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Dotween을 활용해 버튼 이동 연출을 담당하는 스크립트 입니다.
/// </summary>
public class MoveButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField]
    private Button[] buttons;

    [Header("Pos Values")]
    private Vector3[] originalPositions; 
    private Vector3[] targetPositions;
    private Vector3[] startPositions;
     
    [Header("DOTween")]
    private Sequence sequence; // DOTween 시퀀스

    void Awake()
    {
        originalPositions = new Vector3[buttons.Length];
        targetPositions = new Vector3[buttons.Length];
        startPositions = new Vector3[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            originalPositions[i] = buttons[i].transform.position;
            buttons[i].transform.position = originalPositions[i]; // 버튼 위치 초기화
            targetPositions[i] = originalPositions[i]; // 목표 위치는 원래 위치와 동일
            startPositions[i] = originalPositions[i] + Vector3.right * 500f * (i + 1) * 50f; // 시작 위치는 x축 -500만큼 이동
            // i의 위치에 따라 시작 위치를 다르게 설정
            buttons[i].transform.position = startPositions[i]; // 시작 위치 이동
        }
    }

    void Start()
    {
        sequence = DOTween.Sequence(); // 새로운 시퀀스 생성
    
        sequence.AppendCallback(() =>
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                // 각 버튼을 시작 위치에서 목표 위치로 이동 (가속도 적용)
                buttons[i].transform.DOMove(targetPositions[i], 3f).SetEase(Ease.OutCubic);
            }
        });
    }
}
