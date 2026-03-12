using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public enum Speaker { 리아 = 0 }

/// <summary>
/// 대사를 관리하는 시스템 스크립트 입니다.
/// </summary>
public class DialogSystem : MonoBehaviour
{
	public int DialogCount => dialogs != null ? dialogs.Length : 0;

	[SerializeField]
	private Dialog[] dialogs;                        // 현재 분기의 대사 목록
	[SerializeField]
	private Image[] charImages;                     // 캐릭터 Image UI
	[SerializeField]
	private Image[] imageDialogs;                   // 대화창 Image UI
	[SerializeField]
	private TextMeshProUGUI[] textNames;                        // 현재 대사중인 캐릭터 이름 출력 Text UI
	[SerializeField]
	private TextMeshProUGUI[] textDialogues;                    // 현재 대사 출력 Text UI
	[SerializeField]
	private GameObject[] objectArrows;                  // 대사가 완료되었을 때 출력되는 커서 오브젝트
	[SerializeField]
	private float typingSpeed;                  // 텍스트 타이핑 효과의 재생 속도
	[SerializeField]
	private KeyCode keyCodeSkip = KeyCode.Space;    // 타이핑 효과를 스킵하는 키

	// 시작 위치 저장 배열
	// 튜토리얼의 특정 단계에서 UI를 가리는 현상이 있어 이를 보완하기 위해 시작 위치를 저장하고 복원하는 기능 추가
	private Vector3[] startCharImagePositions;
	private Vector3[] startImageDialogPositions;
	private Vector3[] startTextNamePositions;
	private Vector3[] startTextDialoguePositions;
	private Vector3[] startObjectArrowPositions;

	// 인스펙터에서 할당 가능한 목표 위치 (모든 항목을 같은 위치로 이동시키고 싶을 때 사용)
	// 필요하면 배열로 확장하세요.
	// 각 UI 타입별 목표 위치(인스펙터에서 각 요소별로 다른 목표 위치 할당 가능)
	[SerializeField, Tooltip("캐릭터 이미지의 목표 로컬 Y 위치")]
	private float targetCharImagesY = 0f;
	[SerializeField, Tooltip("대화창 이미지의 목표 로컬 Y 위치")]
	private float targetImageDialogsY = 0f;

	private int currentIndex = -1;
	private bool isTypingEffect = false;            // 텍스트 타이핑 효과를 재생중인지
	private Speaker currentSpeaker = Speaker.리아;

	private bool canProceed = false;
	private Coroutine waitCoroutine;

	// 추가: TypingText의 자동 다음 진행을 제어 (범위 재생 시 false로 설정)
	private bool autoAdvanceEnabled = true;
	private bool skipPressedDuringTyping = false; // 스킵으로 현재 대사만 완성했음을 표시
	private Coroutine typingCoroutine; // TypingText 코루틴 참조 저장
	private float lastInputTime = -10f; // 최근 입력 타임스탬프 (unscaled)
	[SerializeField]
	private float inputBufferTime = 0.15f; // 최근 입력을 추가 입력으로 간주할 시간(초)

	private void Awake()
	{
		// 각 배열의 시작(local) 위치를 저장합니다.
		if (charImages != null)
		{
			startCharImagePositions = new Vector3[charImages.Length];
			for (int i = 0; i < charImages.Length; i++)
				if (charImages[i] != null) startCharImagePositions[i] = charImages[i].transform.localPosition;
		}

		if (imageDialogs != null)
		{
			startImageDialogPositions = new Vector3[imageDialogs.Length];
			for (int i = 0; i < imageDialogs.Length; i++)
				if (imageDialogs[i] != null) startImageDialogPositions[i] = imageDialogs[i].transform.localPosition;
		}

		if (textNames != null)
		{
			startTextNamePositions = new Vector3[textNames.Length];
			for (int i = 0; i < textNames.Length; i++)
				if (textNames[i] != null) startTextNamePositions[i] = textNames[i].transform.localPosition;
		}

		if (textDialogues != null)
		{
			startTextDialoguePositions = new Vector3[textDialogues.Length];
			for (int i = 0; i < textDialogues.Length; i++)
				if (textDialogues[i] != null) startTextDialoguePositions[i] = textDialogues[i].transform.localPosition;
		}

		if (objectArrows != null)
		{
			startObjectArrowPositions = new Vector3[objectArrows.Length];
			for (int i = 0; i < objectArrows.Length; i++)
				if (objectArrows[i] != null) startObjectArrowPositions[i] = objectArrows[i].transform.localPosition;
		}
	}

	void Update()
	{
		// 타이핑 효과 재생 중 스페이스바 또는 마우스 왼쪽 클릭 입력 시
		if (isTypingEffect && (Input.GetKeyDown(keyCodeSkip) || Input.GetMouseButtonDown(0)))
		{
			// 현재 실행중인 TypingText 코루틴을 중지 (참조로 안전하게 중지)
			if (typingCoroutine != null)
			{
				StopCoroutine(typingCoroutine);
				typingCoroutine = null;
			}

			// 현재 대사의 전체 텍스트를 즉시 표시
			if (dialogs != null && currentIndex >= 0 && currentIndex < dialogs.Length)
			{
				textDialogues[(int)currentSpeaker].text = dialogs[currentIndex].dialogue;
			}

			// 대사 완료 커서 활성화
			if (objectArrows != null && objectArrows.Length > (int)currentSpeaker && objectArrows[(int)currentSpeaker] != null)
				objectArrows[(int)currentSpeaker].SetActive(true);

			// 타이핑이 끝났음을 알림
			isTypingEffect = false;

			// 스킵으로 인해 현재 대사만 완성된 상태 표시
			skipPressedDuringTyping = true;
			lastInputTime = Time.unscaledTime; // 스킵 입력 시점 기록 (추가 입력 버퍼로 사용)

			// 자동 진행이 활성화되어 있고 다음 대사가 있다면 즉시 다음 대사로 진행
			if (autoAdvanceEnabled && currentIndex + 1 < dialogs.Length)
			{
				canProceed = false;
				if (waitCoroutine != null)
					StopCoroutine(waitCoroutine);
				waitCoroutine = StartCoroutine(WaitBeforeDialog());
			}
		}

		// 입력 이벤트 로깅 및 타임스탬프 기록
		// 이전: 모든 입력에서 타임스탬프를 기록했음 -> 외부 클릭(튜토리얼 진행)이 skip 버퍼로 오인되는 문제 발생
		// 변경: 타임스탬프는 오직 타이핑 중 스킵 처리 시에만 기록합니다.
	}

	// 모든 관련 UI를 targetPos로 이동 (실행 시 즉시 이동)
	public void MoveAllToTarget()
	{
		// 캐릭터 이미지 목표 위치로 이동
		if (charImages != null)
		{
			for (int i = 0; i < charImages.Length; i++)
				if (charImages[i] != null)
				{
					var pos = charImages[i].transform.localPosition;
					pos.y = targetCharImagesY;
					charImages[i].transform.localPosition = pos;
				}
		}

		// 대화창 이미지 목표 위치로 이동
		if (imageDialogs != null)
		{
			for (int i = 0; i < imageDialogs.Length; i++)
				if (imageDialogs[i] != null)
				{
					var pos = imageDialogs[i].transform.localPosition;
					pos.y = targetImageDialogsY;
					imageDialogs[i].transform.localPosition = pos;
				}
		}
	}

	// 저장된 시작 위치로 복원
	public void ResetAllToStart()
	{
		if (charImages != null && startCharImagePositions != null)
		{
			for (int i = 0; i < charImages.Length && i < startCharImagePositions.Length; i++)
				if (charImages[i] != null)
				{
					var pos = charImages[i].transform.localPosition;
					pos.y = startCharImagePositions[i].y;
					charImages[i].transform.localPosition = pos;
				}
		}

		if (imageDialogs != null && startImageDialogPositions != null)
		{
			for (int i = 0; i < imageDialogs.Length && i < startImageDialogPositions.Length; i++)
				if (imageDialogs[i] != null)
				{
					var pos = imageDialogs[i].transform.localPosition;
					pos.y = startImageDialogPositions[i].y;
					imageDialogs[i].transform.localPosition = pos;
				}
		}
	}

	public Coroutine PlayDialogRange(int startIndex, int count, Action onComplete = null)
	{
		// 안전성 검사
		if (dialogs == null || dialogs.Length == 0 || count <= 0)
		{
			onComplete?.Invoke();
			return null;
		}

		if (startIndex < 0) startIndex = 0;
		if (startIndex >= dialogs.Length)
		{
			onComplete?.Invoke();
			return null;
		}

		int lastIndex = Mathf.Min(startIndex + count - 1, dialogs.Length - 1);

		// 현재 대사 관련 코루틴 정리
		if (waitCoroutine != null)
		{
			StopCoroutine(waitCoroutine);
			waitCoroutine = null;
		}
		if (isTypingEffect)
		{
			// string 기반 호출 대신 참조 기반으로 안전하게 중지
			if (typingCoroutine != null)
			{
				StopCoroutine(typingCoroutine);
				typingCoroutine = null;
			}
			isTypingEffect = false;
		}

		// 자동 진행 비활성화 및 진행 블로킹
		autoAdvanceEnabled = false;
		canProceed = false; // 외부 업데이트/즉시 진행 차단

		return StartCoroutine(PlayDialogRangeCoroutine(startIndex, lastIndex, () =>
		{
			// 재생 완료 후 원복
			autoAdvanceEnabled = true;
			canProceed = true; // 필요하면 true로 복원 (또는 onComplete에서 제어)
			onComplete?.Invoke();
		}));
	}

	private IEnumerator PlayDialogRangeCoroutine(int start, int end, Action onComplete)
	{
		// 이전 화자 UI 비활성화
		if (currentIndex >= 0)
			InActiveObjects((int)currentSpeaker);

		// 현재 인덱스를 start-1로 만들어 SetNextDialog가 정상 작동하도록 함
		currentIndex = start - 1;

		for (int idx = start; idx <= end; idx++)
		{
			// 다음 대사 표시 (SetNextDialog는 currentIndex++ 내부)
			SetNextDialog();

			// 타이핑 완료될 때까지 대기
			yield return new WaitUntil(() => !isTypingEffect);

			// 사용자가 타이핑 중에 스킵을 눌러 현재 대사를 완성한 경우:
			if (skipPressedDuringTyping)
			{
				// 최근 입력(스킵) 타임스탬프가 남아 있으면 이를 추가 입력으로 간주하여 즉시 진행
				if (Time.unscaledTime - lastInputTime <= inputBufferTime)
				{
					skipPressedDuringTyping = false;
				}
				else
				{
					yield return new WaitUntil(() => Input.GetKeyDown(keyCodeSkip) || Input.GetMouseButtonDown(0));
					skipPressedDuringTyping = false;
				}
			}
			else
			{
				// 스킵이 아닌 정상 완료라면 기존처럼 잠시 대기 후 자동 진행
				yield return new WaitForSecondsRealtime(0.5f);
			}
		}

		// 범위 재생 끝나면 자동 진행 원복
		autoAdvanceEnabled = true;

		onComplete?.Invoke();
	}

	public void Setup()
	{
		canProceed = false;
		if (waitCoroutine != null)
			StopCoroutine(waitCoroutine);
		waitCoroutine = StartCoroutine(WaitBeforeDialog());
	}

	// 1초 대기 후 canProceed를 true로 만드는 코루틴 추가
	private IEnumerator WaitBeforeDialog()
	{
		yield return new WaitForSecondsRealtime(1f);
		canProceed = true;
		SetNextDialog();
	}

	public bool UpdateDialog()
	{
		if (!canProceed)
			return false;

		// 마지막 대사라면 true 반환
		if (currentIndex == dialogs.Length - 1 && !isTypingEffect)
		{
			return true;
		}

		return false;
	}

	private void SetNextDialog()
	{
		currentIndex++;

		// 현재 화자 설정
		currentSpeaker = dialogs[currentIndex].speaker;

		// 캐릭터 이미지 활성화
		charImages[(int)currentSpeaker].gameObject.SetActive(true);

		// 대화창 활성화
		imageDialogs[(int)currentSpeaker].gameObject.SetActive(true);

		// 현재 화자 이름 텍스트 활성화 및 설정
		textNames[(int)currentSpeaker].gameObject.SetActive(true);
		textNames[(int)currentSpeaker].text = dialogs[currentIndex].speaker.ToString();

		// 화자의 대사 텍스트 활성화 및 설정 (Typing Effect)
		textDialogues[(int)currentSpeaker].gameObject.SetActive(true);
		typingCoroutine = StartCoroutine(TypingText());
	}

	private void InActiveObjects(int index)
	{
		imageDialogs[index].gameObject.SetActive(false);
		textNames[index].gameObject.SetActive(false);
		textDialogues[index].gameObject.SetActive(false);
		objectArrows[index].SetActive(false);
	}

	public void OffDialog(int index)
	{
		charImages[index].gameObject.SetActive(false);
		imageDialogs[index].gameObject.SetActive(false);
		textNames[index].gameObject.SetActive(false);
		textDialogues[index].gameObject.SetActive(false);
		objectArrows[index].SetActive(false);
	}

	private IEnumerator TypingText()
	{
		int index = 0;
		isTypingEffect = true;

		// 텍스트를 한글자씩 타이핑치듯 재생
		while (index < dialogs[currentIndex].dialogue.Length)
		{
			textDialogues[(int)currentSpeaker].text = dialogs[currentIndex].dialogue.Substring(0, index);
			index++;
			yield return new WaitForSecondsRealtime(typingSpeed);
		}

		isTypingEffect = false;
		typingCoroutine = null; // 코루틴 정상 종료 시 참조 null로 변경
								// 대사가 완료되었을 때 출력되는 커서 활성화
		objectArrows[(int)currentSpeaker].SetActive(true);

		// --- 자동 진행은 플래그로 제어 ---
		if (autoAdvanceEnabled)
		{
			yield return new WaitForSecondsRealtime(0.5f); // 대사 끝난 후 잠깐 대기(선택)
			if (dialogs.Length > currentIndex + 1)
			{
				canProceed = false;
				if (waitCoroutine != null)
					StopCoroutine(waitCoroutine);
				waitCoroutine = StartCoroutine(WaitBeforeDialog());
			}
		}
		// 마지막 대사라면 아무것도 하지 않음(튜토리얼 컨트롤러에서 처리)
	}
}

[System.Serializable]
public struct Dialog
{
	public Speaker speaker; // 화자
	[TextArea(3, 5)]
	public string dialogue; // 대사
}
