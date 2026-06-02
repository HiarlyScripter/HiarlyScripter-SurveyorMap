# Changelog — SurveyorMap

Este arquivo contém versão em Português e em Inglês. O Português vem primeiro; o English version is below.

This file includes Portuguese and English versions. Portuguese comes first; English version is below.

## Português

### v1.0.0 local — 2026-06-02 (patch 8 — suspensão de câmera no F8 edit mode)

**Compatibilidade:** R.E.P.O. + BepInEx `5.4.2100`

- **Nova config `FreezeCameraInEditMode = true` (seção `[EditMode]`).** Ao entrar no modo F8, a câmera do jogador é suspensa automaticamente usando a API nativa do jogo (`CameraAim.OverridePlayerAimDisable`). Mover o mouse edita o minimapa sem girar a câmera.
- O controle da câmera é restaurado ao sair do modo F8, junto com o cursor.
- Implementação via API pública do jogo — sem patches, sem Windows API, sem simulação de ESC.
- Se `CameraAim.Instance` for nulo (fora de gameplay), o mod emite um aviso e continua sem travar.
- Nenhuma alteração funcional: minimap, TAB, M, R reset, inimigos, cleanup, DebugLogging inalterados.

### v1.0.0 local — 2026-06-02 (patch 7 — cursor automático no F8 e revisão de documentação)

**Compatibilidade:** R.E.P.O. + BepInEx `5.4.2100`

- **Nova config `UnlockCursorInEditMode = true` (seção `[EditMode]`).** O cursor é liberado automaticamente ao pressionar F8 para entrar no modo de edição, sem precisar apertar ESC. O estado anterior do cursor é restaurado ao sair do modo de edição.
- A liberação do cursor é reaplicada a cada frame enquanto o modo de edição estiver ativo, garantindo que o jogo não reloque o mouse durante a edição.
- Ao alternar o modo de edição, `Input.ResetInputAxes()` é chamado para evitar movimento residual da câmera.
- Se o jogo ainda capturar o mouse após F8, pressione ESC uma vez para liberar manualmente.
- Revisão editorial da documentação: acentuação e pontuação em português corrigidas em README e CHANGELOG.
- Nenhuma alteração funcional: minimap, TAB, M, R reset, inimigos, cleanup, DebugLogging inalterados.

### v1.0.0 local — 2026-06-02 (patch 6 — log cleanup: DebugLogging=false)

**Compatibilidade:** R.E.P.O. + BepInEx `5.4.2100`

- **Nova config `DebugLogging = false` (seção `[Debug]`).** Por padrão, o log fica silencioso em uso normal. Ative `DebugLogging = true` apenas para diagnóstico de classificação de inimigos, sweep e room tracking.
- Logs movidos para debug condicional: `Sweep: active=`, `Enemy marker: id=`, `Map.AddCustom OK`, `Room explored (new)`, `AddMarker skipped`, `M toggle`, `Patched EnemyHealth.*`.
- Logs que continuam sempre visíveis: BuildTag md5=, plugin carregado, resumo de config, Edit mode ON/OFF, Edit mode reset, warnings e erros reais.
- Nenhuma alteração funcional: cores, formas, cleanup, scale, TAB/M/F8/R inalterados.

### v1.0.0 local — 2026-06-02 (patch 5 — alinhamento visual: cor = ameaça)

**Compatibilidade:** R.E.P.O. + BepInEx `5.4.2100`

- **Forma e cor agora representam o mesmo nível de ameaça (redundantes).** Círculo+verde-gelo = Easy; Quadrado+azul = Medium; Triângulo+lilás = High; Estrela+vermelho = Elite/Crítico.
- **Removido modelo `Cor = Família`:** cor por tipo/comportamento (Common/Small/Special/Brute) removida. A cor agora deriva exclusivamente do threat tier, igual à forma.
- Keywords de nome/tipo continuam servindo apenas para **elevar** o threat tier; nunca para escolher cor separada.
- Log de classificação atualizado: campo `family=` removido; mantidos `diff=`, `threat=`, `shape=`, `color=`, `elevated=`, `names=`.

### v1.0.0 local — 2026-06-01 (patch 4 — sistema visual de inimigos v2)

**Compatibilidade:** R.E.P.O. + BepInEx `5.4.2100`

- Novo modelo visual: Forma = Ameaça, Cor = Família (substituído no patch 5).
- Elevação de ameaça por palavra-chave: `veryheavy`, `trudge`, `slow walker`, `boss`, `elite`.
- Quadrado substitui losango: forma Medium agora é quadrado.
- `ShowEnemiesInUnexploredRooms` depreciado: filtro por sala removido da lógica ativa.
- `EnemyMarkerSize` padrão 0.95 (era 0.65). Aplica em ~2s via REPOConfig sem reiniciar.

### v1.0.0 local — 2026-06-01 (patch 3 — UX inimigos + reset editmode)

**Compatibilidade:** R.E.P.O. + BepInEx `5.4.2100`

- Cor separada da forma: cor = nível de perigo/dificuldade; forma = tipo/família do inimigo.
- Filtro pré-spawn em `AddMarker`: inimigos mortos, inativos ou com `CurrentState=Despawn` rejeitados.
- Tecla `R` no modo de edição: reseta para os valores padrão (PosX=24, PosY=120, W=260, H=260, Zoom=2.25, Opacity=0.85). Funciona apenas com F8 ativo.

### v1.0.0 local — 2026-06-01 (patch 2 — marcadores e edit mode)

**Compatibilidade:** R.E.P.O. + BepInEx `5.4.2100`

- Cleanup de marcadores por registry: sweep a cada 2s, ClearAll no carregamento do nível.
- Marcadores com formas (círculo/losango/triângulo/estrela) e cores por dificuldade.
- Modo de edição F8: mover, redimensionar pelo canto, zoom com scroll, `Shift+wheel`, `+/-`, salvar ao sair.
- `RevealRoomsMode = Vanilla` como padrão seguro. `NativeGlobal` é opt-in e também afeta o TAB.

### v1.0.0 local — 2026-06-01

**Compatibilidade:** R.E.P.O. + BepInEx `5.4.2100`

- Rebuild v2 com arquitetura limpa baseada no espelho do mapa nativo.
- Minimapa persistente via câmera nativa `activeTexture`.
- Comportamento TAB-safe: o minimapa some enquanto o TAB nativo está aberto.
- Toggle `M` para mostrar/esconder o minimapa.
- Marcadores de inimigos via `MapCustom`, com cleanup por despawn.
- `RevealRoomsMode`: `Vanilla` (padrão, sem SetExplored), `NativeGlobal` (afeta TAB), `MinimapOnly` bloqueado.

### v1.0.0 local — 2026-05-31

- Rebuild inicial da arquitetura v2.
- Remoção de RuntimeLoop, RuntimeProbeBehaviour, CenterOnPlayer, overlays falsos, RenderTexture próprio e proof HUD.
- Baseline de minimapa nativo, M toggle, TAB-safe, RevealRooms global e ShowEnemies inicial.

---

## English

### v1.0.0 local — 2026-06-02 (patch 8 — camera suspension during F8 edit mode)

**Compatibility:** R.E.P.O. + BepInEx `5.4.2100`

- **New config `FreezeCameraInEditMode = true` (section `[EditMode]`).** When entering F8 edit mode, the player camera look is suspended automatically using the game's own API (`CameraAim.OverridePlayerAimDisable`). Moving the mouse edits the minimap without spinning the camera.
- Camera control is restored when you exit F8 edit mode, together with cursor state.
- Implemented via the game's public API — no patches, no Windows API, no ESC simulation.
- If `CameraAim.Instance` is null (e.g. not in gameplay), the mod emits a warning and continues without crashing.
- No functional changes: minimap, TAB, M, R reset, enemy markers, cleanup, DebugLogging unchanged.

### v1.0.0 local — 2026-06-02 (patch 7 — automatic cursor in F8 edit mode and documentation review)

**Compatibility:** R.E.P.O. + BepInEx `5.4.2100`

- **New config `UnlockCursorInEditMode = true` (section `[EditMode]`).** The cursor is unlocked and made visible automatically when you press F8 to enter edit mode — no need to press ESC first. The previous cursor state is restored when you exit edit mode.
- Cursor unlock is reapplied every frame while edit mode is active, ensuring the game cannot re-lock the cursor during editing.
- `Input.ResetInputAxes()` is called when toggling edit mode to prevent residual camera movement.
- If the game still captures the mouse after pressing F8, press ESC once to release it manually.
- Editorial review of documentation: corrected accents and punctuation in the Portuguese README and CHANGELOG.
- No functional changes: minimap, TAB, M, R reset, enemy markers, cleanup, DebugLogging unchanged.

### v1.0.0 local — 2026-06-02 (patch 6 — log cleanup: DebugLogging=false)

**Compatibility:** R.E.P.O. + BepInEx `5.4.2100`

- **New config `DebugLogging = false` (section `[Debug]`).** Log is silent by default for end users. Set `DebugLogging = true` only for diagnostics (enemy classification, sweep, room tracking).
- Moved to conditional debug: `Sweep: active=`, `Enemy marker: id=`, `Map.AddCustom OK`, `Room explored (new)`, `AddMarker skipped`, `M toggle`, `Patched EnemyHealth.*`.
- Always visible: BuildTag md5=, plugin loaded, config summary, Edit mode ON/OFF, Edit mode reset, real warnings and errors.
- No functional changes: colours, shapes, cleanup, scale, TAB/M/F8/R unchanged.

### v1.0.0 local — 2026-06-02 (patch 5 — visual alignment: colour = threat)

**Compatibility:** R.E.P.O. + BepInEx `5.4.2100`

- **Shape and colour now represent the same threat level (redundant).** Circle+ice-green = Easy; Square+blue = Medium; Triangle+lilac = High; Star+red = Elite/Critical.
- **Removed `Colour = Family` model:** colour-by-behaviour-type (Common/Small/Special/Brute) removed. Colour is now derived exclusively from threat tier, same as shape.
- Name/type keywords still serve only to **elevate** threat tier, never to choose a separate colour.
- Classification log updated: `family=` field removed; `diff=`, `threat=`, `shape=`, `color=`, `elevated=`, `names=` retained.

### v1.0.0 local — 2026-06-01 (patch 4 — enemy visual system v2)

**Compatibility:** R.E.P.O. + BepInEx `5.4.2100`

- New visual model: Shape = Threat, Colour = Family (superseded by patch 5).
- Threat elevation by keyword: `veryheavy`, `trudge`, `slow walker`, `boss`, `elite`.
- Square replaces diamond: Medium threat shape is now a square.
- `ShowEnemiesInUnexploredRooms` deprecated: room-based filtering removed from active logic.
- `EnemyMarkerSize` default 0.95 (was 0.65). Applies within ~2s via REPOConfig without restart.

### v1.0.0 local — 2026-06-01 (patch 3 — enemy UX + edit mode reset)

**Compatibility:** R.E.P.O. + BepInEx `5.4.2100`

- Colour separated from shape: colour = danger/difficulty level; shape = enemy type/family.
- Pre-spawn filter in `AddMarker`: dead, inactive, or `CurrentState=Despawn` enemies rejected.
- `R` key in edit mode resets to default values. Only works when F8 edit mode is active.

### v1.0.0 local — 2026-06-01 (patch 2 — markers and edit mode)

**Compatibility:** R.E.P.O. + BepInEx `5.4.2100`

- Registry-based marker cleanup: 2s sweep, ClearAll on level load.
- Marker shapes (circle/diamond/triangle/star) and colours by difficulty.
- F8 edit mode: move, resize from corner, scroll zoom, `Shift+wheel`, `+/-`, save on exit.
- `RevealRoomsMode = Vanilla` as safe default. `NativeGlobal` is opt-in and also affects TAB.

### v1.0.0 local — 2026-06-01

**Compatibility:** R.E.P.O. + BepInEx `5.4.2100`

- v2 rebuild with a clean native-map mirror architecture.
- Persistent minimap through the native map camera `activeTexture`.
- TAB-safe behaviour: the minimap hides while the native TAB map is open.
- `M` toggle to show/hide the minimap.
- Enemy markers through `MapCustom`, with cleanup on despawn.
- `RevealRoomsMode`: `Vanilla` (default, no SetExplored), `NativeGlobal` (also affects TAB), `MinimapOnly` blocked.

### v1.0.0 local — 2026-05-31

- Initial v2 architecture rebuild.
- Removed RuntimeLoop, RuntimeProbeBehaviour, CenterOnPlayer, fake overlays, owned RenderTexture, and proof HUD.
- Baseline native minimap, M toggle, TAB-safe behaviour, global RevealRooms, and initial ShowEnemies.
