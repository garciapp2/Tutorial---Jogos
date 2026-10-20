"""Junta os quadros gravados pelo CaptureRig (Temp/gif/<nome>) em GIFs para a pagina do itch.io."""
import glob, os, sys
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
OUT = os.path.join(ROOT, "Entrega")

def make(name, width=640, fps=15, skip=0):
    files = sorted(glob.glob(os.path.join(ROOT, "Temp", "gif", name, "*.png")))[skip:]
    if not files:
        print("sem quadros:", name); return
    frames = []
    for f in files:
        im = Image.open(f).convert("RGB")
        h = round(im.height * width / im.width)
        frames.append(im.resize((width, h), Image.LANCZOS).quantize(colors=96, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE))
    out = os.path.join(OUT, f"gif_{name}.gif")
    frames[0].save(out, save_all=True, append_images=frames[1:], duration=int(1000 / fps), loop=0, optimize=True)
    print(out, len(frames), "quadros", os.path.getsize(out) // 1024, "KB")

if __name__ == "__main__":
    for n in sys.argv[1:] or ["menu", "dash", "crystals"]:
        make(n)
