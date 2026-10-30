"""Render docs/mindmap.png from docs/mindmap/recruitment-ats.xmind.

The .xmind file is the source of truth. Edit topic titles there (in XMind or by
hand), then run:

    python docs/mindmap/render_mindmap.py

Requires Pillow and the Inter variable font (Inter-VF.ttf). The first six
branches are drawn on the right, the rest on the left, in file order.
"""

import json
import os
import sys
import zipfile

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
XMIND = os.path.join(HERE, "recruitment-ats.xmind")
OUT = os.path.join(HERE, "..", "mindmap.png")

S = 2  # supersampling factor
W, H = 1860, 1240
CENTER = (708, 527, 1153, 713)
RIGHT_COUNT = 6
MAIN_H, LEAF_H, LEAF_PITCH, BLOCK_GAP = 50, 34, 42, 32
MAIN_GAP, BRACKET_X = 30, 16
MAIN_X_RIGHT, MAIN_X_LEFT_END = 1180, 680

# Tailwind-style (main 400, leaf fill 100, leaf text 800)
PALETTE = [
    ("#f87171", "#fee2e2", "#991b1b"),  # red
    ("#fb923c", "#ffedd5", "#9a3412"),  # orange
    ("#4ade80", "#dcfce7", "#166534"),  # green
    ("#2dd4bf", "#ccfbf1", "#115e59"),  # teal
    ("#60a5fa", "#dbeafe", "#1e40af"),  # blue
    ("#818cf8", "#e0e7ff", "#3730a3"),  # indigo
    ("#6ee7b7", "#d1fae5", "#065f46"),  # emerald
    ("#fbbf24", "#fef3c7", "#92400e"),  # amber
    ("#fb7185", "#ffe4e6", "#9f1239"),  # rose
    ("#e879f9", "#fae8ff", "#86198f"),  # fuchsia
    ("#38bdf8", "#e0f2fe", "#075985"),  # sky
]


def find_font():
    candidates = [
        os.path.join(os.environ.get("LOCALAPPDATA", ""), "Microsoft", "Windows", "Fonts", "Inter-VF.ttf"),
        r"C:\Windows\Fonts\Inter-VF.ttf",
        "/usr/share/fonts/truetype/inter/Inter-VF.ttf",
        "/Library/Fonts/Inter-VF.ttf",
    ]
    for path in candidates:
        if os.path.exists(path):
            return path
    sys.exit("Inter-VF.ttf not found; install the Inter font.")


def font(path, size, weight):
    f = ImageFont.truetype(path, size * S)
    axes = [a["name"] for a in f.get_variation_axes()]
    values = {b"Weight": weight, b"Optical size": max(14, min(32, size))}
    f.set_variation_by_axes([values.get(a, weight) for a in axes])
    return f


def load_tree():
    with zipfile.ZipFile(XMIND) as z:
        sheet = json.loads(z.read("content.json").decode("utf-8"))[0]
    root = sheet["rootTopic"]

    def kids(t):
        return t.get("children", {}).get("attached", [])

    return root["title"], [(m["title"], [c["title"] for c in kids(m)]) for m in kids(root)]


def s(v):
    return int(round(v * S))


def rbox(d, x0, y0, x1, y1, r, fill, outline=None, width=0):
    d.rounded_rectangle([s(x0), s(y0), s(x1), s(y1)], radius=s(r), fill=fill, outline=outline, width=s(width))


def text_w(d, txt, f):
    return d.textlength(txt, font=f) / S


def draw_text(d, cx, cy, txt, f, fill, anchor="mm"):
    d.text((s(cx), s(cy)), txt, font=f, fill=fill, anchor=anchor)


def bezier(d, p0, p3, color, width=2.5):
    (x0, y0), (x3, y3) = p0, p3
    p1 = (x0, y0 + (y3 - y0) * 0.75)
    p2 = (x0 + (x3 - x0) * 0.25, y3)
    pts = []
    for i in range(101):
        t = i / 100
        a, b, c, e = (1 - t) ** 3, 3 * (1 - t) ** 2 * t, 3 * (1 - t) * t ** 2, t ** 3
        pts.append((s(a * x0 + b * p1[0] + c * p2[0] + e * x3), s(a * y0 + b * p1[1] + c * p2[1] + e * y3)))
    d.line(pts, fill=color, width=s(width), joint="curve")


def layout(branches):
    """Return the y centre of each main topic, stacked around the canvas middle."""
    heights = [len(leaves) * LEAF_PITCH for _, leaves in branches]
    total = sum(heights) + BLOCK_GAP * (len(heights) - 1)
    y = (CENTER[1] + CENTER[3]) / 2 - total / 2
    centers = []
    for h in heights:
        centers.append(y + h / 2)
        y += h + BLOCK_GAP
    return centers


def edge_start(side, y, rank):
    """Point on the central box where a branch line starts."""
    x0, y0, x1, y1 = CENTER
    if y0 + 20 < y < y1 - 20:
        return (x1, y) if side == "right" else (x0, y)
    edge_y = y0 if y < (y0 + y1) / 2 else y1
    if side == "right":
        return (x1 - 58 - 68 * rank, edge_y)
    return (x0 + 92 + 60 * rank, edge_y)


def main():
    font_path = find_font()
    f_title = font(font_path, 34, 700)
    f_main = font(font_path, 22, 600)
    f_leaf = font(font_path, 17, 400)

    title, branches = load_tree()
    img = Image.new("RGB", (W * S, H * S), "white")
    d = ImageDraw.Draw(img)

    sides = [("right", branches[:RIGHT_COUNT], 0), ("left", branches[RIGHT_COUNT:], RIGHT_COUNT)]
    mid = (CENTER[1] + CENTER[3]) / 2

    for side, group, offset in sides:
        ys = layout(group)
        # rank = order of distance from the central box, separately above and below;
        # branches level with the box connect to its side and are not ranked
        level = [CENTER[1] + 20 < y < CENTER[3] - 20 for y in ys]
        above = sorted([i for i, y in enumerate(ys) if y < mid and not level[i]], key=lambda i: -ys[i])
        below = sorted([i for i, y in enumerate(ys) if y >= mid and not level[i]], key=lambda i: ys[i])
        rank = {i: r for lst in (above, below) for r, i in enumerate(lst)}
        rank.update({i: 0 for i, lv in enumerate(level) if lv})

        for i, ((name, leaves), cy) in enumerate(zip(group, ys)):
            main_c, leaf_fill, leaf_text = PALETTE[(offset + i) % len(PALETTE)]
            mw = text_w(d, name, f_main) + 40
            if side == "right":
                mx0, mx1 = MAIN_X_RIGHT, MAIN_X_RIGHT + mw
                target = (mx0, cy)
            else:
                mx0, mx1 = MAIN_X_LEFT_END - mw, MAIN_X_LEFT_END
                target = (mx1, cy)

            start = edge_start(side, cy, rank[i])
            if start[1] == cy:
                d.line([s(start[0]), s(cy), s(target[0]), s(cy)], fill=main_c, width=s(2.5))
            else:
                bezier(d, start, target, main_c)

            # bracket + leaves
            ly0 = cy - (len(leaves) - 1) * LEAF_PITCH / 2
            bx = mx1 + BRACKET_X if side == "right" else mx0 - BRACKET_X
            d.line([s(mx1 if side == "right" else mx0), s(cy), s(bx), s(cy)], fill=main_c, width=s(1.5))
            if len(leaves) > 1:
                d.line([s(bx), s(ly0), s(bx), s(ly0 + (len(leaves) - 1) * LEAF_PITCH)], fill=main_c, width=s(1.5))
            for k, leaf in enumerate(leaves):
                ly = ly0 + k * LEAF_PITCH
                lw = text_w(d, leaf, f_leaf) + 24
                if side == "right":
                    lx0 = mx1 + MAIN_GAP
                    lx1 = lx0 + lw
                    d.line([s(bx), s(ly), s(lx0), s(ly)], fill=main_c, width=s(1.5))
                else:
                    lx1 = mx0 - MAIN_GAP
                    lx0 = lx1 - lw
                    d.line([s(lx1), s(ly), s(bx), s(ly)], fill=main_c, width=s(1.5))
                rbox(d, lx0, ly - LEAF_H / 2, lx1, ly + LEAF_H / 2, 6, leaf_fill)
                draw_text(d, (lx0 + lx1) / 2, ly, leaf, f_leaf, leaf_text)

            rbox(d, mx0, cy - MAIN_H / 2, mx1, cy + MAIN_H / 2, 8, main_c)
            draw_text(d, (mx0 + mx1) / 2, cy, name, f_main, "#111111")

    # central topic, drawn last so it sits above the branch lines
    rbox(d, *CENTER, 14, "white", outline="#111111", width=3)
    lines = wrap_title(d, title, f_title, CENTER[2] - CENTER[0] - 80)
    cx, cy = (CENTER[0] + CENTER[2]) / 2, mid
    for n, line in enumerate(lines):
        draw_text(d, cx, cy + (n - (len(lines) - 1) / 2) * 43, line, f_title, "#111111")

    img.resize((W, H), Image.LANCZOS).save(OUT, optimize=True)
    print("wrote", os.path.normpath(OUT))


def wrap_title(d, title, f, max_w):
    head, _, tail = title.partition(" – ")
    lines = [head]
    words, cur = ("– " + tail).split() if tail else [], ""
    for w in words:
        trial = (cur + " " + w).strip()
        if text_w(d, trial, f) <= max_w or not cur:
            cur = trial
        else:
            lines.append(cur)
            cur = w
    if cur:
        lines.append(cur)
    return lines


if __name__ == "__main__":
    main()
