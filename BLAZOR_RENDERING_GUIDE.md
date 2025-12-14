# Blazor 렌더링 가이드

## 📚 개요

Blazor 컴포넌트의 렌더링 라이프사이클, 렌더링 모드, JavaScript Interop 최적화를 다룹니다.

**Pre-rendering이란:**

- 서버 사이드 렌더링이 먼저 수행된 후 클라이언트 렌더링 수행
- 빠른 페이지 로드와 SEO 지원

**Web Assembly 모드에서의 부작용:**

- `OnInitialized()` / `OnParametersSet()` / `SetParameters()` 2회 실행

**2회 실행을 피하는 방법:**

1. **`OnAfterRenderAsync()` + `firstRender` 체크** - 클라이언트 렌더링에서만 실행되도록 제어
2. **Pre-rendering 비활성화** - `@rendermode @(new InteractiveServerRenderMode(prerender: false))`
3. **`PersistentComponentState` 사용** - 서버에서 상태 저장 → 클라이언트에서 재사용
   - 서버: `ApplicationState.PersistAsJson("servers", servers);`
   - 클라이언트: `ApplicationState.TryTakeAsJson<List<Server>>("servers", out var savedServers);`

---

## 📋 핵심 개념

**Blazor의 렌더링 단계:**

```
1️⃣ 컴포넌트 초기화 → 2️⃣ 데이터 로드 → 3️⃣ HTML 렌더링 → 4️⃣ DOM 완성
```

**JavaScript Interop이 필요한 타이밍:**

- ✅ **DOM이 완성된 후** - DOM 요소 조작 가능
- ❌ **DOM 완성 전** - 요소를 찾을 수 없음

---

## 🎯 JavaScript Invoke 시 `OnAfterRenderAsync` 사용

**왜 `OnAfterRenderAsync`인가?**

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)  // ← 첫 렌더링일 때만
    {
        // 1️⃣ 이 시점: DOM이 완성됨 ✅
        // 2️⃣ @ref로 정의한 요소들 접근 가능

        var module = await JS.InvokeAsync<IJSObjectReference>(
            "import",
            "./ServerChartComponent.razor.js"
        );

        // 3️⃣ JavaScript 함수 호출 가능
        await module.InvokeAsync("createChart", canvasElement, data);
    }
}
```

**렌더링 타임라인:**

```
1. OnInitialized() / OnParametersSet()
   └─ DOM 아직 미렌더링 ❌

2. Render (HTML 생성)
   └─ @ref로 정의한 요소들이 DOM에 추가됨

3. OnAfterRenderAsync(firstRender: true)
   └─ DOM 완성 ✅ → JavaScript Invoke 가능 ✅
```

**잘못된 예:**

```csharp
protected override void OnInitialized()
{
    // ❌ 이 시점: @ref 요소가 아직 할당되지 않음
    var module = await JS.InvokeAsync(...);  // 에러!
}
```

---

## 🔀 Blazor 렌더링 모드별 동작

### 1️⃣ InteractiveServer (기본, Pre-rendering 없음)

```csharp
@rendermode InteractiveServer
```

**렌더링 순서:**

```
서버에서 렌더링 (1회)
    ↓
OnInitialized()
OnParametersSet()
    ↓
Render
    ↓
OnAfterRenderAsync(firstRender: true)
    ↓
완료 ✅
```

**특징:**

- ✅ 라이프사이클 메서드 1회만 실행
- ✅ `OnInitialized` + `OnAfterRenderAsync` 둘 다 사용 가능
- ✅ 가장 간단한 방식

---

### 2️⃣ InteractiveServer (Pre-rendering 활성화)

```csharp
@rendermode InteractiveServer
<!-- 또는 명시적으로 -->
@rendermode @(new InteractiveServerRenderMode(prerender: true))
```

**렌더링 순서:**

```
1. 서버에서 Pre-render (첫 번째 실행)
   ├─ OnInitialized()
   ├─ OnParametersSet()
   ├─ Render
   └─ OnAfterRenderAsync(firstRender: true)
        └─ ⚠️ 이 시점: DOM은 서버 메모리에만 존재
        └─ ❌ JavaScript Invoke 불가!

        ↓

2. 클라이언트에서 Hydration (두 번째 실행)
   ├─ OnInitialized()  ← 다시 실행됨!
   ├─ OnParametersSet() ← 다시 실행됨!
   ├─ Render
   └─ OnAfterRenderAsync(firstRender: true)  ← 다시 실행됨!
        └─ ✅ 이 시점: 브라우저 DOM 완성
        └─ ✅ JavaScript Invoke 가능 ✅
```

**문제점:**

- ❌ 라이프사이클 메서드 2회 실행 (중복)
- ❌ 비효율적 (같은 로직 2회)
- ❌ 데이터 로드 2회 (네트워크/DB 부하)

---

### 3️⃣ InteractiveWebAssembly

```csharp
@rendermode InteractiveWebAssembly
<!-- 또는 명시적으로 -->
@rendermode @(new InteractiveWebAssemblyRenderMode(prerender: true))
```

**렌더링 순서:**

```
1. 서버에서 Pre-render (첫 번째 실행)
   ├─ OnInitialized()
   ├─ OnParametersSet()
   ├─ Render
   └─ OnAfterRenderAsync(firstRender: true)
        └─ ❌ JavaScript Invoke 불가 (서버에서 실행)

        ↓

2. WebAssembly가 클라이언트 다운로드 완료 후 (두 번째 실행)
   ├─ OnInitialized()  ← 다시 실행됨!
   ├─ OnParametersSet() ← 다시 실행됨!
   ├─ Render
   └─ OnAfterRenderAsync(firstRender: true)  ← 다시 실행됨!
        └─ ✅ JavaScript Invoke 가능 ✅
```

**특징:**

- ❌ 라이프사이클 메서드 2회 실행 (필수)
- ⚠️ Pre-rendering으로 인한 중복은 피할 수 없음
- ✅ 클라이언트 렌더링이므로 서버 부하 낮음

---

## 🛡️ 2회 실행 문제 해결 방법

### ❌ 방법 1: `OnInitialized` 사용 (권장 안 함)

```csharp
protected override void OnInitialized()
{
    // ❌ Pre-render 시 2회 실행
    // ❌ 첫 번째는 서버, 두 번째는 클라이언트
    // ❌ JavaScript Invoke 불가 (서버에서 실행)
    data = LoadDataFromDatabase();  // 2회 실행
}
```

---

### ✅ 방법 2: `OnAfterRenderAsync` + `firstRender` 체크

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)  // ← 가장 중요!
    {
        // ✅ 첫 번째: 서버 Pre-render (JavaScript Invoke 불가)
        // ✅ 두 번째: 클라이언트 (JavaScript Invoke 가능) ← 실제로는 이것만 실행됨

        try
        {
            var module = await JS.InvokeAsync<IJSObjectReference>(
                "import",
                "./ServerChartComponent.razor.js"
            );

            await module.InvokeAsync("createChart", canvas, data);
        }
        catch (ObjectDisposedException)
        {
            // 서버 Pre-render 중 발생 (무시)
        }
    }
    // if (firstRender == false) 는 실행되지 않음
}
```

**왜 작동하는가?**

- 서버 Pre-render: `JS.InvokeAsync` 실패 → 예외 처리로 무시
- 클라이언트 렌더링: `JS.InvokeAsync` 성공 → 차트 생성

---

### ✅ 방법 3: Pre-rendering 비활성화 (가장 간단)

```csharp
@rendermode @(new InteractiveServerRenderMode(prerender: false))
```

**렌더링 순서:**

```
서버에서만 렌더링 (1회)
    ↓
OnInitialized()    ← 1회만
OnParametersSet()  ← 1회만
    ↓
Render
    ↓
OnAfterRenderAsync(firstRender: true)  ← 1회만
    ↓
✅ 중복 없음, JavaScript Invoke 가능
```

**장점:**

- ✅ 라이프사이클 1회만 실행
- ✅ 코드 간결함
- ✅ 성능 최적화

**단점:**

- ❌ SEO 불리 (처음 로드가 느림)
- ❌ 초기 HTML 크기 커짐

---

### ✅ 방법 4: `PersistentComponentState` 사용 (권장)

**개념:**

```
서버 Pre-render
    ↓
상태 저장 (PersistentComponentState)
    ↓
클라이언트 렌더링
    ↓
저장된 상태 복원 (중복 로드 제거)
```

**구현:**

```csharp
@inject PersistentComponentState ApplicationState
@inject IJSRuntime JS

@code {
    private List<Server> servers = new();

    protected override async Task OnInitializedAsync()
    {
        // 1️⃣ 서버 Pre-render: 서버에서 데이터 로드 + 상태 저장
        // 2️⃣ 클라이언트: 저장된 상태 복원 (DB 접근 안 함)

        if (!ApplicationState.TryTakeAsJson<List<Server>>("servers", out var savedServers))
        {
            // 처음 실행: 서버에서 데이터 로드
            servers = await ServerService.GetServersAsync();

            // 상태 저장 (클라이언트로 전송)
            ApplicationState.PersistAsJson("servers", servers);
        }
        else
        {
            // 클라이언트 렌더링: 저장된 상태 사용
            servers = savedServers ?? new();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // ✅ 이 시점: 데이터 이미 로드됨 (중복 없음)
            var module = await JS.InvokeAsync<IJSObjectReference>(
                "import",
                "./ServerChartComponent.razor.js"
            );

            await module.InvokeAsync("createChart", canvas, servers);
        }
    }
}
```

**렌더링 순서:**

```
1. 서버 Pre-render
   ├─ OnInitializedAsync() 호출
   ├─ DB에서 데이터 로드
   ├─ ApplicationState.PersistAsJson("servers", servers) ← 상태 저장
   ├─ Render
   └─ OnAfterRenderAsync (JavaScript Invoke 불가, 무시)

        ↓

2. HTML + 상태 전송 (네트워크)

        ↓

3. 클라이언트 렌더링
   ├─ OnInitializedAsync() 호출
   ├─ TryTakeAsJson() ← 저장된 상태 복원 (DB 접근 X)
   ├─ servers = savedServers (즉시 할당)
   ├─ Render
   └─ OnAfterRenderAsync(firstRender: true)
       └─ ✅ JavaScript Invoke 성공 ✅
```

**장점:**

- ✅ DB 접근 1회만 (서버)
- ✅ 클라이언트는 저장된 상태 사용 (빠름)
- ✅ SEO 유리 (Pre-rendering 활용)
- ✅ 성능 최적화
- ✅ JavaScript Invoke 안전

**단점:**

- 코드가 복잡함 (선택사항)

---

## 📊 방법별 비교표

| 방법                  | JavaScript Invoke | 중복 실행 | 성능 | SEO | 추천               |
| --------------------- | ----------------- | --------- | ---- | --- | ------------------ |
| `OnInitialized`       | ❌                | ❌ 2회    | ⚠️   | ✅  | ❌ 권장 안 함      |
| `OnAfterRenderAsync`  | ✅                | ⚠️ 2회    | ⚠️   | ✅  | ✅ 간단한 프로젝트 |
| Pre-render 비활성     | ✅                | ✅ 1회    | ✅   | ❌  | ✅ 개발/테스트     |
| `PersistentComponent` | ✅                | ✅ 1회    | ✅✅ | ✅  | ✅ 프로덕션 권장   |

---

## 🎯 상황별 가이드

### 개발/테스트 환경

```csharp
@rendermode @(new InteractiveServerRenderMode(prerender: false))

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        var module = await JS.InvokeAsync<IJSObjectReference>(
            "import", "./chart.razor.js"
        );
        await module.InvokeAsync("createChart", canvas, data);
    }
}
```

### 프로덕션 환경 (권장)

```csharp
@rendermode InteractiveServer
@inject PersistentComponentState ApplicationState

protected override async Task OnInitializedAsync()
{
    if (!ApplicationState.TryTakeAsJson<ChartData>("chartData", out var data))
    {
        data = await LoadChartDataAsync();
        ApplicationState.PersistAsJson("chartData", data);
    }
    ChartData = data ?? new();
}

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        var module = await JS.InvokeAsync<IJSObjectReference>(
            "import", "./chart.razor.js"
        );
        await module.InvokeAsync("createChart", canvas, ChartData);
    }
}
```

---

## 🔑 핵심 정리

| 상황                | 권장 방법             | 이유                     |
| ------------------- | --------------------- | ------------------------ |
| **데이터만 로드**   | `OnInitialized`       | DOM 조작 불필요          |
| **JavaScript 필요** | `OnAfterRenderAsync`  | DOM 완성 후 실행 필수    |
| **WebAssembly**     | `OnAfterRenderAsync`  | Pre-render 2회 실행 필수 |
| **SEO + 성능**      | `PersistentComponent` | 프로덕션 최적화          |
| **빠른 개발**       | Pre-render 비활성     | 렌더링 1회, 코드 간결    |

---

## 📞 참고 자료

- Microsoft Blazor 공식: https://learn.microsoft.com/dotnet/core/fundamentals/reflection/dynamically-loading-and-using-types
- Blazor 라이프사이클: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle
- PersistentComponentState: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/state-management
