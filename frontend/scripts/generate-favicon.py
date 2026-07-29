from PIL import Image
from pathlib import Path

public = Path(__file__).resolve().parents[1] / "public"
images = public / "images"

# Yeşil Gölköy logosu (kullanıcının gönderdiği marka)
src = Image.open(images / "golkoy-logo-new.png").convert("RGBA")
pixels = src.load()
w, h = src.size
minx, miny, maxx, maxy = w, h, 0, 0
for y in range(h):
    for x in range(w):
        r, g, b, a = pixels[x, y]
        if a < 20:
            continue
        if r < 20 and g < 20 and b < 20:
            continue
        minx = min(minx, x)
        miny = min(miny, y)
        maxx = max(maxx, x)
        maxy = max(maxy, y)

cropped = src.crop((minx, miny, maxx + 1, maxy + 1))
cw, ch = cropped.size
out = Image.new("RGBA", (cw, ch), (255, 255, 255, 255))
cp = cropped.load()
op = out.load()
for y in range(ch):
    for x in range(cw):
        r, g, b, a = cp[x, y]
        if a < 20 or (r < 25 and g < 25 and b < 25):
            continue
        op[x, y] = (r, g, b, 255)

side = max(cw, ch)
pad = int(side * 0.10)
canvas_side = side + pad * 2
canvas = Image.new("RGBA", (canvas_side, canvas_side), (255, 255, 255, 255))
canvas.paste(out, (pad + (side - cw) // 2, pad + (side - ch) // 2), out)

ico_sizes = [(16, 16), (32, 32), (48, 48), (64, 64)]
ico_images = [canvas.resize(s, Image.Resampling.LANCZOS) for s in ico_sizes]
ico_images[-1].save(public / "favicon.ico", format="ICO", sizes=ico_sizes)

for name, size in [("logo192.png", 192), ("logo512.png", 512)]:
    canvas.resize((size, size), Image.Resampling.LANCZOS).save(
        public / name, format="PNG", optimize=True
    )

canvas.resize((32, 32), Image.Resampling.LANCZOS).save(
    images / "favicon-32.png", format="PNG", optimize=True
)
canvas.resize((180, 180), Image.Resampling.LANCZOS).save(
    images / "apple-touch-icon.png", format="PNG", optimize=True
)
canvas.save(images / "golkoy-favicon.png", format="PNG", optimize=True)

og = Image.new("RGB", (1200, 630), (255, 255, 255))
logo_og = canvas.resize((460, 460), Image.Resampling.LANCZOS).convert("RGBA")
og.paste(logo_og, ((1200 - 460) // 2, (630 - 460) // 2), logo_og)
og.save(images / "og-default.jpg", format="JPEG", quality=92, optimize=True)

print("yeşil Gölköy favicon hazır")
