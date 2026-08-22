# Signal ZERO

Unity 기반으로 제작한 3인칭 슈팅 게임입니다.

기존에 개발한 **Cord: Marigold**의 게임 시스템을 기반으로, 공모전 출품을 위해 게임의 아트 에셋과 세계관을 새롭게 구성하고 보스전, 보조 로봇, 튜토리얼 등 새로운 게임 플레이 시스템을 추가 개발했습니다.

본 리포지토리는 포트폴리오 공개를 목적으로 프로젝트의 스크립트 코드만 포함하고 있습니다.

---

## 📖 프로젝트 소개

Signal ZERO는 적과 전투하며 스테이지를 진행하는 3인칭 슈팅 게임입니다.

기존 프로젝트의 기본적인 전투와 스테이지 진행 시스템을 기반으로 게임의 모든 아트 에셋을 변경했으며, 공모전 출품을 위한 새로운 게임 경험을 구성하기 위해 보스전과 보조 로봇 시스템, 단계별 튜토리얼 시스템 등을 추가로 구현했습니다.

특히 기존 프로젝트에서 반복되는 일반 적 전투 중심의 게임 구조를 확장하여, 보스의 공격 패턴과 드론 소환을 활용한 보스전과 플레이어를 지원하는 보조 로봇 시스템을 추가했습니다.

| 항목 | 내용 |
| --- | --- |
| 개발 기간 | 1개월 |
| 개발 인원 | 총 4명 |
| 팀 구성 | 아트 3명 / 기획·프로그래밍 1명 (본인) |
| 플랫폼 | PC |
| 개발 엔진 | Unity |
| 개발 언어 | C# |
| 담당 역할 | 게임 기획 / 클라이언트 프로그래밍 |

---

## 🎮 Gameplay

게임 플레이 영상은 아래 링크에서 확인할 수 있습니다.

[Signal ZERO Gameplay Video](https://www.youtube.com/watch?v=Flv2JUqmpLE)

---

## 👨‍💻 My Role

### Client Programming

- 기존 Cord: Marigold의 게임 시스템 확장 및 신규 시스템 구현
- 보스의 공격 패턴 및 드론 소환 시스템 구현
- 보스 미사일 및 발사체 공격 구현
- 보조 로봇의 지원 스킬 및 상태 시스템 구현
- 적 탐색 및 보조 로봇 자동 공격 시스템 구현
- 적 보호막 및 상태 연동 시스템 구현
- 단계별 진행이 가능한 튜토리얼 시스템 구현
- 기존 전투, 스테이지 및 상점 시스템 수정 및 확장
- UI, 컷신 및 게임 진행 관련 기능 구현

---

# 🎯 주요 구현 기능

## 🤖 보스 패턴 및 드론 연동 시스템

보스전에서 단순히 일정한 공격을 반복하는 것이 아니라, 공격 대기 시간 동안 드론을 소환하고 드론의 생존 상태에 따라 보스의 공격 강도를 결정하도록 구현했습니다.

`BossFSM`은 공격 대기 → 드론 소환 → 공격 → 다음 패턴의 흐름을 관리합니다.

보스가 소환한 드론의 수가 일정 수준 이하로 감소하면 다음 공격의 강도를 조절하며, 공격이 종료될 때마다 시퀀스가 증가하도록 구성했습니다.

중간 보스는 미사일과 일반 공격을 번갈아 사용하며, 최종 보스는 별도의 공격 패턴을 사용하도록 보스 타입에 따라 행동을 분기했습니다.

**관련 코드**

- [BossFSM.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/Boss/BossFSM.cs)
- [BossBehaviorSystem.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/Boss/BossBehaviorSystem.cs)
- [BossDroneManager.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/Boss/BossDroneManager.cs)
- [BossStatusManager.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/Boss/BossStatusManager.cs)

---

## 🛡️ 보조 로봇 지원 시스템

플레이어를 지원하는 보조 로봇을 구현하고, 로봇의 종류에 따라 서로 다른 지원 기능을 사용할 수 있도록 구성했습니다.

보조 로봇은 `Guard`, `Shield`, `Heal` 타입으로 구분되며, 각 타입은 플레이어 방어, 보호막, 회복 등의 역할을 수행합니다.

스킬 사용에는 지속 시간과 쿨다운을 적용했으며, 업그레이드 단계에 따라 자동 재장전이나 가장 가까운 적을 자동으로 공격하는 기능이 추가되도록 구현했습니다.

적 탐색은 별도의 `SearchEnemyManager`에서 활성화된 적을 관리하고, 위치를 기준으로 가장 가까운 적을 탐색하도록 구성했습니다.

**관련 코드**

- [SupportBotFSM.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/SupportBot/SupportBotFSM.cs)
- [SupportBotStatusManager.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/SupportBot/SupportBotStatusManager.cs)
- [SupportBotSetting.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/SupportBot/SupportBotSetting.cs)
- [SearchEnemyManager.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/GameManager/SearchEnemyManager.cs)

---

## 📚 단계별 튜토리얼 시스템

게임의 주요 조작과 시스템을 순차적으로 안내할 수 있도록 단계별 튜토리얼 시스템을 구현했습니다.

`TutorialStep`을 추상 클래스로 구성하고, 각 튜토리얼 기능을 독립적인 클래스로 구현하여 새로운 튜토리얼 단계를 추가할 수 있도록 구성했습니다.

`TutorialController`는 현재 진행 중인 튜토리얼 단계를 관리하며, 현재 단계가 완료되면 다음 단계로 이동합니다.

적 생성, 적 처치, 보조 로봇 활성화, 스킬 사용, 상점 이용 등 게임의 주요 기능을 각각 독립적인 튜토리얼 단계로 구성했습니다.

**관련 코드**

- [TutorialController.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/Tutorial/TutorialController.cs)
- [TutorialStep.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/Tutorial/TutorialStep.cs)
- [TutorialStep_SpawnEnemies.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/Tutorial/TutorialStep_SpawnEnemies.cs)
- [TutorialStep_ActiveSupportBot.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/Tutorial/TutorialStep_ActiveSupportBot.cs)
- [TutorialStep_WaitForSkillUse.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/Tutorial/TutorialStep_WaitForSkillUse.cs)
- [TutorialStep_OpenShop.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/Tutorial/TutorialStep_OpenShop.cs)

---

## 🛡️ 적 보호막 시스템

특정 적이 등장하는 동안 다른 일반 적이 보호 상태를 유지하도록 적 보호막 시스템을 구현했습니다.

`EnemyShieldManager`는 보호막 적의 활성화 상태를 관리하며, 보호막이 활성화된 동안 일반 적의 공격 판정을 제한하도록 구성했습니다.

보호막 적이 제거되면 관련 상태를 해제하고, 일반 적이 다시 공격 대상이 될 수 있도록 처리했습니다.

이 시스템은 보조 로봇의 적 탐색 기능과도 연동되어, 보호막을 가진 적을 우선적으로 탐색할 수 있도록 구성했습니다.

**관련 코드**

- [EnemyShieldManager.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/Enemy/EnemyShieldManager.cs)
- [EnemyShield.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/Enemy/EnemyShield.cs)
- [SearchEnemyManager.cs](https://github.com/Thispring/Signal_ZERO-ScriptOnly/blob/main/Script/GameManager/SearchEnemyManager.cs)

---

# 🛠 사용 기술

| 기술 | 활용 |
| --- | --- |
| Unity | 게임 클라이언트 개발 |
| C# | 게임 로직 및 시스템 구현 |
| Unity Physics | Raycast 기반 사격 및 게임 오브젝트 상호작용 처리 |
| Unity UI | 체력, 스킬, 튜토리얼 및 게임 인터페이스 구현 |

---

# 🔗 Links

### 🎥 Gameplay Video

[YouTube - Signal ZERO Gameplay](https://www.youtube.com/watch?v=Flv2JUqmpLE)

---

> 본 리포지토리는 포트폴리오 공개를 목적으로 프로젝트의 스크립트 코드만 포함하고 있습니다.
>
> 게임 에셋 및 전체 Unity 프로젝트 파일은 포함되어 있지 않습니다.
