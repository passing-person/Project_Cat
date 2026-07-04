#!/usr/bin/env python3
"""Regenerate OKLCH LUT .bytes files (RGBAFloat 1089x33 atlas)."""

import math
import os
import struct

LUT_SIZE = 33
OKLAB_AB_RANGE = 0.4

M1 = (
    (0.4122214708, 0.5363325363, 0.0514459929),
    (0.2119034982, 0.6806995451, 0.1073969566),
    (0.0883024619, 0.2817188376, 0.6299787005),
)

M2 = (
    (0.2104542553, 0.7936177850, -0.0040720468),
    (1.9779984951, -2.4285922050, 0.4505937099),
    (0.0259040371, 0.7827717662, -0.8086757660),
)

M2_INV = (
    (1.0, 0.3963377774, 0.2158037573),
    (1.0, -0.1055613458, -0.0638541728),
    (1.0, -0.0894841775, -1.2914855480),
)

M1_INV = (
    (4.0767416621, -3.3077115913, 0.2309699292),
    (-1.2684380046, 2.6097574011, -0.3413193965),
    (-0.0041960863, -0.7034186147, 1.7076147010),
)


def mul(m, v):
    return tuple(sum(m[r][c] * v[c] for c in range(3)) for r in range(3))


def cube_root(v):
    return tuple(math.copysign(abs(x) ** (1.0 / 3.0), x) for x in v)


def pow3(v):
    return tuple(x * x * x for x in v)


def clamp01(x):
    return max(0.0, min(1.0, x))


def linear_rgb_to_oklab(rgb):
    lms = mul(M1, rgb)
    lms = cube_root(lms)
    return mul(M2, lms)


def oklab_to_linear_rgb(lab):
    lms = mul(M2_INV, lab)
    lms = pow3(lms)
    rgb = mul(M1_INV, lms)
    return tuple(clamp01(x) for x in rgb)


def write_raw_rgba_float(path, width, height, pixels):
    with open(path, "wb") as f:
        for y in range(height):
            for x in range(width):
                r, g, b = pixels[y * width + x]
                f.write(struct.pack("<ffff", float(r), float(g), float(b), 1.0))


def bake_rgb_to_oklab():
    width = LUT_SIZE * LUT_SIZE
    height = LUT_SIZE
    pixels = [(0.0, 0.0, 0.0)] * (width * height)
    for z in range(LUT_SIZE):
        b = z / (LUT_SIZE - 1)
        for y in range(LUT_SIZE):
            g = y / (LUT_SIZE - 1)
            for x in range(LUT_SIZE):
                r = x / (LUT_SIZE - 1)
                oklab = linear_rgb_to_oklab((r, g, b))
                px = z * LUT_SIZE + x
                py = y
                pixels[py * width + px] = oklab
    return width, height, pixels


def bake_oklab_to_rgb():
    width = LUT_SIZE * LUT_SIZE
    height = LUT_SIZE
    pixels = [(0.0, 0.0, 0.0)] * (width * height)
    for z in range(LUT_SIZE):
        b = OKLAB_AB_RANGE * (2.0 * z / (LUT_SIZE - 1) - 1.0)
        for y in range(LUT_SIZE):
            a = OKLAB_AB_RANGE * (2.0 * y / (LUT_SIZE - 1) - 1.0)
            for x in range(LUT_SIZE):
                l = x / (LUT_SIZE - 1)
                rgb = oklab_to_linear_rgb((l, a, b))
                px = z * LUT_SIZE + x
                py = y
                pixels[py * width + px] = rgb
    return width, height, pixels


def main():
    out_dir = os.path.join(os.path.dirname(__file__), "LUT")
    os.makedirs(out_dir, exist_ok=True)

    w, h, px = bake_rgb_to_oklab()
    write_raw_rgba_float(os.path.join(out_dir, "Oklch_RgbToOklab_Lut.bytes"), w, h, px)

    w, h, px = bake_oklab_to_rgb()
    write_raw_rgba_float(os.path.join(out_dir, "Oklch_OklabToRgb_Lut.bytes"), w, h, px)

    print(f"Baked LUT .bytes to {out_dir} ({LUT_SIZE}^3 atlas {w}x{h})")


if __name__ == "__main__":
    main()
