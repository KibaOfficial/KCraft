# Copyright (c) 2026 KibaOfficial
# All rights reserved.

from PIL import Image, ImageDraw

# glass.png laden (16x16 vanilla oder 64x64 Faithful)
glass = Image.open("assets/block/glass.png").convert("RGBA")
w, h = glass.size  # z.B. 16x16

# CTM Spritesheet: 4×4 Grid = 16 Tiles
# Jedes Tile = w×h px
sheet = Image.new("RGBA", (w * 4, h * 4), (0, 0, 0, 0))

def make_tile(neighbors):
    tile = glass.copy()
    draw = ImageDraw.Draw(tile)
    
    left, right, up, down = neighbors
    inner = glass.getpixel((w//2, h//2))
    b = 1
    
    # Seiten ohne Ecken
    if left:
        draw.rectangle([0, 1, b-1, h-2], fill=inner)  # y=1 bis y=14
    if right:
        draw.rectangle([w-b, 1, w-1, h-2], fill=inner)
    if up:
        draw.rectangle([1, 0, w-2, b-1], fill=inner)
    if down:
        draw.rectangle([1, h-b, w-2, h-1], fill=inner)

    # Ecken nur wenn beide Seiten Nachbarn haben
    if left and up:
        draw.rectangle([0, 0, b-1, b-1], fill=inner)
    if right and up:
        draw.rectangle([w-b, 0, w-1, b-1], fill=inner)
    if left and down:
        draw.rectangle([0, h-b, b-1, h-1], fill=inner)
    if right and down:
        draw.rectangle([w-b, h-b, w-1, h-1], fill=inner)
    
    return tile

# 16 Kombinationen generieren (Bits: left, right, up, down)
for i in range(16):
    left  = bool(i & 1)
    right = bool(i & 2)
    up    = bool(i & 4)
    down  = bool(i & 8)
    
    tile = make_tile((left, right, up, down))
    
    tx = (i % 4) * w
    ty = (i // 4) * h
    sheet.paste(tile, (tx, ty))

sheet.save("assets/dev/faithful/glass_ctm.png")
print(f"CTM Spritesheet generiert: {w*4}x{h*4}px, 16 Tiles")