from PIL import Image
from pathlib import Path

public = Path(__file__).resolve().parents[1] / "public"
images = public / "images"

src = Image.open(images / "golkoy-header-logo.png").convert("RGBA")


def make_square(size, pad_ratio=0.08):
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    margin = max(1, int(size * pad_ratio))
    max_w = size - 2 * margin
    max_h = size - 2 * margin
    ratio = min(max_w / src.width, max_h / src.height)
    nw = max(1, int(src.width * ratio))
    nh = max(1, int(src.height * ratio))
    logo = src.resize((nw, nh), Image.Resampling.LANCZOS)
    canvas.paste(logo, ((size - nw) // 2, (size - nh) // 2), logo)
    return canvas


for size, name in [
    (32, "favicon-32.png"),
    (48, "favicon-48.png"),
    (96, "favicon-96.png"),
    (180, "apple-touch-icon.png"),
    (192, "favicon-192.png"),
    (192, "golkoy-favicon.png"),
    (72, "icon-72.png"),
    (192, "icon-192.png"),
]:
    make_square(size).save(images / name, format="PNG", optimize=True)

make_square(192).save(public / "logo192.png", format="PNG", optimize=True)
make_square(512, 0.1).save(public / "logo512.png", format="PNG", optimize=True)

sizes = [16, 32, 48, 64]
ico_images = [make_square(s) for s in sizes]
ico_images[-1].save(public / "favicon.ico", format="ICO", sizes=[(s, s) for s in sizes])

print("seffaf Golkoy Gurme ikonlari hazir")
