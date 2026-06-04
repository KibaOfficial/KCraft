# Copyright (c) 2026 KibaOfficial
# All rights reserved.

from PIL import Image

glass = Image.open("assets/block/glass.png").convert("RGBA")
w, h = glass.size
pixels = glass.load()

print(f"Größe: {w}x{h}")

# Rahmendicke finden — von links nach rechts bis sich der Pixel ändert
corner = pixels[0, 0]
print(f"Eckfarbe: {corner}")

for x in range(w):
    if pixels[x, h//2] != corner:
        print(f"Rahmendicke horizontal: {x}px")
        break

for y in range(h):
    if pixels[w//2, y] != corner:
        print(f"Rahmendicke vertikal: {y}px")
        break