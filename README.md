# 원룸 맞춤형 「가구 치수(W×D×H) 기반 AR 문열림·동선 검증기」
### AR Furniture Space Verifier (UaaL Hybrid Architecture)

**별도의 3D 모델링 파일 없이 단순 가구 치수(가로×세로×높이) 텍스트만으로 실제 원룸 공간에 가상 체적을 즉각 배치하고, 주변 벽면 및 기존 가구와의 물리적 간섭과 최소 통행 동선을 검증하는 모바일 AR 유틸리티입니다.** 

안드로이드 네이티브(Android Studio / Kotlin)의 유연한 UI/UX와 유니티 엔진(Unity / C# / AR Foundation)의 고성능 공간 연산 엔진을 UaaL(Unity as a Library) IPC 브릿지로 결합하여 온디바이스에서 독립 실행됩니다.

---

### 1. 프로젝트 개요

| 항목 | 내용 |
| :--- | :--- |
| **프로젝트명** | **원룸 맞춤형 가구 치수(W×D×H) 기반 AR 문열림·동선 검증기** |
| **저장소** | https://github.com/BBBB0601/AMP_FinalTerm_Project.git |
| **수행 기간** | 2026-09-01 \~ 2026-12-15 (2학기) |
| **소속 / 과정** | 한성대학교 컴퓨터공학부 · 2026학년도 2학기 고급모바일프로그래밍 [B분반] |
| **프로젝트 유형** | 모바일 AR 하이브리드 유틸리티 (Android Native + Unity as a Library) |
| **문서 버전** | v1.0 (2026-10-15) |

---

### 2. 배경 및 문제 정의

1인 가구 및 청년층의 중고 거래(당근마켓, 번개장터 등) 활성화로 가구 구매 빈도는 증가했으나, 원룸의 좁은 공간 특성으로 인해 구매 후 치수 착오 문제가 빈번하게 발생하고 있습니다.

| 문제 | 현재 사용자가 겪는 한계 | 본 시스템의 해결 방식 |
| :--- | :--- | :--- |
| **3D 에셋 부재** | 중고 거래 시 3D 모델링 파일이 제공되지 않아 AR 가구 배치를 시도할 수 없음 | 단순 수치($W \times D \times H$) 입력만으로 런타임에 1:1 스케일 3D 박스 메시를 절차적으로 동적 생성 |
| **문열림 및 동선 간섭** | 줄자 측정만으로는 가구 배치 후 냉장고/화장실 문열림 궤적이나 사람 통행 가능 여부를 체감하기 어려움 | AR 공간 상에 OBB(Oriented Bounding Box) 충돌 영역 및 최소 통행 폭(Clearance) 가이드 라인을 시각화 |
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
  - 치수($W \times D \times H$) 직접 입력 및 중고거래 복사 텍스트 정규식 자동 추출 인터페이스
  - 로컬 측정 이력 저장(SharedPreferences / Room) 및 검증 결과 리포트 대시보드
  - 시스템 스크린샷 캡처 및 외부 공유 연동
* **Unity AR 시뮬레이션 레이어:**
  - ARCore / AR Foundation 기반 수평 바닥면 및 수직 벽면 인식
  - 사용자 터치 지점 레이캐스팅(Raycasting) 기반 가상 체적 앵커링
  - $1:1$ 스케일 절차적(Procedural) 3D 박스 메시 동적 생성
  - 3차원 OBB(Oriented Bounding Box) 기반 실시간 충돌 간섭 연산
  - 통행 여유 공간(Clearance 60cm 기준) 바닥 가이드 라인 투영
* **Android-Unity IPC Bridge:**
  - JNI 및 `UnitySendMessage` 기반 JSON 규격 양방향 데이터 직렬화/역직렬화 통신
* **[선택/확장] Tripo AI 연동 (SFR-008):**
  - 가구 대표 사진 기반 단일 이미지 3D 메시 추론 REST API 연동

#### Out of Scope
* 가구 유형별 가동 부위 개폐 시뮬레이션(문짝 부채꼴 회전 궤적, 서랍 인출 기구학 - 안정적 MVP 구현을 위해 보류)
* iOS 환경 지원 (Android 단일 타겟)
* 다중 사용자 실시간 AR 세션 동기화 (Cloud Anchors 멀티플레이)
* 외부 클라우드 DB 및 계정 관리 백엔드 서버 구축

---

### 5. 주요 산출물

| 구분 | 산출물 명칭 | 파일 위치 / 경로 |
| :--- | :--- | :--- |
| **소프트웨어** | UaaL 하이브리드 안드로이드 앱 | `android/`, `unity/` |
| **설계 문서** | 소프트웨어 요구명세서 (SRS) | `docs/requirements.md` (로컬 원본: `요구사항 분석.md`) |
| **다이어그램** | Level 0 / 1 데이터 흐름도 (DFD) | `docs/DFD_데이터흐름도.png`, `.drawio` |
| **통신 규격** | Android-Unity IPC JSON 스키마 | `docs/contracts/` |
| **형상 관리** | 통합 형상 관리 및 LFS 설정 | `.gitignore`, `.gitattributes` |

---

### 6. 이해관계자 및 역할 (R&R)

| 구분 | 담당 | 역할 및 세부 업무 |
| :--- | :--- | :--- |
| **지도** | **지도교수님** | 프로젝트 방향성 검토, 산학/산업 연계 적합성 평가 및 마일스톤 점검 |
| **Unity 총괄** | **김기현** | 프로젝트 총괄, Unity 절차적 3D 체적(Procedural Mesh) 동적 생성, 실시간 OBB 충돌 판정 알고리즘 구현 |
| **Android FE** | **최주환** | Android Native UI/UX 디자인, 가구 치수 입력 및 중고거래 텍스트 정규식 파싱, 로컬 데이터베이스 관리, 결과 리포트 |
| **AR & Bridge** | **진승호** | Unity AR Foundation 기반 공간 인식 및 평면 앵커링, UaaL(Unity as a Library) 안드로이드 모듈 통합, Android-Unity 양방향 IPC Bridge 구축 |

---

### 7. 마일스톤 및 진행 현황

| 시점 | 마일스톤 | 진행 상태 |
| :--- | :--- | :---: |
| **2026-09 초** | 프로젝트 주제 선정 및 제안서 작성 | 완료 |
| **2026-09 말** | UaaL 하이브리드 아키텍처 채택 및 DFD 설계 | 완료 |
| **2026-10 초** | 소프트웨어 요구사항 분석서(SRS) 확정 | 완료 |
| **2026-10 중** | GitHub 모노레포 구축 및 초기 환경 세팅 | 완료 |
| **2026-10 말** | Android Native UI 프로토타입 + Unity 절차적 큐브 생성 구현 (중간평가) | 진행 중 |
| **2026-11 중** | UaaL 라이브러리 익스포트 및 Android-Unity IPC 양방향 통신 검증 | 예정 |
| **2026-11 말** | ARCore 평면 인식, OBB 충돌 판정 및 동선 가이드 통합 | 예정 |
| **2026-12 초** | 온디바이스 실측 오차 테스트, 성능 최적화 및 최종 데모 시연 | 예정 |

---

### 8. 아키텍처 및 핵심 설계 원칙

```text
furniture-ar-verifier/
├── android/                           # Android Studio 프로젝트 (네이티브 UI/UX)
│   ├── app/                           # 메인 안드로이드 애플리케이션 모듈 (Kotlin)
│   └── unityLibrary/                  # Unity에서 Export한 AR 라이브러리 모듈
├── unity/                             # Unity 프로젝트 (AR 및 3D 엔진)
│   ├── Assets/                        # C# 스크립트, AR Foundation 세팅, 셰이더
│   ├── Packages/                      # 패키지 종속성 매니페스트
│   └── ProjectSettings/               # 엔진 빌드 및 그래픽 세팅
└── docs/                              # 공식 산출물 및 인터페이스 규격
    ├── contracts/                     # Android-Unity IPC JSON 메시지 스키마
    └── DFD_데이터흐름도.png              # 데이터 흐름도
```

#### 핵심 아키텍처 원칙:
1. **관심사의 명확한 분리 (Separation of Concerns):**
   - 폼 입력, 텍스트 파싱, 파일 입출력, 권한 처리는 Android Native의 생명주기를 따릅니다.
   - 공간 추적, 셰이더 렌더링, 3D 물리/충돌 연산은 Unity 런타임에 전담시킵니다.
2. **비동기 IPC 통신 및 데이터 무결성:**
   - 안드로이드에서 유니티로 치수 전달 시 `UnitySendMessage`를 통해 표준 JSON 문자열을 비동기 전달합니다.
   - 유니티에서 안드로이드로 결과(충돌 여부, 앵커링 상태) 반환 시 C# AndroidJavaClass / JNI Interface를 통해 콜백을 호출합니다.
3. **온디바이스 독립성 (Self-contained):**
   - 네트워크 연결이 불안정한 원룸 환경을 고려하여, 모든 AR 시뮬레이션 및 데이터 연산은 100% 로컬 디바이스 CPU/GPU에서 완결합니다.

---

### 9. 기술 스택 (Tech Stack)

| 영역 | 기술 및 도구 | 버전 / 비고 |
| :--- | :--- | :--- |
| **Android Native** | Kotlin, Android SDK, Android Studio | Android 10 (API 29) 이상, Kotlin Coroutines, Room |
| **Game Engine** | Unity Engine (C#) | Unity 2022.3 LTS 이상 |
| **AR Framework** | AR Foundation, ARCore XR Plugin | 평면 감지(Plane Detection), 레이캐스팅 |
| **Architecture** | Unity as a Library (UaaL) | JNI, UnitySendMessage, JSON 직렬화 |
| **협업 & 형상관리** | Git, GitHub, Git LFS | Monorepo 구조, GitHub Flow 브랜치 전략 |

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
