# Agent State — SurveyorMap v2

**Atualizado:** 2026-06-02
**Por:** Claude (checkpoint pos-validacao gameplay)

---

## Estado Atual

| Item | Valor |
|---|---|
| Branch | `codex-exec` |
| Commit HEAD | `4d42c8a` |
| Build hash | `8bdba81b` |
| Static Audit | PASS 45/45 |
| Gameplay (manual) | PASS — confirmado pelo usuario (build 8bdba81b) |
| Tag | `checkpoint/validated-gameplay-before-log-cleanup-20260602` |
| DLL instalado | `REPO - Test\BepInEx\plugins\HiarlyScripter-SurveyorMap\SurveyorMap.dll` |
| ZIP local | `releases\HiarlyScripter-SurveyorMap-v1.0.0-local.zip` |
| Publicado | NAO |
| Push | NAO |

## Arquitetura Atual (v2)

| Arquivo | Responsabilidade |
|---|---|
| `src/Core.cs` | SurveyorMapPlugin — entrypoint, M toggle, OnGUI, sweep timer |
| `src/SurveyorMapConfig.cs` | ConfigEntry bindings |
| `src/NativeMapMirror.cs` | Camera "Dirt Finder Map Camera" + activeTexture + TAB detection — INTOCADO |
| `src/RevealRoomsService.cs` | Harmony LevelGenerator.GenerateDone -> RoomVolume.SetExplored() — INTOCADO |
| `src/EnemyMapMarkerService.cs` | SpawnRPC/DespawnRPC -> MapCustom markers; ThreatTier; sweep |
| `src/SurveyorMapDiagnostics.cs` | JSON runtime state para validators |

## Sistema Visual (commit 4d42c8a — APROVADO)

| Ameaca | Forma | Cor | Hex |
|---|---|---|---|
| Low | Circulo | Verde-gelo | `#DFFFE8` |
| Medium | Quadrado | Azul escuro | `#1E6BFF` |
| High | Triangulo | Lila | `#C084FC` |
| Elite | Estrela | Vermelho/coral | `#FF3B30` |

Forma e cor derivam exclusivamente de ThreatTier (redundantes).
Keywords elevam tier apenas; nao escolhem cor por familia.

## Validacao Manual (2026-06-02, build 8bdba81b)

Confirmado PASS:
- Minimap HUD bottom-left com fidelidade nativa
- TAB abre normalmente, minimap some durante TAB e volta ao soltar
- M toggle funcional
- F8 edit mode funcional (drag, resize, zoom, +/-)
- R reset no edit mode funcional
- Inimigos aparecem com sistema visual correto
- EnemyMarkerSize = 0.95 via REPOConfig (aplica em ~2s)
- Cleanup/despawn em ~2s
- Sem amarelo/dourado nos marcadores

## Proximo Passo

**Log cleanup** — adicionar `DebugLogging = false` config e tornar logs repetitivos condicionais.
Ver `docs/NEXT_ACTIONS.md` para especificacao completa.

Nao alterar: NativeMapMirror, RevealRoomsService, Core (minimap/TAB/M/F8/R), sistema visual.
Nao publicar. Nao push. Nao mexer no Default profile.
