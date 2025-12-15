# Node.js 패키지 사용 가이드

## 📚 개요

Blazor 프로젝트에서 JavaScript 라이브러리를 관리하는 방법을 설명합니다.

---

## 🎯 빠른 요약

### JavaScript 이용하기

```
1️⃣ JSRuntime 주입
   ↓
2️⃣ JavaScript 모듈 로드
   ↓
3️⃣ Function Invoke
   ↓
✅ JavaScript 실행
```

**코드 예시**:

```csharp
[Inject]
private IJSRuntime JS { get; set; }

// 모듈 로드
var module = await JS.InvokeAsync<IJSObjectReference>(
    "import",
    "./Components/Controls/ServerChartComponent.razor.js"
);

// 함수 호출
await module.InvokeAsync("createOverallChart", canvas, data);
```

---

### Node.js 패키지 로드하기

| 방식           | 용도        | 상황                       |
| -------------- | ----------- | -------------------------- |
| **CDN 방식**   | 개발용      | ✅ 패키지 1-2개만 필요     |
| **로컬 배치**  | 프로덕션    | 👍 권장 - 패키지 소수      |
| **NPM + 빌드** | 자동화 필요 | 💡 많은 패키지 존재할 경우 |

---

## 🔍 CDN 로드와 글로벌 변수 접근 흐름

### 📍 현재 프로젝트 구조

**App.razor에서 CDN 로드:**

```html
<!-- App.razor (라인 12) -->
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
```

**JavaScript에서 글로벌 접근:**

```javascript
// ServerChartComponent.razor.js
export function createOverallChart(canvas, data) {
  const ctx = canvas.getContext("2d");

  // CDN에서 로드된 글로벌 Chart 변수 사용
  charts.overall = new Chart(ctx, {
    type: "doughnut",
    data: data
    // ...
  });
}
```

### 🔗 로드 → 글로벌 접근 흐름

```
1️⃣ HTML 페이지 로드 (App.razor)
   ↓
2️⃣ <script src="https://cdn.jsdelivr.net/npm/chart.js"></script> 실행
   ↓
3️⃣ CDN에서 chart.js 다운로드 및 파싱
   ↓
4️⃣ 글로벌 window 객체에 Chart 생성자 등록
   window.Chart = Chart클래스
   ↓
5️⃣ 이후 로드되는 모든 JavaScript 모듈에서 접근 가능
   ↓
6️⃣ ServerChartComponent.razor.js에서 new Chart() 사용
   (전체 경로: new window.Chart(...) ≈ new Chart(...))
```

### 💡 개발자가 알 수 있는 방법

#### 방법 1️⃣: 브라우저 DevTools (F12)

**콘솔 탭에서 직접 확인:**

```javascript
// 브라우저 콘솔에 입력
typeof Chart;

// 출력 결과
("function"); // ← Chart 생성자 함수 존재!
```

**Chart 객체 내용 확인:**

```javascript
// 브라우저 콘솔에서
Object.keys(Chart);
// 출력: defaults, helpers, plugins, version, ...

Chart.version;
// 출력: "4.4.0" (또는 로드된 버전)
```

#### 방법 2️⃣: HTML 소스 확인

```html
<!-- App.razor 확인 -->
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
↑ 이 줄이 있으면 → window.Chart 글로벌 변수 생성됨
```

#### 방법 3️⃣: NPM 패키지 문서 확인

CDN 제공자 (jsDelivr, unpkg) 페이지에서 확인:

**jsDelivr 방식:**

```
https://www.jsdelivr.net/npm/chart.js/

→ "UMD build" 또는 "Browser Global" 섹션 확인
→ <script> 태그로 로드하면 window.Chart 생성
```

**Chart.js 공식 문서:**

```
https://www.chartjs.org/docs/latest/getting-started/

→ "Installation" → "Quickstart" 섹션
→ <script> 방식 사용하면 new Chart() 가능
```

### 📊 CDN 라이브러리의 글로벌 변수 이름

| 라이브러리 | CDN | 글로벌 변수    | 사용 예                    |
| ---------- | --- | -------------- | -------------------------- |
| Chart.js   | ✅  | `Chart`        | `new Chart(ctx, {...})`    |
| jQuery     | ✅  | `jQuery` / `$` | `$('.div')`                |
| Bootstrap  | ✅  | `bootstrap`    | `new bootstrap.Modal(...)` |
| Lodash     | ✅  | `_`            | `_.map(arr, fn)`           |
| Moment.js  | ✅  | `moment`       | `moment().format()`        |
| Three.js   | ✅  | `THREE`        | `new THREE.Scene()`        |
| D3.js      | ✅  | `d3`           | `d3.select('body')`        |
| Axios      | ⚠️  | `axios`        | `axios.get('/api')`        |

### 🔎 글로벌 변수명 찾는 방법

**패키지마다 글로벌 변수명이 다릅니다!** 공식 문서에서 확인하세요.

**예시 - Chart.js:**

```html
<!-- 공식 문서 예제 -->
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
<script>
  const ctx = document.getElementById('myChart').getContext('2d');
  const myChart = new Chart(ctx, {...});  ← 여기서 Chart가 글로벌 변수임을 알 수 있음
</script>
```

**찾는 순서:**

1. 패키지 공식 웹사이트 방문
2. "Installation", "Getting Started", "CDN" 섹션 찾기
3. 예제 코드 확인 → 글로벌 변수명 파악
4. 불확실하면 `typeof 변수명`으로 브라우저 콘솔에서 확인

| 패키지    | 공식 문서                                              |
| --------- | ------------------------------------------------------ |
| Chart.js  | https://www.chartjs.org/docs/latest/getting-started/   |
| jQuery    | https://jquery.com/                                    |
| Bootstrap | https://getbootstrap.com/docs/5.0/getting-started/cdn/ |
| Three.js  | https://threejs.org/docs/                              |
| D3.js     | https://d3js.org/                                      |
| Lodash    | https://lodash.com/                                    |

---

### 🎓 결론

**"어떻게 개발자가 알 수 있어?"**

1. ✅ **App.razor의 `<script>` 태그 확인**
   - CDN URL을 보면 어떤 라이브러리가 로드되는지 알 수 있음
2. ✅ **라이브러리 공식 문서 확인**
   - "Browser/CDN 사용법" 섹션에서 글로벌 변수명 기재
3. ✅ **브라우저 DevTools 콘솔에서 확인**

   - `typeof Chart` → `"function"` 이면 사용 가능
   - `Chart.version` → 로드된 버전 확인

4. ✅ **JavaScript 소스 코드 분석**
   - `new Chart(...)` 사용 → Chart 라이브러리 필요
   - `new bootstrap.Modal(...)` 사용 → Bootstrap 필요

---

## 🔄 3가지 사용 방식 비교

| 방식           | 방법                     | 개발 | 프로덕션 | 추천          |
| -------------- | ------------------------ | ---- | -------- | ------------- |
| **CDN**        | `<script src="cdn.url">` | ✅   | ⚠️       | 패키지 1-2개  |
| **로컬 배치**  | wwwroot에 파일 저장      | ⚠️   | ✅       | 프로덕션 권장 |
| **NPM + 빌드** | npm install 후 번들링    | ✅   | ✅       | 많은 패키지   |

---

## 1️⃣ CDN 방식 (현재 사용)

### 사용 중인 예: Chart.js

```html
<!-- App.razor (라인 12) -->
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
```

### ✅ 장점

- 설치 불필요
- 간단함 (한 줄만 추가)
- 빠른 프로토타이핑

### ❌ 단점

- 인터넷 필수
- CDN 신뢰성 의존
- 버전 관리 어려움
- 오프라인 작동 불가

### 📝 CDN 찾기

- **jsDelivr**: https://www.jsdelivr.com
- **unpkg**: https://unpkg.com
- **cdnjs**: https://cdnjs.com

---

## 2️⃣ 로컬 배치 방식 (권장 - 프로덕션)

### 단계별 가이드

#### Step 1: 파일 다운로드

```bash
# 방법 1: curl 사용 (macOS/Linux)
curl -o ServerManagement/wwwroot/lib/chart.js/chart.min.js \
  https://cdn.jsdelivr.net/npm/chart.js/dist/chart.min.js

# 방법 2: 브라우저에서 다운로드
# https://cdn.jsdelivr.net/npm/chart.js/dist/chart.min.js
# 우클릭 → 다른 이름으로 저장
```

#### Step 2: 폴더 구조 생성

```
ServerManagement/
  wwwroot/
    lib/
      chart.js/
        chart.min.js              ← 다운로드한 파일
        chart.js                  ← (선택) 비축소 버전
```

#### Step 3: App.razor 수정

```html
<!-- 변경 전 (CDN) -->
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>

<!-- 변경 후 (로컬) -->
<script src="lib/chart.js/chart.min.js"></script>
```

### ✅ 장점

- 🚀 빠른 로딩 (네트워크 지연 없음)
- 📴 오프라인 작동 가능
- 🔒 프로덕션 신뢰성 높음
- 📦 버전 관리 명확
- 🛡️ CDN 장애 영향 없음

### ❌ 단점

- 수동 업데이트 필요
- 저장소 크기 증가 (약 100KB)
- 버전 관리 책임

---

## 3️⃣ NPM + 로컬 빌드 (고급 - 선택사항)

### 요구사항

```bash
# Node.js 설치 필요
node --version    # v14+
npm --version     # v6+
```

### 단계별 가이드

#### Step 1: package.json 생성

```bash
cd ServerManagement
npm init -y
```

#### Step 2: 패키지 설치

```bash
npm install chart.js
```

생성되는 구조:

```
ServerManagement/
  node_modules/
    chart.js/      ← npm 설치 폴더
  package.json     ← 의존성 목록
  package-lock.json
```

#### Step 3: 파일 복사 (수동 또는 스크립트)

```bash
# wwwroot에 복사
cp node_modules/chart.js/dist/chart.min.js wwwroot/lib/chart.js/
```

#### Step 4: App.razor 수정

```html
<script src="lib/chart.js/chart.min.js"></script>
```

### ✅ 장점

- 📦 의존성 자동 관리
- 🔄 버전 업데이트 쉬움
- 🎯 정확한 버전 제어
- 📝 package.json에 기록
- 🤝 팀 협업 용이

### ❌ 단점

- Node.js 설치 필수
- 복잡도 증가
- 빌드 프로세스 필요
- 저장소 크기 크게 증가

---

## 📚 JavaScript 이용하기 (Blazor에서)

### 기본 흐름

```
1️⃣ JSRuntime 주입 → 2️⃣ JavaScript 모듈 로드 → 3️⃣ Function Invoke
```

### 코드 구현

#### Step 1: JSRuntime 주입

```csharp
[Inject]
private IJSRuntime JS { get; set; } = null!;
```

#### Step 2: JavaScript 모듈 로드

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        // JavaScript 모듈 로드
        var module = await JS.InvokeAsync<IJSObjectReference>(
            "import",                                      // 명령어
            "./Components/Controls/ServerChartComponent.razor.js"  // 파일 경로
        );
    }
}
```

#### Step 3: Function Invoke (함수 호출)

```csharp
// JavaScript 함수 실행
var chart = await module.InvokeAsync<IJSObjectReference>(
    "createOverallChart",      // 함수명
    canvasElement,             // 인자 1
    chartData                  // 인자 2
);
```

### 실제 사용 예시 (우리 프로젝트)

**ServerChartComponent.razor (C# 부분)**

```csharp
@inject IStatisticsService StatisticsService
@implements IAsyncDisposable

@code {
    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    private IJSObjectReference? module;
    private ElementReference overallChartCanvas;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // 1️⃣ 모듈 로드
            module = await JS.InvokeAsync<IJSObjectReference>(
                "import",
                "./Components/Controls/ServerChartComponent.razor.js"
            );

            var stats = StatisticsService.GetServerStatistics();
            var data = new { /* 데이터 */ };

            // 2️⃣ JavaScript 함수 호출
            await module.InvokeAsync(
                "createOverallChart",
                overallChartCanvas,
                data
            );
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (module is not null)
        {
            await module.InvokeVoidAsync("disposeCharts");
            await module.DisposeAsync();
        }
    }
}
```

**ServerChartComponent.razor.js (JavaScript 부분)**

```javascript
export function createOverallChart(canvas, data) {
  const ctx = canvas.getContext("2d");

  const chart = new Chart(ctx, {
    type: "doughnut",
    data: data,
    options: {
      /* 옵션 */
    }
  });

  return chart;
}

export function disposeCharts() {
  // 정리 코드
}
```

---

## 📝 JavaScript 모듈 작성 가이드

### 기본 구조

JavaScript 모듈은 Blazor에서 로드할 함수들을 `export`로 선언해야 합니다.

```javascript
// 함수 정의
function helper() {
  // 내부 로직 (export 불필요)
}

// Blazor에서 호출할 함수들
export function publicFunction(arg1, arg2) {
  helper(); // 내부 함수 사용 가능
  return result;
}

export function anotherPublicFunction() {
  // 로직
}
```

### Step 1️⃣: 모듈 생성

파일명: `ServerChartComponent.razor.js`

```javascript
// 차트 객체 저장소 (메모리 관리용)
const charts = {};

// 내부 함수 (export하지 않음)
function initializeChartOptions() {
  return {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: "top"
      }
    }
  };
}

// Blazor에서 호출할 공개 함수
export function createOverallChart(canvas, data) {
  const ctx = canvas.getContext("2d");

  const chart = new Chart(ctx, {
    type: "doughnut",
    data: data,
    options: initializeChartOptions()
  });

  // 메모리 관리: 차트 참조 저장
  charts["overall"] = chart;

  return chart;
}

export function createDetailChart(canvas, data) {
  const ctx = canvas.getContext("2d");

  const chart = new Chart(ctx, {
    type: "bar",
    data: data,
    options: initializeChartOptions()
  });

  charts["detail"] = chart;

  return chart;
}

// 정리 함수 (컴포넌트 언마운트 시 호출)
export function disposeCharts() {
  Object.values(charts).forEach(chart => {
    if (chart) {
      chart.destroy(); // Chart.js 메모리 해제
    }
  });

  // 저장소 초기화
  Object.keys(charts).forEach(key => {
    delete charts[key];
  });
}
```

### Step 2️⃣: Blazor 컴포넌트에서 모듈 로드

```csharp
@inject IStatisticsService StatisticsService
@implements IAsyncDisposable

@code {
    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    private IJSObjectReference? module;
    private ElementReference overallChartCanvas;
    private ElementReference detailChartCanvas;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // 1️⃣ 모듈 로드 (한 번만 실행)
            module = await JS.InvokeAsync<IJSObjectReference>(
                "import",
                "./Components/Controls/ServerChartComponent.razor.js"
            );

            // 2️⃣ 데이터 준비
            var stats = StatisticsService.GetServerStatistics();
            var overallData = new { /* 데이터 */ };
            var detailData = new { /* 데이터 */ };

            // 3️⃣ JavaScript 함수 호출
            await module.InvokeAsync(
                "createOverallChart",
                overallChartCanvas,
                overallData
            );

            await module.InvokeAsync(
                "createDetailChart",
                detailChartCanvas,
                detailData
            );
        }
    }

    // 컴포넌트 언마운트 시 정리
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (module is not null)
        {
            // JavaScript 메모리 해제
            await module.InvokeVoidAsync("disposeCharts");
            await module.DisposeAsync();
        }
    }
}
```

### Step 3️⃣: HTML에서 canvas 요소 참조

```razor
@rendermode InteractiveServer

<div class="charts-container">
    <canvas @ref="overallChartCanvas"
            id="overallChart"
            width="400"
            height="300"></canvas>

    <canvas @ref="detailChartCanvas"
            id="detailChart"
            width="400"
            height="300"></canvas>
</div>
```

### 📋 작성 체크리스트

| 항목                                | 확인               |
| ----------------------------------- | ------------------ |
| ✅ `export` 키워드로 공개 함수 선언 | 필수               |
| ✅ `@ref`로 canvas/DOM 요소 받기    | 필수 (DOM 조작 시) |
| ✅ 메모리 정리 함수 (`dispose`)     | 권장               |
| ✅ 내부 함수는 `export` 하지 않기   | 권장               |
| ✅ 에러 처리 추가                   | 권장               |

### 💡 모범 사례 (Best Practices)

```javascript
// ✅ 좋은 예
export function processData(data) {
  try {
    return data.map(item => ({
      id: item.id,
      value: item.value * 2
    }));
  } catch (error) {
    console.error("데이터 처리 실패:", error);
    return [];
  }
}

// ❌ 피해야 할 것
export function badExample() {
  // 전역 변수 수정 금지
  window.globalData = [];

  // console.log만 사용하는 무의미한 코드
  console.log("작동됨");
}

// ✅ 메모리 관리
const resources = new Map();

export function allocateResource(id, data) {
  resources.set(id, data);
}

export function freeResources() {
  resources.forEach((data, id) => {
    // 정리 로직
    data.destroy?.();
  });
  resources.clear();
}
```

---

### 💡 핵심 패턴

```
Blazor (C#)
    ↓ (IJSRuntime)
JavaScript
    ↓ (라이브러리 활용)
Chart.js
    ↓
결과 (차트 렌더링)
```

---

## 📊 시나리오별 추천

### 🚀 개발 단계 (현재)

```
👉 CDN 방식 사용
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
```

### 🏗️ 프로덕션 배포

```
👉 로컬 배치 방식으로 전환
<script src="lib/chart.js/chart.min.js"></script>
```

### 📦 많은 패키지 사용 (10개 이상)

```
👉 NPM + 빌드 파이프라인 구축
npm install로 자동 관리, 버전 통일, 번들링 자동화
```

---

## 🔗 CDN 제공 라이브러리 예시

### ✅ CDN 버전 있음 (추천 라이브러리)

```
Chart.js          https://cdn.jsdelivr.net/npm/chart.js
Bootstrap         https://cdn.jsdelivr.net/npm/bootstrap
jQuery            https://cdn.jsdelivr.net/npm/jquery
Lodash            https://cdn.jsdelivr.net/npm/lodash
Moment.js         https://cdn.jsdelivr.net/npm/moment
D3.js             https://cdn.jsdelivr.net/npm/d3
Three.js          https://cdn.jsdelivr.net/npm/three
Animate.css       https://cdn.jsdelivr.net/npm/animate.css
Axios             https://cdn.jsdelivr.net/npm/axios
Font Awesome      https://cdn.jsdelivr.net/npm/font-awesome
```

### ❌ CDN 버전 없음 (NPM 필수)

```
React
Vue.js
Angular
Express.js (서버 런타임)
Next.js
TypeScript 컴파일러
Webpack
Babel
```

---

## 💡 Blazor 프로젝트 구조

```
ServerManagement/
  Components/
    Controls/
      ServerChartComponent.razor       ← Blazor 컴포넌트
      ServerChartComponent.razor.js    ← JavaScript 구현
      ServerChartComponent.razor.css   ← 스타일
  wwwroot/
    lib/
      chart.js/
        chart.min.js                   ← 라이브러리 (로컬 배치)
      bootstrap/
      ...
    app.css
  App.razor                            ← 라이브러리 로드
  Program.cs
```

---

## 🎯 결론

| 단계                | 권장 방식 | 이유         |
| ------------------- | --------- | ------------ |
| **개발/학습**       | CDN       | 빠른 시작    |
| **테스트**          | CDN       | 간단함       |
| **프로덕션**        | 로컬 배치 | 안정성, 성능 |
| **대규모 프로젝트** | NPM       | 버전 관리    |

---

## 🗂️ NPM + 빌드 방식의 폴더 구조 (상세)

### 파일/폴더 위치 및 역할

```
ServerManagement/                          ← 프로젝트 루트
  ├─ Components/                           ← Blazor 코드 (변경 없음)
  │  ├─ Pages/
  │  │  └─ Dashboard.razor
  │  ├─ Controls/
  │  │  └─ ServerChartComponent.razor      ← HTML + C# 코드
  │  └─ App.razor
  │
  ├─ Models/                               ← C# 모델
  │
  ├─ wwwroot/                              ← 브라우저가 접근
  │  ├─ lib/
  │  │  ├─ chart.js/
  │  │  │  └─ chart.min.js
  │  │  └─ bundle.min.js                   ← webpack 빌드 결과
  │  └─ css/
  │
  ├─ src/                                  ← 개발자가 작성한 원본 JS
  │  └─ js/
  │     ├─ index.js                        ← 진입점
  │     ├─ chart-wrapper.js                ← Chart 로직
  │     └─ utils.js                        ← 유틸리티
  │
  ├─ dist/                                 ← webpack 빌드 폴더 (자동 생성)
  │  └─ bundle.min.js
  │
  ├─ node_modules/                         ← npm 설치 폴더 (자동 생성)
  │  └─ chart.js/ ...
  │
  ├─ package.json                          ← NPM 설정
  ├─ package-lock.json                     ← NPM 버전 고정
  ├─ webpack.config.js                     ← 빌드 설정
  ├─ Program.cs
  └─ ServerManagement.csproj
```

### 파일/폴더별 관리 책임

| 파일/폴더             | 위치          | 역할          | 누가 관리      | 설명                    |
| --------------------- | ------------- | ------------- | -------------- | ----------------------- |
| **Razor 컴포넌트**    | Components/   | UI + C# 로직  | 개발자         | 변경 없음, Blazor 코드  |
| **원본 JavaScript**   | src/js/       | JS 구현       | 개발자 ⭐️     | 개발자가 직접 작성/수정 |
| **번들 결과물**       | wwwroot/lib/  | 최종 JS       | webpack (자동) | 빌드 시 자동 생성       |
| **라이브러리**        | node_modules/ | 설치된 패키지 | npm (자동)     | npm install로 설치      |
| **package.json**      | 루트          | NPM 설정      | 개발자 ⭐️     | 의존성 버전 관리        |
| **webpack.config.js** | 루트          | 빌드 규칙     | 개발자 ⭐️     | 입력/출력 경로 정의     |
| **dist/**             | 루트          | 임시 폴더     | webpack (자동) | 중간 빌드 폴더 (선택)   |

---

## 🔄 NPM + 빌드 동작 순서

```
1️⃣ 개발자가 src/js/chart-wrapper.js 수정
                ↓
2️⃣ npm run dev 명령 (또는 자동 감시 모드)
                ↓
3️⃣ webpack이 src/js/*.js 감지
                ↓
4️⃣ 모든 JS 파일 수집 및 분석
                ↓
5️⃣ Chart.js 라이브러리 포함
                ↓
6️⃣ 하나의 파일로 번들링 (bundle.min.js)
                ↓
7️⃣ wwwroot/lib/bundle.min.js 자동 생성
                ↓
8️⃣ Blazor에서 <script src="lib/bundle.min.js"> 로드
                ↓
9️⃣ 브라우저가 차트 표시
```

---

## 🔑 핵심 정리

| 구분           | CDN/로컬     | NPM + 빌드                          |
| -------------- | ------------ | ----------------------------------- |
| **Razor 위치** | Components/  | Components/ (동일)                  |
| **JS 위치**    | wwwroot/lib/ | src/js/ (개발), wwwroot/lib/ (결과) |
| **설정 파일**  | 없음         | package.json + webpack.config.js    |
| **자동화**     | 없음         | npm run dev/build                   |
| **번들링**     | 불필요       | 필수                                |
| **버전 관리**  | 수동         | 자동 (package.json)                 |

---

## 📞 참고 자료

- jsDelivr: https://www.jsdelivr.com
- unpkg: https://unpkg.com
- Chart.js 공식: https://www.chartjs.org
- Bootstrap CDN: https://getbootstrap.com/docs/5.0/getting-started/cdn/
