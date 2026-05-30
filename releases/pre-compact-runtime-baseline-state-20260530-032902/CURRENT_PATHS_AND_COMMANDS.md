# CURRENT_PATHS_AND_COMMANDS.md
# Caminhos e comandos operacionais do projeto SurveyorMap

---

## Raiz Ativa do Projeto

```
C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-SurveyorMap\
```

## Caminho do csproj

```
C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-SurveyorMap\src\SurveyorMap.csproj
```

## Comando de Build (Release, 0 erros esperado)

```powershell
cd "C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-SurveyorMap\src"
$env:REPO_MANAGED_DIR = "E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed"
dotnet build -c Release
```

## Caminho da DLL Build

```
C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-SurveyorMap\build\SurveyorMap.dll
```

## Caminho da DLL Instalada no REPO - Test

```
C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test\BepInEx\plugins\HiarlyScripter-SurveyorMap\SurveyorMap.dll
```

## Caminho do LogOutput.log (REPO - Test)

```
C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test\BepInEx\LogOutput.log
```

## Caminho do Config do SurveyorMap (REPO - Test)

```
C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test\BepInEx\config\com.hiarlyscripter.surveyormap.cfg
```

## Comando PowerShell — Copiar DLL para REPO - Test (LiteralPath, path com espaço)

```powershell
$src = "C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-SurveyorMap\build\SurveyorMap.dll"
$dst = "C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test\BepInEx\plugins\HiarlyScripter-SurveyorMap\SurveyorMap.dll"
Copy-Item -LiteralPath $src -Destination $dst -Force
Write-Host "DLL instalada em: $dst"
```

## Comando para Calcular Hash (verificar build vs instalado)

```powershell
$src = "C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-SurveyorMap\build\SurveyorMap.dll"
$dst = "C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test\BepInEx\plugins\HiarlyScripter-SurveyorMap\SurveyorMap.dll"
$buildMd5 = (Get-FileHash -Algorithm MD5 $src).Hash
$testMd5  = (Get-FileHash -Algorithm MD5 $dst).Hash
Write-Host "Build : $buildMd5"
Write-Host "Test  : $testMd5"
Write-Host "Match : $($buildMd5 -eq $testMd5)"
```

## DLL Atual (última build estável)

- **Timestamp:** 2026-05-29 21:10:41
- **MD5:** B0A5A93C7DDF7DCDC5DF30D15CA7B803
- **Build:** 0 erros, 0 warnings

## Managed Dir do Jogo

```
E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed
```

## Não alterar o perfil Default

O perfil Default pode conter outros mods de teste. O alvo de instalação é exclusivamente REPO - Test.
