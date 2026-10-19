# Cold Ember

Projeto **Introdução ao Unity (Tutorial)**, da disciplina Jogos Digitais (Insper).
Partiu do tutorial "Introdução Rápida" e ganhou os extras da rubrica.

> Você é a última brasa de uma fogueira congelada: uma bola de vidro com fogo dentro. Atravesse a
> cripta de gelo, recupere os 5 fragmentos de chama (o fogo cresce a cada um) e volte para
> reacender a fogueira antes que o calor acabe.

- **Jogar no navegador (itch.io):** _adicionar o link da página aqui_
- **Engine:** Unity 6000.6.4f1 (Universal 2D / URP), Input System
- **Plataforma de entrega:** WebGL

## Como jogar

| Ação | Teclado | Controle Xbox |
| --- | --- | --- |
| Mover | WASD / setas | Analógico esquerdo / D-pad |
| Dash (intocável, gasta 1,5 s de calor) | Espaço / Shift | A / RT |
| Pausar | Esc / P | Start |
| Navegar nos menus | Setas / mouse | Analógico / D-pad + A |
| Voltar nos menus | Esc | B |

- **Calor = tempo.** O calor cai 1 s por segundo. Carvões devolvem 3 s (em sequência rápida sobem
  a escada de notas e dão bônus de combo) e fragmentos devolvem 5 s. No gelo do chão o calor cai 2,5x
  mais rápido. Com pouco calor a tela esfria, a vinheta fecha e o coração bate. Se o calor zerar, a
  brasa apaga.
- **Perigos:** espíritos de gelo patrulham em linha reta, caçadores glaciais perseguem quando te
  enxergam e cristais de gelo machucam ao toque. São 3 corações.
- **Vitória:** com os 5 fragmentos uma seta aponta a fogueira. A tela final mostra o tempo, a
  pontuação com bônus de calor e de vida, e uma nota de S a C.
- **Segredo:** o fragmento do canto noroeste fica atrás de cristais. O dash atravessa sem dano.

## O que foi feito (rubrica)

| Item | Implementação |
| --- | --- |
| Tutorial | `Assets/Scenes/Tutorial.unity`: Player (círculo vermelho, Rigidbody2D com gravidade 0, CircleCollider2D), paredes com BoxCollider2D, fundo da câmera alterado e `PlayerMovement` com `speed` público aplicado no `FixedUpdate`. O jogador do jogo final usa o mesmo script e os mesmos componentes. |
| Tempo | Cronômetro do tempo total na tela e no fim (com recorde). O calor é um cronômetro regressivo usado como mecânica: coletáveis somam tempo, o dash e o gelo consomem, e a tela esfria conforme o tempo acaba. |
| Inimigos | `PatrolEnemy` (vai e volta numa direção fixa), `HunterEnemy` (patrulha, persegue com linha de visão, procura e volta para casa pelo caminho percorrido) e cristais parados (`DamageOnTouch`). |
| Visual | Pixel art própria feita com [pxl](https://github.com/crsolver/claude-pxl), paleta de 24 cores, fonte bitmap 5x7, fundo em redemoinho por shader e tela de fliperama (florada, vinheta, curvatura, franja de cor). |
| Gamefeel | Pausa de impacto, tremor e empurrão de câmera, zoom, clarão, partículas em pixel, molas de amassar, rastro do dash, números que saltam, barra de calor com atraso, combo em escada pentatônica, nocaute em câmera lenta. Valores em `Fx.Tune`. |
| Áudio | Trilha do menu, trilha do jogo em 3 camadas que entram com a tensão (base, bateria, melodia) e 16 efeitos sintetizados. |
| UI | Vida, pontos, calor, tempo, fragmentos, faixas, pausa, opções (volumes, tremor, clarões, tela de fliperama), derrota e resultado com nota. |
| Controle | Input System com teclado, mouse e gamepad/Xbox no jogo e em todos os menus. As dicas na tela trocam conforme o dispositivo. |
| Level design | Arena de 46x30 (`Assets/Levels/arena.txt`) com 4 alas, corredores patrulhados, sala congelada, covis de caçadores e fragmento secreto. |
| Menu inicial | Logo animado (o "O" de COLD é a brasa quicando), Jogar, Como jogar (com os sprites do jogo), Opções e Sair (oculto no WebGL). |
| Sem dead-end ou softlock | Derrota por vida ou por calor, sempre com "Tentar de novo" e "Menu inicial". A pausa tem Reiniciar e Menu. Todos os itens são alcançáveis (BFS no mapa). Caçadores desistem e voltam. |

## Estrutura

```
Assets/
  Scenes/      MainMenu, Game (build) e Tutorial (resultado do tutorial)
  Scripts/     Core (GameManager, Fx, GameInput, AudioManager, CameraRig, Backdrop, Transition),
               Player, Enemies, World, UI (HUD, PixelMenu, OptionsPanel, MainMenu), Render (PixelText, PixelPanel)
  Shaders/     Swirl.shader: o fundo em redemoinho
  Editor/      GameBuilder.cs: monta as cenas (menu "Cold Ember")
  Levels/      arena.txt: o mapa (# parede, c carvão, F fragmento, ^ cristais, h/v espírito,
               H caçador, ~ gelo, P jogador, B fogueira)
  Art/Pixel/   sprites e animações exportados do pxl
Tools/
  art/cold_ember.px.js   toda a pixel art, em código pxl
  gen_audio.py           síntese da trilha e dos efeitos
  gen_font.py            atlas da fonte bitmap
```

- Arte: clone o pxl em `Tools/claude-pxl` e rode `sh Tools/art/sync.sh`.
- Mapa ou arte mudaram: **Cold Ember > Construir tudo** no Unity.
- Build: **Cold Ember > Gerar build WebGL** (sai em `Builds/WebGL`, Gzip com *decompression fallback*,
  pronto para o itch.io: compacte o conteúdo da pasta e envie como jogo HTML).

## Créditos dos assets

| Asset | Origem | Licença |
| --- | --- | --- |
| Pixel art (`Assets/Art/Pixel`) | Feita pelo autor em código com o [pxl](https://github.com/crsolver/claude-pxl) (`Tools/art/cold_ember.px.js`) | Próprio (pxl: MIT) |
| Paleta, fonte bitmap 5x7 e estilo de interface | Do jogo Rebound, do mesmo autor | Próprio |
| Músicas e efeitos (`Assets/Audio`) | Sintetizados pelo autor (`Tools/gen_audio.py`) | Próprio |
| Input Actions padrão | Template Universal 2D do Unity | Unity Companion License |

Nenhum asset de terceiros foi baixado.
