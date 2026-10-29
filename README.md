# 원룸 맞춤형 「가구 치수(W×D×H) 기반 AR 문열림·동선 검증기」
### AR Furniture Space Verifier (UaaL Hybrid Architecture)

**별도의 3D 모델링 파일 없이 단순 가구 치수(가로×세로×높이) 텍스트만으로 실제 원룸 공간에 가상 체적을 즉각 배치하고, 주변 벽면 및 기존 가구와의 물리적 간섭과 최소 통행 동선을 검증하는 모바일 AR 유틸리티입니다.** 

안드로이드 네이티브(Android Studio / Kotlin / Jetpack Compose)의 유연한 UI/UX와 유니티 엔진(Unity / C# / AR Foundation)의 고성능 공간 연산 엔진을 UaaL(Unity as a Library) IPC 브릿지로 결합하여 온디바이스에서 독립 실행됩니다.

---

### 1. 프로젝트 개요

| 항목 | 내용 |
| :--- | :--- |
| **프로젝트명** | **원룸 맞춤형 가구 치수(W×D×H) 기반 AR 문열림·동선 검증기 (RoomFit AR)** |
| **저장소** | https://github.com/BBBB0601/AMP_FinalTerm_Project.git |
| **수행 기간** | 2026-09-01 \~ 2026-12-15 (2학기) |
| **소속 / 과정** | 한성대학교 컴퓨터공학부 · 2026학년도 2학기 고급모바일프로그래밍 [B분반] |
| **프로젝트 유형** | 모바일 AR 하이브리드 유틸리티 (Android Native + Unity as a Library) |
| **문서 버전** | v1.1 (2026-10-29 WBS 로드맵 반영) |

---

### 2. 배경 및 문제 정의

1인 가구 및 청년층의 중고 거래(당근마켓, 번개장터 등) 활성화로 가구 구매 빈도는 증가했으나, 원룸의 좁은 공간 특성으로 인해 구매 후 치수 착오 문제가 빈번하게 발생하고 있습니다.

| 문제 | 현재 사용자가 겪는 한계 | 본 시스템의 해결 방식 |
| :--- | :--- | :--- |
| **3D 에셋 부재** | 중고 거래 시 3D 모델링 파일이 제공되지 않아 AR 가구 배치를 시도할 수 없음 | 단순 수치($W \times D \times H$) 입력만으로 런타임에 1:1 스케일 3D 박스 메시를 절차적으로 동적 생성 |
| **문열림 및 동선 간섭** | 줄자 측정만으로는 가구 배치 후 냉장고/화장실 문열림 궤적이나 사람 통행 가능 여부를 체감하기 어려움 | AR 공간 상에 OBB(Oriented Bounding Box) 충돌 영역 및 최소 통행 폭(Clearance 60cm) 가이드 라인을 시각화 |
| **네이티브 조작 불편** | 순수 게임 엔진 기반 앱은 텍스트 복사/붙여넣기, 목록 스크롤, 시스템 공유 등 모바일 기본 UX가 불편함 | 치수 입력/파싱/목록 관리는 Android Native로, 3D/AR 연산은 Unity로 분리하는 UaaL 아키텍처 채택 |

---

### 3. 목적 및 목표

**목적:** 3D 모델링 전문 지식이나 고가의 장비 없이도, 스마트폰 하나로 중고 가구 치수를 파싱하여 원룸 내 물리적 배치 적합성을 10초 이내에 시뮬레이션할 수 있는 환경을 제공한다.

| # | 목표 | 측정 지표 및 기준 |
| :---: | :--- | :--- |
| **G1** | 텍스트 기반 치수 자동 파싱 | 중고거래 본문 복사 텍스트에서 가로/세로/높이 정규식 추출 성공률 90% 이상 |
| **G2** | 런타임 3D 체적 절차적 생성 | 수치 입력 즉시 $1:1$ 실제 물리 스케일의 절차적 큐브 메시 렌더링 |
| **G3** | 실시간 OBB 충돌 및 간섭 판정 | 기존 공간/벽면과의 3차원 충돌 감지 및 경고 하이라이트 실시간 렌더링 |
| **G4** | 렌더링 및 통신 성능 유지 | AR 뷰 렌더링 30 FPS 이상 유지 및 Android-Unity IPC 통신 지연 16ms 이내 |
| **G5** | 온디바이스 독립 구동 | 외부 백엔드 서버 없이 100% 로컬 환경에서 단독 실행 |

---

### 4. 개발 범위 (Scope)

#### In Scope
* **Android Native UI/UX 레이어:**
  - 치수($W \times D \times H$) 직접 입력 및 중고거래 복사 텍스트 정규식 자동 추출 인터페이스 (SCR-001)
  - 로컬 측정 이력 저장(Room DB) 및 검증 결과 리포트 대시보드 (SFR-007)
  - 서비스 환경 설정 및 다국어 지원 (SCR-003, SCR-004)
  - 시스템 스크린샷 캡처 및 외부 공유 연동
* **Unity AR 시뮬레이션 레이어:**
  - ARCore / AR Foundation 기반 수평 바닥면 및 수직 벽면 인식 (SCR-002, SFR-004)
  - 사용자 터치 지점 레이캐스팅(Raycasting) 기반 가상 체적 앵커링 및 조작
  - $1:1$ 스케일 절차적(Procedural) 3D 박스 메시 동적 생성 및 치수 라벨 렌더링 (SFR-003)
  - 3차원 OBB(Oriented Bounding Box) 기반 실시간 충돌 간섭 연산 및 경고 셰이더 (SFR-005)
  - 통행 여유 공간(Clearance 60cm 기준) 바닥 점선 가이드 라인 투영 (SFR-006)
* **Android-Unity IPC Bridge:**
  - JNI 및 `UnitySendMessage` 기반 JSON 규격 양방향 데이터 직렬화/역직렬화 통신 (SFR-002)
* **[선택/확장] Tripo AI 연동 (SFR-008):**
  - 가구 대표 사진 기반 단일 이미지 3D 메시 추론 REST API 연동

#### Out of Scope
* 가구 유형별 가동 부위 개폐 시뮬레이션(문짝 부채꼴 회전 궤적, 서랍 인출 기구학 - 안정적 MVP 구현을 위해 보류)
* iOS 환경 지원 (Android 단일 타겟)
* 다중 사용자 실시간 AR 세션 동기화 (Cloud Anchors 멀티플레이)
* 외부 클라우드 DB 및 계정 관리 백엔드 서버 구축

---

### 5. 주요 산출물

| 구분 | 산출물 명칭 | 파일 위치 / 경로 | 비고 |
| :--- | :--- | :--- | :--- |
| **소프트웨어** | UaaL 하이브리드 안드로이드 앱 | `android/`, `unity/` | 안드로이드 네이티브 + Unity 라이브러리 |
| **개발 로드맵** | **팀 WBS 및 단계별 개발 로드맵** | [`docs/고모프_프로젝트_개발로드맵.md`](docs/고모프_프로젝트_개발로드맵.md) | **역할 분담, 주차별 체크리스트, 통신 규격** |
| **설계 문서** | 소프트웨어 요구명세서 (SRS) | `docs/requirements.md` (로컬: `요구사항 분석.md`) | 요구사항 12종 및 IPO 명세 |
| **다이어그램** | Level 0 / 1 데이터 흐름도 (DFD) | `docs/DFD_데이터흐름도.png`, `.drawio` | 데이터 흐름 및 프로세스 |
| **통신 규격** | Android-Unity IPC JSON 스키마 | `docs/contracts/` | 양방향 메시지 규격 정의 |
| **형상 관리** | 통합 형상 관리 및 LFS 설정 | `.gitignore`, `.gitattributes` | 모노레포 버전 관리 |

---

### 6. 이해관계자 및 역할 (R&R)

초반에는 서로의 코드를 건드리지 않고 각자 독립된 개발 환경(Android Studio / Unity)에서 단독 모듈을 구현한 뒤, 사전에 정의된 JSON 통신 규격으로 연동합니다.

| 구분 | 담당 | 역할 및 세부 업무 | 담당 요구사항 |
| :--- | :--- | :--- | :--- |
| **지도** | **지도교수님** | 프로젝트 방향성 검토, 산학/산업 연계 적합성 평가 및 마일스톤 점검 | - |
| **Unity 총괄** | **김기현** | 프로젝트 총괄, 1:1 절차적 3D 체적(Procedural Mesh) 동적 생성, 실시간 OBB 충돌 판정 엔진, 60cm 동선 가이드라인 렌더링 | `SFR-003`, `SFR-005`, `SFR-006` |
| **Android FE** | **최주환** | Android Native UI/UX(Jetpack Compose), 가구 치수 입력 및 중고거래 텍스트 정규식 파서, Room DB 로컬 데이터 관리, 화면 캡처 공유 | `SFR-001`, `SFR-007`, `INR-001` |
| **AR & Bridge** | **진승호** | Unity AR Foundation 기반 바닥/벽면 평면 인식 및 앵커링, UaaL 모듈화 익스포트, Android-Unity 양방향 IPC Bridge 구축 | `SFR-002`, `SFR-004`, `PER-001`, `COR-001` |

---

### 7. 마일스톤 및 WBS 진행 현황

> 각 파트별 세부 단위 태스크와 자가 체크리스트는 [`docs/고모프_프로젝트_개발로드맵.md`](docs/고모프_프로젝트_개발로드맵.md)를 참조하십시오.

#### 3단계 협업 파이프라인

```
[Phase 1: 독립 모듈 개발] ───> [Phase 2: 모듈 결합 & UaaL 브릿지] ───> [Phase 3: 고도화 & 검증]
 (각자 로컬에서 단독 구현)           (Unity AR 통합 + IPC 연결)              (DB, 설정, 실측 테스트)
```

| 단계 | 추진 기간 | 핵심 마일스톤 및 세부 태스크 | 진행 상태 |
| :--- | :--- | :--- | :---: |
| **기획·설계** | 2026-09 \~ 10 초 | 프로젝트 주제 선정, SRS 요구분석 확정, DFD 설계, GitHub 모노레포 구축 | 완료 |
| **Phase 1** | 2026-10 중 \~ 10 말 | **개별 독립 모듈 로컬 단독 구현 (중간평가 목표)**<br>• [Android] SCR-001 치수 수동 입력 폼, 당근 정규식 파서, 가구 프리셋 카드<br>• [Unity 3D] SFR-003 절차적 1:1 큐브 메시 생성기, 실측 라벨, OBB 충돌 감지 로직<br>• [Unity AR] SFR-004 ARCore 바닥/벽면 평면 감지, 조준 레티클, ARAnchor 조작 | **진행 중** |
| **Phase 2** | 2026-11 초 \~ 11 중 | **모듈 간 결합 및 UaaL IPC 브릿지 연동**<br>• [Unity 결합] AR 평면 상에 절차적 큐브 스냅 앵커링 결합, 60cm 동선 가이드라인 렌더링<br>• [UaaL 통합] Unity 프로젝트를 AAR 라이브러리로 익스포트하여 Android Studio에 통합<br>• [IPC 연동] Android $\rightarrow$ Unity 치수 전달(JSON) 및 Unity $\rightarrow$ Android 충돌 경고 콜백 연결 | 예정 |
| **Phase 3** | 2026-11 말 \~ 12 초 | **부가 기능 고도화 및 품질 검증 (최종 데모 시연)**<br>• [로컬 영속화] Room DB 가구 배치 이력 저장 및 화면 캡처 공유 Intent 연결<br>• [환경 설정] 다국어(한국어/영어/일본어/중국어) 및 단위(cm/inch) 설정 지원 (SCR-003, 004)<br>• [품질 검증] 실제 원룸 줄자 대비 AR 가상 체적 오차율(±3cm) 및 30 FPS 성능 합동 검증 | 예정 |

---

### 8. 아키텍처 및 핵심 설계 원칙

```text
AMP_FinalTerm_Project/
├── android/                           # Android Studio 프로젝트 (네이티브 UI/UX)
│   ├── app/                           # 메인 안드로이드 애플리케이션 모듈 (Kotlin, Compose)
│   └── unityLibrary/                  # Unity에서 Export한 AR 라이브러리 모듈
├── unity/                             # Unity 프로젝트 (AR 및 3D 엔진)
│   ├── Assets/                        # C# 스크립트, AR Foundation 세팅, 셰이더
│   ├── Packages/                      # 패키지 종속성 매니페스트
│   └── ProjectSettings/               # 엔진 빌드 및 그래픽 세팅
└── docs/                              # 공식 산출물 및 인터페이스 규격
    ├── 고모프_프로젝트_개발로드맵.md     # 팀 WBS 및 단계별 개발 로드맵 (상세)
    ├── contracts/                     # Android-Unity IPC JSON 메시지 스키마
    └── DFD_데이터흐름도.png              # 데이터 흐름도
```

#### 사전 합의 IPC 데이터 통신 규격 (JSON)
* **Android $\rightarrow$ Unity (`UnitySendMessage`):**
  ```json
  {
    "name": "원목 단독 책상",
    "width": 1.20,
    "depth": 0.60,
    "height": 0.75,
    "unit": "meter"
  }
  ```
  *(주의: Android 입력값 cm를 100으로 나눈 미터 단위로 전달)*

* **Unity $\rightarrow$ Android (JNI Callback):**
  ```json
  {
    "isColliding": true,
    "target": "WALL",
    "message": "벽면 침범 발생"
  }
  ```

---

### 9. 기술 스택 (Tech Stack)

| 영역 | 기술 및 도구 | 버전 / 비고 |
| :--- | :--- | :--- |
| **Android Native** | Kotlin, Android SDK, Jetpack Compose | Android 10 (API 29) 이상, Coroutines, Room |
| **Game Engine** | Unity Engine (C#) | Unity 2022.3 LTS 이상 |
| **AR Framework** | AR Foundation, ARCore XR Plugin | 평면 감지(Plane Detection), Raycasting, ARAnchor |
| **Architecture** | Unity as a Library (UaaL) | JNI, UnitySendMessage, JSON 직렬화 |
| **협업 & 형상관리** | Git, GitHub, Git LFS | Monorepo 구조, develop 중심 브랜치 전략 |

---

### 10. 리스크 관리 및 대응 전략

| 리스크 | 시스템 영향 | 대응 전략 |
| :--- | :--- | :--- |
| **단색 장판/벽면 인식 실패** | 특징점이 부족하여 ARCore 평면 트래킹이 유실될 위험 | 평면 탐색 가이드 UI를 제공하고, 트래킹 유실 시 사용자 수동 앵커 조정 모드 지원 |
| **UaaL 연동 시 메모리 부족** | Android와 Unity 런타임이 동시 적재되어 OOM 크래시 발생 가능 | AR 뷰 종료 시 `UnityPlayer.unload()` 호출로 엔진 자원을 해제하고 저사양 텍스처 사용 |
| **IPC 통신 포맷 불일치** | 안드로이드와 유니티 간 데이터 파싱 오류로 인한 런타임 예외 | `docs/contracts/`에 JSON Schema를 사전 정의하여 계약 기반(Contract-first)으로 통신 필드 검증 |
| **실측 치수 대비 공간 왜곡** | 배치된 가상 가구 크기가 실제와 다를 경우 잘못된 구매 유도 | 절차적 메시 버텍스 연산 단위를 $1.0 = 1\text{m}$의 실제 미터 단위로 엄격히 고정하고 $\pm 3\text{cm}$ 이내로 보정 |

---

### 11. 로컬 개발 및 빌드 환경 설정

#### 요구 사양
* Android Studio Iguana (2023.2.1) 이상
* Unity 2022.3 LTS (Android Build Support, OpenJDK, Android SDK & NDK 툴체인 포함)
* ARCore 지원 실제 안드로이드 디바이스 (USB 디버깅 활성화 필수)

#### 브랜치 체크아웃 및 작업
```bash
# 1. 저장소 클론
git clone https://github.com/BBBB0601/AMP_FinalTerm_Project.git
cd AMP_FinalTerm_Project

# 2. 통합 개발 브랜치 전환
git checkout develop

# 3. 본인 파트 기능 브랜치 생성 후 작업 시작
git checkout -b feat/unity-ar-bridge    # 예: AR 및 브릿지 작업 시
```
