# REFERENCE_MODS_AUDIT.md
# Auditoria arquitetural dos mods de referência — bússola, não fonte de código.

**REGRA ABSOLUTA: NUNCA copiar código destes mods. Usar apenas como guia arquitetural.**

---

## dig-Minimap

**Caminho:** `...\profiles\Default\BepInEx\plugins\dig-Minimap\Minimap.dll.old`
**Cache decompilado:** `C:\Users\Hiarly\.claude\PROJETOS\REPO\_refaudit\Minimap-src\Minimap.decompiled.cs`

### Como inicia
- Plugin herda `BaseUnityPlugin`
- No `Awake`: `gameObject.transform.parent = null` + `hideFlags = 61` (equivalente a DontDestroyOnLoad implícito)
- Não usa coroutine para lifecycle principal — usa `Update()` e `OnGUI()` diretamente no plugin

### Como mantém minimap vivo
- `DontDestroyOnLoad` via `hideFlags = 61` — o plugin não é destruído em transições de cena
- Gate simples: `SemiFunc.RunIsLevel() && GameDirector.instance != null && (int)GameDirector.instance.currentState == 2`
- Detecta TAB: `mapToolController.Active` — quando TAB aberto, não desenha e restaura zoom

### Como usa a câmera nativa
- **NUNCA cria RenderTexture própria**
- **NUNCA chama `camera.Render()`**
- Lê `camera.activeTexture` — a textura que o jogo já renderiza a cada frame
- Mantém `Map.Instance.ActiveSet(true)` e deixa o mapa ativo
- Quando TAB abre: restaura câmera/ActiveParent se estiverem desabilitados

### Como evita quebrar TAB
- Não desabilita a câmera nativa
- Não desabilita `map.ActiveParent`
- Só pausa captura quando TAB está aberto, mas restaura estado imediatamente

### Lições para SurveyorMap
- `DontDestroyOnLoad` é obrigatório → já aplicado (linha 59)
- `camera.activeTexture` é o caminho correto → já aplicado (`NativeMapCaptureProvider`)
- Gate mínimo → já simplificado com `IsGameplayState()`

---

## clay-BetterMap

**Caminho:** `...\profiles\Default\BepInEx\plugins\clay-BetterMap\BetterMap.dll.old`
**Cache decompilado:** `C:\Users\Hiarly\.claude\PROJETOS\REPO\_refaudit\BetterMap-src\BetterMap.decompiled.cs`

### MapCustom / MapCustomEntity
- Injeta componente `MapCustom` nos `EnemyParent`/`PlayerAvatar` via Harmony patches
- Patches usados: postfix em `SpawnRPC` (adiciona) e `DespawnRPC` (remove)
- Quando remove: destrói o entity além do componente (`MapCustom.mapCustomEntity`)

### RevealRooms
- Postfix em `LevelGenerator.GenerateDone`
- Chama `RoomVolume.SetExplored()` em cada sala
- Deixa o mapa nativo renderizar — não cria overlay próprio

### Enemy Markers
- Acessa `Enemy.Rigidbody` e `Enemy.HasRigidbody` via reflexão
- Cor do jogador: reflexão em `PlayerAvatarVisuals.color`
- Sprites em código puro (sem assets externos): quadrado, círculo, triângulo

### Relevância para fases futuras
| Feature | Abordagem a usar |
|---|---|
| ShowEnemies | `SpawnRPC`/`DespawnRPC` patches + `MapCustom` approach |
| RevealRooms | `LevelGenerator.GenerateDone` postfix + `RoomVolume.SetExplored()` |

**Não implementar nada daqui até o baseline PASS estar confirmado.**
