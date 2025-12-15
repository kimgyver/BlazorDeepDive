# Blazor 학습 가이드

## 📌 Application Types (Render Mode)

### 1️⃣ Blazor SSR (Server Side Rendering)

- **실행 위치:** 서버
- **특징:** 서버에서 렌더링 후 정적 HTML을 클라이언트에 전송
- **Parameter 받기:**
  - `[Parameter]` ← 부모 컴포넌트 또는 라우트에서
  - `[SupplyParameterFromForm]` ← Form 제출 (Blazor SSR만 가능)
  - `[SupplyParameterFromQuery]` ← 쿼리 문자열 (?a=b&c=d)
- **StreamRendering:** 빠른 초기 렌더링 + 동적 결과 지연 표시 (Blazor SSR만 가능)

### 2️⃣ Server Interactivity (InteractiveServer)

- **실행 위치:** 서버
- **특징:** SignalR (WebSocket) 연결로 실시간 양방향 통신
- **UI 처리:** 서버에서 이벤트 처리, 세션/상태 관리
- **@rendermode 설정:**
  - 컴포넌트 레벨: `@rendermode InteractiveServer`
  - 페이지 레벨: `@rendermode` 지시문
  - 전역 (App.razor): 기본값 설정
- **Pre-rendering 옵션:** `@rendermode InteractiveServer(prerender: false)`
- **장점:** 보안 강함
- **단점:** 네트워크 지연 가능
- **대상:** 소규모 서비스 (동시접속 < 1000)

**주요 기능:**

- `<Virtualize>`: 보이는 부분만 렌더링 (대량 리스트)
- `@bind` / `@bind-Value`: 양방향 바인딩 (InteractiveServer에서만 가능)

### 3️⃣ WebAssembly (InteractiveWebAssembly)

- **실행 위치:** 브라우저 (클라이언트)
- **특징:** 전체 애플리케이션이 브라우저에서 실행
- **자동 Pre-rendering:** WebAssembly는 자동으로 서버에서 Pre-rendering됨
- **DI 설정 필수:** Host 프로젝트 + Client 프로젝트 모두 필요
- **장점:** 빠른 응답성, 네트워크 비용 적음
- **단점:** 보안 약함 (코드 노출), DI 복잡
- **대상:** 대규모 서비스 (동시접속 > 1000), 보안 이슈 적을 경우

**주요 차이점:**

| 항목         | Server              | WebAssembly             |
| ------------ | ------------------- | ----------------------- |
| **DB 접근**  | ✅ 직접 가능        | ❌ API로만 가능         |
| **보안**     | 강함 (코드 숨김)    | 약함 (코드 노출)        |
| **네트워크** | SignalR (지연 가능) | 없음 (빠름)             |
| **인증**     | SignalR 채널        | JWT 토큰 (LocalStorage) |
| **DI Setup** | Host만 필요         | Host + Client 모두 필요 |

**WebAssembly에서는 API 필수:**

```csharp
// ❌ 불가능 (DB 접근)
public class ServerService : IServerService {
    public async Task GetServersAsync() {
        var servers = await dbContext.Servers.ToListAsync();  // ❌ 브라우저에서 불가
    }
}

// ✅ 가능 (API 호출)
public class ServerApiService : IServerService {
    private readonly HttpClient http;

    public async Task GetServersAsync() {
        return await http.GetFromJsonAsync("/api/servers");  // ✅ API로 통신
    }
}
```

---

## 🎯 Component Features

### Arbitrary Attributes

**개념:** React의 `...rest props`와 유사 - 유연한 파라미터 전달

```razor
<div class="input-group mb-3 input-width" @attributes=OtherAttributes>
    <!-- 추가 속성 적용 -->
</div>

@code {
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? OtherAttributes { get; set; }
}
```

**사용 예시:**

```razor
<MyComponent placeholder="검색..." aria-label="검색" data-test="search-box" />
```

---

### @bind / @bind-Value (Two-way Binding)

**조건:** InteractiveServer 또는 InteractiveWebAssembly에서만 작동

**내부 메커니즘:** 3개 파라미터 자동 연결

- `Value="..."` - 현재 값
- `ValueChanged="..."` - 값 변경 핸들러
- `ValueExpression="..."` - 식 추적

**사용법:**

```razor
<!-- HTML 표준 요소 (@bind 사용) -->
<input @bind="serverName" />
<textarea @bind="description"></textarea>
<select @bind="selectedCity">
    <option value="toronto">Toronto</option>
</select>

<!-- Blazor 컴포넌트 (@bind-Value 사용) -->
<InputText @bind-Value="server.Name" />
<InputNumber @bind-Value="server.Port" />

<!-- 실시간 반응성 (매 입력마다 업데이트) -->
<input @bind-value="searchText" @bind-value:event="oninput" />
```

---

### Cascading Parameters

**개념:** 부모에서 자식 트리 전체로 데이터 공유

```razor
<!-- 부모: Pages/Servers.razor -->
<CascadingValue Value="@selectedCity" Name="SelectedCity">
    <MainLayout />
</CascadingValue>

@code {
    private string selectedCity = "Toronto";
}
```

```razor
<!-- 자식 (손자, 증손자 포함): ServerComponent.razor -->
[CascadingParameter(Name = "SelectedCity")]
public string SelectedCity { get; set; }
```

**제한사항:**

- ❌ Render Mode 경계를 넘을 수 없음 → DI 사용해야 함
- ✅ 모든 깊이의 자식에 전파됨 (직접 자식뿐만 아니라)

---

### DynamicComponent

**개념:** 런타임에 표시할 컴포넌트 동적 결정

```csharp
@code {
    private Type componentType;
    private Dictionary<string, object> parameters;

    private void SelectComponent(bool isAdmin) {
        if (isAdmin) {
            componentType = typeof(AdminPanel);
        } else {
            componentType = typeof(UserPanel);
        }

        parameters = new() { { "UserId", userId } };
    }
}
```

```razor
<DynamicComponent Type="@componentType" Parameters="@parameters" />
```

**사용 사례:**

- 사용자 권한에 따라 다른 컴포넌트 표시
- 플러그인 시스템
- 동적 폼 레이아웃

---

### EditForm (Form Processing)

**작동:** 모든 Render Mode에서 작동

**검증 메커니즘:**

```razor
<EditForm Model="@server" OnValidSubmit="HandleValidSubmit" OnInvalidSubmit="HandleInvalidSubmit">
    <!-- 1단계: 검증 규칙 활성화 -->
    <DataAnnotationsValidator />

    <!-- 2단계: 전체 에러 표시 -->
    <ValidationSummary />

    <!-- 3단계: 필드별 에러 표시 -->
    <InputText @bind-Value="server.Name" />
    <ValidationMessage For="@(() => server.Name)" />

    <button type="submit">저장</button>
</EditForm>

@code {
    private Server server = new();

    // ✅ 검증 성공 - DB 저장
    private void HandleValidSubmit() {
        ServerService.Save(server);
    }

    // ❌ 검증 실패 - 에러 표시만 (저장 안 함)
    private void HandleInvalidSubmit() {
        Console.WriteLine("입력값 검증 실패!");
    }
}
```

**Model Validation (DataAnnotations):**

```csharp
public class Server {
    [Required(ErrorMessage = "서버명은 필수입니다")]
    [StringLength(50, MinimumLength = 3)]
    public string Name { get; set; }

    [Range(1, 65535, ErrorMessage = "포트는 1-65535 사이여야 합니다")]
    public int Port { get; set; }
}
```

---

### EventCallback Parameter

**개념:** 자식에서 부모로 이벤트 알림 (React의 콜백, Angular의 @Output)

```razor
<!-- 부모: ServerListComponent.razor -->
<ServerComponent OnDelete="HandleDelete" />

@code {
    private void HandleDelete(int serverId) {
        Console.WriteLine($"삭제: {serverId}");
    }
}
```

```razor
<!-- 자식: ServerComponent.razor -->
[Parameter]
public EventCallback<int> OnDelete { get; set; }

<button @onclick="() => OnDelete.InvokeAsync(Server.Id)">삭제</button>
```

---

### @inherits

**개념:** Razor 컴포넌트가 C# 클래스에서 상속

```csharp
// Base.cs
public class ServerComponentBase : ComponentBase {
    protected List<Server> servers = new();

    protected virtual async Task LoadServersAsync() {
        // 공통 로직
    }
}
```

```razor
<!-- ServerComponent.razor -->
@inherits ServerComponentBase

<div>
    @foreach (var server in servers) {
        <div>@server.Name</div>
    }
</div>
```

---

## 🌐 JavaScript Interop (Blazor에서 JavaScript 이용하기)

### 0️⃣ Node.js Package 로드하기 (필요시)

**3가지 방식:**

| 방식           | 용도     | 상황                       |
| -------------- | -------- | -------------------------- |
| **CDN 방식**   | 개발용   | ✅ 패키지 1-2개만 필요     |
| **로컬 배치**  | 프로덕션 | 👍 권장 - 패키지 소수      |
| **NPM + 빌드** | 자동화   | 💡 많은 패키지 존재할 경우 |

### 1️⃣ JavaScript 작성 (xxxx.razor.js)

```javascript
// 패키지 기능 접근 (CDN으로 로드된 글로벌 변수)
export function createChart(canvas, data) {
  const ctx = canvas.getContext("2d");
  return new Chart(ctx, {
    // ← 글로벌 Chart 변수
    type: "doughnut",
    data: data
  });
}

// 커스텀 함수 (localStorage, DOM 조작 등)
export function saveToLocalStorage(key, value) {
  localStorage.setItem(key, value);
}

export function getFromLocalStorage(key) {
  return localStorage.getItem(key);
}
```

### 2️⃣ Blazor에서 JavaScript 호출

```csharp
@inject IJSRuntime JS

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender) {
            try {
                // 1. 모듈 로드 (import)
                var module = await JS.InvokeAsync<IJSObjectReference>(
                    "import",
                    "./Components/Controls/ServerChartComponent.razor.js"
                );

                // 2. 함수 호출 (invoke)
                await module.InvokeAsync(
                    "createChart",
                    canvasElement,
                    chartData
                );

                // 3. 커스텀 함수 호출
                var saved = await module.InvokeAsync<string>(
                    "getFromLocalStorage",
                    "username"
                );
            }
            catch (JSException ex) {
                // 에러 처리 (예상치 못한 에러 자주 발생)
                Logger.LogError($"JS 오류: {ex.Message}");
            }
        }
    }
}
```

### 글로벌 변수명 찾는 방법

**패키지마다 글로벌 변수명이 다릅니다!**

| 패키지    | 글로벌 변수    | 공식 문서                                              |
| --------- | -------------- | ------------------------------------------------------ |
| Chart.js  | `Chart`        | https://www.chartjs.org/docs/latest/getting-started/   |
| jQuery    | `jQuery` / `$` | https://jquery.com/                                    |
| Bootstrap | `bootstrap`    | https://getbootstrap.com/docs/5.0/getting-started/cdn/ |
| Three.js  | `THREE`        | https://threejs.org/docs/                              |
| D3.js     | `d3`           | https://d3js.org/                                      |
| Lodash    | `_`            | https://lodash.com/                                    |

**찾는 순서:**

1. 패키지 공식 웹사이트 → "Installation" 또는 "Getting Started" 섹션
2. "CDN" 또는 "Browser" 섹션에서 글로벌 변수명 확인
3. 예제 코드로 사용법 파악
4. 불확실하면 브라우저 콘솔: `typeof Chart`로 확인

---

## 🔄 Lifecycle & Rendering

### Pre-rendering

**개념:** 서버에서 먼저 HTML 생성 → 브라우저로 전송 (빠른 초기 로딩 + SEO)

**부작용 (WebAssembly 모드):**

```csharp
// OnInitialized / OnParametersSet / SetParameters 이 2번 실행됨
// 1회차: 서버에서 (Pre-rendering)
// 2회차: 클라이언트에서 (Hydration)
```

**해결 방법:**

**1️⃣ OnAfterRenderAsync + firstRender 체크**

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
        // 한 번만 실행 (클라이언트에서만)
        // JavaScript Interop이 필요한 경우 사용
    }
}
```

**2️⃣ Pre-rendering 비활성화**

```razor
@rendermode InteractiveServer(prerender: false)
```

**3️⃣ PersistentComponentState 사용 (권장)**

```csharp
// 서버: 1회차에서 결과 저장
ApplicationState.PersistAsJson("servers", servers);

// 클라이언트: 2회차에서 저장된 값 재사용
if (!ApplicationState.TryTakeAsJson<List<Server>>("servers", out var savedServers)) {
    servers = await ServerService.GetServersAsync();  // 서버: DB 접근
} else {
    servers = savedServers;  // 클라이언트: 캐시된 값 사용
}
```

---

### @ref (Child Reference)

**개념:** 부모에서 자식 컴포넌트의 public 메서드 호출 (React의 `useRef`, Angular의 `ViewChild`)

```razor
<!-- 부모 -->
<SearchBarComponent @ref="searchBar" />
<button @onclick="ClearSearch">검색 초기화</button>

@code {
    private SearchBarComponent searchBar;

    private void ClearSearch() {
        searchBar?.ClearFilter();  // 자식의 public 메서드 호출
    }
}
```

```razor
<!-- 자식: SearchBarComponent -->
public void ClearFilter() {
    searchText = "";
    // 필터 초기화 로직
}
```

**여러 ref 관리:**

```csharp
private Dictionary<int, RowComponent> rows = new();

<RowComponent @ref="rows[item.Id]" Item="@item" />
```

---

### @rendermode 지시문

```razor
<!-- 컴포넌트 레벨 설정 -->
@rendermode InteractiveServer

<!-- Pre-rendering 비활성화 -->
@rendermode InteractiveServer(prerender: false)

<!-- WebAssembly -->
@rendermode InteractiveWebAssembly

<!-- App.razor에서 전역 설정 -->
@rendermode InteractiveServer
```

---

### StateHasChanged() (Render Forcing)

**개념:** Blazor가 변경을 감지하지 못할 때 수동으로 렌더링 요청

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender) {
    if (firstRender) {
        message = "Updated";
        StateHasChanged();  // 렌더링 강제
    }
}

// 백그라운드 스레드에서 상태 업데이트
await Task.Run(() => {
    message = "Updated from background";
    InvokeAsync(StateHasChanged);  // 반드시 InvokeAsync 사용
});
```

⚠️ **주의:** OnInitialized()에서 사용하면 무한 루프 발생 가능

---

## 🎨 Component Patterns

### RenderFragment (Templated Component)

**개념:** React의 `children props`, Vue의 `slot`과 유사

**기본 사용:**

```razor
<!-- 부모 -->
<CardComponent>
    <h2>제목</h2>
    <p>내용</p>
</CardComponent>

<!-- CardComponent -->
[Parameter]
public RenderFragment? ChildContent { get; set; }

<div class="card">
    @ChildContent
</div>
```

**제네릭 + Context (RenderFragment<TValue>):**

```razor
<!-- 부모 -->
<ListComponent Items="@servers">
    <ItemTemplate Context="server">
        <div>@server.Name - @server.City</div>
    </ItemTemplate>
</ListComponent>

<!-- ListComponent -->
@typeparam TItem

<div>
    @foreach (var item in Items) {
        @ItemTemplate(item)  // Context로 각 아이템 전달
    }
</div>

@code {
    [Parameter]
    public List<TItem> Items { get; set; }

    [Parameter]
    public RenderFragment<TItem> ItemTemplate { get; set; }
}
```

---

### State Management

**전역 상태 관리 (Redux처럼):**

```csharp
// AppState.cs
public class AppState {
    public List<Server> Servers { get; set; } = new();
    public event Action OnStateChanged;

    public void UpdateServers(List<Server> servers) {
        Servers = servers;
        OnStateChanged?.Invoke();
    }
}

// Program.cs
builder.Services.AddScoped<AppState>();

// 사용 컴포넌트
@inject AppState AppState

@code {
    protected override void OnInitialized() {
        AppState.OnStateChanged += StateHasChanged;
    }
}
```

**부모-자식 상태 전달:**

```razor
<!-- @Parameter 또는 @CascadingParameter로 수신 -->
[Parameter]
public Server Server { get; set; }

[CascadingParameter]
public AppState AppState { get; set; }
```

---

### SynchronizationContext

**개념:** 서버 스레드와 브라우저 스레드 간 동기화 (Server Interactivity)

```csharp
// ❌ 잘못된 예: UI 상태에 직접 접근
await Task.Run(() => {
    message = "Updated";  // ❌ 스레드 불안전
});

// ✅ 올바른 예: SynchronizationContext 사용
await Task.Run(() => {
    var syncContext = SynchronizationContext.Current;
    syncContext?.Post(_ => {
        message = "Updated from background";  // ✅ 안전
        InvokeAsync(StateHasChanged);
    }, null);
});
```

---

## ⚡ Performance & Lists

### Virtualize

**개념:** 보이는 부분만 렌더링 (대량 리스트에 필수)

```razor
@if (servers != null) {
    <Virtualize Items="@servers" Context="server">
        <div @key="server.Id">  <!-- @key 필수 -->
            @server.Name
        </div>
    </Virtualize>
}

@code {
    private List<Server> servers;
}
```

**조건:** InteractiveServer 또는 InteractiveWebAssembly에서만 작동

### @key 디렉티브

**개념:** 리스트 아이템 추적 (순서 변경 시 상태 보존)

```razor
<!-- ❌ @key 없음: 입력값이 섞임 -->
@foreach (var item in items) {
    <input @bind-value="item.Name" />
}

<!-- ✅ @key 있음: ID로 추적 -->
@foreach (var item in items) {
    <div @key="item.Id">  <!-- item.Id로 추적 -->
        <input @bind-value="item.Name" />
    </div>
}
```

**필수 사용 사례:**

- Virtualize 컴포넌트
- 리스트 순서 변경 가능
- 동적 리스트 추가/삭제

---

## 🎯 Event Handling

### @on{event} 문법

```razor
<!-- 기본 클릭 -->
<button @onclick="HandleClick">클릭</button>

<!-- preventDefault: 기본 동작 방지 -->
<form @onsubmit:preventDefault="HandleSubmit">
    <input type="submit" />
</form>

<!-- stopPropagation: 이벤트 전파 중단 -->
<div @onclick="ParentClick">
    부모
    <button @onclick:stopPropagation="ChildClick">
        자식 (부모 클릭 안 됨)
    </button>
</div>

<!-- 변경 감지 이벤트 -->
<input @onchange="HandleChange" />
<input @onkeypress="HandleKeyPress" />
```

### Lambda 캡처링 주의

```razor
<!-- ❌ 잘못된 예: item의 마지막 값만 사용됨 -->
@foreach (var item in items) {
    <button @onclick="() => Delete(item)">
        삭제
    </button>
}

<!-- ✅ 올바른 예: 변수에 저장 -->
@foreach (var item in items) {
    @{
        var itemCopy = item;
    }
    <button @onclick="() => Delete(itemCopy)">
        삭제
    </button>
}
```

---

## 🔒 Navigation

### NavigationLock

**개념:** 페이지 이탈 방지 (폼 변경여부 확인)

```razor
<NavigationLock OnBeforeInternalNavigation="OnBeforeInternalNavigation"
               ConfirmExternalNavigation="true" />

@code {
    private async Task OnBeforeInternalNavigation(LocationChangingContext context) {
        if (hasUnsavedChanges) {
            context.PreventNavigation();
            // 경고 표시 또는 저장 처리
        }
    }
}
```

---

## 📊 DI Lifetime

| 수명          | Blazor SSR | Blazor Server  | Blazor WebAssembly        |
| ------------- | ---------- | -------------- | ------------------------- |
| **Transient** | 요청마다   | 호출마다       | 호출마다                  |
| **Scoped**    | 요청마다   | 연결당 1개 ⭐️ | 없음 (Transient처럼 작동) |
| **Singleton** | 앱 전체    | 앱 전체        | 앱 전체                   |

**Blazor Server에서 Scoped가 중요한 이유:** 사용자별로 독립적인 상태 유지

---

## 📞 참고 자료

- Blazor 공식 문서: https://learn.microsoft.com/blazor
- Chart.js: https://www.chartjs.org
- Virtualization: https://learn.microsoft.com/blazor/components/virtualization
