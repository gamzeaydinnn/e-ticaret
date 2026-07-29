from PIL import Image
from pathlib import Path

public = Path(__file__).resolve().parents[1] / "public"
images = public / "images"

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

pad = 8
cropped = src.crop(
    (max(0, minx - pad), max(0, miny - pad), min(w, maxx + 1 + pad), min(h, maxy + 1 + pad))
)
cropped.save(images / "golkoy-header-logo.png", format="PNG", optimize=True)

cw, ch = cropped.size
side = max(cw, ch)
pad2 = int(side * 0.12)
canvas = Image.new("RGBA", (side + pad2 * 2, side + pad2 * 2), (0, 0, 0, 0))
canvas.paste(cropped, (pad2 + (side - cw) // 2, pad2 + (side - ch) // 2), cropped)

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

ico_sizes = [(16, 16), (32, 32), (48, 48), (64, 64)]
ico_images = [canvas.resize(s, Image.Resampling.LANCZOS) for s in ico_sizes]
ico_images[-1].save(public / "favicon.ico", format="ICO", sizes=ico_sizes)

print("arka plansiz yesil Golkoy header + favicon hazir")
