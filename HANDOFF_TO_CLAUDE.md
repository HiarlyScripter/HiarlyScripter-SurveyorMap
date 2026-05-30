# Handoff tecnico para Claude Code - SurveyorMap

Data do handoff: 2026-05-24  
Autor do handoff: Codex  
Escopo desta etapa: documentacao e checkpoint. Nenhuma logica do mod foi alterada nesta etapa.

## 1. Identificacao do projeto

- Nome do mod: `SurveyorMap`
- GUID: `com.hiarlyscripter.surveyormap`
- Versao atual: `1.0.0`
- Raiz do projeto: `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap`
- DLL gerado: `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\build\SurveyorMap.dll`
- DLL instalado no r2modman: `C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\Default\BepInEx\plugins\HiarlyScripter-SurveyorMap\SurveyorMap.dll`
- Perfil r2modman usado: `C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\Default`
- Jogo alvo: R.E.P.O. Steam appid `3241660`
- Build alvo observado no Steam appmanifest: `23250495`
- Pasta do jogo: `E:\SteamLibrary\steamapps\common\REPO`
- Managed assemblies: `E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed`
- BepInEx local observado: `5.4.23.5`
- Dependencia declarada no manifest: `BepInEx-BepInExPack-5.4.2100`
- Unity version observada em `globalgamemanagers`: `2022.3.67f2`
- Target framework: `netstandard2.1`
- Hash SHA256 do DLL em `build` e no r2modman: `77F84E2E849DC0D8E28EA893680A726A41E2B2F235F0C8EB3C04023641573B16`

## 2. Objetivo do mod

`SurveyorMap` deve ser um minimapa HUD client-side para R.E.P.O. com overlay proprio de salas e marcadores.

Comportamento esperado:

- O HUD deve aparecer somente durante gameplay real em level/run valido.
- Nao deve aparecer em startup, photosensitivity warning, menu, loading, lobby, shop/truck ou transicoes.
- O minimapa deve ficar no canto inferior esquerdo.
- Layout esperado: `BottomLeft`, `PosX=24`, `PosY=120`, `Width=260`, `Height=260`, `UseSquareMap=true`, `Scale=1`.
- Toggle esperado: tecla `M`.
- O mod usa a camera nativa `Dirt Finder Map Camera` para timing, escala, viewport, RenderTexture e conversao world-to-map.
- A RenderTexture propria atual e criada como `SurveyorMap.RenderTexture`, default `256x256`, `ARGB32`, depth `16`, `antiAliasing=1`, `filter=Bilinear`.
- O mapa nativo e usado como base/camera, mas o reveal visual foi migrado para overlay proprio do SurveyorMap.

Configs principais existentes:

- `General.EnableMinimap`
- `General.EnableSpectatorMode`
- `General.SafeMode`
- `Controls.ToggleKey`
- `Layout.PositionPreset`
- `Layout.Scale`
- `Layout.Opacity`
- `Layout.ShowBorder`
- `Layout.BorderOpacity`
- `Layout.BorderThickness`
- `Layout.BackgroundOpacity`
- `Layout.MapOpacity`
- `Layout.SoftFrame`
- `Layout.UseSquareMap`
- `Layout.PosX`
- `Layout.PosY`
- `Layout.Width`
- `Layout.Height`
- `Map.Zoom`
- `Map.RotateWithPlayer`
- `Map.CenterOnPlayer`
- `Map.SmoothShapes`
- `Reveal.RevealRooms`
- `Reveal.RevealMode`: `Off`, `ExploredOnly`, `FullReveal`, `OnLevelStart`
- `Enemies.ShowEnemies`
- `Enemies.EnemyDetectionMode`: `Off`, `NearbyOnly`, `All`
- `Enemies.EnemyDetectionRadius`
- `Enemies.EnemyUpdateInterval`
- `Enemies.EnemyMarkerStyle`
- `Enemies.MaxEnemyMarkers`
- `Markers.ShowSelfMarker`
- `Markers.ShowTeammates`
- `Markers.EnableNames`
- `Markers.ClampMarkersToBounds`
- `Markers.MarkerScale`
- `Visual.ShowNativeQuestionMarks`
- `Visibility.HideInLobby`
- `Visibility.HideInShop`
- `Visibility.HideWhenInventoryOpen`
- `Diagnostics.LogLevel`
- `Diagnostics.DebugEnemyTelemetry`
- `Diagnostics.EnableDebugOverlay`
- `Performance.UpdateRate`
- `Performance.CaptureFPS`
- `Performance.CaptureOnlyWhenVisible`
- `Performance.PauseCaptureWhenToggleOff`
- `Performance.RenderTextureSize`
- `Performance.EnableProfiling`
- `Performance.ProfilingLogInterval`
- `Performance.RenderWarningMs`
- `Performance.AutoThrottleCapture`

Features implementadas de verdade:

- HUD `Canvas` em `ScreenSpaceOverlay`.
- Layout BottomLeft 260x260.
- Gate de gameplay por `RunState`.
- Captura controlada da camera nativa em RenderTexture.
- Ocultacao do visual nativo fora do HUD.
- Overlay proprio de salas via `SurveyorRoomOverlayGraphic`.
- Cache de sala por level via `SurveyorRoomMapCache`, coletando `RoomVolume`, bounds/corners, `MapModule` count e `MapObject` count.
- Reveal proprio do overlay com `FullReveal`, `OnLevelStart` e `ExploredOnly`.
- Enemy scan por `EnemyDirector.enemiesSpawned` e `FindObjectsOfType<Enemy>()`.
- Enemy markers projetados por `Dirt Finder Map Camera` usando `Map.Instance.Scale` e `Map.Instance.OverLayerParent`.
- Telemetria de inimigos com contadores de rejeicao.
- Throttle de captura por `CaptureFPS`.

Features ainda nao implementadas ou nao finalizadas:

- Visual premium final.
- Formas/cores calibradas para todos os tipos de inimigo.
- Teammates visualmente aprovados.
- Nomes/labels polidos.
- Rebuild visual detalhado de floor/walls/doors a partir de `MapObject` nativo.
- Suporte final a package/publicacao/Thunderstore.
- Teste visual conclusivo de todos os tipos de inimigo.

## 3. Estado atual real

### Funcionando confirmado

- Build compila com `0 erros / 0 warnings`.
- DLL do build foi copiado para o perfil r2modman e hash bate com o build.
- Log confirma carregamento do plugin `SurveyorMap 1.0.0`.
- Log confirma layout `BottomLeft`, anchor/pivot `(0,0)`, pos `(24,120)`, size `(260,260)`.
- Log confirma HUD oculto em menu: `HUD visible=False`, `RunState=Menu`, `allowed=False`.
- Log confirma entrada em level: `RunState=Level`, `allowed=True`, `reason=generated-level-ready`.
- Log confirma `NativeMapCapture` com `render=render-ok`, camera `Dirt Finder Map Camera`, texture `256x256`.
- Log confirma `RoomOverlay` renderizando: exemplo `rooms=81`, `projected=57`, `mode=FullReveal`.
- Log confirma enemy scan real encontrando inimigos: `totalEnemiesFound`, `directorParents`, `sceneEnemies`, `activeEnemies`, `aliveEnemies`.
- Log atual mostra que, apos a ultima correcao, `validEnemies` e `renderedEnemyMarkers` chegaram a valores acima de zero: exemplos `validEnemies=1..3`, `renderedEnemyMarkers=1..3`.
- Log confirma que `noRenderer` deixou de bloquear: `noRenderer=0`; tambem houve um caso `noRendererButAccepted=1`.

### Parcial/inconclusivo

- Enemy ESP compila e o log prova markers renderizados pelo codigo, mas ainda precisa de confirmacao visual humana em video/screenshot depois do ultimo DLL instalado.
- `NearbyOnly` funciona por log, mas muitos inimigos ficam fora do raio: `outOfRange` alto. Pode ser comportamento esperado.
- Room overlay foi confirmado por log e pelo usuario como funcionando, mas a fidelidade exata de paredes/portas ainda e aproximada.
- Native texture as vezes foi descrita visualmente pelo usuario como preta/crua em fases anteriores. O log atual diz `baseMapTextureVisible=True`, mas a utilidade visual da textura nativa nao deve ser assumida como final.
- Reducao de spam foi melhorada, mas ainda ha repeticao de `EnemyScan` e `RoomOverlay` no log em runtime.

### Quebrado/bugado/conhecido

- O problema mais recente antes desta documentacao era Enemy ESP nao aparecer porque `NearbyOnly` rejeitava muitos inimigos por `outOfRange`. Depois da correcao de `noRenderer`, logs passaram a mostrar markers renderizados quando algum inimigo entra no raio.
- Ainda ha spam/ruido de log:
  - `EnemyScan` aparece repetidamente.
  - `RoomOverlay` aparece repetidamente quando `mapObjects` muda.
  - Warnings iniciais de APIs do jogo nulas aparecem no startup: `RunIsLobby unavailable right now`, etc.
- Log mostra conflito/atividade de `Map Expand` alterando camera:
  - `Modified map camera size from 2,25 to 5`
  - `Modified map camera size from 5 to 2,52`
  - `Modified map camera size from 2,52 to 5`
- Log mostra erro Unity nao necessariamente causado por SurveyorMap:
  - `MissingFieldException: Field not found: UnityEngine.Transform .PlayerAvatar.localCameraTransform`
- Log final mostra:
  - `Releasing render texture that is set as Camera.targetTexture!`
  Este risco deve ser investigado; pode indicar release enquanto a camera ainda referencia a RenderTexture.
- Mensagem de log ainda enganosa no codigo: `Marker system bypassed for native HUD stabilization phase.` Ela esta obsoleta, porque enemy/room markers existem.
- Nao foi encontrado no log atual o texto exato `NativeMapCapture final success=True`, mas houve spam historico de `NativeMapCapture` com metricas por captura antes da reducao de logs.

## 4. Historico tecnico das tentativas

1. Estado inicial: minimap aparecia no menu e no jogo como quadrado vazio/centralizado; RenderTexture existia mas parecia preta/vazia; layout quebrava para `100x100` central; havia spam de log.
2. Gate inicial foi endurecido usando RunState, Player API, LevelGenerator e modules/layers. Menu/loading/startup melhoraram.
3. Layout foi corrigido para BottomLeft, `260x260`, `PosX=24`, `PosY=120`, anchor/pivot `(0,0)`.
4. Native map visual foi escondido para impedir camera/quad nativo aparecendo no centro da tela; RawImage passou a ser a unica area visual do minimap HUD.
5. RenderTexture foi controlada com `camera.Render()` e throttle/cache.
6. Performance foi tratada com `CaptureFPS`, `RenderTextureSize`, `CaptureOnlyWhenVisible`, `PauseCaptureWhenToggleOff`, cache de question markers, profiling opcional e logs rate-limited.
7. Foi auditado `dig-Minimap`; ele usa a camera `Dirt Finder Map Camera`, `camera.activeTexture` e `GUI.DrawTexture`, sem resolver reveal proprio.
8. Foi auditado `clay-BetterMap`; ele usa `RoomVolume.SetExplored()` no `LevelGenerator.GenerateDone` e `MapCustom` para players/enemies.
9. Foi descoberto que `RoomVolume.SetExplored()` nao redesenha planta visual; ele marca `Explored=true` e chama `MapModule.Hide()`. Hipotese descartada: usar `SetExplored()` como motor visual principal.
10. Foi criada arquitetura hibrida:
    - camera/viewport/scale/timing nativos;
    - renderer de salas proprio no HUD.
11. Enemy markers inicialmente usavam matematica manual `world - playerCenter`, causando posicoes inconsistentes.
12. Foi decompilado `Map.CustomPositionSet` e `DirtFinderMapPlayer`: a transformacao nativa e `world * Map.Scale + Map.OverLayerParent.position`, depois camera nativa para viewport.
13. Enemy projection foi migrada para `TryProjectWorldToHud`.
14. Enemy scan real foi criado com `EnemyDirector.enemiesSpawned` + `FindObjectsOfType<Enemy>()`.
15. Um filtro `noRenderer` bloqueou todos os inimigos validos. Hipotese descartada: renderer obrigatorio.
16. Filtro foi alterado: renderer e opcional. Posicao tenta `CenterTransform`, transform chamado `Center`, `EnemyParent.transform`, `Enemy.transform`, renderer bounds e fallback.
17. Log posterior mostrou `validEnemies > 0` e `renderedEnemyMarkers > 0`, mas confirmacao visual final ainda e recomendada.

## 5. Arquitetura atual do codigo

Arquivo principal:

- `src\Core.cs`

Classes principais:

- `SurveyorMapPlugin`
  - BepInEx plugin.
  - GUID, nome e versao.
  - Cria Harmony, configura logs, configura settings, cria controller.
- `SurveyorMapConfig`
  - Todas as configs `Config.Bind`.
  - `MigrateLayoutDefaults()` atualiza defaults para layout e features recentes.
- `SurveyorMapController`
  - Controla HUD, gate, update loop, capture, overlay, inimigos e visibilidade.
  - `CreateHud()` cria Canvas, panel, RawImage da textura, `RoomOverlay`, `MarkerRoot`, borda e debug text.
  - `ApplyLayout()` define tamanho/posicao/anchor/pivot.
  - `RuntimeUpdateCore()` controla tick do HUD.
  - `RefreshVisuals()` aplica gate, capture, room overlay, enemy cache e markers.
  - `IsHudGateOpenForCapture()` bloqueia menu/loading/toggle/inventory.
  - `UpdateNativeMapTexture()` chama captura nativa e registra estado de base map.
  - `UpdateRoomMapOverlay()` atualiza cache/projecao de salas.
  - `RebuildRoomOverlay()` projeta corners das salas para HUD.
  - `UpdateEnemyCache()` chama scan de inimigos.
  - `UpdateEnemyMarkers()` cria/posiciona markers no `MarkerRoot`.
- `NativeMapCaptureProvider`
  - Resolve camera `Dirt Finder Map Camera`.
  - Cria `SurveyorMap.RenderTexture`.
  - Chama `camera.Render()` controlado.
  - Oculta visual nativo com `HideNativeVisuals()`.
  - Projeta world para HUD em `TryProjectWorldToHud()`.
- `SurveyorRoomMapCache`
  - Coleta `RoomVolume`, `BoxCollider`, `MeshCollider`, renderers fallback.
  - Mantem `SurveyorRoomData` por level.
  - Calcula exploration e vizinhos simples.
- `SurveyorRoomOverlayGraphic`
  - `UnityEngine.UI.Graphic` custom.
  - Desenha quads e linhas de salas no overlay.
- `EnemyScanStats`
  - Guarda telemetria do enemy scan.
  - Gera resumo com aceitos/rejeicoes.
- `MarkerView`, `MarkerPool`, `MarkerShapeSprites`
  - UI markers para self/enemy/possiveis teammates.
- `GameApi`
  - Isola chamadas/reflect APIs do R.E.P.O.
  - RunState, player, enemy, RoomVolume, LevelGenerator, EnemyDirector, etc.
- `SurveyorMapPatches`
  - Existe como classe Harmony vazia/reservada.
  - Nao ha patches Harmony ativos de gameplay neste estado.

Onde cada coisa acontece:

- HUD criado: `SurveyorMapController.CreateHud()`
- HUD renderizado: Canvas `ScreenSpaceOverlay`, `HudTextureView` RawImage, `SurveyorRoomOverlayGraphic`, marker UI images.
- Tamanho/posicao: `SurveyorMapController.ApplyLayout()`
- Estado menu/run: `GameApi.GetRunState()` e `SurveyorMapController.IsHudGateOpenForCapture()`
- Captura do mapa/camera: `NativeMapCaptureProvider.Capture()`
- RenderTexture: `NativeMapCaptureProvider.ResolveRenderTexture()`
- Render controlado: `NativeMapCaptureProvider.RenderCameraOnce()`
- Logs: `LogHelper`, `LogRunStateIfChanged`, `UpdateNativeMapTexture`, `TryLogEnemyScanSummary`, `RebuildRoomOverlay`
- Configs: `SurveyorMapConfig`
- APIs R.E.P.O. usadas:
  - `SemiFunc`
  - `GameDirector`
  - `LevelGenerator`
  - `Map`
  - `MapLayer`
  - `MapModule`
  - `MapObject`
  - `RoomVolume`
  - `EnemyDirector`
  - `EnemyParent`
  - `Enemy`
  - `EnemyHealth`
  - `PlayerAvatar`
  - `PlayerController`

## 6. Dependencias e referencias

Referencias do `.csproj`:

- `Assembly-CSharp`
  - `$(GameManagedDir)\Assembly-CSharp.dll`
  - Resolvido localmente como `E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed\Assembly-CSharp.dll`
- `BepInEx`
  - `$(BepInExCoreDir)\BepInEx.dll`
  - Fallback local: `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\_refs\BepInEx.dll`
- `0Harmony`
  - `$(BepInExCoreDir)\0Harmony.dll`
  - Fallback local: `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\_refs\0Harmony.dll`
- `UnityEngine`
  - `$(GameManagedDir)\UnityEngine.dll`
- `UnityEngine.CoreModule`
  - `$(GameManagedDir)\UnityEngine.CoreModule.dll`
- `UnityEngine.UI`
  - `$(GameManagedDir)\UnityEngine.UI.dll`
- `UnityEngine.InputLegacyModule`
  - `$(GameManagedDir)\UnityEngine.InputLegacyModule.dll`
- `UnityEngine.UIModule`
  - `$(GameManagedDir)\UnityEngine.UIModule.dll`
- `UnityEngine.IMGUIModule`
  - `$(GameManagedDir)\UnityEngine.IMGUIModule.dll`
- `UnityEngine.PhysicsModule`
  - `$(GameManagedDir)\UnityEngine.PhysicsModule.dll`
  - Necessario para `BoxCollider`/`MeshCollider`.
- `UnityEngine.TextRenderingModule`
  - `$(GameManagedDir)\UnityEngine.TextRenderingModule.dll`
- `PhotonUnityNetworking`
  - `$(GameManagedDir)\PhotonUnityNetworking.dll`
- `PhotonRealtime`
  - `$(GameManagedDir)\PhotonRealtime.dll`

Fragilidade:

- `REPO_MANAGED_DIR` precisa apontar para `E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed`.
- `_refs` contem BepInEx/Harmony locais; se ausentes, definir `REPO_BEPINEX_CORE_DIR`.
- APIs internas do R.E.P.O. sao acessadas por reflection em varios pontos; updates do jogo podem quebrar campos/metodos.
- Map Expand tambem altera a camera do mapa e pode interferir com `orthographicSize`/target texture.

## 7. Build

Comando usado:

```powershell
$env:REPO_MANAGED_DIR='E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed'
dotnet build "C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\src\SurveyorMap.csproj" -c Release
```

Diretorio recomendado para rodar:

```text
C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap
```

Resultado do ultimo build observado:

```text
SurveyorMap -> C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\build\SurveyorMap.dll
Compilacao com exito.
0 Aviso(s)
0 Erro(s)
```

DLL final:

```text
C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\build\SurveyorMap.dll
```

## 8. Instalacao local

Pasta exata do plugin no r2modman:

```text
C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\Default\BepInEx\plugins\HiarlyScripter-SurveyorMap
```

Arquivo copiado para la:

```text
C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\Default\BepInEx\plugins\HiarlyScripter-SurveyorMap\SurveyorMap.dll
```

Hash instalado:

```text
77F84E2E849DC0D8E28EA893680A726A41E2B2F235F0C8EB3C04023641573B16
```

Mods de referencia presentes no perfil:

- `C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\Default\BepInEx\plugins\clay-BetterMap`
- `C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\Default\BepInEx\plugins\dig-Minimap`

Observacao: nesses diretorios, os DLLs estavam como `.dll.old` em auditorias anteriores:

- `BetterMap.dll.old`
- `Minimap.dll.old`

Map Expand estava ativo no log:

- `C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\Default\BepInEx\plugins\Ranily-Map_Expand`

Nenhum mod foi desabilitado, renomeado ou restaurado nesta etapa de handoff.

Como restaurar alteracao desta etapa:

- A etapa de handoff criou apenas `HANDOFF_TO_CLAUDE.md` e a pasta `releases\handoff-to-claude-current-state`.
- Nao houve alteracao de logica do mod.

## 9. Logs importantes

Log completo:

```text
C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\Default\BepInEx\LogOutput.log
```

Carregamento e layout:

```text
[Info   :   BepInEx] Loading [SurveyorMap 1.0.0]
[Info   :SurveyorMap] [SurveyorMap] SurveyorMap v1.0.0 loading.
[Info   :SurveyorMap] [SurveyorMap] Capabilities: teammates=enabled, enemies=enabled, spectator=enabled, ui=enabled
[Info   :SurveyorMap] [SurveyorMap] Layout applied: PositionPreset=BottomLeft, anchorMin=(0.00, 0.00), anchorMax=(0.00, 0.00), pivot=(0.00, 0.00), anchoredPosition=(24.00, 120.00), size=(260.00, 260.00)
[Info   :SurveyorMap] [SurveyorMap] HUD canvas created. renderMode=ScreenSpaceOverlay, sortingOrder=1100, panelSize=(260.00, 260.00), panelAnchor=(0.00, 0.00), active=False
```

Menu/gate:

```text
[Info   :SurveyorMap] [SurveyorMap] Run state: RunState=Menu, inferredLevel=False, RunIsLobby=False, RunIsShop=False, MenuLevel=False, RunIsLevel=False, spectating=False, PlayerAvatarLocal=False, levelObjects=False, levelGenDone=False, levelGenerated=False, modulesSpawned=0, playerApiReady=False, allowed=False, reason=not-run-level
[Info   :SurveyorMap] [SurveyorMap] HUD visible=False, reason=Menu, toggle=True, playerReady=False, enableMinimap=True, allowed=False, nativeTextureReady=False, canvasActive=False
```

Level/captura/overlay:

```text
[Info   :SurveyorMap] [SurveyorMap] Run state: RunState=Level, inferredLevel=True, RunIsLobby=False, RunIsShop=False, MenuLevel=False, RunIsLevel=True, spectating=False, PlayerAvatarLocal=True, levelObjects=True, levelGenDone=True, levelGenerated=True, modulesSpawned=22, playerApiReady=True, allowed=True, reason=generated-level-ready
[Info   :SurveyorMap] [SurveyorMap] NativeMapCapture: ready=True, visiblePixels=True, mapPopulated=True, render=render-ok, camera=Dirt Finder Map Camera, texture=256x256 format=ARGB32 depth=16 aa=1 hdr=False mipmaps=False filter=Bilinear, source=surveyormap.rendertexture, mapActive=True, activeParent=False, nativeVisualHidden=True, questionMarkersHidden=True, questionMarkers=59, modules=22, layers=8, reason=native-map-rendered
[Info   :SurveyorMap] [SurveyorMap] RoomOverlay: levelId=2762878, rooms=80, explored=80, projected=56, mapObjects=1896, mapModules=22, mode=FullReveal
[Info   :SurveyorMap] [SurveyorMap] HUD visible=True, reason=Level, toggle=True, playerReady=True, enableMinimap=True, allowed=True, nativeTextureReady=True, canvasActive=True
```

Enemy scan antes de inimigos entrarem no raio:

```text
[Info   :SurveyorMap] [SurveyorMap] EnemyScan: mode=NearbyOnly, radius=25,0, totalEnemiesFound=16, directorParents=8, sceneEnemies=8, activeEnemies=16, aliveEnemies=16, validEnemies=0, renderedEnemyMarkers=0, noRendererButAccepted=0, rejected{null=0, inactive=0, invalidScene=0, despawn=0, parentNotSpawned=0, parentMismatch=0, dead=0, noRenderer=0, noTransform=0, outOfRange=16, duplicate=0, projection=0}, baseMapReady=True, baseMapTextureVisible=True, roomOverlayRooms=56
```

Enemy scan depois de inimigos validos no raio:

```text
[Info   :SurveyorMap] [SurveyorMap] EnemyScan: mode=NearbyOnly, radius=25,0, totalEnemiesFound=15, directorParents=8, sceneEnemies=7, activeEnemies=14, aliveEnemies=14, validEnemies=2, renderedEnemyMarkers=2, noRendererButAccepted=1, rejected{null=0, inactive=1, invalidScene=0, despawn=0, parentNotSpawned=0, parentMismatch=0, dead=0, noRenderer=0, noTransform=0, outOfRange=10, duplicate=3, projection=0}, baseMapReady=True, baseMapTextureVisible=True, roomOverlayRooms=57
[Info   :SurveyorMap] [SurveyorMap] EnemyScan: mode=NearbyOnly, radius=25,0, totalEnemiesFound=15, directorParents=8, sceneEnemies=7, activeEnemies=14, aliveEnemies=14, validEnemies=3, renderedEnemyMarkers=3, noRendererButAccepted=0, rejected{null=0, inactive=1, invalidScene=0, despawn=0, parentNotSpawned=0, parentMismatch=0, dead=0, noRenderer=0, noTransform=0, outOfRange=8, duplicate=6, projection=0}, baseMapReady=True, baseMapTextureVisible=True, roomOverlayRooms=57
```

Map Expand:

```text
[Info   :   BepInEx] Loading [Map Expand 1.0.0]
[Info   :Map Expand] [Map Expand] Configuration loaded. Camera size set to: 2,52
[Info   :Map Expand] [Map Expand] Modified map camera size from 2,25 to 5
[Info   :Map Expand] [Map Expand] Modified map camera size from 5 to 2,52
[Info   :Map Expand] [Map Expand] Modified map camera size from 2,52 to 5
```

Erros/warnings relevantes:

```text
[Warning:SurveyorMap] [SurveyorMap] RunIsLobby unavailable right now: Object reference not set to an instance of an object
[Warning:SurveyorMap] [SurveyorMap] RunIsShop unavailable right now: Object reference not set to an instance of an object
[Warning:SurveyorMap] [SurveyorMap] MenuLevel unavailable right now: Object reference not set to an instance of an object
[Warning:SurveyorMap] [SurveyorMap] RunIsLevel unavailable right now: Object reference not set to an instance of an object
[Warning:SurveyorMap] [SurveyorMap] LevelGenDone unavailable right now: Object reference not set to an instance of an object
[Error  : Unity Log] MissingFieldException: Field not found: UnityEngine.Transform .PlayerAvatar.localCameraTransform Due to: Could not find field in class
[Error  : Unity Log] Releasing render texture that is set as Camera.targetTexture!
```

## 10. Problema principal a resolver agora

O proximo problema tecnico recomendado e reduzir ambiguidade/spam e validar visualmente Enemy ESP depois da ultima correcao.

Prioridade real atual:

1. Confirmar visualmente que `renderedEnemyMarkers > 0` corresponde a markers visiveis no minimapa.
2. Reduzir spam de `EnemyScan` e `RoomOverlay` para no maximo uma linha a cada 5s, mesmo quando valores oscilam.
3. Investigar `Releasing render texture that is set as Camera.targetTexture!`.
4. Validar se Map Expand altera `orthographicSize` depois da captura e se o SurveyorMap precisa revalidar/projetar apos essa mudanca.
5. Se os markers aparecem mas parecem deslocados, atacar somente projection/world-to-map.

Prioridades antigas de gate/layout ja tem evidencia positiva:

- Menu/gate: log mostra HUD oculto no menu.
- HUD level: log mostra HUD visivel no level.
- Layout: log mostra BottomLeft 260x260.
- Captura: log mostra render-ok e textura 256x256.

Nao voltar para `RoomVolume.SetExplored()` como reveal visual principal.

## 11. Riscos tecnicos

- Dependencia da camera nativa `Dirt Finder Map Camera`.
- Interferencia do Map Expand alterando camera size em momentos diferentes.
- RenderTexture pode ser liberada enquanto ainda esta em `Camera.targetTexture`.
- APIs internas do jogo podem mudar (`EnemyParent.Enemy`, `EnemyDirector.enemiesSpawned`, `RoomVolume`, `Map`).
- `FindObjectsOfType` ainda existe no enemy scan e no cache de salas, mas nao por frame; mesmo assim pode custar em mapas grandes.
- `RoomOverlay` usa bounds/colliders; nao representa portas/paredes com fidelidade final.
- Logs ainda podem crescer rapido.
- Gate depende de combinacao de `SemiFunc`, `LevelGenerator`, player API e module count; updates do jogo podem mudar timing.
- Config runtime atual tem `SafeMode=false`, apesar do default ser `true`. Isso deve ser observado no teste.
- Mensagem obsoleta `Marker system bypassed for native HUD stabilization phase` pode confundir diagnostico.

## 12. Proximo plano recomendado

1. Preservar o checkpoint `releases\handoff-to-claude-current-state`.
2. Rodar o jogo com o DLL atual e coletar novo video + `LogOutput.log`.
3. Confirmar visualmente os enemy markers quando log mostrar `renderedEnemyMarkers > 0`.
4. Se nao aparecer visualmente, ativar `Diagnostics.DebugEnemyTelemetry=true` e testar uma run curta.
5. Corrigir spam:
   - `EnemyScan` somente a cada 5s ou em mudanca significativa.
   - `RoomOverlay` somente quando level/cache mudar, nao por pequenas oscilacoes de `mapObjects`.
6. Investigar `Releasing render texture that is set as Camera.targetTexture!` em `ReleaseOwnedRenderTexture()` e `RenderCameraOnce()`.
7. Revalidar camera apos Map Expand alterar `orthographicSize`.
8. Build.
9. Instalar DLL no r2modman.
10. Testar menu, loading, level, enemy markers e log.

## 13. Lista de arquivos que o Claude provavelmente precisara alterar

- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\src\Core.cs`
  - Quase toda a logica esta aqui.
  - Corrigir spam, RenderTexture release, projection, telemetry e futuras melhorias.
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\src\SurveyorMap.csproj`
  - Somente se novas referencias Unity forem necessarias.
- `C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\Default\BepInEx\config\com.hiarlyscripter.surveyormap.cfg`
  - Apenas para teste local de configs. Nao tratar como fonte de codigo.

## 14. Lista de arquivos que o Claude NAO deve alterar

- Outros mods em `C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\Default\BepInEx\plugins\*`
- `clay-BetterMap`
- `dig-Minimap`
- `Ranily-Map_Expand`
- README/package/icon/Thunderstore/GitHub, salvo se o usuario pedir explicitamente publicacao/documentacao.
- Qualquer projeto fora de `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap`
- `C:\Users\Hiarly\.claude\*` sem confirmacao explicita do usuario.
- Arquivos de jogo em `E:\SteamLibrary\steamapps\common\REPO\*`

## Checkpoint criado

Pasta:

```text
C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\releases\handoff-to-claude-current-state
```

Conteudo copiado:

- `src`
- `package`
- `build`
- `DEVLOG.md`
- `icon-surveyormap.html`
- `LICENSE`
- `NuGet.Config`
- `README.md`
- `thunderstore.toml`
- `runtime-config\com.hiarlyscripter.surveyormap.cfg`

Observacao: `src\obj` foi copiado junto porque a pasta `src` foi copiada recursivamente. Nao e fonte principal, mas preserva estado de build.

## Arquivos atuais principais do projeto

- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\.gitignore`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\DEVLOG.md`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\HiarlyScripter-SurveyorMap-v1.0.0.zip`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\icon-surveyormap.html`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\LICENSE`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\NuGet.Config`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\README.md`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\thunderstore.toml`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\build\SurveyorMap.dll`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\build\SurveyorMap.pdb`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\package\manifest.json`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\package\README.md`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\package\CHANGELOG.md`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\package\icon.png`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\package\plugins\HiarlyScripter-SurveyorMap\SurveyorMap.dll`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\src\Core.cs`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\src\SurveyorMap.csproj`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\_refs\BepInEx.dll`
- `C:\Users\Hiarly\.codex\PROJETOS\REPO\HiarlyScripter-SurveyorMap\_refs\0Harmony.dll`

## Confirmacao final desta etapa

- Nenhuma logica do mod foi alterada nesta etapa.
- Nenhuma feature foi implementada nesta etapa.
- Nenhum arquivo foi apagado.
- Nada foi publicado.
- Nada foi enviado ao GitHub.
- Nenhum outro mod/projeto foi modificado.
