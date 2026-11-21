# Blazor 렌더링 가이드

> 프리렌더·라이프사이클·JS Interop **타이밍** 전용.  
> API 치트시트 → [`BLAZOR_LEARNING_GUIDE.md`](./BLAZOR_LEARNING_GUIDE.md)  
> Chart/CDN → [`NODE_JS_PACKAGES_GUIDE.md`](./NODE_JS_PACKAGES_GUIDE.md)

---

## 개요

훅 이름·Dispose·`OnParametersSet` 요약 표 → [`BLAZOR_LEARNING_GUIDE.md`](./BLAZOR_LEARNING_GUIDE.md) Lifecycle 절.

**Pre-rendering:** interactive 컴포넌트라도 **먼저 정적 HTML에 가까운 출력을 서버(또는 프리렌더 패스)에서 그리는 것**. 첫 페인트·SEO에 유리.

**부작용:** 프리렌더 ON이면 `OnInitialized` / `OnParametersSet` / `SetParametersAsync` 등이 **두 번** 실행될 수 있음 (Interactive Server·WASM 공통).

**대응 요약:**

1. JS/DOM → `OnAfterRenderAsync` + 가드 (`firstRender`, JS 가능 여부)
2. 단순화 → `prerender: false`
3. 데이터 중복 완화 → `PersistentComponentState` (권장)

```razor
@rendermode @(new InteractiveServerRenderMode(prerender: false))
```

---

## 핵심: JS는 DOM 완성 후

```
OnInitialized / OnParametersSet  → DOM 미준비, @ref 미할당
        ↓
Render
        ↓
OnAfterRenderAsync(firstRender) → DOM·@ref 준비 → IJSRuntime 호출
```

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (!firstRender) return;

    // 프리렌더 패스에서는 JS가 실패할 수 있음 → try/catch 또는 RendererInfo 등으로 가드
    try
    {
        var module = await JS.InvokeAsync<IJSObjectReference>(
            "import", "./Components/Controls/ServerChartComponent.razor.js");
        await module.InvokeAsync("createChart", canvasElement, data);
    }
    catch (JSException)
    {
        // 프리렌더/미지원 환경 — 무시 또는 로그만
    }
    catch (InvalidOperationException)
    {
        // JS interop 불가 시점
    }
}
```

`OnInitialized`에서 `await JS.InvokeAsync` → 실패하기 쉬움.

---

## 모드별 타임라인 (개념)

> 실제 기본 prerender 여부는 템플릿/.NET 버전에 따름. **“기본 = prerender 없음”이 아님.** 명시하거나 기본 ON을 가정하는 편이 안전.

### A. InteractiveServer · `prerender: false`

```
회로 연결 후 1회:
  OnInitialized → OnParametersSet → Render → OnAfterRenderAsync(firstRender: true)
  → JS 가능
```

중복 초기화 적음. SEO·첫 HTML은 약할 수 있음.

### B. InteractiveServer · prerender ON (흔한 기본)

```
1) 프리렌더 패스
   OnInitialized → OnParametersSet → Render → OnAfterRenderAsync(firstRender: true)
   → 브라우저 DOM 아님 → JS 불가/실패

2) Interactive 연결 후
   훅 다시 실행 → OnAfterRenderAsync(firstRender: true) 등
   → 브라우저 DOM → JS 가능
```

데이터 로드가 두 번이면 Persist로 완화.

### C. InteractiveWebAssembly · prerender ON

```
1) 서버 프리렌더 (훅 1회차, JS 불가)
2) WASM 로드 후 클라에서 훅 다시 (JS 가능)
```

“WASM이라 중복을 피할 수 없다”기보다 **프리렌더를 끄거나 Persist**로 조절.

### D. InteractiveAuto

초기 Server 쪽 interactive → 이후 WASM 가능. 프리렌더·DI를 **양쪽** 전제로 설계.

---

## 2회 실행·JS 문제 대응

### 비권장: 데이터+JS를 전부 `OnInitialized`에

프리렌더면 2회·JS 실패.

### JS 전용: `OnAfterRenderAsync`

- **역할:** DOM/차트/포커스 등  
- **한계:** `firstRender`만으로 “클라에서 한 번만”이 보장되진 않음(프리렌더에서도 firstRender일 수 있음).  
  → JS 호출은 **실패를 삼키거나**, `PersistentComponentState`로 데이터를 맞춘 뒤, 클라에서만 성공하는 호출에 의존.  
  → 더 엄밀히 하려면 `IJSRuntime` 가능 여부 / 렌더링 컨텍스트 확인(버전별 API).

### 단순: prerender 끄기

```razor
@rendermode @(new InteractiveServerRenderMode(prerender: false))
@rendermode @(new InteractiveWebAssemblyRenderMode(prerender: false))
```

장점: 라이프사이클 단순. 단점: SEO·첫 페인트.

### 권장(데이터): `PersistentComponentState`

```csharp
@inject PersistentComponentState ApplicationState

protected override async Task OnInitializedAsync()
{
    if (!ApplicationState.TryTakeFromJson<List<Server>>("servers", out var saved)
        || saved is null)
    {
        servers = await ServerService.GetServersAsync();
        ApplicationState.PersistAsJson("servers", servers);
    }
    else
    {
        servers = saved;
    }
}
```

- **Persist** 하는 패스: 보통 프리렌더(또는 첫 실행)에서 데이터 로드  
- **Take** 하는 패스: 이미 직렬화된 값 재사용 → DB/API 중복 감소  

메서드명은 SDK에 따라 `TryTakeFromJson` / `TryTakeAsJson` 등 확인.  
“실패 = 무조건 서버, 성공 = 무조건 클라”라고만 외우지 말고, **Take 실패 시 로드+Persist** 패턴으로 기억.

JS는 그 다음 `OnAfterRenderAsync`에서.

```csharp
// Persist로 데이터 맞춘 뒤 — JS는 AfterRender에서만
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (!firstRender) return;
    try
    {
        var module = await JS.InvokeAsync<IJSObjectReference>(
            "import", "./Components/Controls/ServerChartComponent.razor.js");
        await module.InvokeAsync("createChart", canvas, servers);
    }
    catch (JSException) { /* 프리렌더 등 */ }
}
```

---

## 방법 비교

| 방법 | JS | 데이터 중복 | SEO/첫 HTML | 비고 |
| --- | --- | --- | --- | --- |
| 데이터만 `OnInitialized` (prerender ON) | ❌ | 2회 가능 | ✅ | JS 넣지 말 것 |
| `OnAfterRenderAsync` + 가드 | ✅(클라) | 데이터는 별개 | ✅ | JS용 |
| `prerender: false` | ✅ | 1회에 가까움 | 약함 | 개발·단순 앱 |
| Persist + AfterRender | ✅ | 데이터 1회에 가깝게 | ✅ | 프로덕션에 유리 |

---

## 상황별

| 상황 | 권장 |
| --- | --- |
| 데이터만 | `OnInitializedAsync` (+ Persist if prerender) |
| JS 필요 | `OnAfterRenderAsync` + 가드 |
| 빠른 개발 | `prerender: false` |
| SEO + 중복 방지 | Persist + AfterRender for JS |

---

## StateHasChanged (렌더링 강제)

```csharp
await InvokeAsync(StateHasChanged);
```

백그라운드·타이머 등에서 UI 필드 갱신 시.  
`OnAfterRender` 안에서 조건 없이 호출하면 루프 위험.

---

## 참고

- [Component lifecycle](https://learn.microsoft.com/aspnet/core/blazor/components/lifecycle)
- [Prerendering](https://learn.microsoft.com/aspnet/core/blazor/components/prerender)
- [PersistentComponentState / state](https://learn.microsoft.com/aspnet/core/blazor/components/state-management)
- [JS interop](https://learn.microsoft.com/aspnet/core/blazor/js-interop/)
