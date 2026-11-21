# Blazor에서 JavaScript 라이브러리 사용 가이드

> Chart.js 등 **브라우저 JS 라이브러리**를 Blazor에 붙이는 방법 (CDN / wwwroot / NPM).  
> 제목의 “Node.js”는 NPM 생태계를 쓸 때 해당. 필수는 아님.  
> JS **호출 타이밍** → [`BLAZOR_RENDERING_GUIDE.md`](./BLAZOR_RENDERING_GUIDE.md)  
> Blazor API 전반 → [`BLAZOR_LEARNING_GUIDE.md`](./BLAZOR_LEARNING_GUIDE.md)

---

## 빠른 요약

```
IJSRuntime 주입 → OnAfterRenderAsync에서 import → InvokeAsync → IAsyncDisposable로 해제
```

```csharp
[Inject] private IJSRuntime JS { get; set; } = null!;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (!firstRender) return;

    module = await JS.InvokeAsync<IJSObjectReference>(
        "import",
        "./Components/Controls/ServerChartComponent.razor.js");

    await module.InvokeAsync("createOverallChart", canvas, data);
}
```

스크립트 로드 방식:

| 방식 | 언제 |
| --- | --- |
| **CDN** | 개발·패키지 1–2개 |
| **wwwroot 로컬** | 프로덕션 권장(소수 패키지) |
| **NPM (+ 선택적 번들)** | 의존성·버전이 많을 때 |

---

## 1. CDN (개발)

```html
<!-- App.razor / App 레이아웃 -->
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
```

### CDN → 글로벌 → 모듈에서 쓰는 흐름

```
1. 브라우저가 App HTML 로드
2. <script src="...chart.js"> 실행
3. 라이브러리가 window.Chart 등록  (UMD/브라우저 빌드)
4. 이후 import한 *.razor.js 에서 new Chart(...) 가능
5. Blazor는 IJSRuntime으로 export 함수만 호출
```

```javascript
// ServerChartComponent.razor.js
export function createOverallChart(canvas, data) {
  const ctx = canvas.getContext("2d");
  // window.Chart — CDN 스크립트가 만든 글로벌
  return new Chart(ctx, { type: "doughnut", data });
}
```

### “Chart라는 이름을 어떻게 아나?”

1. 공식 Getting Started / CDN 예제에 `new Chart(...)`가 나옴  
2. F12 콘솔: `typeof Chart` → `"function"` 이면 로드됨  
3. `Chart.version` 등으로 버전 확인  
4. App의 `<script src=...>`가 실제로 로드됐는지 Network 탭 확인  

| 라이브러리 | 전형적 글로벌 |
| --- | --- |
| Chart.js | `Chart` |
| jQuery | `jQuery` / `$` |
| Bootstrap 5 | `bootstrap` |
| Lodash | `_` |
| D3 | `d3` |

**단점:** 오프라인·CDN 장애·버전 고정 약함 → 프로덕션은 로컬 배치 권장.

CDN: [jsDelivr](https://www.jsdelivr.com) · [unpkg](https://unpkg.com) · [cdnjs](https://cdnjs.com)

---

## 2. wwwroot 로컬 배치 (프로덕션 권장)

```bash
mkdir -p wwwroot/lib/chart.js
curl -L -o wwwroot/lib/chart.js/chart.min.js \
  https://cdn.jsdelivr.net/npm/chart.js/dist/chart.umd.min.js
```

```html
<script src="lib/chart.js/chart.min.js"></script>
```

```
Project/
  wwwroot/lib/chart.js/chart.min.js   ← 브라우저가 받는 파일
  Components/.../Foo.razor.js         ← export 함수 (Chart 글로벌 사용)
  App.razor                           ← <script src="lib/...">
```

장점: 오프라인·버전 고정·CDN 비의존. 단점: 수동 업데이트.

---

## 3. NPM (선택)

Node는 **빌드 머신/개발 PC**에만 있으면 됨. 런타임에 Node가 필요하지 않음.

```bash
npm init -y
npm install chart.js
cp node_modules/chart.js/dist/chart.umd.min.js wwwroot/lib/chart.js/chart.min.js
```

### 최소 폴더 구조 (복사만 하는 경우)

```
Project/
  package.json                 ← npm 의존성 (개발용)
  node_modules/chart.js/       ← npm install 결과 (보통 git 제외)
  wwwroot/lib/chart.js/
    chart.min.js               ← 복사본 → <script>로 로드
  Components/Controls/
    ServerChartComponent.razor
    ServerChartComponent.razor.js
```

### 번들까지 쓸 때 (선택, 패키지 많을 때)

```
Project/
  src/js/index.js              ← 개발자가 쓰는 진입 JS (선택)
  package.json
  webpack.config.js / esbuild  ← 선택 도구
  wwwroot/lib/bundle.min.js    ← 빌드 결과물만 배포
```

```
npm install → (선택) 번들 → wwwroot에 산출물 → App.razor script
```

Blazor 기본 템플릿에 webpack **필수 아님**. 소수 라이브러리는 **복사만**으로 충분.  
React/Vue를 Blazor와 한 페이지에 섞는 구성은 이 가이드 범위 밖.

---

## Blazor ↔ JS 모듈 패턴 (따라 하기 예시)

App(또는 레이아웃)에 Chart.js 로드 후:

```html
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
<!-- 또는 <script src="lib/chart.js/chart.min.js"></script> -->
```

### `ServerChartComponent.razor.js`

```javascript
const charts = {};

export function createOverallChart(canvas, data) {
  const ctx = canvas.getContext("2d");
  // CDN/wwwroot 스크립트가 만든 글로벌 Chart
  const chart = new Chart(ctx, {
    type: "doughnut",
    data: data,
    options: { responsive: true, maintainAspectRatio: false },
  });
  charts.overall = chart;
  return chart;
}

export function disposeCharts() {
  Object.values(charts).forEach((c) => c?.destroy?.());
  Object.keys(charts).forEach((k) => delete charts[k]);
}
```

### `ServerChartComponent.razor`

```razor
@rendermode InteractiveServer
@implements IAsyncDisposable
@inject IJSRuntime JS

<canvas @ref="overallChartCanvas" width="400" height="300"></canvas>

@code {
    private ElementReference overallChartCanvas;
    private IJSObjectReference? module;

    // Chart.js data 형식에 맞게 구성
    private object ChartData => new
    {
        labels = new[] { "Online", "Offline" },
        datasets = new[]
        {
            new { data = new[] { 12, 3 }, backgroundColor = new[] { "#4caf50", "#f44336" } }
        }
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        try
        {
            module = await JS.InvokeAsync<IJSObjectReference>(
                "import",
                "./Components/Controls/ServerChartComponent.razor.js");

            await module.InvokeAsync(
                "createOverallChart",
                overallChartCanvas,
                ChartData);
        }
        catch (JSException ex)
        {
            Console.Error.WriteLine(ex); // 프리렌더·스크립트 미로드 등
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

**체크리스트**

- [ ] `<script>`로 Chart.js 로드 → 콘솔 `typeof Chart === "function"`
- [ ] `export`로 Blazor가 호출할 함수 공개
- [ ] DOM은 `@ref` + **AfterRender** (Initialized에서 Invoke 금지)
- [ ] `destroy` + 모듈 `DisposeAsync`
- [ ] Interactive 렌더 모드

---

## 권장 진행

| 단계 | 방식 |
| --- | --- |
| 학습/프로토타입 | CDN |
| 프로덕션 (소수 lib) | wwwroot 로컬 |
| 의존성 많음 | NPM + 복사/가벼운 번들 |

---

## 폴더 예

```
Project/
  Components/Controls/
    ServerChartComponent.razor
    ServerChartComponent.razor.js
  wwwroot/lib/chart.js/chart.min.js
  App.razor          ← script 태그
```

---

## 참고

- [JS interop](https://learn.microsoft.com/aspnet/core/blazor/js-interop/)
- [Chart.js](https://www.chartjs.org)
- [jsDelivr](https://www.jsdelivr.com)
