// Cold Ember: toda a pixel art do jogo, escrita com pxl (github.com/crsolver/claude-pxl).
// Paleta: a mesma de 24 cores do Rebound, mais os tons claros de time e os tons de fundo.
//   node Tools/pxl.mjs build Tools/art/cold_ember.px.js

pal({
  k: "#0b0912", n: "#15112a", d: "#231c40", s: "#3d3769", f: "#7a75aa", b: "#d8d0bb", w: "#fff6e0",
  r: "#ef4b55", R: "#8d2140", l: "#ff9d8c",
  o: "#f58a3a", y: "#f8cf4e", g: "#b9772a",
  e: "#56c86f", E: "#237a5b",
  t: "#38c7b6", T: "#1b6b78", a: "#aaf3e4",
  u: "#4b8ff5", U: "#2a4ba8", c: "#a3cbff",
  p: "#a35fe6", P: "#5b2f96", v: "#d9b5ff",
  h: "#8a5a3c", H: "#55342a",
  // chao da cripta e do gelo (tons de fundo "fight" e "map" do Rebound)
  N: "#110f22", m: "#1b1834", M: "#262248",
  F: "#0c141e", G: "#162a3c", Q: "#234556",
})
ramps("kRrlw", "kgoyw", "kTtaw", "kUucw", "kPpvw", "kndsfbw", "kHh")

// ------------------------------------------------------------------ jogador
// A brasa: bola vermelha (o circulo do tutorial) com brilho e contorno.
sprite("player", { w: 13, h: 13, outline: "k" },
  circ(6, 6, 6, "r"),
  shade((x, y) => x + y > 15 && (x - 6) ** 2 + (y - 6) ** 2 <= 36),
  light([4, 4, 3]),
  px(3, 3, "w"), px(4, 3, "w"), px(3, 4, "w"))

// Labareda que sai do topo da brasa; cresce com o calor (escala feita no jogo).
anim("flame", { w: 9, h: 12, frames: 6, fps: 12, outline: "k" }, (c) => {
  const k = c.phase * Math.PI * 2
  const sway = Math.round(Math.sin(k) * 1.2)
  const top = Math.round((1 - Math.cos(k * 2)) * 0.9)
  c.ellipse(4, 8, 4, 3, "r"); c.poly([0, 8, 4 + sway, top, 9, 8], "r")
  c.ellipse(4, 9, 3, 2, "o"); c.poly([1, 9, 4 - sway, top + 3, 8, 9], "o")
  c.ellipse(4, 10, 2, 1, "y"); c.poly([2, 10, 4 + sway, top + 6, 7, 10], "y")
  c.px(4, 10, "w"); c.px(4, 9, "w")
})

// ------------------------------------------------------------------ coletaveis
sprite("_coal", { w: 11, h: 10 },
  poly([1, 3, 4, 0, 8, 0, 11, 4, 10, 9, 3, 10, 0, 7], "s"),
  poly([1, 3, 4, 0, 8, 0, 7, 3, 2, 5], "f"),
  poly([3, 10, 10, 9, 11, 6, 6, 9], "d"),
  px(4, 1, "b"), px(5, 1, "b"))
anim("coal", { w: 11, h: 10, fps: 6, outline: "k" },
  stamp("_coal", 0, 0),
  line(2, 6, 5, 4, "r"), line(5, 4, 9, 6, "r"), line(5, 4, 6, 8, "r"),
  line(3, 6, 5, 5, "o"), line(5, 5, 8, 6, "o"), px(5, 6, "o"), px(6, 7, "o"),
  glow("roy", { r: 2, bands: 2, strength: 0.55 }),
  frame(px(5, 5, "y")), frame(px(5, 5, "y"), px(4, 5, "y")), frame(px(5, 5, "w"), px(4, 5, "y"), px(6, 6, "y"), px(7, 6, "y")), frame(px(5, 5, "y"), px(6, 6, "y")))

// Fragmento de chama: um cristal com fogo preso dentro.
anim("fragment", { w: 11, h: 15, frames: 4, fps: 8, outline: "k" }, (c) => {
  c.poly([5, 0, 11, 6, 5, 15, 0, 6], "r")
  c.poly([5, 2, 9, 6, 5, 13, 1, 6], "o")
  const k = c.phase * Math.PI * 2
  const s = Math.round(Math.sin(k))
  c.poly([5, 4 + Math.max(0, s), 8, 8, 5, 12, 2, 8], "y")
  c.rect(4 + (s > 0 ? 1 : 0), 8, 2, 2, "w")
  c.px(3, 3, "l"); c.px(2, 4, "l")
})

// ------------------------------------------------------------------ fogueira
sprite("_logs", { w: 22, h: 8 },
  line(2, 6, 19, 2, "h", 3), line(2, 2, 19, 6, "H", 3),
  px(3, 6, "g"), px(18, 6, "g"))
sprite("_stones", { w: 24, h: 6 },
  ...[1, 6, 11, 16, 21].map((x, i) => ellipse(x + 1, 3, 2, 2, i % 2 ? "s" : "f")),
  ...[1, 6, 11, 16, 21].map((x) => px(x, 2, "b")))

sprite("bonfire_off", { w: 24, h: 20, outline: "k" },
  stamp("_logs", 1, 9), stamp("_stones", 0, 14),
  poly([8, 13, 10, 4, 12, 13], "c"), poly([11, 13, 14, 2, 16, 13], "u"), poly([14, 13, 16, 7, 18, 13], "c"),
  line(13, 4, 13, 11, "w"), px(10, 6, "w"))

anim("bonfire_lit", { w: 24, h: 22, frames: 4, fps: 10, outline: "k" }, (c) => {
  const k = c.phase * Math.PI * 2
  const sw = Math.round(Math.sin(k) * 1.5)
  c.stamp("_logs", 1, 11)
  c.poly([4, 16, 12 + sw, 0 + Math.round(Math.abs(Math.sin(k)) * 2), 20, 16], "r")
  c.poly([6, 16, 12 - sw, 4, 18, 16], "o")
  c.poly([8, 16, 12 + sw, 8, 16, 16], "y")
  c.rect(11, 13, 3, 3, "w")
  c.stamp("_stones", 0, 16)
})

// ------------------------------------------------------------------ inimigos
// Espirito de gelo (patrulha): meio fantasma, espelhado.
anim("ghost", { mx: true, fps: 5, outline: "k" }, `
  ....aaa
  ..aaaaa
  .aaaaaa
  .aaaaaa
  aaakkaa
  aaakwaa
  aaaaaaa
  taaaaaa
  ttaaaaa
  tttaaaa
  tttttaa
  ttttttt`,
  frame(grid(0, 11, "_tt_tt_")),
  frame(grid(0, 11, "t_tt_tt")))

// Cacador glacial (persegue): cristal roxo com chifres e olhos acesos.
anim("hunter", { mx: true, fps: 4, outline: "k" }, `
  .v......
  .pv...v.
  .ppv.pv.
  ..pppppp
  .ppvvppp
  pppvpppp
  pPPyyPPp
  pPkyykPp
  ppPPPPpp
  pppPPppp
  .pPPPPPp
  .PP..PPP`,
  frame(), frame(grid(1, 0, "..\n..\nv.")))

// Cristais de gelo (perigo parado).
sprite("spikes", { w: 16, h: 14, outline: "k" },
  poly([0, 14, 3, 5, 6, 14], "U"), poly([4, 14, 8, 0, 12, 14], "u"), poly([10, 14, 13, 6, 16, 14], "U"),
  poly([5, 14, 8, 2, 8, 14], "c"), line(8, 2, 8, 12, "w"), poly([1, 14, 3, 7, 3, 14], "u"), poly([11, 14, 13, 8, 13, 14], "u"))

// ------------------------------------------------------------------ cenario
// Chao da cripta: lajes escuras com junta de um pixel. Sem pontinhos de enfeite.
sprite("floor_a", { w: 16, h: 16 }, rect(0, 0, 16, 16, "N"), hline(0, 0, 16, "m"), vline(0, 0, 16, "m"))
sprite("floor_b", { w: 16, h: 16 }, stamp("floor_a", 0, 0), line(4, 5, 8, 8, "m"), line(8, 8, 7, 12, "m"), px(9, 9, "m"))
sprite("floor_c", { w: 16, h: 16 }, stamp("floor_a", 0, 0), rect(1, 8, 15, 1, "m"), vline(8, 1, 7, "m"))
group("floor", ["floor_a", "floor_a", "floor_a", "floor_a", "floor_b", "floor_c", "floor_c"])

// Gelo do chao (zona congelada): placas azuladas com reflexo diagonal.
sprite("frost_a", { w: 16, h: 16 }, rect(0, 0, 16, 16, "G"), hline(0, 0, 16, "Q"), vline(0, 0, 16, "Q"),
  line(3, 13, 12, 4, "Q"), line(5, 14, 14, 5, "Q"), px(11, 3, "c"))
sprite("frost_b", { w: 16, h: 16 }, rect(0, 0, 16, 16, "G"), hline(0, 0, 16, "Q"), vline(0, 0, 16, "Q"),
  line(2, 9, 7, 4, "Q"), px(12, 11, "a"), px(13, 12, "Q"))

// Paredes: blocos de pedra fria vistos de cima, com neve na borda de cima e
// face de tijolos onde ha chao embaixo. Quatro variantes (borda em cima? face embaixo?).
const top = (c) => {
  c.rect(0, 0, 16, 16, "s")
  c.hline(0, 7, 16, "d"); c.vline(7, 0, 7, "d"); c.vline(11, 8, 8, "d"); c.vline(3, 8, 8, "d")
  c.hline(0, 0, 16, "f"); c.px(8, 1, "f"); c.px(0, 8, "f"); c.px(12, 8, "f")
}
const snow = (c) => {
  c.rect(0, 0, 16, 3, "w"); c.hline(0, 3, 16, "b")
  for (const x of [2, 7, 12]) c.px(x, 3, "w")
  for (const x of [4, 10]) c.px(x, 4, "b")
}
const face = (c) => {
  c.rect(0, 10, 16, 6, "d"); c.hline(0, 10, 16, "k"); c.hline(0, 13, 16, "n")
  c.vline(5, 11, 2, "n"); c.vline(13, 11, 2, "n"); c.vline(9, 14, 2, "n"); c.vline(1, 14, 2, "n")
  c.hline(0, 15, 16, "k")
}
sprite("wall_0", { w: 16, h: 16 }, top)
sprite("wall_n", { w: 16, h: 16 }, top, snow)
sprite("wall_s", { w: 16, h: 16 }, top, face)
sprite("wall_ns", { w: 16, h: 16 }, top, snow, face)

// ------------------------------------------------------------------ interface
sprite("heart", { w: 9, h: 8, outline: "k" }, `
  .rr...rr.
  rlrr.rrrr
  rlrrrrrrr
  rrrrrrrrR
  .rrrrrrR.
  ..rrrrR..
  ...rrR...
  ....R....`)
sprite("heart_empty", { base: "heart", swap: { r: "d", l: "d", R: "n" } })

sprite("icon_heat", { w: 7, h: 9, outline: "k" },
  poly([0, 9, 3, 0, 7, 9], "r"), poly([1, 9, 3, 3, 6, 9], "o"), poly([2, 9, 3, 6, 5, 9], "y"))
sprite("icon_clock", { w: 9, h: 9, outline: "k" },
  circ(4, 4, 4, "b"), ring(4, 4, 4, "f"), vline(4, 2, 3, "k"), hline(4, 4, 2, "k"))
sprite("icon_fragment", { w: 7, h: 9, outline: "k" },
  poly([3, 0, 7, 4, 3, 9, 0, 4], "o"), poly([3, 2, 5, 4, 3, 7, 1, 4], "y"), px(3, 4, "w"))
sprite("icon_frost", { w: 9, h: 9 },
  line(4, 0, 4, 8, "c"), line(0, 4, 8, 4, "c"), line(1, 1, 7, 7, "a"), line(7, 1, 1, 7, "a"), px(4, 4, "w"))

sprite("arrow", { w: 7, h: 7, outline: "k" }, poly([3, 0, 7, 7, 3, 5, 0, 7], "y"), px(3, 2, "w"))
sprite("alert", { w: 5, h: 9, outline: "k" }, rect(0, 0, 5, 9, "y"), rect(2, 1, 1, 4, "k"), px(2, 6, "k"))

// Teclas e botoes para a tela de como jogar.
sprite("key", { w: 11, h: 11 }, rect(0, 0, 11, 11, "k"), rect(1, 1, 9, 9, "b"), rect(1, 8, 9, 2, "f"), hline(1, 1, 9, "w"))
sprite("pad_a", { w: 11, h: 11 }, circ(5, 5, 5, "k"), circ(5, 5, 4, "e"), circ(5, 4, 3, "e"), px(3, 2, "w"))
sprite("pad_b", { base: "pad_a", swap: { e: "r" } })
sprite("pad_stick", { w: 11, h: 11 }, circ(5, 5, 5, "k"), circ(5, 5, 4, "s"), circ(5, 5, 2, "f"), px(4, 4, "b"))
sprite("pad_start", { w: 11, h: 7 }, rect(0, 0, 11, 7, "k"), rect(1, 1, 9, 5, "s"), hline(3, 3, 5, "b"))

// Pecas de painel de interface (brancas, coloridas no jogo): cantos cortados em 1 pixel.
pal({ W: "#ffffff" })
sprite("ui_notch", ".WWW.\nWWWWW\nWWWWW\nWWWWW\n.WWW.")
sprite("ui_pixel", "W")

// Silhueta branca da brasa: o clarao de quando ela apanha (e o quadro congelado do nocaute).
sprite("player_white", { base: "player", swap: { r: "W", R: "W", l: "W", w: "W" } })

// ------------------------------------------------------------------ a brasa: bola de vidro com fogo dentro
// Casca escura atras, o fogo no meio (cresce com os fragmentos, no jogo) e o vidro na frente.
pal({ D: "#5a1428", X: "#7a1e34" })
sprite("orb_back", { w: 13, h: 13, outline: "k" }, circ(6, 6, 6, "R"), circ(6, 6, 5, "D"), ellipse(6, 9, 4, 2, "X"))
sprite("orb_glass", { w: 15, h: 15 },
  ring(7, 7, 6, "r"), px(3, 4, "l"), px(4, 3, "l"), px(4, 4, "w"), px(3, 5, "l"), px(5, 3, "l"),
  px(10, 11, "l"), px(11, 10, "l"))
anim("core", { w: 9, h: 11, frames: 6, fps: 12 }, (c) => {
  const k = c.phase * Math.PI * 2
  const sway = Math.round(Math.sin(k) * 1.2)
  const top = Math.round((1 - Math.cos(k * 2)) * 0.9)
  c.ellipse(4, 8, 4, 2, "r"); c.poly([0, 8, 4 + sway, top, 9, 8], "r")
  c.ellipse(4, 8, 3, 2, "o"); c.poly([1, 9, 4 - sway, top + 3, 8, 9], "o")
  c.ellipse(4, 9, 2, 1, "y"); c.poly([2, 10, 4 + sway, top + 6, 7, 10], "y")
  c.px(4, 9, "w"); c.px(4, 8, "w")
})
// versao montada para o logo do menu
sprite("orb_logo", { w: 15, h: 15 }, stamp("orb_back", 0, 0), stamp("core", 3, 2, { frame: 1 }), stamp("orb_glass", 0, 0))
