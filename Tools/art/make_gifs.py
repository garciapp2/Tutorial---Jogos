"""Junta os quadros gravados pelo CaptureRig (Temp/gif/<nome>) em GIFs e capturas para a pagina do itch.io.

O CaptureRig grava a camera numa textura de 600x480, que nao inclui o canvas da tela de fliperama
(linhas de varredura e moldura). Aqui eles sao aplicados por cima, do mesmo jeito que no jogo.
"""
import glob
import os
import sys

import numpy as np
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
OUT = os.path.join(ROOT, "Entrega")
INK = np.array([11, 9, 18], dtype=np.float64)


def crt(im, scanlines=True):
    """Moldura preta de 5 px logicos com cantos de raio 14, e uma linha escura por pixel logico."""
    a = np.asarray(im.convert("RGB"), dtype=np.float64)
    h, w = a.shape[:2]
    s = w / 300.0  # pixels de tela por pixel logico
    if scanlines:
        rows = (np.arange(h) // max(1, int(round(s / 2)))) % 2 == 1
        a[rows] = a[rows] * (1 - 0.44) + INK * 0.44
    ys, xs = np.mgrid[0:h, 0:w]
    m, r = 5 * s, 14 * s
    # distancia ate o retangulo interno com cantos arredondados
    dx = np.maximum(0, np.maximum(m + r - xs, xs - (w - m - r)))
    dy = np.maximum(0, np.maximum(m + r - ys, ys - (h - m - r)))
    outside = np.hypot(dx, dy) > r
    a[outside] = INK
    return Image.fromarray(a.astype(np.uint8), "RGB")


def make(name, width=480, fps=15, skip=0, colors=64):
    files = sorted(glob.glob(os.path.join(ROOT, "Temp", "gif", name, "*.png")))[skip:]
    if not files:
        print("sem quadros:", name)
        return
    frames = []
    for f in files:
        im = crt(Image.open(f), scanlines=False)
        h = round(im.height * width / im.width)
        frames.append(im.resize((width, h), Image.LANCZOS).quantize(colors=colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE))
    out = os.path.join(OUT, f"gif_{name}.gif")
    frames[0].save(out, save_all=True, append_images=frames[1:], duration=int(1000 / fps), loop=0, optimize=True)
    print(out, len(frames), "quadros", os.path.getsize(out) // 1024, "KB")


def shot(src, name):
    out = os.path.join(OUT, name)
    crt(Image.open(src)).save(out)
    print(out)


if __name__ == "__main__":
    for n in sys.argv[1:] or ["menu", "dash", "crystals"]:
        make(n)
