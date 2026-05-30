# REFERENCE_MODS_CONTEXT.md
# Contexto dos mods de referência — bússola arquitetural, não fonte de código.

---

## Regra absoluta

**NUNCA copiar código dos mods referência.**
Usar apenas para entender arquitetura, lifecycle, APIs e comportamento.
Não mexer nos mods referência.
Não mexer no perfil Default onde eles residem.

---

## dig-Minimap — referência de lifecycle/render/minimap nativo

**Caminho:**
```
C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\Default\BepInEx\plugins\dig-Minimap
```
(DLL com extensão `.old` — decompilar antes de auditar)

**Decompilado em (cache local):**
```
C:\Users\Hiarly\.claude\PROJETOS\REPO\_refaudit\Minimap-src\Minimap.decompiled.cs
```

**Lições aprendidas:**
- Plugin faz `gameObject.transform.parent = null` + `hideFlags = 61` no Awake → DontDestroyOnLoad implícito
- Usa `Update()` + `OnGUI()` no próprio plugin — sem coroutine frágil
- Captura: lê `camera.activeTexture` (textura que o jogo já renderiza) — NUNCA cria RT própria, NUNCA chama `camera.Render()`
- Mantém `Map.Instance.ActiveSet(true)` e deixa ativo
- Gate: `SemiFunc.RunIsLevel() && GameDirector.instance != null && (int)GameDirector.instance.currentState == 2`
- Detecta TAB via `mapToolController.Active` — quando TAB aberto, restaura zoom ao default e não desenha

**Por que o SurveyorMap falhou:**
- Não tinha `DontDestroyOnLoad` explícito → plugin destruído na primeira troca de cena
- Captura manual com `camera.Render()` + RT própria → causava erros de RenderTexture e conflito com câmera nativa
- Gate excessivamente restritivo com muitas condições AND → qualquer falha bloqueava HUD pra sempre

---

## clay-BetterMap — referência de reveal/markers/entities

**Caminho:**
```
C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\Default\BepInEx\plugins\clay-BetterMap
```
(DLL com extensão `.old`)

**Decompilado em (cache local):**
```
C:\Users\Hiarly\.claude\PROJETOS\REPO\_refaudit\BetterMap-src\BetterMap.decompiled.cs
```

**Lições aprendidas (para fase futura — NÃO implementar ainda):**
- Injeta componente `MapCustom` nos `EnemyParent`/`PlayerAvatar` via Harmony patches em `SpawnRPC`/`DespawnRPC`
- Reveal de salas via `RoomVolume.SetExplored()` no postfix de `LevelGenerator.GenerateDone`
- Deixa o mapa nativo renderizar os markers — não cria overlay próprio para enemies
- Usa reflexão para acessar `Enemy.Rigidbody` e `Enemy.HasRigidbody`
- Acessa cor do jogador via reflexão em `PlayerAvatarVisuals.color`
- Sprites criados em código (sem assets externos): quadrado, círculo, triângulo
- Estrutura: `MapCustom.mapCustomEntity` — quando remove, destrói o entity além do componente

**Relevância para fase futura:**
- Quando reativar ShowEnemies: usar `SpawnRPC`/`DespawnRPC` patches + `MapCustom` approach
- Quando reativar RevealRooms: usar `LevelGenerator.GenerateDone` postfix + `RoomVolume.SetExplored()`

---

## Script de inspeção DLL

```powershell
# Para (re)decompilar os mods referência:
$tmp = "C:\Users\Hiarly\.claude\PROJETOS\REPO\_refaudit"
New-Item -ItemType Directory -Path $tmp -Force
Copy-Item "...\dig-Minimap\Minimap.dll.old"     "$tmp\Minimap.dll"  -Force
Copy-Item "...\clay-BetterMap\BetterMap.dll.old" "$tmp\BetterMap.dll" -Force
& ilspycmd "$tmp\Minimap.dll"  -o "$tmp\Minimap-src"
& ilspycmd "$tmp\BetterMap.dll" -o "$tmp\BetterMap-src"
```
