"""Arte da pagina do itch.io: capa (630x500), banner (960x180) e fundo (1920x1080).

Monta tudo em pixels logicos com os sprites do jogo (Tools/art/out, gerados pelo pxl),
a fonte bitmap do jogo e o mesmo redemoinho do menu, e amplia sem filtro.
Uso:  python Tools/art/itch_art.py   ->   Entrega/
"""
import math
import os
import random

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, "..", "..")
SPR = os.path.join(HERE, "out", "sprites")
ANI = os.path.join(HERE, "out", "anims")
OUT = os.path.join(ROOT, "Entrega")

INK = (11, 9, 18)
BG_MENU = [(18, 14, 38), (34, 26, 72), (56, 40, 108)]
BLUE_L, BLUE_D = (163, 203, 255), (42, 75, 168)
ORANGE, RED_D, YELLOW = (245, 138, 58), (141, 33, 64), (248, 207, 78)
WHITE, BONE, FOG = (255, 246, 224), (216, 208, 187), (122, 117, 170)


# ------------------------------------------------------------------ fundo em redemoinho
def swirl(w, h, block=1, t=7.0, dim=0.0, colors=BG_MENU):
    """O mesmo desenho do shader do jogo, em numpy."""
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float64)
    gx, gy = w / block, h / block
    p = np.stack([(np.floor(xs / block) + 0.5) / gx, (np.floor(ys / block) + 0.5) / gy], -1)
    q = (p - 0.5) * np.array([w / h, 1.0]) * 2.3
    tt = t * 0.06
    r = np.hypot(q[..., 0], q[..., 1])
    a = np.arctan2(q[..., 1], q[..., 0]) + 0.75 * np.sin(r * 1.3 - tt * 2.0) + tt * 0.8
    qx, qy = r * np.cos(a), r * np.sin(a)
    for i in range(1, 4):
        f = float(i)
        qx, qy = qx + 0.62 / f * np.sin(qy * f * 1.3 + tt * 3 + f * 1.7), qy + 0.62 / f * np.cos(qx * f * 1.1 - tt * 2.3 + f * 2.9)
    v = 0.5 + 0.5 * np.sin(qx * 1.15 + qy * 0.9 + np.sin(qy * 0.7 - tt))
    c0, c1, c2 = (np.array(c, dtype=np.float64) for c in colors)
    out = np.where(v[..., None] < 0.3, c0, np.where(v[..., None] < 0.56, c1, np.where(v[..., None] < 0.82, (c1 + c2) / 2, c2)))
    out = out * (1 - dim)
    return Image.fromarray(out.astype(np.uint8), "RGB").convert("RGBA")


def snow(img, count, seed=3):
    rng = random.Random(seed)
    px = img.load()
    for _ in range(count):
        x, y = rng.randrange(img.width), rng.randrange(img.height)
        c = BLUE_L if rng.random() < 0.6 else WHITE
        px[x, y] = c + (255,)


# ------------------------------------------------------------------ sprites
def sprite(name):
    return Image.open(os.path.join(SPR, name + ".png")).convert("RGBA")


def frame(name, i=0):
    strip = Image.open(os.path.join(ANI, name + ".png")).convert("RGBA")
    import json
    n = len(json.load(open(os.path.join(ANI, name + ".json")))["frames"])
    w = strip.width // n
    return strip.crop((i * w, 0, (i + 1) * w, strip.height))


def put(canvas, im, cx, cy, scale=1):
    if scale != 1:
        im = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
    canvas.alpha_composite(im, (int(cx - im.width / 2), int(cy - im.height / 2)))


def orb(scale=1, core_frame=1):
    """A brasa: casca, fogo grande dentro e vidro por cima."""
    o = sprite("orb_back").copy()
    core = frame("core", core_frame)
    o.alpha_composite(core, ((o.width - core.width) // 2, 2))
    o.alpha_composite(sprite("orb_glass"), (0, 0))
    return o.resize((o.width * scale, o.height * scale), Image.NEAREST)


def glow(canvas, cx, cy, radius, color, strength=0.55, bands=4):
    """Halo em aneis chapados (pixel art, sem degrade suave)."""
    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    px = layer.load()
    for y in range(max(0, cy - radius), min(canvas.height, cy + radius + 1)):
        for x in range(max(0, cx - radius), min(canvas.width, cx + radius + 1)):
            d = math.hypot(x - cx, y - cy) / radius
            if d >= 1:
                continue
            k = math.ceil((1 - d) * bands) / bands
            px[x, y] = color + (int(255 * strength * k * k),)
    canvas.alpha_composite(layer)


# ------------------------------------------------------------------ fonte do jogo
_font = None


def font():
    global _font
    if _font is None:
        atlas = Image.open(os.path.join(ROOT, "Assets", "Resources", "PixelFont.png")).convert("RGBA")
        table = {}
        for line in open(os.path.join(ROOT, "Assets", "Resources", "PixelFont.txt"), encoding="utf-8"):
            parts = line.split()
            if len(parts) == 3:
                table[chr(int(parts[0]))] = (int(parts[1]), int(parts[2]))
        _font = (atlas, table)
    return _font


def text_width(s, scale=1):
    _, table = font()
    return (sum(table.get(c, table["?"])[1] + 1 for c in s) - 1) * scale


def text(canvas, s, x, y, color, scale=1, shadow=INK, under=None):
    """Escreve com o topo das maiusculas em y. `under` e a copia deslocada para baixo (como no logo)."""
    atlas, table = font()
    passes = []
    if under:
        passes.append((under, 0, round(scale * 0.7)))
    if shadow:
        passes.append((shadow, scale, scale))
    passes.append((color, 0, 0))
    for col, ox, oy in passes:
        cx = x
        for c in s:
            gx, gw = table.get(c, table["?"])
            g = atlas.crop((gx, 0, gx + gw, 11))
            tint = Image.new("RGBA", g.size, col + (255,))
            tint.putalpha(g.getchannel("A"))
            if scale != 1:
                tint = tint.resize((gw * scale, 11 * scale), Image.NEAREST)
            canvas.alpha_composite(tint, (cx + ox, y - 2 * scale + oy))
            cx += (gw + 1) * scale


def styled(canvas, s, x, y, top, bottom, scale):
    """Logo B: contorno de 1 px de fonte, sombra dura e degrade vertical."""
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            if dx or dy:
                text(canvas, s, x + dx * scale, y + dy * scale, INK, scale, shadow=None)
    text(canvas, s, x + 2 * scale, y + 2 * scale, INK, scale, shadow=None)
    layer = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    text(layer, s, x, y, (255, 255, 255), scale, shadow=None)
    grad = Image.new("RGBA", canvas.size)
    gp = grad.load()
    for yy in range(canvas.height):
        k = min(1, max(0, (yy - y) / (7 * scale)))
        col = tuple(int(top[i] + (bottom[i] - top[i]) * k) for i in range(3)) + (255,)
        for xx in range(canvas.width):
            gp[xx, yy] = col
    grad.putalpha(layer.getchannel("A"))
    canvas.alpha_composite(grad)


def logo(canvas, cx, y, scale, orb_scale):
    """COLD (azul) com a brasa no lugar do O, e EMBER (laranja) embaixo."""
    w_c = text_width("C", scale)
    o_w = 5 * scale
    w_cold = w_c + scale + o_w + scale + text_width("LD", scale)
    x = cx - w_cold // 2
    styled(canvas, "C", x, y, WHITE, BLUE_L, scale)
    ocx, ocy = x + w_c + scale + o_w // 2, y + int(3.5 * scale)
    glow(canvas, ocx, ocy, 14 * orb_scale, ORANGE, 0.5)
    put(canvas, orb(orb_scale), ocx, ocy)
    styled(canvas, "LD", x + w_c + scale + o_w + scale, y, WHITE, BLUE_L, scale)
    w_e = text_width("EMBER", scale)
    styled(canvas, "EMBER", cx - w_e // 2, y + 10 * scale, YELLOW, ORANGE, scale)
    return y + 17 * scale


def floor_strip(canvas, y):
    """Uma fileira de paredes com neve, como as da cripta."""
    wall = sprite("wall_ns")
    for x in range(-4, canvas.width, 16):
        canvas.alpha_composite(wall, (x, y))


# ------------------------------------------------------------------ pecas
def cover():
    W, H, S = 126, 100, 5
    c = swirl(W, H, block=1, t=9.0)
    snow(c, 40)
    logo(c, W // 2, 10, 3, 1)
    floor_strip(c, 84)
    # elenco em cima da fileira de paredes
    put(c, frame("ghost", 0), 16, 75)
    put(c, frame("hunter", 1), 110, 76)
    put(c, sprite("spikes"), 34, 77)
    put(c, sprite("spikes"), 92, 77)
    for x in (50, 76):
        glow(c, x, 77, 7, ORANGE, 0.35, 3)
        put(c, frame("coal", 2), x, 78)
    glow(c, 63, 74, 9, YELLOW, 0.45, 3)
    put(c, frame("fragment", 1), 63, 74)
    c.convert("RGB").resize((W * S, H * S), Image.NEAREST).save(os.path.join(OUT, "capa_630x500.png"))


def banner():
    W, H, S = 192, 30, 5
    c = swirl(W, H, block=1, t=4.0)
    snow(c, 30, seed=8)
    scale = 2
    title = "COLD EMBER"
    w = text_width(title, scale)
    x = (W - w) // 2
    y = (H - 7 * scale) // 2
    # o O vira a brasa
    w_c = text_width("C", scale)
    styled(c, "C", x, y, WHITE, BLUE_L, scale)
    ocx, ocy = x + w_c + scale + 5, y + 7
    glow(c, ocx, ocy, 12, ORANGE, 0.5)
    put(c, orb(1), ocx, ocy)
    styled(c, "LD", x + w_c + scale + 10 + scale, y, WHITE, BLUE_L, scale)
    styled(c, "EMBER", x + text_width("COLD ", scale), y, YELLOW, ORANGE, scale)
    put(c, frame("ghost", 0), 20, 16)
    put(c, frame("hunter", 0), W - 20, 16)
    c.convert("RGB").resize((W * S, H * S), Image.NEAREST).save(os.path.join(OUT, "itch_banner_960x150.png"))


def background():
    W, H, S = 480, 270, 4
    c = swirl(W, H, block=2, t=12.0, dim=0.35)
    snow(c, 220, seed=11)
    c.convert("RGB").resize((W * S, H * S), Image.NEAREST).save(os.path.join(OUT, "itch_fundo_1920x1080.png"))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    cover()
    banner()
    background()
    print("ok")


def loading_logo():
    """Logo da tela de carregamento do WebGL (fundo transparente)."""
    W, H, S = 80, 44, 3
    c = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    logo(c, W // 2, 4, 2, 1)
    big = c.resize((W * S, H * S), Image.NEAREST)
    big.save(os.path.join(OUT, "loading_logo.png"))
    big.save(os.path.join(HERE, "web", "loading_logo.png"))  # usado pelo GameBuilder na build WebGL


if __name__ == "__main__":
    loading_logo()
