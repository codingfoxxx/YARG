# Como compilar este fork (Windows)

> **Estado: rascunho.** Os passos abaixo vêm da documentação do upstream e da leitura do projeto. Serão confirmados (com tempos, tamanhos e problemas encontrados) no primeiro build real feito a partir deste fork; até lá, trate como provisório.

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
5. Gerar o executável:
   - pela interface: **File → Build Profiles → Windows → Build**; ou
   - pela linha de comando (com o Hub aberto para o editor enxergar a licença):
     ```
     "<pasta do editor>\Unity.exe" -batchmode -quit -projectPath "<pasta do YARG>" ^
       -buildTarget Win64 -buildWindows64Player "<saída>\YARG.exe" -logFile "<saída>\build.log"
     ```

## Testes do YARG.Core

```
cd YARG.Core
dotnet test YARG.Core.UnitTests/YARG.Core.UnitTests.csproj
```
Em Windows configurado em português, um teste de formatação (`EngineTimerTests.ToString_FormatsStartedTimerWithSixDecimalPlaces`) falha por causa da vírgula decimal; com `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` todos passam. Linha de base em 2026-09-29: 547 aprovados, 1 falha (essa), 2 ignorados.

## Onde o jogo guarda dados

`%USERPROFILE%\AppData\LocalLow\YARC\YARG\<release|nightly|dev>\` (configurações, perfis, mapeamentos, cache de músicas, logs). O argumento `-persistent-data-path <pasta>` troca esse local (útil para testes isolados).
