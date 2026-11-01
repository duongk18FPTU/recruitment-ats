"""srsdiagram: vẽ sơ đồ SRS theo một kiểu thống nhất (use case, screen flow, sequence, state, ERD).

Mỗi sơ đồ ra 3 file: .svg, .png (gấp đôi) và .drawio (sửa tiếp bằng draw.io).
Không sửa STYLE trong từng dự án; muốn đổi kiểu thì sửa skill ve-diagram-srs cho cả nhóm.
Cần: Python 3, Pillow, Playwright cho Python (Chromium), font Liberation Sans (gói fonts-liberation).
"""
import math
import os
import re
import subprocess
from html import escape

from PIL import ImageFont

VERSION = '1.0'

# ============================================================== kiểu vẽ cố định
STYLE = {
    'font_family': 'Liberation Sans',
    'seq_fs': 12,          # chữ trong sequence
    'state_fs': 13,        # tên trạng thái (đậm)
    'label_fs': 12,        # nhãn trên đường nối
    'uc_fs': 13,           # tên use case
    'uc_title_fs': 17,     # tên khung hệ thống (đậm)
    'actor_fs': 14,        # tên actor (đậm)
    'screen_fs': 13,       # tên màn
    'erd_fs': 11,          # chữ trong bảng ERD
    'line': '#000000',
    'uc_line': '#334155',
    'uc_box_fill': '#F8FAFC',
    'erd_main': ('#DAE8FC', '#6C8EBF'),
    'erd_ext': ('#F5F5F5', '#666666'),
    'screen_colors': {
        'van_hanh': ('#e1d5e7', '#9673a6'),
        'tien': ('#ffe6cc', '#d79b00'),
        'quay': ('#fff2cc', '#d6b656'),
        'cong_khai': ('#d5e8d4', '#82b366'),
        'dang_nhap': ('#dae8fc', '#6c8ebf'),
    },
    'menu_line': '#888888',
    'png_scale': 2,
}


# ============================================================== chữ
def _font_file(bold=False, italic=False):
    k = (bold, italic)
    style = {(False, False): 'Regular', (True, False): 'Bold', (False, True): 'Italic', (True, True): 'Bold Italic'}[k]
    name = {(False, False): 'Regular', (True, False): 'Bold', (False, True): 'Italic', (True, True): 'BoldItalic'}[k]
    p = f'/usr/share/fonts/truetype/liberation/LiberationSans-{name}.ttf'
    if os.path.exists(p):
        return p
    try:
        out = subprocess.run(['fc-match', '-f', '%{file}', f'Liberation Sans:style={style}'], capture_output=True, text=True).stdout
        if 'Liberation' in out and os.path.exists(out):
            return out
    except Exception:
        pass
    # Windows, macOS: Arial có cùng số đo chữ với Liberation Sans nên bố cục không đổi
    win = {(False, False): 'arial.ttf', (True, False): 'arialbd.ttf', (False, True): 'ariali.ttf', (True, True): 'arialbi.ttf'}[k]
    mac = {(False, False): 'Arial.ttf', (True, False): 'Arial Bold.ttf', (False, True): 'Arial Italic.ttf',
           (True, True): 'Arial Bold Italic.ttf'}[k]
    for p in (os.path.join(os.environ.get('WINDIR', r'C:\Windows'), 'Fonts', win),
              f'/System/Library/Fonts/Supplemental/{mac}', f'/Library/Fonts/{mac}'):
        if os.path.exists(p):
            return p
    raise RuntimeError('Thiếu font Liberation Sans: cài gói fonts-liberation rồi chạy lại để chữ đo đúng kích thước.')


_fcache = {}


def font(size, bold=False, italic=False):
    k = (size, bold, italic)
    if k not in _fcache:
        _fcache[k] = ImageFont.truetype(_font_file(bold, italic), size)
    return _fcache[k]


def tw(text, size=13, bold=False, italic=False):
    return font(size, bold, italic).getlength(text)


def lh(size):
    return round(size * 1.25)


def _atoms(word):
    if '/' not in word or len(word) < 18:
        return [word]
    parts, cur = [], ''
    for ch in word:
        if ch == '/' and cur:
            parts.append(cur)
            cur = ch
        else:
            cur += ch
    parts.append(cur)
    return parts


def wrap(text, maxw, fs):
    out = []
    for para in text.split('\n'):
        cur = ''
        for word in para.split(' '):
            first = True
            for a in _atoms(word):
                sep = ' ' if (first and cur) else ''
                t = cur + sep + a
                if tw(t, fs) <= maxw or not cur:
                    cur = t
                else:
                    out.append(cur)
                    cur = a
                first = False
        out.append(cur)
    return out


# ============================================================== mô hình hình học
class Doc:
    def __init__(self):
        self.items = []
        self.n = 0

    def _add(self, **kw):
        self.n += 1
        kw['id'] = f'c{self.n}'
        self.items.append(kw)
        return kw['id']

    def rect(self, x, y, w, h, fill='#ffffff', stroke='#000000', sw=1, r=0, dashed=False, lines=None, fs=13,
             bold=False, italic=False, color='#000000', align='center', valign='middle', pad=6):
        return self._add(k='rect', x=x, y=y, w=w, h=h, fill=fill, stroke=stroke, sw=sw, r=r, dashed=dashed,
                         lines=lines or [], fs=fs, bold=bold, italic=italic, color=color, align=align, valign=valign, pad=pad)

    def ellipse(self, x, y, w, h, fill='#ffffff', stroke='#000000', sw=1, lines=None, fs=13, bold=False, color='#000000'):
        return self._add(k='ellipse', x=x, y=y, w=w, h=h, fill=fill, stroke=stroke, sw=sw, lines=lines or [], fs=fs,
                         bold=bold, color=color)

    def text(self, x, y, lines, fs=13, bold=False, italic=False, anchor='start', color='#000000', bg=None):
        if isinstance(lines, str):
            lines = [lines]
        w = max([tw(l, fs, bold, italic) for l in lines] + [1])
        return self._add(k='text', x=x, y=y, lines=lines, fs=fs, bold=bold, italic=italic, anchor=anchor, color=color,
                         bg=bg, w=w, h=len(lines) * lh(fs))

    def actor(self, cx, top, label, fs=13, bold=False):
        return self._add(k='actor', cx=cx, top=top, label=label, fs=fs, bold=bold)

    def icon(self, kind, cx, top, label, fs=13, bold=False):
        return self._add(k=kind, cx=cx, top=top, label=label, fs=fs, bold=bold)

    def start_state(self, cx, cy, r=10):
        return self._add(k='start', cx=cx, cy=cy, r=r)

    def end_state(self, cx, cy, r=11):
        return self._add(k='end', cx=cx, cy=cy, r=r)

    def frame(self, x, y, w, h, op, guard=None, fs=13, fill=None, name=None):
        return self._add(k='frame', x=x, y=y, w=w, h=h, op=op, guard=guard, fs=fs, fill=fill, name=name)

    def note(self, x, y, w, h, lines, fs=12):
        return self._add(k='note', x=x, y=y, w=w, h=h, lines=lines, fs=fs)

    def table(self, x, y, w, name, rows, fs=11, fill='#DAE8FC', stroke='#6C8EBF', dashed=False):
        rh = lh(fs) + 4
        h = rh * (1 + len(rows)) + 4
        return self._add(k='table', x=x, y=y, w=w, h=h, name=name, rows=rows, fs=fs, fill=fill, stroke=stroke,
                         dashed=dashed, rh=rh)

    def line(self, pts, dashed=False, start=None, end=None, sw=1, color='#000000', dash='6 4'):
        return self._add(k='line', pts=[tuple(p) for p in pts], dashed=dashed, start=start, end=end, sw=sw, color=color,
                         dash=dash)

    def bounds(self):
        xs, ys = [], []
        for it in self.items:
            k = it['k']
            if k in ('rect', 'ellipse', 'frame', 'note', 'table'):
                xs += [it['x'], it['x'] + it['w']]
                ys += [it['y'], it['y'] + it['h']]
            elif k == 'text':
                x0 = it['x'] - (it['w'] / 2 if it['anchor'] == 'middle' else it['w'] if it['anchor'] == 'end' else 0)
                xs += [x0, x0 + it['w']]
                ys += [it['y'], it['y'] + it['h']]
            elif k in ('actor', 'boundary', 'control', 'entity', 'database'):
                lw = max(tw(l, it['fs'], it['bold']) for l in it['label'].split('\n'))
                xs += [it['cx'] - max(24, lw / 2), it['cx'] + max(24, lw / 2)]
                ys += [it['top'], it['top'] + head_h(it)]
            elif k in ('start', 'end'):
                xs += [it['cx'] - it['r'], it['cx'] + it['r']]
                ys += [it['cy'] - it['r'], it['cy'] + it['r']]
            elif k == 'line':
                for p in it['pts']:
                    xs.append(p[0])
                    ys.append(p[1])
        return min(xs), min(ys), max(xs), max(ys)

    def svg(self, pad=16):
        x0, y0, x1, y1 = self.bounds()
        W, H = x1 - x0 + 2 * pad, y1 - y0 + 2 * pad
        ox, oy = pad - x0, pad - y0
        o = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{W:.0f}" height="{H:.0f}" viewBox="0 0 {W:.0f} {H:.0f}" '
             f'font-family="{STYLE["font_family"]}, Arial, Helvetica, sans-serif">',
             '<rect width="100%" height="100%" fill="#ffffff"/>', f'<g transform="translate({ox:.1f},{oy:.1f})">']
        for it in self.items:
            o.append(_SVG[it['k']](it))
        o.append('</g></svg>')
        return '\n'.join(o)

    def drawio(self, name='Page-1'):
        cells = ['<mxCell id="0"/>', '<mxCell id="1" parent="0"/>']
        for it in self.items:
            cells += _DIO[it['k']](it)
        return ('<mxfile host="app.diagrams.net"><diagram id="d1" name="' + escape(name) + '">'
                '<mxGraphModel dx="1200" dy="800" grid="1" gridSize="10" guides="1" tooltips="1" connect="1" arrows="1" '
                'fold="1" page="0" pageScale="1" pageWidth="1100" pageHeight="850" math="0" shadow="0"><root>'
                + ''.join(cells) + '</root></mxGraphModel></diagram></mxfile>')


def head_h(it):
    return 40 + 4 + len(it['label'].split('\n')) * lh(it['fs'])


# ------------------------------------------------------------- SVG
def _t(x, y, s, fs, bold=False, italic=False, anchor='start', color='#000000'):
    return (f'<text x="{x:.1f}" y="{y:.1f}" font-size="{fs}" fill="{color}" text-anchor="{anchor}"'
            + (' font-weight="bold"' if bold else '') + (' font-style="italic"' if italic else '')
            + f' xml:space="preserve">{escape(s)}</text>')


def _block(x, ytop, lines, fs, bold=False, italic=False, anchor='start', color='#000000'):
    return ''.join(_t(x, ytop + i * lh(fs) + fs * 0.95, l, fs, bold, italic, anchor, color) for i, l in enumerate(lines))


def _s_rect(it):
    d = ' stroke-dasharray="6 4"' if it['dashed'] else ''
    s = (f'<rect x="{it["x"]:.1f}" y="{it["y"]:.1f}" width="{it["w"]:.1f}" height="{it["h"]:.1f}" rx="{it["r"]}" '
         f'fill="{it["fill"]}" stroke="{it["stroke"]}" stroke-width="{it["sw"]}"{d}/>')
    if it['lines']:
        th = len(it['lines']) * lh(it['fs'])
        ytop = it['y'] + (it['h'] - th) / 2 if it['valign'] == 'middle' else it['y'] + it['pad']
        if it['align'] == 'center':
            s += _block(it['x'] + it['w'] / 2, ytop, it['lines'], it['fs'], it['bold'], it['italic'], 'middle', it['color'])
        else:
            s += _block(it['x'] + it['pad'], ytop, it['lines'], it['fs'], it['bold'], it['italic'], 'start', it['color'])
    return s


def _s_ellipse(it):
    s = (f'<ellipse cx="{it["x"] + it["w"] / 2:.1f}" cy="{it["y"] + it["h"] / 2:.1f}" rx="{it["w"] / 2:.1f}" '
         f'ry="{it["h"] / 2:.1f}" fill="{it["fill"]}" stroke="{it["stroke"]}" stroke-width="{it["sw"]}"/>')
    if it['lines']:
        th = len(it['lines']) * lh(it['fs'])
        s += _block(it['x'] + it['w'] / 2, it['y'] + (it['h'] - th) / 2, it['lines'], it['fs'], it['bold'], False,
                    'middle', it['color'])
    return s


def _s_text(it):
    s = ''
    if it['bg']:
        x0 = it['x'] - (it['w'] / 2 if it['anchor'] == 'middle' else it['w'] if it['anchor'] == 'end' else 0)
        s += f'<rect x="{x0 - 2:.1f}" y="{it["y"] - 1:.1f}" width="{it["w"] + 4:.1f}" height="{it["h"] + 2:.1f}" fill="{it["bg"]}"/>'
    return s + _block(it['x'], it['y'], it['lines'], it['fs'], it['bold'], it['italic'], it['anchor'], it['color'])


def _label_below(it, ytop):
    return _block(it['cx'], ytop, it['label'].split('\n'), it['fs'], it['bold'], False, 'middle')


def _s_actor(it):
    cx, t = it['cx'], it['top']
    st = 'stroke="#000" stroke-width="1.3" fill="none"'
    return (f'<circle cx="{cx}" cy="{t + 6}" r="6" fill="#fff" stroke="#000" stroke-width="1.3"/>'
            f'<line x1="{cx}" y1="{t + 12}" x2="{cx}" y2="{t + 27}" {st}/>'
            f'<line x1="{cx - 11}" y1="{t + 17}" x2="{cx + 11}" y2="{t + 17}" {st}/>'
            f'<line x1="{cx}" y1="{t + 27}" x2="{cx - 10}" y2="{t + 40}" {st}/>'
            f'<line x1="{cx}" y1="{t + 27}" x2="{cx + 10}" y2="{t + 40}" {st}/>') + _label_below(it, t + 44)


def _s_boundary(it):
    cx, t = it['cx'], it['top']
    st = 'stroke="#000" stroke-width="1.3" fill="none"'
    return (f'<line x1="{cx - 19}" y1="{t + 6}" x2="{cx - 19}" y2="{t + 34}" {st}/>'
            f'<line x1="{cx - 19}" y1="{t + 20}" x2="{cx - 9}" y2="{t + 20}" {st}/>'
            f'<circle cx="{cx + 5}" cy="{t + 20}" r="14" fill="#fff" stroke="#000" stroke-width="1.3"/>') + _label_below(it, t + 44)


def _s_control(it):
    cx, t = it['cx'], it['top']
    return (f'<circle cx="{cx}" cy="{t + 22}" r="14" fill="#fff" stroke="#000" stroke-width="1.3"/>'
            f'<path d="M{cx + 5},{t + 3} L{cx - 1},{t + 8} L{cx + 5},{t + 13}" fill="none" stroke="#000" stroke-width="1.3"/>'
            ) + _label_below(it, t + 44)


def _s_entity(it):
    cx, t = it['cx'], it['top']
    return (f'<circle cx="{cx}" cy="{t + 19}" r="14" fill="#fff" stroke="#000" stroke-width="1.3"/>'
            f'<line x1="{cx - 15}" y1="{t + 35}" x2="{cx + 15}" y2="{t + 35}" stroke="#000" stroke-width="1.3"/>'
            ) + _label_below(it, t + 44)


def _s_database(it):
    cx, t = it['cx'], it['top']
    w, e = 30, 5
    x = cx - w / 2
    return (f'<path d="M{x},{t + 6 + e} L{x},{t + 34} A{w / 2},{e} 0 0 0 {x + w},{t + 34} L{x + w},{t + 6 + e}" '
            f'fill="#fff" stroke="#000" stroke-width="1.3"/>'
            f'<ellipse cx="{cx}" cy="{t + 6 + e}" rx="{w / 2}" ry="{e}" fill="#fff" stroke="#000" stroke-width="1.3"/>'
            ) + _label_below(it, t + 44)


def _s_start(it):
    return f'<circle cx="{it["cx"]}" cy="{it["cy"]}" r="{it["r"]}" fill="#000"/>'


def _s_end(it):
    return (f'<circle cx="{it["cx"]}" cy="{it["cy"]}" r="{it["r"]}" fill="#fff" stroke="#000" stroke-width="1.3"/>'
            f'<circle cx="{it["cx"]}" cy="{it["cy"]}" r="{it["r"] - 4}" fill="#000"/>')


def tag_w(op, fs, name=None):
    return tw(op, fs, True) + (tw(' ' + name, fs) if name else 0) + 22


def _s_frame(it):
    x, y, w, h, fs = it['x'], it['y'], it['w'], it['h'], it['fs']
    tg = tag_w(it['op'], fs, it.get('name'))
    s = f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" fill="{it["fill"] or "none"}" stroke="#000" stroke-width="1.1"/>'
    s += (f'<path d="M{x:.1f},{y:.1f} L{x + tg:.1f},{y:.1f} L{x + tg:.1f},{y + 14:.1f} L{x + tg - 6:.1f},{y + 20:.1f} '
          f'L{x:.1f},{y + 20:.1f} Z" fill="#fff" stroke="#000" stroke-width="1.1"/>')
    s += _t(x + 7, y + 14.5, it['op'], fs, True)
    if it.get('name'):
        s += _t(x + 7 + tw(it['op'], fs, True), y + 14.5, ' ' + it['name'], fs)
    if it['guard']:
        gx = x + tg + 8
        s += f'<rect x="{gx - 2:.1f}" y="{y + 2:.1f}" width="{tw(it["guard"], fs) + 4:.1f}" height="{lh(fs) + 1:.1f}" fill="#fff"/>'
        s += _t(gx, y + 15, it['guard'], fs)
    return s


def _s_note(it):
    x, y, w, h, c = it['x'], it['y'], it['w'], it['h'], 8
    return (f'<path d="M{x},{y} L{x + w - c},{y} L{x + w},{y + c} L{x + w},{y + h} L{x},{y + h} Z" fill="#fff" stroke="#000" stroke-width="1"/>'
            f'<path d="M{x + w - c},{y} L{x + w - c},{y + c} L{x + w},{y + c}" fill="none" stroke="#000" stroke-width="1"/>'
            ) + _block(x + 6, y + 5, it['lines'], it['fs'])


def _s_table(it):
    x, y, w, h, rh, fs = it['x'], it['y'], it['w'], it['h'], it['rh'], it['fs']
    d = ' stroke-dasharray="6 4"' if it['dashed'] else ''
    s = f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{it["fill"]}" stroke="{it["stroke"]}" stroke-width="1.2"{d}/>'
    s += _t(x + w / 2, y + rh * 0.5 + fs * 0.38 + 2, it['name'], fs, True, anchor='middle')
    s += f'<line x1="{x}" y1="{y + rh + 2}" x2="{x + w}" y2="{y + rh + 2}" stroke="{it["stroke"]}" stroke-width="1"{d}/>'
    cx = x + 8 + max([tw(k, fs, True) for k, _ in it['rows']] + [0]) + 10
    for i, (k, c) in enumerate(it['rows']):
        yy = y + rh + 2 + i * rh + rh * 0.5 + fs * 0.38 + 2
        if k:
            s += _t(x + 8, yy, k, fs, True)
        s += _t(cx, yy, c, fs)
    return s


def _marker(kind, tip, prev, color):
    if not kind or kind == 'none':
        return ''
    dx, dy = tip[0] - prev[0], tip[1] - prev[1]
    L = math.hypot(dx, dy) or 1
    ux, uy = dx / L, dy / L
    px, py = -uy, ux
    X, Y = tip

    def P(a, b):
        return (X - ux * a + px * b, Y - uy * a + py * b)

    def pt(p):
        return f'{p[0]:.1f},{p[1]:.1f}'
    st = f'stroke="{color}" stroke-width="1.2"'
    if kind == 'block':
        return f'<path d="M{pt(P(0, 0))} L{pt(P(10, 4.5))} L{pt(P(10, -4.5))} Z" fill="{color}" {st}/>'
    if kind == 'open':
        return f'<path d="M{pt(P(10, 5))} L{pt(P(0, 0))} L{pt(P(10, -5))}" fill="none" {st}/>'
    if kind == 'tri_open':
        return f'<path d="M{pt(P(0, 0))} L{pt(P(14, 8))} L{pt(P(14, -8))} Z" fill="#fff" {st}/>'
    out = ''
    if kind in ('ERmandOne', 'ERoneToMany'):
        a = 6 if kind == 'ERmandOne' else 16
        out += f'<path d="M{pt(P(a, 6))} L{pt(P(a, -6))}" {st}/>'
        if kind == 'ERmandOne':
            out += f'<path d="M{pt(P(11, 6))} L{pt(P(11, -6))}" {st}/>'
    if kind in ('ERzeroToMany', 'ERoneToMany'):
        out += f'<path d="M{pt(P(11, 0))} L{pt(P(0, 6))} M{pt(P(11, 0))} L{pt(P(0, -6))} M{pt(P(11, 0))} L{pt(P(0, 0))}" fill="none" {st}/>'
        if kind == 'ERzeroToMany':
            c = P(17, 0)
            out += f'<circle cx="{c[0]:.1f}" cy="{c[1]:.1f}" r="4.5" fill="#fff" {st}/>'
    if kind == 'ERzeroToOne':
        out += f'<path d="M{pt(P(6, 6))} L{pt(P(6, -6))}" {st}/>'
        c = P(14, 0)
        out += f'<circle cx="{c[0]:.1f}" cy="{c[1]:.1f}" r="4.5" fill="#fff" {st}/>'
    return out


def _s_line(it):
    pts = it['pts']
    d = f' stroke-dasharray="{it["dash"]}"' if it['dashed'] else ''
    s = f'<path d="M{" L".join(f"{x:.1f},{y:.1f}" for x, y in pts)}" fill="none" stroke="{it["color"]}" stroke-width="{it["sw"]}"{d}/>'
    return s + _marker(it['end'], pts[-1], pts[-2], it['color']) + _marker(it['start'], pts[0], pts[1], it['color'])


_SVG = {'rect': _s_rect, 'ellipse': _s_ellipse, 'text': _s_text, 'actor': _s_actor, 'boundary': _s_boundary,
        'control': _s_control, 'entity': _s_entity, 'database': _s_database, 'start': _s_start, 'end': _s_end,
        'frame': _s_frame, 'note': _s_note, 'table': _s_table, 'line': _s_line}


# ------------------------------------------------------------- draw.io
def _v(lines, bold=False):
    s = '<br>'.join(escape(l) for l in lines)
    return escape(f'<b>{s}</b>' if bold else s, quote=True)


def _g(x, y, w, h):
    return f'<mxGeometry x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" as="geometry"/>'


def _c(id_, value, style, geo):
    return f'<mxCell id="{id_}" value="{value}" style="{style}" vertex="1" parent="1">{geo}</mxCell>'


def _d_rect(it):
    st = (f'rounded={1 if it["r"] else 0};arcSize={it["r"] * 2};absoluteArcSize=1;whiteSpace=wrap;html=1;'
          f'fillColor={it["fill"]};strokeColor={it["stroke"]};strokeWidth={it["sw"]};fontSize={it["fs"]};'
          f'fontColor={it["color"]};align={it["align"]};verticalAlign={"middle" if it["valign"] == "middle" else "top"};'
          + ('dashed=1;' if it['dashed'] else '') + ('fontStyle=1;' if it['bold'] else 'fontStyle=2;' if it['italic'] else ''))
    return [_c(it['id'], _v(it['lines']), st, _g(it['x'], it['y'], it['w'], it['h']))]


def _d_ellipse(it):
    st = (f'ellipse;whiteSpace=wrap;html=1;fillColor={it["fill"]};strokeColor={it["stroke"]};fontSize={it["fs"]};'
          f'fontColor={it["color"]};' + ('fontStyle=1;' if it['bold'] else ''))
    return [_c(it['id'], _v(it['lines']), st, _g(it['x'], it['y'], it['w'], it['h']))]


def _d_text(it):
    x0 = it['x'] - (it['w'] / 2 if it['anchor'] == 'middle' else it['w'] if it['anchor'] == 'end' else 0)
    al = {'start': 'left', 'middle': 'center', 'end': 'right'}[it['anchor']]
    st = (f'text;html=1;whiteSpace=nowrap;align={al};verticalAlign=top;spacing=0;fontSize={it["fs"]};fontColor={it["color"]};'
          + (f'labelBackgroundColor={it["bg"]};' if it['bg'] else '')
          + ('fontStyle=1;' if it['bold'] else 'fontStyle=2;' if it['italic'] else ''))
    return [_c(it['id'], _v(it['lines']), st, _g(x0, it['y'], it['w'] + 4, it['h']))]


def _icon(it, shape, w, h, dy=0):
    st = (f'{shape};html=1;verticalLabelPosition=bottom;verticalAlign=top;labelPosition=center;align=center;'
          f'outlineConnect=0;fontSize={it["fs"]};' + ('fontStyle=1;' if it['bold'] else ''))
    return [_c(it['id'], _v(it['label'].split('\n')), st, _g(it['cx'] - w / 2, it['top'] + dy, w, h))]


def _d_start(it):
    r = it['r']
    return [_c(it['id'], '', 'ellipse;html=1;shape=startState;fillColor=#000000;strokeColor=#000000;',
               _g(it['cx'] - r, it['cy'] - r, 2 * r, 2 * r))]


def _d_end(it):
    r = it['r']
    return [_c(it['id'], '', 'ellipse;html=1;shape=endState;fillColor=#000000;strokeColor=#000000;',
               _g(it['cx'] - r, it['cy'] - r, 2 * r, 2 * r))]


def _d_frame(it):
    tg = tag_w(it['op'], it['fs'], it.get('name'))
    st = (f'shape=umlFrame;whiteSpace=wrap;html=1;pointerEvents=0;width={tg:.0f};height=20;fontSize={it["fs"]};'
          f'align=left;verticalAlign=top;spacingLeft=4;spacingTop=-2;'
          + (f'fillColor={it["fill"]};' if it['fill'] else 'fillColor=none;swimlaneFillColor=none;'))
    val = f"<b>{escape(it['op'])}</b>" + (' ' + escape(it['name']) if it.get('name') else '')
    out = [_c(it['id'], escape(val, quote=True), st, _g(it['x'], it['y'], it['w'], it['h']))]
    if it['guard']:
        out.append(_c(it['id'] + 'g', escape(escape(it['guard']), quote=True),
                      f'text;html=1;align=left;verticalAlign=top;spacing=0;fontSize={it["fs"]};labelBackgroundColor=#ffffff;',
                      _g(it['x'] + tg + 8, it['y'] + 2, tw(it['guard'], it['fs']) + 6, lh(it['fs']))))
    return out


def _d_note(it):
    st = f'shape=note;whiteSpace=wrap;html=1;size=8;align=left;verticalAlign=top;spacingLeft=4;fontSize={it["fs"]};'
    return [_c(it['id'], _v(it['lines']), st, _g(it['x'], it['y'], it['w'], it['h']))]


def _d_table(it):
    rows = ''.join(f'<tr><td style="padding:1px 8px 1px 6px;"><b>{escape(k)}</b></td><td style="padding:1px 6px;">{escape(c)}</td></tr>'
                   for k, c in it['rows'])
    html = (f'<div style="font-weight:bold;text-align:center;padding:3px 0;border-bottom:1px solid {it["stroke"]};">'
            f'{escape(it["name"])}</div><table style="width:100%;font-size:{it["fs"]}px;border-collapse:collapse;">{rows}</table>')
    st = (f'rounded=0;whiteSpace=wrap;html=1;verticalAlign=top;align=left;spacing=0;overflow=fill;fillColor={it["fill"]};'
          f'strokeColor={it["stroke"]};fontSize={it["fs"]};' + ('dashed=1;' if it['dashed'] else ''))
    return [_c(it['id'], escape(html, quote=True), st, _g(it['x'], it['y'], it['w'], it['h']))]


_ARW = {None: ('none', 0), 'none': ('none', 0), 'block': ('block', 1), 'open': ('open', 0), 'tri_open': ('block', 0),
        'ERmandOne': ('ERmandOne', 0), 'ERzeroToMany': ('ERzeroToMany', 0), 'ERoneToMany': ('ERoneToMany', 0),
        'ERzeroToOne': ('ERzeroToOne', 0)}


def _d_line(it):
    e, ef = _ARW[it['end']]
    s, sf = _ARW[it['start']]
    st = (f'html=1;rounded=0;endArrow={e};endFill={ef};startArrow={s};startFill={sf};strokeColor={it["color"]};'
          f'strokeWidth={it["sw"]};' + ('dashed=1;' if it['dashed'] else '') + ('endSize=14;' if it['end'] == 'tri_open' else ''))
    pts = it['pts']
    mid = ''.join(f'<mxPoint x="{x:.1f}" y="{y:.1f}"/>' for x, y in pts[1:-1])
    geo = (f'<mxGeometry relative="1" as="geometry"><mxPoint x="{pts[0][0]:.1f}" y="{pts[0][1]:.1f}" as="sourcePoint"/>'
           f'<mxPoint x="{pts[-1][0]:.1f}" y="{pts[-1][1]:.1f}" as="targetPoint"/>'
           + (f'<Array as="points">{mid}</Array>' if mid else '') + '</mxGeometry>')
    return [f'<mxCell id="{it["id"]}" value="" style="{st}" edge="1" parent="1">{geo}</mxCell>']


_DIO = {'rect': _d_rect, 'ellipse': _d_ellipse, 'text': _d_text,
        'actor': lambda it: _icon(it, 'shape=umlActor', 22, 40),
        'boundary': lambda it: _icon(it, 'shape=umlBoundary;whiteSpace=wrap', 40, 30, 5),
        'control': lambda it: _icon(it, 'ellipse;shape=umlControl;whiteSpace=wrap', 28, 32, 4),
        'entity': lambda it: _icon(it, 'ellipse;shape=umlEntity;whiteSpace=wrap', 30, 30, 4),
        'database': lambda it: _icon(it, 'shape=cylinder3;whiteSpace=wrap;boundedLbl=1;backgroundOutline=1;size=5', 30, 34, 6),
        'start': _d_start, 'end': _d_end, 'frame': _d_frame, 'note': _d_note, 'table': _d_table, 'line': _d_line}


# ============================================================== xuất file
def save(diagrams, outdir='out'):
    """diagrams: {tên file: (Doc, tên trang)}. Ghi .svg, .drawio và .png cho từng sơ đồ."""
    os.makedirs(outdir, exist_ok=True)
    svgs = []
    for name, (doc, title) in diagrams.items():
        base = os.path.join(outdir, name)
        open(base + '.svg', 'w', encoding='utf-8').write(doc.svg())
        open(base + '.drawio', 'w', encoding='utf-8').write(doc.drawio(title))
        svgs.append(base + '.svg')
    from playwright.sync_api import sync_playwright
    with sync_playwright() as p:
        b = p.chromium.launch()
        pg = b.new_page(device_scale_factor=STYLE['png_scale'])
        for f in svgs:
            svg = open(f, encoding='utf-8').read()
            m = re.search(r'width="(\d+)" height="(\d+)"', svg)
            pg.set_viewport_size({'width': max(200, int(m.group(1))), 'height': max(100, int(m.group(2)))})
            pg.set_content(f'<html><body style="margin:0;background:#fff">{svg}</body></html>')
            pg.locator('svg').screenshot(path=f[:-4] + '.png')
        b.close()
    return [f[:-4] + '.png' for f in svgs]


# ============================================================== sequence diagram
class Seq:
    """Lifeline: (khoá, loại, nhãn); loại là actor | boundary | control | entity | database."""
    FS = STYLE['seq_fs']
    BAR = 10

    def __init__(self, title, parts, cap=400):
        self.title, self.parts, self.cap = title, parts, cap
        self.ix = {k: i for i, (k, _, _) in enumerate(parts)}
        self.kind = {k: kd for k, kd, _ in parts}
        self.ev = []

    def call(self, a, b, text):
        self.ev.append(['call', a, b, text])

    def reply(self, a, b, text):
        self.ev.append(['reply', a, b, text])

    def self_(self, a, text):
        self.ev.append(['self', a, a, text])

    def frag(self, op, guard=None):
        self.ev.append(['frag', op, guard])

    def else_(self, guard):
        self.ev.append(['else', guard])

    def end(self):
        self.ev.append(['end'])

    def ref(self, a, b, text):
        self.ev.append(['ref', a, b, text])

    def note(self, a, b, text, w=220):
        self.ev.append(['note', a, b, text, w])

    def build(self):
        FS, BAR, LH = self.FS, self.BAR, lh(self.FS)
        P, n = self.parts, len(self.parts)
        hw = [max(tw(l, FS, True) for l in lab.split('\n')) for _, _, lab in P]
        num, cons = 0, []
        for e in self.ev:
            if e[0] in ('call', 'reply', 'self'):
                num += 1
                e.append(wrap(f'{num}. {e[3]}', self.cap, FS))
                lw = max(tw(l, FS) for l in e[4])
                i, j = self.ix[e[1]], self.ix[e[2]]
                cons.append((i, i + 1, lw + 50) if e[0] == 'self' else (min(i, j), max(i, j), lw + 32))
            elif e[0] == 'note':
                lines = []
                for para in e[3].split('\n'):
                    lines += wrap(para, e[4] - 14, 12)
                e.append(lines)
                i, j = self.ix[e[1]], self.ix[e[2]]
                w = max(tw(l, 12) for l in lines) + 16
                if i == j:
                    if i + 1 < n:
                        cons.append((i, i + 1, w / 2 + 34))
                    if i > 0:
                        cons.append((i - 1, i, w / 2 + 34))
                else:
                    cons.append((min(i, j), max(i, j), w - 30))
            elif e[0] == 'ref':
                e.append(wrap(e[3], 360, FS))
                i, j = self.ix[e[1]], self.ix[e[2]]
                cons.append((min(i, j), max(i, j), max(tw(l, FS) for l in e[4])))
        x = [0.0] * n
        x[0] = 30 + max(hw[0] / 2, 24)
        for i in range(1, n):
            x[i] = x[i - 1] + max(hw[i - 1] / 2 + hw[i] / 2 + 34, 120)
        for i, j, need in sorted(cons, key=lambda c: c[1] - c[0]):
            if j < n and x[j] - x[i] < need:
                dlt = need - (x[j] - x[i])
                for k in range(j, n):
                    x[k] += dlt
        top_head = 34
        hh = max(head_h({'label': lab, 'fs': FS}) for _, _, lab in P)
        y_life = top_head + hh + 6
        cur = y_life + 18
        bars = {k: [] for k, _, _ in P}
        done, msgs, labels, frames, refs, notes, stack = [], [], [], [], [], [], []
        last_y = {k: None for k, _, _ in P}
        ev = self.ev

        def touch(*ks):
            for f in stack:
                for k in ks:
                    f['parts'].add(self.ix[k])

        def edge_x(k, side):
            st = bars[k]
            if not st:
                return x[self.ix[k]]
            return x[self.ix[k]] + (BAR / 2 if side > 0 else -BAR / 2) + (len(st) - 1) * 5

        def restore(f, yy):
            for k in bars:
                snap, curl = f['snap'][k], bars[k]
                for ent in curl:
                    if not any(ent is s0 for s0 in snap):
                        done.append((k, ent[0], yy, ent[1]))
                newl = []
                for i0, s0 in enumerate(snap):
                    if any(s0 is c for c in curl):
                        newl.append(s0)
                    else:
                        ne = [yy, s0[1], s0[2]]
                        snap[i0] = ne
                        newl.append(ne)
                bars[k] = newl

        def next_from(idx, k):
            for e2 in ev[idx + 1:]:
                if e2[0] in ('call', 'reply', 'self'):
                    return e2[1] == k
                if e2[0] in ('ref', 'note'):
                    return False
            return False

        for idx, e in enumerate(ev):
            t = e[0]
            if t in ('call', 'reply'):
                a, b, lines = e[1], e[2], e[4]
                ya = cur + len(lines) * LH + 3
                right = self.ix[b] > self.ix[a]
                if t == 'call':
                    if not bars[a] and self.kind[a] != 'actor':
                        bars[a].append([ya - 6, 0, True])
                    xs = edge_x(a, 1 if right else -1)
                    if self.kind[b] != 'actor':
                        bars[b].append([ya, len(bars[b]), False])
                    xe = edge_x(b, -1 if right else 1)
                else:
                    xs = edge_x(a, 1 if right else -1)
                    xe = edge_x(b, -1 if right else 1)
                    if bars[a]:
                        s0, lvl, _ = bars[a].pop()
                        done.append((a, s0, ya, lvl))
                    if bars[b] and bars[b][-1][2] and not next_from(idx, b):
                        s0, lvl, _ = bars[b].pop()
                        done.append((b, s0, ya + 6, lvl))
                msgs.append((t, [(xs, ya), (xe, ya)]))
                labels.append((min(xs, xe) + 6, cur, lines))
                touch(a, b)
                last_y[a] = last_y[b] = ya
                cur = ya + 11
            elif t == 'self':
                a, lines = e[1], e[4]
                y1 = cur + len(lines) * LH + 3
                y2 = y1 + 12
                if not bars[a] and self.kind[a] != 'actor':
                    bars[a].append([y1 - 6, 0, True])
                xs = edge_x(a, 1)
                msgs.append(('call', [(xs, y1), (xs + 26, y1), (xs + 26, y2), (xs, y2)]))
                labels.append((xs + 6, cur, lines))
                touch(a)
                last_y[a] = y2
                cur = y2 + 11
                if bars[a] and bars[a][-1][2] and not next_from(idx, a):
                    s0, lvl, _ = bars[a].pop()
                    done.append((a, s0, y2 + 6, lvl))
            elif t == 'frag':
                f = {'op': e[1], 'guard': e[2], 'top': cur, 'parts': set(), 'seps': [], 'depth': len(stack), 'inner': 0,
                     'snap': {k: list(v) for k, v in bars.items()}}
                stack.append(f)
                cur = cur + 28
            elif t == 'else':
                f = stack[-1]
                sy = cur + 1
                restore(f, sy)
                f['seps'].append((sy, e[1]))
                cur = sy + LH + 8
            elif t == 'end':
                f = stack.pop()
                f['bottom'] = cur - 2
                if f['op'] != 'alt':
                    restore(f, f['bottom'])
                if stack:
                    stack[-1]['parts'] |= f['parts']
                    stack[-1]['inner'] = max(stack[-1]['inner'], f['inner'] + 1)
                frames.append(f)
                cur = f['bottom'] + 12
            elif t == 'ref':
                a, b, lines = e[1], e[2], e[4]
                ia, ib = sorted((self.ix[a], self.ix[b]))
                h = 22 + len(lines) * LH + 6
                refs.append({'ia': ia, 'ib': ib, 'top': cur + 2, 'h': h, 'lines': lines})
                touch(a, b)
                if stack:
                    stack[-1]['inner'] = max(stack[-1]['inner'], 1)
                cur = cur + 2 + h + 12
            elif t == 'note':
                a, b, lines = e[1], e[2], e[5]
                ia, ib = sorted((self.ix[a], self.ix[b]))
                w = max(tw(l, 12) for l in lines) + 14
                h = len(lines) * lh(12) + 10
                if ia == ib:
                    nx = x[ia] - w / 2
                else:
                    nx, w = x[ia] - 20, max(w, x[ib] - x[ia] + 40)
                notes.append((nx, cur, w, h, lines))
                touch(a, b)
                cur += h + 12
        y_end = cur + 6
        for k, st in bars.items():
            while st:
                s0, lvl, _ = st.pop()
                y1 = (last_y[k] or s0) + 6
                if y1 > s0 + 4:
                    done.append((k, s0, y1, lvl))
        fr = []
        for f in frames:
            ps = f['parts'] or {0}
            pad = 16 + 10 * f['inner']
            fx0 = x[min(ps)] - pad - (8 if min(ps) == 0 else 0)
            fx1 = x[max(ps)] + pad
            need = tag_w(f['op'], FS) + (tw(f'[{f["guard"]}]', FS) if f['guard'] else 0) + 26
            for _, g in f['seps']:
                need = max(need, tw(f'[{g}]', FS) + 26)
            fr.append((fx0, max(fx1, fx0 + need), f))
        xmin = min([x[0] - max(hw[0] / 2, 30) - 18] + [a for a, _, _ in fr] + [nx - 12 for nx, _, _, _, _ in notes])
        xmax = max([x[-1] + max(hw[-1] / 2, 30) + 18] + [b + 12 for _, b, _ in fr]
                   + [lx + max(tw(l, FS) for l in ls) + 14 for lx, _, ls in labels] + [nx + w + 14 for nx, _, w, _, _ in notes])
        d = Doc()
        d.frame(xmin, 0, xmax - xmin, y_end + 12, 'sd', None, FS, name=self.title)
        for i in range(n):
            d.line([(x[i], y_life), (x[i], y_end)], dashed=True, dash='5 4')
        for i, (k, kind, lab) in enumerate(P):
            top = top_head + (hh - head_h({'label': lab, 'fs': FS}))
            (d.actor(x[i], top, lab, FS, True) if kind == 'actor' else d.icon(kind, x[i], top, lab, FS, True))
        for k, y0, y1, lvl in done:
            xi = x[self.ix[k]]
            d.rect(xi - BAR / 2 + lvl * 5, y0, BAR, max(y1 - y0, 8), fill='#ffffff', stroke='#000000', sw=1)
        for r in refs:
            rx0, rx1 = x[r['ia']] - 14, x[r['ib']] + 14
            w = max(rx1 - rx0, max(tw(l, FS) for l in r['lines']) + 40)
            d.frame(rx0, r['top'], w, r['h'], 'ref', None, FS, fill='#ffffff')
            d.text(rx0 + w / 2, r['top'] + 22, r['lines'], FS, anchor='middle')
        for fx0, fx1, f in sorted(fr, key=lambda t: t[2]['depth']):
            d.frame(fx0, f['top'], fx1 - fx0, f['bottom'] - f['top'], f['op'], f'[{f["guard"]}]' if f['guard'] else None, FS)
            for sy, g in f['seps']:
                d.line([(fx0, sy), (fx1, sy)], dashed=True, dash='6 4')
                d.text(fx0 + 8, sy + 3, [f'[{g}]'], FS, bg='#ffffff')
        for nx, ny, w, h, lines in notes:
            d.note(nx, ny, w, h, lines, 12)
        for kind, pts in msgs:
            (d.line(pts, end='block') if kind == 'call' else d.line(pts, dashed=True, dash='6 4', end='open'))
        for lx, ly, lines in labels:
            d.text(lx, ly, lines, FS, bg='#ffffff')
        return d


# ============================================================== use case diagram
class UseCase:
    """Khung hệ thống ở giữa, use case một cột; use case dùng chung (include/extend) cột phải trong khung;
    actor chính bên trái, actor phụ (hệ thống ngoài) bên phải."""

    def __init__(self, title):
        self.title = title
        self.actors = []    # (key, label, side)
        self.ucs = []       # (key, name)
        self.shared = []    # (key, name)
        self.assoc = []     # (actor, uc)
        self.gen = []       # (con, cha)
        self.inc = []       # (gốc, được include)
        self.ext = []       # (mở rộng, gốc)

    def actor(self, key, label, side='left'):
        self.actors.append((key, label, side))

    def uc(self, key, name):
        self.ucs.append((key, name))

    def shared_uc(self, key, name):
        self.shared.append((key, name))

    def link(self, actor, *ucs):
        for u in ucs:
            self.assoc.append((actor, u))

    def generalize(self, child, parent):
        self.gen.append((child, parent))

    def include(self, base, target):
        self.inc.append((base, target))

    def extend(self, ext, base):
        self.ext.append((ext, base))

    def build(self):
        fs, S = STYLE['uc_fs'], STYLE['uc_line']
        eh, step = 38, 50
        ew = max([260] + [tw(nm, fs) + 60 for _, nm in self.ucs])
        rw = max([220] + [tw(nm, fs) + 60 for _, nm in self.shared])
        bx0 = 215
        ucx = bx0 + 45 + ew / 2
        top = 96
        ypos = {k: top + i * step for i, (k, _) in enumerate(self.ucs)}
        geo = {k: (ucx, ypos[k], ew) for k, _ in self.ucs}
        rx = ucx + ew / 2 + 120 + rw / 2 if self.shared else None
        used = []
        for k, _ in self.shared:
            rel = [ypos[b] for b, t in self.inc if t == k] + [ypos[b] + 2 * step for e, b in self.ext if e == k]
            y = sum(rel) / len(rel) if rel else top
            while any(abs(y - u) < step for u in used):
                y += step
            used.append(y)
            geo[k] = (rx, y, rw)
        last = max(g[1] for g in geo.values())
        bx1 = (rx + rw / 2 + 30) if self.shared else (ucx + ew / 2 + 45)
        d = Doc()
        d.rect(bx0, 30, bx1 - bx0, last + eh / 2 + 24 - 30, fill=STYLE['uc_box_fill'], stroke=S, sw=1.3)
        d.text((bx0 + bx1) / 2, 42, [self.title], STYLE['uc_title_fs'], bold=True, anchor='middle', color='#0f172a')
        # actor
        apos = {}
        parents = {c: p for c, p in self.gen}
        left_y = []
        for key, label, side in self.actors:
            if key in parents and not any(a == key for a, _ in self.assoc):
                continue
            ys = [geo[u][1] for a, u in self.assoc if a == key] or [top]
            cy = sum(ys) / len(ys)
            if side == 'left':
                while any(abs(cy - u) < 150 for u in left_y):
                    cy += 150
                left_y.append(cy)
                apos[key] = (80, cy, side)
            else:
                apos[key] = (bx1 + 100, cy, side)
        for c, p in self.gen:
            if c not in apos:
                px, py, ps = apos[p]
                apos[c] = (px, py + 212, ps)
        for key, label, side in self.actors:
            ax, ay, _ = apos[key]
            d.actor(ax, ay - 17, label, STYLE['actor_fs'], True)
        for c, p in self.gen:
            cx_, cy_, _ = apos[c]
            px, py, _ = apos[p]
            d.line([(cx_, cy_ - 17), (px, py + 51)], end='tri_open', sw=1.2)
        for a, u in self.assoc:
            ax, ay, side = apos[a]
            ux, uy, w = geo[u]
            if side == 'left':
                d.line([(ax + 14, ay), (ux - w / 2, uy)], color=S, sw=1.1)
            else:
                d.line([(ux + w / 2, uy), (ax - 14, ay)], color=S, sw=1.1)
        for k, nm in self.ucs + self.shared:
            ux, uy, w = geo[k]
            d.ellipse(ux - w / 2, uy - eh / 2, w, eh, fill='#ffffff', stroke=S, sw=1.2, lines=[nm], fs=fs, color='#0f172a')

        def ept(k, px, py):
            ux, uy, w = geo[k]
            dx, dy = px - ux, py - uy
            t = 1 / math.sqrt(dx * dx / (w / 2) ** 2 + dy * dy / (eh / 2) ** 2)
            return ux + dx * t, uy + dy * t
        done_lab = set()
        for b, t in self.inc:
            x1, y1 = geo[b][0] + geo[b][2] / 2, geo[b][1]
            x2, y2 = ept(t, x1, y1)
            d.line([(x1, y1), (x2, y2)], dashed=True, end='open', color=S, sw=1.1)
            if t not in done_lab:
                done_lab.add(t)
                ys = [geo[bb][1] for bb, tt in self.inc if tt == t]
                d.text((x1 + x2) / 2 - 6, min(ys + [geo[t][1]]) - 26, ['«include»'], fs, anchor='middle', color=S)
        for e, b in self.ext:
            bx, by = geo[b][0] + geo[b][2] / 2, geo[b][1]
            x1, y1 = ept(e, bx, by)
            d.line([(x1, y1), (bx + 2, by + 5)], dashed=True, end='open', color=S, sw=1.1)
            d.text((bx + x1) / 2 + 2, (by + y1) / 2 + 10, ['«extend»'], fs, anchor='middle', color=S)
        return d


# ============================================================== state diagram
class StateDiagram:
    """Trạng thái đặt theo lưới (cột, hàng). Đường nối tự đi thẳng hoặc gấp vuông góc."""
    COLW, ROWH, SW, SH, X0, Y0 = 400, 120, 170, 50, 130, 50

    def __init__(self):
        self.nodes = {}   # key -> (x, y, w, h, kind, name)
        self.trans = []   # (src, dst, label)
        self.inits = []
        self.finals = []
        self.loops = []

    def state(self, key, name, col, row):
        self.nodes[key] = (self.X0 + col * self.COLW, self.Y0 + row * self.ROWH, self.SW, self.SH, 'state', name)

    def choice(self, key, col, row):
        x = self.X0 + col * self.COLW + self.SW / 2 - 13
        y = self.Y0 + row * self.ROWH + self.SH / 2 - 13
        self.nodes[key] = (x, y, 26, 26, 'choice', '')

    def initial(self, target, label=None):
        self.inits.append((target, label))

    def final(self, source):
        self.finals.append(source)

    def go(self, src, dst, label=None, route=None):
        """route: None (tự chọn), 'h', 'v', 'vh' (dọc rồi ngang), 'hv' (ngang rồi dọc)."""
        self.trans.append((src, dst, label, route))

    def self_loop(self, key, label):
        self.loops.append((key, label))

    def build(self):
        fs = STYLE['label_fs']
        d = Doc()
        N = self.nodes

        def cx(k):
            return N[k][0] + N[k][2] / 2

        def cy(k):
            return N[k][1] + N[k][3] / 2
        for k, (x, y, w, h, kind, name) in N.items():
            if kind == 'state':
                d.rect(x, y, w, h, fill='#ffffff', stroke='#000000', sw=1.3, r=12, lines=[name], fs=STYLE['state_fs'], bold=True)
            else:
                c = (x + 13, y + 13)
                d.line([(c[0] - 13, c[1]), (c[0], c[1] - 13), (c[0] + 13, c[1]), (c[0], c[1] + 13), (c[0] - 13, c[1])], sw=1.3)

        def lab_h(xa, xb, y, text, below=False):
            if text:
                yy = y + 4 if below else y - lh(fs) - 4
                d.text((xa + xb) / 2, yy, [text], fs, anchor='middle', bg='#ffffff')

        def lab_v(x, ya, yb, text, left=False):
            if text:
                d.text(x - 8 if left else x + 8, (ya + yb) / 2 - lh(fs) / 2, [text], fs, anchor='end' if left else 'start', bg='#ffffff')
        groups = {}
        for t in self.trans:
            groups.setdefault(frozenset((t[0], t[1])), []).append(t)
        for key, ts in groups.items():
            n = len(ts)
            for i, (a, b, text, route) in enumerate(ts):
                xa, ya, wa, ha = N[a][:4]
                xb, yb, wb, hb = N[b][:4]
                same_row = abs(cy(a) - cy(b)) < 1
                same_col = abs(cx(a) - cx(b)) < 1
                r = route or ('h' if same_row else 'v' if same_col else 'vh')
                if r == 'h':
                    off = (i - (n - 1) / 2) * 20
                    y = cy(a) + off
                    x1, x2 = (xa + wa, xb) if xb > xa else (xa, xb + wb)
                    d.line([(x1, y), (x2, y)], end='open', sw=1.2)
                    lab_h(x1, x2, y, text, below=(n > 1 and i == n - 1))
                elif r == 'v':
                    off = (i - (n - 1) / 2) * 50
                    x = cx(a) + off
                    y1, y2 = (ya + ha, yb) if yb > ya else (ya, yb + hb)
                    d.line([(x, y1), (x, y2)], end='open', sw=1.2)
                    lab_v(x, y1, y2, text, left=(n > 1 and i == 0))
                elif r == 'vh':
                    x = cx(a)
                    y1 = ya + ha if yb > ya else ya
                    y = cy(b)
                    x2 = xb if xb > x else xb + wb
                    d.line([(x, y1), (x, y), (x2, y)], end='open', sw=1.2)
                    lab_h(x, x2, y, text)
                else:  # 'hv'
                    y = cy(a)
                    x1 = xa + wa if xb > xa else xa
                    x = cx(b)
                    y2 = yb if yb > y else yb + hb
                    d.line([(x1, y), (x, y), (x, y2)], end='open', sw=1.2)
                    lab_h(x1, x, y, text)
        for k, text in self.inits:
            x, y, w, h = N[k][:4]
            sx = x - 150
            d.start_state(sx - 10, cy(k), 10)
            d.line([(sx, cy(k)), (x, cy(k))], end='open', sw=1.2)
            lab_h(sx, x, cy(k), text)
        for k in self.finals:
            x, y, w, h = N[k][:4]
            d.line([(x + w, cy(k)), (x + w + 34, cy(k))], end='open', sw=1.2)
            d.end_state(x + w + 46, cy(k), 11)
        for k, text in self.loops:
            x, y, w, h = N[k][:4]
            c = cx(k)
            d.line([(c - 30, y), (c - 30, y - 28), (c + 30, y - 28), (c + 30, y)], end='open', sw=1.2)
            lab_h(c - 30, c + 30, y - 28, text)
        return d


# ============================================================== screen flow
class ScreenFlow:
    """Màn đặt theo lưới (cột, hàng). Màu theo nhóm quyền; khung nét đứt gom màn mở từ menu."""
    COLW, ROWH, BW, BH, X0, Y0 = 230, 64, 170, 44, 50, 60

    def __init__(self, legend=None):
        self.boxes = {}
        self.links = []
        self.menus = []
        self.legend = legend or {}

    def screen(self, key, name, col, row, cat='van_hanh', w=None):
        self.boxes[key] = (self.X0 + col * self.COLW, self.Y0 + row * self.ROWH, w or self.BW, self.BH, name, cat)

    def menu(self, keys, label):
        self.menus.append((keys, label))

    def link(self, a, b, both=False, auto=False, route=None, dya=0, dyb=0, via_x=None):
        """route: None (tự chọn), 'v' (dọc), 'hv' (ngang rồi vào cạnh trên/dưới của đích), 'hvh' (ngang, dọc, ngang)."""
        self.links.append((a, b, both, auto, route, dya, dyb, via_x))

    def build(self):
        d = Doc()
        B = self.boxes
        for keys, label in self.menus:
            xs = [B[k][0] for k in keys] + [B[k][0] + B[k][2] for k in keys]
            ys = [B[k][1] for k in keys] + [B[k][1] + B[k][3] for k in keys]
            d.rect(min(xs) - 20, min(ys) - 40, max(xs) - min(xs) + 40, max(ys) - min(ys) + 60, fill='none',
                   stroke=STYLE['menu_line'], sw=1.2, r=10, dashed=True)
            d.text(min(xs) - 8, min(ys) - 32, [label], 12, italic=True, color='#555555')
        for k, (x, y, w, h, name, cat) in B.items():
            f, s = STYLE['screen_colors'][cat]
            d.rect(x, y, w, h, fill=f, stroke=s, sw=1.2, lines=[name], fs=STYLE['screen_fs'])
        for a, b, both, auto, route, dya, dyb, via_x in self.links:
            xa, ya, wa, ha = B[a][:4]
            xb, yb, wb, hb = B[b][:4]
            ca, cb = ya + ha / 2 + dya, yb + hb / 2 + dyb
            same_col = abs(xa - xb) < 1
            r = route or ('v' if same_col else 'h' if abs(ca - cb) < 1 else 'hvh')
            if r == 'v':
                x = xa + wa / 2
                pts = [(x, ya + ha), (x, yb)] if yb > ya else [(x, ya), (x, yb + hb)]
            elif r == 'h':
                pts = [(xa + wa, ca), (xb, ca)] if xb > xa else [(xa, ca), (xb + wb, ca)]
            elif r == 'hv':
                x = xb + wb / 2
                pts = [(xa + wa, ca), (x, ca), (x, yb if yb > ca else yb + hb)]
            else:
                bus = via_x if via_x is not None else ((xa + wa + xb) / 2 if xb > xa else (xa + xb + wb) / 2)
                pts = [(xa + wa if xb > xa else xa, ca), (bus, ca), (bus, cb), (xb if xb > xa else xb + wb, cb)]
            d.line(pts, end='block', start='block' if both else None, sw=1.1, dashed=auto)
        # chú thích
        ly = max(v[1] + v[3] for v in B.values()) + (50 if self.menus else 40)
        lx = self.X0
        used = []
        for v in B.values():
            if v[5] not in used:
                used.append(v[5])
        for cat in used:
            f, s = STYLE['screen_colors'][cat]
            txt = self.legend.get(cat, cat)
            d.rect(lx, ly, 26, 16, fill=f, stroke=s)
            d.text(lx + 34, ly + 1, [txt], 12)
            lx += 34 + tw(txt, 12) + 40
        if self.menus:
            d.text(lx, ly + 1, ['Khung nét đứt: các màn mở từ menu'], 12)
            lx += tw('Khung nét đứt: các màn mở từ menu', 12) + 40
        if any(l[3] for l in self.links):
            d.text(lx, ly + 1, ['Mũi tên nét đứt: hệ thống tự chuyển'], 12)
        return d


# ============================================================== ERD
class ERD:
    """Bảng đặt theo cột lưới (col) và toạ độ y. Quan hệ: kind '1-n', '1-1n', '01-n', '1-01', '1-1'."""
    COLW, X0 = 275, 20
    KIND = {'1-n': ('ERmandOne', 'ERzeroToMany'), '1-1n': ('ERmandOne', 'ERoneToMany'),
            '01-n': ('ERzeroToOne', 'ERzeroToMany'), '1-01': ('ERmandOne', 'ERzeroToOne'), '1-1': ('ERmandOne', 'ERmandOne')}

    def __init__(self):
        self.tables = {}
        self.rels = []
        self.fs = STYLE['erd_fs']
        self.rh = lh(self.fs) + 4

    def table(self, key, name, cols, col, y, external=False, w=None):
        """cols: [(khoá, cột)], khoá là 'PK', 'FK', 'UQ', 'PK, FK', 'FK, UQ' hoặc ''."""
        w = w or (185 if external else 215)
        h = self.rh * (1 + len(cols)) + 4
        self.tables[key] = dict(x=self.X0 + col * self.COLW, y=y, w=w, h=h, name=name, cols=cols, ext=external)

    def rel(self, parent, child, fk_col, kind='1-n', off=16):
        self.rels.append((parent, child, fk_col, kind, off))

    def row_y(self, key, colname):
        t = self.tables[key]
        names = [c for _, c in t['cols']]
        i = names.index(colname) if colname in names else 0
        return t['y'] + self.rh + 2 + (i + 0.5) * self.rh

    def build(self):
        d = Doc()
        for k, t in self.tables.items():
            f, s = STYLE['erd_ext'] if t['ext'] else STYLE['erd_main']
            d.table(t['x'], t['y'], t['w'], t['name'], t['cols'], fs=self.fs, fill=f, stroke=s, dashed=t['ext'])
        for p, c, fk, kind, off in self.rels:
            P, C = self.tables[p], self.tables[c]
            ms, me = self.KIND[kind]
            if abs(P['x'] - C['x']) < 1:  # cùng cột: nối dọc
                x = P['x'] + min(P['w'], C['w']) / 2
                pts = [(x, P['y'] + P['h']), (x, C['y'])] if C['y'] > P['y'] else [(x, P['y']), (x, C['y'] + C['h'])]
            else:
                yc = self.row_y(c, fk)
                right = C['x'] > P['x']
                xp = P['x'] + P['w'] if right else P['x']
                xc = C['x'] if right else C['x'] + C['w']
                if P['y'] + self.rh + 2 <= yc <= P['y'] + P['h'] - 4:
                    pts = [(xp, yc), (xc, yc)]
                else:
                    yp = self.row_y(p, P['cols'][0][1])
                    bus = xp + off if right else xp - off
                    pts = [(xp, yp), (bus, yp), (bus, yc), (xc, yc)]
            d.line(pts, start=ms, end=me, sw=1.1)
        return d
