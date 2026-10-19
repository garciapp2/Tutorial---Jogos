"""Gera o atlas da fonte bitmap 5x7 (a mesma do Rebound) para o Unity.

Le os glifos de Tools/font_glyphs.json e escreve:
  Assets/Resources/PixelFont.png  (branco sobre transparente, 11 linhas de altura)
  Assets/Resources/PixelFont.txt  (uma linha por glifo: "<codigo> <x> <largura>")
"""
import json, os
from PIL import Image

HERE = os.path.dirname(__file__)
OUT = os.path.join(HERE, "..", "Assets", "Resources")
data = json.load(open(os.path.join(HERE, "font_glyphs.json"), encoding="utf-8"))
ROWS, BASE = 11, 2

def expand(body):
    lines = body.split("/")
    w = len(lines[0]); blank = "." * w
    rows = [blank, blank] + lines
    while len(rows) < ROWS: rows.append(blank)
    return rows

def compose(base, mark, lower):
    rows = expand(data["body"][base]); mk = data["marks"][mark].split("/")
    w = max(len(rows[0]), len(mk[0]))
    pad = lambda r: "." * ((w - len(r)) // 2) + r + "." * (w - len(r) - (w - len(r)) // 2)
    out = [pad(r) for r in rows]
    start = BASE if lower else 0
    for i, r in enumerate(mk): out[start + i] = pad(r)
    return out

glyphs = {c: expand(b) for c, b in data["body"].items()}
for c, (base, mark) in data["accented"].items():
    glyphs[c] = compose(base, mark, c == c.lower())
for c, base in (("ç", "c"), ("Ç", "C")):
    rows = expand(data["body"][base]); w = len(rows[0])
    rows[9] = "." * (w - 2) + "#."; rows[10] = "." * (w - 3) + "#.."
    glyphs[c] = rows

width = 1 + sum(len(r[0]) + 2 for r in glyphs.values())
img = Image.new("RGBA", (width, ROWS), (0, 0, 0, 0))
px = img.load(); x = 1; table = []
for c, rows in glyphs.items():
    w = len(rows[0])
    for y in range(ROWS):
        for i in range(w):
            if rows[y][i] == "#": px[x + i, y] = (255, 255, 255, 255)
    table.append(f"{ord(c)} {x} {w}")
    x += w + 2
os.makedirs(OUT, exist_ok=True)
img.save(os.path.join(OUT, "PixelFont.png"))
open(os.path.join(OUT, "PixelFont.txt"), "w", encoding="utf-8").write("\n".join(table) + "\n")
print(len(table), "glifos,", width, "px de largura")
