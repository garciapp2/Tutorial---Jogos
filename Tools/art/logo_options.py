"""Tres propostas de logo lado a lado: Entrega/logo_opcoes.png"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from itch_art import *
from PIL import Image

def outlined(c, s, x, y, col, scale, outline=INK, thick=1, shadow=True):
    # contorno de `thick` pixels logicos + sombra dura
    for dx in range(-thick, thick + 1):
        for dy in range(-thick, thick + 1):
            if dx or dy:
                text(c, s, x + dx * scale, y + dy * scale, outline, scale, shadow=None)
    if shadow:
        text(c, s, x + scale * (thick + 1), y + scale * (thick + 1), outline, scale, shadow=None)
    text(c, s, x, y, col, scale, shadow=None)

def gradient_text(c, s, x, y, top, bottom, scale):
    layer = Image.new("RGBA", c.size, (0, 0, 0, 0))
    text(layer, s, x, y, (255, 255, 255), scale, shadow=None)
    a = layer.getchannel("A")
    g = Image.new("RGBA", c.size)
    px = g.load()
    for yy in range(c.height):
        k = min(1, max(0, (yy - y) / (7 * scale)))
        col = tuple(int(top[i] + (bottom[i] - top[i]) * k) for i in range(3))
        for xx in range(c.width):
            px[xx, yy] = col + (255,)
    g.putalpha(a)
    c.alpha_composite(g)

def with_orb(c, word_first, x, y, scale, draw):
    # desenha "C", a brasa no lugar do O e "LD"
    w_c = text_width("C", scale)
    draw("C", x, y)
    ocx, ocy = x + w_c + scale + int(2.5 * scale), y + int(3.5 * scale)
    glow(c, ocx, ocy, 6 * scale, ORANGE, 0.5)
    put(c, orb(max(1, scale // 3)), ocx, ocy)
    draw("LD", x + w_c + scale + 5 * scale + scale, y)

def panel(variant):
    W, H, S = 150, 70, 4
    c = swirl(W, H, block=1, t=9.0)
    snow(c, 25)
    scale = 3
    wc = text_width("COLD", scale); we = text_width("EMBER", scale)
    xc, xe = (W - wc) // 2, (W - we) // 2
    if variant == "A":   # branca com contorno, so a brasa colorida (como o REBOUND)
        d = lambda s, x, y: outlined(c, s, x, y, WHITE, scale // 3 * 3 and scale, thick=1)
        with_orb(c, "COLD", xc, 8, scale, lambda s, x, y: outlined(c, s, x, y, WHITE, scale))
        outlined(c, "EMBER", xe, 36, WHITE, scale)
        label = "A  BRANCA COM CONTORNO"
    elif variant == "B":  # gelo -> fogo em degrade, com contorno
        def cold(s, x, y):
            outlined(c, s, x, y, INK, scale, shadow=True)
            gradient_text(c, s, x, y, WHITE, BLUE_L, scale)
        with_orb(c, "COLD", xc, 8, scale, cold)
        outlined(c, "EMBER", xe, 36, INK, scale)
        gradient_text(c, "EMBER", xe, 36, YELLOW, ORANGE, scale)
        label = "B  GELO E FOGO EM DEGRADE"
    else:                 # pesada: traco engrossado, contorno grosso e sombra longa
        def heavy(s, x, y, col):
            for k in range(4, 0, -1):
                text(c, s, x + k, y + k, INK, scale, shadow=None)
            for dx in (-2, -1, 0, 1, 2):
                for dy in (-2, -1, 0, 1, 2):
                    text(c, s, x + dx, y + dy, INK, scale, shadow=None)
            text(c, s, x, y, col, scale, shadow=None)
            text(c, s, x + 1, y, col, scale, shadow=None)
        with_orb(c, "COLD", xc, 8, scale, lambda s, x, y: heavy(s, x, y, BLUE_L))
        heavy("EMBER", xe, 36, ORANGE)
        label = "C  PESADA"
    text(c, label, 4, H - 9, BONE, 1)
    return c.resize((W * S, H * S), Image.NEAREST)

ps = [panel(v) for v in "ABC"]
out = Image.new("RGBA", (ps[0].width, sum(p.height for p in ps) + 8 * 2), INK + (255,))
y = 0
for p in ps:
    out.alpha_composite(p, (0, y)); y += p.height + 8
out.convert("RGB").save(os.path.join(OUT, "logo_opcoes.png"))
print("ok")
