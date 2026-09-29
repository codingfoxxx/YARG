# Sobre este fork / About this fork

**PT-BR.** Este é o fork pessoal de Lucas Raposo ([@codingfoxxx](https://github.com/codingfoxxx)) do [YARG](https://github.com/YARC-Official/YARG) (Yet Another Rhythm Game). O objetivo é um jogo de 5 trastes polido para jogar com **controle de Xbox** no PC, com uma ferramenta separada que gera charts a partir de qualquer áudio ([yarg-autochart](https://github.com/codingfoxxx/yarg-autochart)). **Não é oficial e não tem afiliação com a YARC.** Para o jogo oficial, use o [YARC Launcher](https://github.com/YARC-Official/YARC-Launcher).

**EN.** Unofficial personal fork of YARG focused on 5-fret guitar with an Xbox gamepad. Not affiliated with YARC. Changes are listed in [CHANGELOG-FORK.md](CHANGELOG-FORK.md).

## Base

| | |
|---|---|
| Upstream | `YARC-Official/YARG`, branch `dev` @ `275e9a13` (2026-09-26) |
| YARG.Core | `YARC-Official/YARG.Core` @ `e2d44e8d` (submódulo, sem alterações) |
| Unity | 6000.3.5f2 |
| Branch deste fork | `pessoal` (as branches `master` e `dev` espelham o upstream) |

## Princípios

- Mudanças mínimas e isoladas, para o fork continuar fácil de atualizar a partir do upstream (`git merge upstream/dev`).
- Todo ponto de edição em arquivo do upstream leva o comentário `// [pessoal]` (`git grep "\[pessoal\]"` lista todos).
- Mudança de comportamento de gameplay sempre fica atrás de uma configuração.
- Cada mudança está descrita, com o motivo, em [CHANGELOG-FORK.md](CHANGELOG-FORK.md).

## Licenças

- O código do YARG e do YARG.Core é **LGPL-3.0** ([LICENSE](LICENSE)); este fork mantém a mesma licença e todos os avisos originais.
- Componentes de terceiros mantêm as próprias licenças (tabela "External Licenses" do [README](README.md)). Destaques: a biblioteca de áudio **BASS** é gratuita só para uso **não comercial**, e dois efeitos sonoros são **CC BY-NC 4.0**. Por isso, este fork e os builds dele são distribuídos apenas para uso não comercial.
- Inventário completo: `LICENSES.md` do repositório [yarg-autochart](https://github.com/codingfoxxx/yarg-autochart).
- Código-fonte correspondente a cada build publicado: a tag `pessoal-vX.Y.Z` de mesmo nome do Release.

## Compilar

Ver [BUILD.md](BUILD.md).
