# 🗺️ SurveyorMap

[![Thunderstore](https://img.shields.io/thunderstore/dt/HiarlyScripter/SurveyorMap?style=flat-square&logo=thunderstore&label=Thunderstore)](https://thunderstore.io/c/repo/p/HiarlyScripter/SurveyorMap/)
[![R.E.P.O.](https://img.shields.io/badge/R.E.P.O.-Build%2023250495-blue?style=flat-square)](https://store.steampowered.com/app/3241660/REPO/)
[![BepInEx](https://img.shields.io/badge/BepInEx-5.4.2100-yellow?style=flat-square)](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)
[![Licença](https://img.shields.io/badge/licença-crédito%20obrigatório-red?style=flat-square)](LICENSE)

> Minimapa persistente e client-side no HUD inferior esquerdo — espelha o mapa nativo do R.E.P.O. sem overlays falsos.

---

## ✨ O que faz

O SurveyorMap lê a câmera de mapa real do jogo e exibe um minimapa persistente no canto inferior esquerdo da tela durante o gameplay. Mesma geometria, mesmas cores e mesma fidelidade que o TAB — sem render textures próprias, sem overlays falsos. Ajuste posição, tamanho e zoom sem sair do jogo, com cursor liberado e câmera congelada durante a edição.

---

## 🎯 Funcionalidades

- 🗺️ **Minimapa persistente** — HUD inferior esquerdo, visível apenas durante o gameplay; some nos menus e no lobby
- 📷 **Fidelidade nativa** — lê a câmera real do mapa (`activeTexture`); mesma geometria, cores e paredes que o TAB
- **`M`** — pressione para mostrar ou ocultar o minimapa sem afetar o TAB nativo
- 🛡️ **TAB seguro** — o minimapa some automaticamente ao abrir o TAB; o mapa nativo não é afetado
- 🎛️ **Modo de edição F8** — cursor liberado e câmera congelada automaticamente; arraste, redimensione e ajuste sem girar o personagem
- 🔍 **Scroll** (no F8) — ajusta o tamanho do minimapa
- 🔎 **`+` / `-`** — ajustam o zoom dentro ou fora do F8 *(configurável)*
- 👁️ **Enemy markers opcionais** — forma e cor indicam o nível de ameaça; inimigos mortos removidos em ~2s
- 🔇 **Silencioso por padrão** — `DebugLogging=false`; emite apenas uma linha no log em uso normal
- 🟢 **Client-side** — nenhum outro jogador precisa instalar; sem RPCs; sem alteração de rede

---

## 🎮 Controles

| Tecla | Contexto | Ação |
|---|---|---|
| `M` | Qualquer | Mostrar / ocultar o minimapa |
| `F8` | Qualquer | Entrar / sair do modo de edição |
| Arrastar | F8 ativo | Mover o minimapa |
| Scroll | F8 ativo | Ajustar tamanho |
| `+` / `-` | F8 ativo | Ajustar zoom |
| `R` | F8 ativo | Resetar para valores padrão |
| `+` / `-` | Gameplay normal | Ajustar zoom *(com `EnableZoomHotkeysOutsideEdit=true`)* |

---

## 👁️ Marcadores de inimigos

Forma e cor indicam o nível de ameaça de forma redundante — para máxima legibilidade no minimapa pequeno.

| Ameaça | Forma | Cor | Hex |
|---|---|---|---|
| Baixa (Easy) | Círculo | Verde-gelo | `#DFFFE8` |
| Média (Medium) | Quadrado | Azul escuro | `#1E6BFF` |
| Alta (High) | Triângulo | Lilás | `#C084FC` |
| Crítica (Elite) | Estrela | Vermelho/coral | `#FF3B30` |

---

## ⚙️ Configurações

| Seção | Chave | Padrão | Descrição |
|---|---|---|---|
| Minimap | `EnableMinimap` | `true` | Liga/desliga o HUD do minimapa |
| Minimap | `ToggleKey` | `M` | Tecla para alternar o minimapa |
| Minimap | `Width` / `Height` | `260` | Tamanho inicial em pixels |
| Minimap | `PosX` / `PosY` | `24` / `120` | Posição inicial (offset da borda esquerda/inferior) |
| Minimap | `Zoom` | `2.25` | Zoom ortográfico da câmera de mapa |
| Minimap | `Opacity` | `0.85` | Opacidade (0 = invisível, 1 = opaco) |
| Features | `ShowEnemies` | `true` | Mostrar marcadores de inimigos |
| Features | `EnemyMarkerSize` | `0.95` | Escala dos marcadores (0.30–2.00) |
| Features | `RevealRoomsMode` | `Vanilla` | `Vanilla` = padrão · `NativeGlobal` = revela salas via SetExplored (também afeta TAB) |
| EditMode | `EditModeKey` | `F8` | Tecla do modo de edição |
| EditMode | `UnlockCursorInEditMode` | `true` | Libera o cursor ao entrar no F8 |
| EditMode | `FreezeCameraInEditMode` | `true` | Congela a câmera ao entrar no F8 |
| EditMode | `EnableZoomHotkeysOutsideEdit` | `true` | `+`/`-` ajustam o zoom fora do modo F8 |
| Debug | `DebugLogging` | `false` | Log verboso — use apenas para diagnóstico |

---

## 👥 Multiplayer

> **Apenas o jogador que quer o minimapa precisa instalar.**

| Cenário | Resultado |
|---|---|
| ✅ Só você tem o mod | O minimapa aparece apenas para você — outros não são afetados |
| ✅ Todos têm o mod | Cada jogador vê seu próprio minimapa normalmente |
| ❌ Ninguém tem o mod | Nenhum efeito |

O SurveyorMap não envia RPCs, não altera a rede e não requer instalação pelos demais jogadores.

---

## 📦 Instalação

**Via r2modman (recomendado):**
1. Instale o **BepInExPack**
2. Procure e instale o **SurveyorMap** no Thunderstore
3. Clique em **Start modded**

**Via manual:**
1. Instale o BepInExPack
2. Copie a DLL para: `BepInEx/plugins/HiarlyScripter-SurveyorMap/SurveyorMap.dll`

---

## 📋 Notas

- `RevealRoomsMode = NativeGlobal` chama `RoomVolume.SetExplored()` — também revela salas no TAB nativo. É um opt-in documentado; o padrão é `Vanilla`.
- `DebugLogging = false` por padrão — o mod emite apenas uma linha no log em uso normal. Ative somente para diagnóstico.

---

## 📄 Licença

[Licença customizada](LICENSE) — uso e estudo permitidos. **Crédito ao autor obrigatório** em qualquer redistribuição ou trabalho derivado.

---

*Mod criado por **[HiarlyScripter](https://discord.com/users/hiarly_ferreira)** · BepInEx `5.4.2100` · R.E.P.O. Build `23250495`*
