## 🗺️ SurveyorMap v1.0.0

Minimapa persistente e client-side para R.E.P.O. — espelha o mapa nativo do jogo no HUD inferior esquerdo, com marcadores opcionais de inimigos e modo de edição completo in-game.

---

### ✨ Destaques

- **Minimapa nativo** — lê a câmera real do mapa (`activeTexture`); mesma geometria, cores e paredes que o TAB
- **TAB seguro** — o minimapa some automaticamente ao abrir o TAB; o mapa nativo não é alterado por padrão
- **`M`** para mostrar/ocultar · **`F8`** para entrar no modo de edição
- **Modo de edição F8** — cursor liberado e câmera congelada automaticamente; arraste, scroll (tamanho), `+`/`-` (zoom), `R` (reset)
- **Zoom fora do F8** — `+`/`-` ajustam o zoom durante o gameplay normal *(ativado por padrão)*
- **Enemy markers** — forma e cor por nível de ameaça (círculo/quadrado/triângulo/estrela); inimigos mortos removidos em ~2s
- **Client-side** — nenhum outro jogador precisa instalar; sem RPCs; sem alteração de rede
- **Log silencioso** — `DebugLogging=false`; emite apenas uma linha em uso normal

---

### 🎮 Controles

| Tecla | Contexto | Ação |
|---|---|---|
| `M` | Qualquer | Mostrar / ocultar o minimapa |
| `F8` | Qualquer | Entrar / sair do modo de edição |
| Scroll | F8 ativo | Ajustar tamanho |
| `+` / `-` | F8 ativo ou gameplay | Ajustar zoom |
| `R` | F8 ativo | Resetar para padrão |

---

### 📦 Instalação

**Via r2modman:** procure `SurveyorMap` no Thunderstore e instale.

**Via manual:** copie `SurveyorMap.dll` para `BepInEx/plugins/HiarlyScripter-SurveyorMap/`.

**Dependência:** BepInExPack `5.4.2100`

---

### 📋 Observações

- `RevealRoomsMode = NativeGlobal` revela salas via `SetExplored()` e também afeta o TAB nativo. Opt-in — o padrão é `Vanilla`.
- `DebugLogging = false` por padrão. Ative somente para diagnóstico.

---

📖 [README completo e changelog no repositório](https://github.com/HiarlyScripter/HiarlyScripter-SurveyorMap)

---

*BepInEx `5.4.2100` · R.E.P.O. Build `23250495` · Mod por [HiarlyScripter](https://discord.com/users/hiarly_ferreira)*
