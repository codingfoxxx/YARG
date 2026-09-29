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

#### Verificação
- Compilação: todo o código do jogo compilado com o Roslyn do Unity (`yarg-autochart/tools/unity-compile-check`), 0 erros. Ainda **sem** teste no jogo rodando (depende do Unity 6000.3.5f2 instalado e do Smart App Control; ver `PROGRESS.md` no repositório de ferramentas).
