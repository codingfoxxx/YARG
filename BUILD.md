# Como compilar este fork (Windows)

> **Confirmado em 2026-09-29** com um build real deste fork: Unity 6000.3.5f2, Windows 11 (com Smart App Control ligado), sem Blender. Primeiro build pela linha de comando em ~14 min (importação + compilação + build). Pasta `Library` com 3,6 GB, executável com 533 MB, editor instalado com 7,6 GB.

## Requisitos

| Item | Versão | Para quê |
|---|---|---|
| Git + Git LFS | Git 2.4x+, LFS 3.x | o repositório guarda texturas, fontes e modelos no LFS |
| Unity Hub | 3.x | licença Personal (gratuita) e instalação do editor |
| **Unity Editor** | **6000.3.5f2** (changeset `3fa8bc678cb0`) | versão exigida pelo projeto (`ProjectSettings/ProjectVersion.txt`) |
| Blender | 4.5 LTS | o Unity usa o Blender para importar `Assets/Art/Meshes/Obsolete/Notes.blend`, ainda usado pelo tema de notas "Rectangular" e pelo preview de notas; sem ele essas notas ficam sem modelo |
| .NET SDK | 10.x | compilar/testar o YARG.Core e as ferramentas (`dotnet test`) |
| Espaço em disco | ~20 GB | editor (~7 GB) + pasta `Library` (~5-8 GB) + build |

O suporte a build para Windows (Mono) já vem com o editor para Windows; não é preciso marcar módulos extras nem o Visual Studio.

## Passos

1. Clonar com submódulos e LFS:
   ```
   git clone -b pessoal --recursive https://github.com/codingfoxxx/YARG.git
   cd YARG
   git lfs pull
   ```
2. No Unity Hub: entrar na conta, ativar a licença Personal (Preferences → Licenses) e instalar o **6000.3.5f2** (pelo arquivo de versões da Unity, `unityhub://6000.3.5f2/3fa8bc678cb0`), desmarcando Visual Studio e documentação.
3. Instalar o Blender e deixar o `.blend` associado a ele (o instalador faz isso; na versão portátil, `blender.exe --register`).
4. Abrir o projeto pelo Hub. Se aparecer o aviso de Safe Mode, clique **Ignore** (os scripts do editor precisam rodar para restaurar dependências). O NuGet restaura os pacotes sozinho na abertura; se faltar algo, menu **NuGet → Restore Packages**. O primeiro import é demorado.
   - **Build só pela linha de comando (modo batch):** o NuGetForUnity **não** chega a restaurar os pacotes. Enquanto eles faltam, a compilação falha, e o editor nunca roda o código dele. Restaure antes os pacotes do `Assets/packages.config` em `Assets/Packages/<Id>.<Versão>/lib/<framework>/`, nas versões exatas. O `scripts/build-jogo.ps1` do repositório de ferramentas faz isso com `dotnet restore`. **Não escreva os `.meta` das DLLs à mão:** um `.meta` de PluginImporter feito à mão fez o editor ignorar a DLL sem avisar. Deixe o editor gerar.
5. Gerar o executável:
   - pela interface: **File → Build Profiles → Windows → Build**; ou
   - pela linha de comando (com o Hub aberto para o editor enxergar a licença):
     ```
     "<pasta do editor>\Unity.exe" -batchmode -quit -projectPath "<pasta do YARG>" ^
       -buildTarget Win64 -buildWindows64Player "<saída>\YARG.exe" -logFile "<saída>\build.log"
     ```

## Windows 11 com Smart App Control

Testado em 2026-09-29 com o Unity **6000.6.3f1**: com o Smart App Control em modo de bloqueio, o editor **não compila scripts**. O Windows bloqueia DLLs .NET sem assinatura que vêm com o próprio Unity (`Data\Tools\BuildPipeline\Compilation\ApiUpdater\ApiUpdater.MovedFromExtractor.dll`, entre outras; eventos 3077 em *Microsoft-Windows-CodeIntegrity/Operational*, política `VerifiedAndReputableDesktop`), e o log termina em "Scripts have compiler errors" sem nenhum erro de C#.

Com o **6000.3.5f2**, a versão do projeto, **funciona**. As mesmas ferramentas sem assinatura carregam (têm reputação), o build sai e o `YARG.exe` gerado, também sem assinatura, abre normalmente. Durante o build o Windows ainda barrou alguns arquivos auxiliares (`IlInterpreterAnalyzer.dll` do pacote `com.unity.pipeline`, `libarchive-13.dll`, `libexpat-1.dll` e uma DLL temporária), sem impedir o resultado. A decisão é por arquivo, pela reputação na nuvem, então pode mudar com o tempo.

Sem desligar a proteção, dá para **verificar se o código compila** com o compilador C# do próprio Unity, que é assinado pela Microsoft: ver `tools/unity-compile-check` no repositório [yarg-autochart](https://github.com/codingfoxxx/yarg-autochart). Desligar o Smart App Control é irreversível (só volta reinstalando o Windows).

## Testes do YARG.Core

```
cd YARG.Core
dotnet test YARG.Core.UnitTests/YARG.Core.UnitTests.csproj
```
Em Windows configurado em português, um teste de formatação (`EngineTimerTests.ToString_FormatsStartedTimerWithSixDecimalPlaces`) falha por causa da vírgula decimal; com `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` todos passam. Linha de base em 2026-09-29: 547 aprovados, 1 falha (essa), 2 ignorados.

## Onde o jogo guarda dados

`%USERPROFILE%\AppData\LocalLow\YARC\YARG\<release|nightly|dev>\` (configurações, perfis, mapeamentos, cache de músicas, logs). O argumento `-persistent-data-path <pasta>` troca esse local (útil para testes isolados).
