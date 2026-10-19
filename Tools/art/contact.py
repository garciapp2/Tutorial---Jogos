"""Folha de contato das sprites geradas (revisao rapida): Tools/art/out/_contact.png"""
import glob, os, sys
from PIL import Image
OUT = os.path.join(os.path.dirname(__file__), "out")
scale = int(sys.argv[1]) if len(sys.argv) > 1 else 6
files = sorted(glob.glob(os.path.join(OUT, "sprites", "*.png"))) + sorted(glob.glob(os.path.join(OUT, "anims", "*.png")))
ims = [Image.open(f).convert("RGBA") for f in files]
W = 1400; x = y = 8; rowh = 0; places = []
for im in ims:
    w, h = im.width * scale, im.height * scale
    if x + w > W: x = 8; y += rowh + 8; rowh = 0
    places.append((x, y)); x += w + 8; rowh = max(rowh, h)
sheet = Image.new("RGBA", (W, y + rowh + 8), (21, 17, 42, 255))
for im, (px, py) in zip(ims, places):
    sheet.alpha_composite(im.resize((im.width * scale, im.height * scale), Image.NEAREST), (px, py))
sheet.save(os.path.join(OUT, "_contact.png")); print(len(ims), "sprites")
