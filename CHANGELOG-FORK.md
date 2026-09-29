# CHANGELOG do fork

Tudo o que este fork muda em relação ao upstream (`YARC-Official/YARG`), com o motivo.
Mudanças de gameplay sempre ficam atrás de uma configuração; o padrão é o comportamento do upstream, salvo correção de bug comprovada.

## [Não lançado]

### Base
- Branch `pessoal` criada a partir do upstream `dev` @ `275e9a13` (2026-09-26), com YARG.Core @ `e2d44e8d`.
  Motivo: o `dev` contém correções de sincronia de áudio e de calibração posteriores ao release v0.15.0 (#1558, #1589, #1535, #1653).

### Documentação e licenças
- `FORK.md`, `CHANGELOG-FORK.md`, `BUILD.md` e um aviso de fork no topo do `README.md`.
- `NOTICE` (fork não oficial, não comercial, data e autor das modificações) e `COPYING` com o texto da GPL-3.0 (gnu.org), que a LGPL-3.0 exige acompanhar e que faltava no upstream.

### Código

#### Histerese nos gatilhos analógicos (controle de Xbox)
- **O que muda:** um botão analógico (ex.: gatilho LT/RT usado como traste) agora pode ter um **ponto de soltura** abaixo do ponto de acionamento. Novo parâmetro por controle, `ReleaseThreshold`: fração do ponto de acionamento em que o botão solta (1 = sem histerese, igual ao upstream). No preset padrão de controle para guitarra de 5 trastes, os gatilhos (verde = LT, azul = RT) usam **0,75**: apertam em 50% do curso, como antes, e só soltam abaixo de 37,5%.
- **Por quê:** no upstream, `pressionado = valor >= 0,5`, sem histerese. Com o gatilho parado perto do meio do curso (dedo cansado ou relaxado segurando um sustain), cada oscilação em torno de 0,5 vira uma soltura e um novo aperto, o que derruba sustains e gera toques fantasmas (que o anti-ghosting pune). 0,75 é o mesmo valor que o próprio Input System do Unity usa nos botões dele (`InputSettings.buttonReleaseThreshold` do projeto); o pipeline de bindings do YARG não passa por essa lógica.
- **Evidência** (testes em `yarg-autochart/tests/EngineTests/AnalogTriggerTests.cs`, com a engine real do YARG.Core e o sinal de um gatilho XInput simulado a 250 Hz / 8 bits): com o dedo pairando em 0,5 ± 0,06 durante um sustain, sem histerese o sustain caiu nas 20 execuções (504 solturas falsas); com histerese, em nenhuma. Apertando e soltando o gatilho por inteiro, o resultado é idêntico nos dois casos. Com `ReleaseThreshold = 1` a regra é exatamente a do upstream (testado ponto a ponto).
- **Configuração:** o valor fica salvo por controle em `profiles/bindings.json` (`"ReleaseThreshold": "0.75"`, sempre com ponto decimal, independente do idioma do Windows). Só bindings novos de gamepad recebem o preset; bindings já salvos não mudam. **Pendente:** controle deslizante na tela de edição de binds (precisa editar o prefab no Unity).
- Arquivos: `Assets/Script/Input/Bindings/AnalogButtonHysteresis.cs` (novo, sem dependência do Unity), `ButtonBinding.cs`, `ControlBinding.cs` (`ActuationSettings.ButtonReleaseThreshold`), `Defaults/BindingCollection.Gamepad.cs`.

#### Jogar sem teclado conectado
- **Correção de bug:** `GameManager.Update` e a busca da biblioteca de músicas usavam `Keyboard.current` sem checar nulo. Quando o Input System não tem nenhum teclado registrado (raro no Windows, mas possível em PC só com controle), `Keyboard.current` é nulo e a exceção a cada frame interrompia o resto do `Update` da partida, antes de o `SongRunner` avançar o relógio da música. Agora a checagem de Esc/Ctrl+Tab só acontece se houver teclado.
- Arquivos: `Assets/Script/Gameplay/GameManager.cs`, `Assets/Script/Menu/MusicLibrary/SongSearchingField.cs`.
- O mesmo para o mouse (`Mouse.current`), que é lido a cada frame da partida para esconder o cursor: `Gameplay/HUD/HideCursor.cs`, `Gameplay/HUD/Pause/PracticePause.cs`, `Gameplay/HUD/Practice/PracticeSectionMenu.cs`, `Menu/Main/MainMenuBackground.cs`.

#### Calibração de entrada escalada pela velocidade da música
- **Correção de bug:** a calibração de áudio e a de vídeo entram no relógio do jogo multiplicadas pela velocidade da música (`SongRunner`: `AudioCalibration * SongSpeed`), porque um atraso real de X ms vale X × velocidade no tempo da música. A calibração de entrada do perfil entrava sem esse fator (`BasePlayer`), então no modo prática fora de 100% quem tem calibração de entrada era julgado deslocado (a 50%, uma calibração de 100 ms compensava o dobro do devido). Agora é multiplicada pela velocidade nos dois lugares em que é aplicada. A 100%, nada muda. Replays guardam os tempos já ajustados, então não são afetados.
- Arquivo: `Assets/Script/Gameplay/Player/BasePlayer.cs`.

#### Calibração guiada (Configurações → Abrir calibrador)
- **Instruções claras** (pt-BR e inglês) antes de começar: o que fazer, qual botão usar no controle (a palhetada, direcional para baixo, ou o A), usar o mesmo fone/caixa de sempre e seguir o som, não a tela. Upstream: duas linhas fixas em inglês.
- **Duas passadas** da música de calibração (30 s, ~40 toques; upstream: 15 s, ~20 toques). Com erro humano de 15 ms, 95% dos resultados ficam a menos de 5,7 ms do atraso real, contra 8,4 ms com uma passada (400 jogadores simulados em `CalibrationMathTests`).
- **Descarte de toques por toque**: cada toque é medido contra a batida mais próxima e só os fora da curva (mais de max(50 ms, 3 desvios robustos) da mediana) saem. O filtro do upstream comparava cada toque com o anterior, então uma batida perdida também descartava o toque bom seguinte.
- **Resultado explicado**: atraso medido, consistência (±ms: boa/razoável/baixa), toques usados/descartados e os valores atuais. Contador de toques durante a medição.
- **O jogador escolhe onde salvar**:
  - *Salvar para todos* (A, recomendado): calibração de áudio global com o mesmo valor que o calibrador do upstream gravava. Também zera a calibração de entrada do perfil que calibrou; senão o total desse jogador seria a medida mais o ajuste antigo.
  - *Salvar no perfil* (Y): calibração de entrada do perfil que tocou (já existia no YARG, só não tinha ferramenta), como ajuste em cima da calibração de áudio atual, para quem tem controles diferentes em perfis diferentes. O texto avisa que, se a calibração de áudio mudar depois, os perfis precisam ser calibrados de novo.
  - *Repetir* e *Voltar* também disponíveis. Os botões só aparecem 1 s depois do fim da música, para um toque atrasado não escolher uma opção sem querer. Upstream: gravava a calibração global direto.
- **Resultado não confiável é recusado**: mais de 25% dos toques descartados, ou atraso acima de 300 ms. Perto de meia batida (375 ms), um toque adiantado e um atrasado ficam iguais para a medição e a mediana pode errar 750 ms; upstream aceitava.
- Toques no primeiro 0,3 s da segunda passada são ignorados (a recarga da música atrasa um pouco a primeira batida).
- Correção: voltar e recomeçar não inscreve mais o handler de input duas vezes (cada toque contaria em dobro).
- Textos longos usam fonte menor (a caixa de texto da cena é de uma linha, 64 pt, sem ajuste automático).
- Arquivos: `Assets/Script/Menu/Calibrator/Calibrator.cs`, `CalibrationMath.cs` (novo, sem dependência do Unity), `Assets/StreamingAssets/lang/en-US.json` e `pt-BR.json` (chaves novas em `Menu.Calibrator`; os outros idiomas caem no inglês).

#### Opção "Escanear Tudo ao Iniciar" (Configurações → Músicas)
- **Qualidade de vida, desligada por padrão** (padrão = comportamento do upstream). Ao abrir, o jogo só lê o cache de músicas (varredura rápida), então uma pasta nova (por exemplo, gerada pelo autochart) só aparece depois de *Escanear Músicas*. Ligada, a abertura faz a varredura completa e as músicas novas aparecem sozinhas; o custo é abrir mais devagar com bibliotecas grandes.
- Arquivos: `Assets/Script/Settings/SettingsManager.Settings.cs` (`FullScanOnStartup`), `SettingsManager.cs` (aba Músicas), `Assets/Script/Persistent/LoadingScreen.cs`, textos em `en-US.json` e `pt-BR.json`.

#### Verificação
- Compilação: todo o código do jogo compilado com o Roslyn do Unity (`yarg-autochart/tools/unity-compile-check`), 0 erros e os mesmos 26 avisos do upstream. Ainda **sem** teste no jogo rodando (depende do Unity 6000.3.5f2 instalado e do Smart App Control; ver `PROGRESS.md` no repositório de ferramentas).
