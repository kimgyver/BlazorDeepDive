# Blazor 학습 가이드

> Render mode·컴포넌트 API·실무 패턴 치트시트.  
> 프리렌더/JS 타이밍 상세 → [`BLAZOR_RENDERING_GUIDE.md`](./BLAZOR_RENDERING_GUIDE.md)  
> Chart.js·CDN/wwwroot → [`NODE_JS_PACKAGES_GUIDE.md`](./NODE_JS_PACKAGES_GUIDE.md)

---

## Application Types (Render Mode)

### 1. Static SSR (정적 서버 렌더링)

- **실행:** 서버에서 HTML 생성 → 클라이언트로 전송 (기본 상호작용 없음)
- **파라미터:**
  - `[Parameter]` ← 부모 또는 `@page` 경로 (`/items/{id:int}`)
  - `[SupplyParameterFromForm]` ← **정적 SSR 폼 POST** (EditForm / HTML form)에 특히 중요
  - `[SupplyParameterFromQuery]` ← `?a=b` (**SSR·Interactive 모두**에서 사용 가능 — SSR 전용 아님)
- **StreamRendering:** 초기 HTML을 먼저 보내고 이후 스트리밍으로 채움 (**정적 SSR** 맥락)

### 2. Interactive Server (`InteractiveServer`)

- **실행:** 서버. **SignalR**로 UI 이벤트·상태 동기화
- **설정 위치:** 컴포넌트 / 페이지 / 전역(`Routes`·`App` 등)
- **프리렌더:** .NET 8+에서 interactive 모드는 보통 **기본 prerender ON**  
  끄기: `@rendermode @(new InteractiveServerRenderMode(prerender: false))`
- **강점:** 비밀·민감 로직을 서버에 두기 쉬움  
- **약점:** 회로·네트워크 지연, 서버 부하(동시 회로 수)
- **선택 기준:** “동시접속 숫자”가 아니라 **보안·서버 상태·운영 모델**로 고름

**Interactive에서 의미 있는 기능:** `<Virtualize>`, `@bind` / `@bind-Value`, `NavigationLock` 등

### 3. Interactive WebAssembly (`InteractiveWebAssembly`)

- **실행:** 브라우저(.NET WASM)
- **프리렌더:** 기본이 **ON인 경우가 많음** (모드가 “강제 유발”이 아니라 **옵션 기본값**). `prerender: false`로 가능
- **DI:** Host(프리렌더·서버 쪽)와 Client(브라우저) **양쪽에 필요한 서비스 등록**이 흔한 패턴
- **선택 이유:** 서버 UI 회로 부담 ↓, 오프라인/클라 연산, API만 호출하는 구조  
  ❌ “동접 > 1000이라서”, ❌ “보안이 약해서/낮아서”

| 항목 | Interactive Server | Interactive WASM |
| --- | --- | --- |
| **데이터** | 서버에서 DB/서비스 가능 (컴포넌트 직접 DbContext는 비권장) | **API/`HttpClient`만** (브라우저에서 DB 불가) |
| **비밀** | 서버에 두기 용이 | 클라에 심지 말 것 → API 인증 |
| **UI 전송** | SignalR 회로 | 다운로드 후 로컬 실행 (API 왕복은 별개) |
| **인증(전형)** | 쿠키/회로 연계 등 서버 중심 | 브라우저 토큰 저장(JWT 등) + API — XSS 주의 |
| **DI 등록** | Host 중심 | Host + Client |

```csharp
// WASM: DB 직접 ❌ → API ✅
return await http.GetFromJsonAsync<List<Server>>("/api/servers");
```

### 4. Interactive Auto

- **초기:** 대개 **Server interactivity**로 빠르게 시작
- **이후:** WASM 다운로드되면 **WASM으로 전환** 가능
- Host/Client DI·상태·프리렌더를 **양쪽 전제**로 설계

---

## Routing & Layout

### `@page` / 라우트 파라미터

```razor
@page "/servers"
@page "/servers/back_from/{cityName}"
@page "/servers/{id:int}"
@page "/server/{id:int?}"
```

```csharp
[Parameter] public string? CityName { get; set; }
[Parameter] public int? Id { get; set; }
```

- 경로 제약을 붙이면 매칭·타입이 안전해짐: `int`, `guid`, `datetime`, `*` 등
- 같은 페이지에 `@page`를 **여러 개** 둘 수 있음
- 쿼리는 `[SupplyParameterFromQuery]`, 경로는 `[Parameter]` + `@page` 템플릿

### 레이아웃 (`@layout` / `@Body`)

```razor
@* MainLayout.razor — 이 레포 ServerManagement/Components/Layout *@
@inherits LayoutComponentBase
<div class="page">
    <NavMenu />
    <article>@Body</article>
</div>
```

```razor
@* 페이지에서 (기본 레이아웃이 아니면) *@
@layout MainLayout
@page "/dashboard"
```

페이지 내용이 레이아웃의 `@Body` 자리에 삽입됨. React `<Outlet />` / Angular 부모 `router-outlet`에 가깝음.

### `Routes` / `NavLink` / `PageTitle` / `FocusOnNavigate`

```razor
@* Routes.razor 요지 *@
<Router AppAssembly="...">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

```razor
<NavLink href="/servers" Match="NavLinkMatch.Prefix">Servers</NavLink>
<PageTitle>Servers</PageTitle>
```

- `NavLink`: 활성 라우트에 CSS 클래스
- `PageTitle`: 브라우저 탭 제목
- `FocusOnNavigate`: 이동 후 포커스(접근성)

---

## Component Features

### Arbitrary Attributes

React `...rest`와 유사.

```razor
<div class="input-group" @attributes="OtherAttributes"></div>

@code {
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? OtherAttributes { get; set; }
}
```

### @bind / @bind-Value

**조건:** **Interactive** 모드 (Server / WASM / Auto). 정적 SSR만으로는 이벤트 기반 양방향 바인딩 불가.

내부적으로 `Value` + `ValueChanged` + `ValueExpression` 연결.

```razor
<input @bind="serverName" />
<InputText @bind-Value="server.Name" />

<!-- 입력마다 갱신 -->
<input @bind="searchText" @bind:event="oninput" />
<!-- 또는 컴포넌트 -->
<InputText @bind-Value="searchText" @bind-Value:event="oninput" />
```

- HTML 요소: `@bind` / `@bind:event`
- `InputText` 등: `@bind-Value`

```razor
@* 세부: get/set/after (Interactive) *@
<input @bind:get="name" @bind:set="OnNameSet" @bind:after="OnNameChanged" />
```

### Cascading Parameters

```razor
<CascadingValue Value="@selectedCity" Name="SelectedCity">
    <ChildTree />
</CascadingValue>

@code {
    private string selectedCity = "Toronto";
}
```

```csharp
[CascadingParameter(Name = "SelectedCity")]
public string? SelectedCity { get; set; }
```

- `Name`을 쓰면 수신 쪽도 **`Name`을 맞춰야** 함
- **Render mode 경계를 넘지 못함** → 경계 너머는 **DI**
- 같은 트리·같은 모드 안에서는 깊이 제한 없이 전파

### DynamicComponent

```razor
@code {
    private Type? componentType;
    private Dictionary<string, object>? parameters;

    void SelectComponent(bool isAdmin)
    {
        componentType = isAdmin ? typeof(AdminPanel) : typeof(UserPanel);
        parameters = new() { ["UserId"] = userId };
    }
}

<DynamicComponent Type="@componentType" Parameters="@parameters" />
```

런타임 타입 선택. **코드 스플리팅(lazy assembly)과는 별개**.

### EditForm

모든 렌더 모드에서 사용 가능. 정적 SSR에서는 `[SupplyParameterFromForm]`과 함께 쓰는 패턴이 중요.

```csharp
public class Server
{
    [Required(ErrorMessage = "서버명은 필수입니다")]
    [StringLength(50, MinimumLength = 3)]
    public string Name { get; set; } = "";

    [Range(1, 65535)]
    public int Port { get; set; }
}
```

```razor
<EditForm Model="@server" OnValidSubmit="HandleValidSubmit" OnInvalidSubmit="HandleInvalidSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />
    <InputText @bind-Value="server.Name" />
    <ValidationMessage For="@(() => server.Name)" />
    <InputNumber @bind-Value="server.Port" />
    <button type="submit">저장</button>
</EditForm>

@code {
    private Server server = new();

    void HandleValidSubmit() => ServerService.Save(server);
    void HandleInvalidSubmit() { /* 검증 실패 — 저장 안 함 */ }
}
```

### EventCallback

Angular `@Output`, React 콜백 props와 유사.

```razor
@* 부모 *@
<ServerCard OnDelete="HandleDelete" />
@code {
    void HandleDelete(int id) => Console.WriteLine($"삭제: {id}");
}
```

```razor
@* 자식 ServerCard *@
[Parameter] public EventCallback<int> OnDelete { get; set; }
<button @onclick="() => OnDelete.InvokeAsync(Server.Id)">삭제</button>
```

### @inherits

```csharp
public class ServerComponentBase : ComponentBase
{
    protected List<Server> servers = new();
    protected virtual Task LoadServersAsync() => Task.CompletedTask;
}
```

```razor
@inherits ServerComponentBase
@foreach (var s in servers) { <div>@s.Name</div> }
```

### 인증 UI (요약)

- 페이지: `@attribute [Authorize]` + `AuthorizeRouteView`
- UI 조각: `AuthorizeView` (역할/정책)
- 진입 차단 vs 화면 일부 표시를 구분

### ErrorBoundary

Interactive에서 자식 **렌더** 예외 경계. 모든 비동기/이벤트 예외를 막지는 않음.

---

## JavaScript Interop (요약)

상세·Chart.js → `NODE_JS_PACKAGES_GUIDE.md` / 타이밍 → `BLAZOR_RENDERING_GUIDE.md`

1. 스크립트: CDN(개발) / `wwwroot`(프로덕션 권장) / NPM+번들(선택)
2. collocated `*.razor.js` + `export`
3. `@inject IJSRuntime` → `OnAfterRenderAsync`에서 `import` → `InvokeAsync`
4. `IJSObjectReference`는 **`IAsyncDisposable`로 해제**
5. 예외·로깅 필수

---

## Lifecycle & Rendering (요약)

### 라이프사이클 (시간 순서 →)

| Construct | Initialise | Receive parameter | Render | Destruct |
| --- | --- | --- | --- | --- |
| ctor / `[Inject]`·`@inject` | `OnInitialized[Async]` | `SetParametersAsync` | `ShouldRender` | `Dispose` / `DisposeAsync` |
| | | `OnParametersSet[Async]` | `OnAfterRender[Async]` | |
| DI·필드 준비 | 첫 파라미터 후 **1회** 데이터 로드 | 적용 가로채기·억제·전처리 (`base` 필수) | 렌더 **직전** 여부 필터 | 이벤트·리소스·JS 모듈 해제 |
| | | 반영 후 반응·검증·재조회 | 렌더 **직후** · `firstRender` · JS/`@ref` | |

```
시간 →  Construct → Initialise → Receive → Render → Destruct
재진입 →              (최초만)     ←── StateHasChanged / 파라미터·이벤트 ──→
```

- 칸 안 위→아래도 시간 순. Receive: `SetParametersAsync` → (최초면 Initialise) → `OnParametersSet*`. Render: `ShouldRender` → 마크업 → `OnAfterRender*`.
- 프리렌더 ON이면 Initialise~Render가 패스당 한 번씩 → 최대 2회.

```csharp
protected override async Task OnParametersSetAsync()
{
    // /servers/{id} 가 바뀌면 여기가 다시 돈다
    server = await ServerService.GetAsync(Id);
}

protected override async Task SetParametersAsync(ParameterView parameters)
{
    if (Locked) return; // OnParametersSet 호출 자체를 막을 때
    await base.SetParametersAsync(parameters);
}

protected override bool ShouldRender() => !_suppressRender;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (!firstRender) return;
    module = await JS.InvokeAsync<IJSObjectReference>("import", "./Chart.razor.js");
}
```

### IDisposable / IAsyncDisposable

```razor
@implements IDisposable
@implements IAsyncDisposable

@code {
    public void Dispose()
    {
        AppState.OnChange -= StateHasChanged; // 이벤트 구독 해제
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

- 동기 정리 → `IDisposable`
- JS 모듈·비동기 해제 → `IAsyncDisposable` (Chart 컴포넌트 패턴)

### Pre-rendering

서버(또는 프리렌더 패스)에서 HTML을 먼저 그림 → SEO·첫 페인트.  
프리렌더 ON이면 `OnInitialized` / `OnParametersSet` 등이 **두 번** 돌 수 있음 (Server·WASM 공통 이슈).

**대응:**

1. `OnAfterRenderAsync(firstRender)` — **JS/DOM** (프리렌더 패스에서 JS 실패 가능 → 가드)
2. `prerender: false` — 단순, SEO·첫 HTML은 손해 가능
3. `PersistentComponentState` — 데이터 중복 로드 완화 (권장 패턴)

```csharp
@inject PersistentComponentState ApplicationState

protected override async Task OnInitializedAsync()
{
    if (!ApplicationState.TryTakeFromJson<List<Server>>("servers", out var saved) || saved is null)
    {
        servers = await ServerService.GetServersAsync();
        ApplicationState.PersistAsJson("servers", servers);
    }
    else
    {
        servers = saved; // 이미 Persist된 값 재사용
    }
}
```

상세 타임라인 → `BLAZOR_RENDERING_GUIDE.md`

### @ref

부모 → 자식 **public 메서드**. React `useRef` / Angular `ViewChild`에 가깝음.  
DOM 고급 조작은 `ElementReference` + **JS interop**.

```razor
<SearchBar @ref="searchBar" />
<button @onclick="() => searchBar?.ClearFilter()">초기화</button>

@code {
    private SearchBar? searchBar;
}
```

```csharp
// SearchBar.razor
public void ClearFilter() => searchText = "";
```

### @rendermode

```razor
@rendermode InteractiveServer
@rendermode InteractiveWebAssembly
@rendermode InteractiveAuto
@rendermode @(new InteractiveServerRenderMode(prerender: false))
```

전역은 라우트 골격(`Routes`/`App`)에서 기본 모드 지정하는 패턴이 흔함.

### StateHasChanged / InvokeAsync

Blazor가 자동 감지하지 못할 때(백그라운드 작업 등) UI 갱신.

```csharp
await InvokeAsync(StateHasChanged);
// 또는
await InvokeAsync(() => { message = "..."; });
```

⚠️ **무한 루프 주의:** 조건 없는 `OnAfterRender` + `StateHasChanged`.  
`OnInitialized`에서 한 번 호출한다고 바로 무한 루프가 나는 것은 아님.

### ShouldRender

Render 단계 **직전** 필터. 불필요한 재렌더를 줄이려 할 때만 override(병목일 때). `false`여도 이벤트 핸들러는 실행될 수 있음 — “입력 무시”용이 아님.

### CSS isolation

```
ServerComponent.razor
ServerComponent.razor.css   ← 이 컴포넌트에만 스코프된 CSS
```

빌드가 선택자에 스코프 속성을 붙여 다른 컴포넌트로 새는 것을 줄임. 전역은 `wwwroot` CSS.

### MarkupString (XSS 주의)

```razor
@((MarkupString)html)
```

인코딩을 **우회**함. React `dangerouslySetInnerHTML`과 같음. **sanitize 아님** — 신뢰 가능한 HTML만.

### InputFile

```razor
<InputFile OnChange="OnFileChange" />
@code {
    async Task OnFileChange(InputFileChangeEventArgs e)
    {
        var file = e.File;
        await using var stream = file.OpenReadStream(maxAllowedSize: 1024 * 1024);
        // 업로드 처리
    }
}
```

Interactive에서 파일 선택·스트림 읽기.

---

## Component Patterns

### RenderFragment

자식 마크업 / named 슬롯. React children·Angular ng-content에 대응.

```razor
@* 기본 슬롯 *@
<Card>
    <h2>제목</h2>
    <p>내용</p>
</Card>

@* Card.razor *@
[Parameter] public RenderFragment? ChildContent { get; set; }
<div class="card">@ChildContent</div>
```

```razor
@* 제네릭 템플릿 *@
<ListView Items="servers">
    <ItemTemplate Context="server">
        <div>@server.Name — @server.City</div>
    </ItemTemplate>
</ListView>
```

```razor
@* ListView.razor *@
@typeparam TItem
@foreach (var item in Items)
{
    @ItemTemplate!(item)
}
@code {
    [Parameter] public List<TItem> Items { get; set; } = new();
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }
}
```

```razor
@* named 슬롯 *@
<Panel>
    <Header>제목</Header>
    <Body>본문</Body>
</Panel>

@* Panel.razor *@
[Parameter] public RenderFragment? Header { get; set; }
[Parameter] public RenderFragment? Body { get; set; }
<div>@Header</div>
<div>@Body</div>
```

### State management

```csharp
// AppState.cs
public class AppState
{
    public List<Server> Servers { get; private set; } = new();
    public event Action? OnChange;

    public void SetServers(List<Server> servers)
    {
        Servers = servers;
        OnChange?.Invoke();
    }
}

// Program.cs — Server: Scoped ≈ circuit
builder.Services.AddScoped<AppState>();
```

```razor
@inject AppState AppState
@implements IDisposable

@code {
    protected override void OnInitialized()
        => AppState.OnChange += StateHasChanged;

    public void Dispose()
        => AppState.OnChange -= StateHasChanged;
}
```

- 트리: `[Parameter]` / `[CascadingParameter]`
- Singleton에 **사용자별 상태** 넣지 말 것 (Server)

### SynchronizationContext (Interactive Server)

UI 필드는 회로 동기화 컨텍스트에서.

```csharp
await InvokeAsync(() =>
{
    message = "Updated from background";
});
```

`SynchronizationContext.Post`보다 **`InvokeAsync`를 우선** 기억.

---

## Performance & Lists

### Virtualize

Interactive에서만. 보이 영역에 가까운 항목만 렌더.

```razor
<Virtualize Items="@servers" Context="server">
    <div @key="server.Id">@server.Name</div>
</Virtualize>
```

### @key

리스트 정체성. 순서 변경·삭제·Virtualize 시 입력 상태 꼬임 방지.

```razor
@* ❌ key 없음 — 삭제/재정렬 시 입력값이 다른 행으로 섞일 수 있음 *@
@foreach (var item in items)
{
    <input @bind="item.Name" @bind:event="oninput" />
}

@* ✅ *@
@foreach (var item in items)
{
    <div @key="item.Id">
        <input @bind="item.Name" @bind:event="oninput" />
    </div>
}
```

---

## Event Handling

```razor
<button @onclick="HandleClick">클릭</button>
<form @onsubmit="HandleSubmit" @onsubmit:preventDefault>
<button @onclick="ChildClick" @onclick:stopPropagation>
```

최신 C#에서는 `foreach` 람다 캡처가 예전처럼 “마지막 값만” 문제가 되지 않는 경우가 많음. 구버전/혼동 시에만 로컬 복사 패턴을 고려.

---

## Navigation

### NavigationManager

`NavigateTo`, URI·base URI. 프로그래밍 네비게이션.

```csharp
NavigationManager.NavigateTo($"/servers/{id}");
```

### NavigationLock

Interactive. 미저장 이탈 방지.

```razor
<NavigationLock OnBeforeInternalNavigation="OnBeforeInternalNavigation"
                ConfirmExternalNavigation="true" />
```

---

## QuickGrid (이 레포 데모)

경로: `ServerManagement/Components/Pages/QuickGridDemo.razor` (`/quickgrid`)

정렬·템플릿 열·페이지네이션이 있는 테이블 UI. **Interactive** 권장.

```razor
@using Microsoft.AspNetCore.Components.QuickGrid
@rendermode InteractiveServer

<QuickGrid Items="servers.AsQueryable()" Pagination="paginationState">
    <PropertyColumn Property="s => s.Name" />
    <PropertyColumn Property="s => s.City" Sortable="true" />
    <TemplateColumn Title="Status">
        @(context.IsOnline ? "Online" : "Offline")
    </TemplateColumn>
</QuickGrid>
<Paginator State="paginationState" />

@code {
    private List<Server>? servers;
    private PaginationState paginationState = new() { ItemsPerPage = 5 };
}
```

패키지: `Microsoft.AspNetCore.Components.QuickGrid`

---

## Browser storage (WASM 데모)

경로: `WebAssemblyDemo.Client/Pages/BrowserStorageStep1.razor`, `Step2`, `ContainerStorage.cs`

페이지 간 값을 넘기는 두 가지 감각:

**1) 이 데모의 `ContainerStorage` (DI 메모리 — 새로고침 시 사라짐)**

```csharp
// Program.cs (Client)
builder.Services.AddScoped<ContainerStorage>();

// Step1
ContainerStorage.SetMessage(data);
NavigationManager.NavigateTo("/browser_wasm_2");

// Step2 — AfterRender에서 읽은 뒤 StateHasChanged
data = ContainerStorage.GetMessage();
```

**2) 실제 브라우저 저장소 (주석 처리된 패턴 / 실무)**

```csharp
// JS interop
await JS.InvokeVoidAsync("sessionStorage.setItem", "demo-data", data);
data = await JS.InvokeAsync<string>("sessionStorage.getItem", "demo-data");

// 또는 Blazor Protected* (암호화·권장 방향)
// ProtectedSessionStorage / ProtectedLocalStorage
await sessionStorage.SetAsync("demo-data", data);
var result = await sessionStorage.GetAsync<string>("demo-data");
```

- `localStorage` / `sessionStorage`: 새로고침 후에도 유지(정책에 따라)
- DI `ContainerStorage`: **같은 WASM 앱 세션·Scoped** 안에서의 간단 전달 학습용
- 민감 정보는 Protected storage 또는 서버 세션 검토

---

## HTTP / 데이터 접근

- `@inject HttpClient` 또는 typed `AddHttpClient<T>()`
- JWT 등: **`DelegatingHandler`** (클라이언트 가로채기)
- API 서버 **middleware**와는 다른 층
- **Server:** DB는 `Repository`/`Service` + DI (이 레포 `IServerService`, EF Repository). 컴포넌트에서 `DbContext` 직접 사용은 비권장
- **WASM:** API만 (`ServersApiRepository` 패턴)

```csharp
// Server 쪽 서비스 주입 예
@inject IServerService ServerService
servers = ServerService.GetServers();
```

---

## DI Lifetime

| 수명 | Static SSR (요청) | Interactive Server | Interactive WASM |
| --- | --- | --- | --- |
| **Transient** | resolve마다 | resolve마다 | resolve마다 |
| **Scoped** | ≈ HTTP 요청 | ≈ **SignalR circuit** | ≈ **앱(WebAssembly host) 수명** |
| **Singleton** | 프로세스 전역 | 프로세스 전역 (사용자 간 공유 주의) | 앱 인스턴스 전역 |

**Server에서 Scoped가 중요한 이유:** 사용자(회로)별 상태 분리. Singleton에 유저 상태 넣으면 섞임.

---

## Parameter 출처 정리

| 출처 | 속성 |
| --- | --- |
| 부모 / `@page` 경로 | `[Parameter]` |
| 쿼리 `?key=val` | `[SupplyParameterFromQuery]` |
| 정적 SSR 폼 POST | `[SupplyParameterFromForm]` |

---

## 참고

- https://learn.microsoft.com/aspnet/core/blazor
- https://learn.microsoft.com/aspnet/core/blazor/components/lifecycle
- https://learn.microsoft.com/aspnet/core/blazor/components/virtualization
- https://learn.microsoft.com/aspnet/core/blazor/components/state-management
- https://learn.microsoft.com/aspnet/core/blazor/fundamentals/routing
- https://learn.microsoft.com/aspnet/core/blazor/components/quickgrid
- https://learn.microsoft.com/aspnet/core/blazor/js-interop/call-dotnet-from-javascript#browser-storage
