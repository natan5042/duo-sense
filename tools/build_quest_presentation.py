# -*- coding: utf-8 -*-
"""
Présentation code « Système de Quêtes » – Duo Sense (9 slides).
Code ultra-lisible : font 21px, ~12-15 lignes, ... entre les blocs.
"""
from __future__ import annotations

import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.enum.shapes import MSO_SHAPE

ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / "Presentation_Quetes_DuoSense_v4.pptx"

SLIDE_W = 13.333
SLIDE_H = 7.5
MX = 0.55

BG       = RGBColor(0x1A, 0x1A, 0x2E)
BG_CARD  = RGBColor(0x22, 0x22, 0x3A)
ACCENT   = RGBColor(0x00, 0xD2, 0xFF)
ACCENT2  = RGBColor(0x7B, 0x61, 0xFF)
GREEN    = RGBColor(0x4E, 0xC9, 0xB0)
ORANGE   = RGBColor(0xFF, 0xA6, 0x57)
WHITE    = RGBColor(0xFF, 0xFF, 0xFF)
GRAY     = RGBColor(0xA0, 0xA0, 0xB8)
DIM      = RGBColor(0x66, 0x66, 0x80)

CODE_BG     = (0x0D, 0x11, 0x17)
CODE_GUTTER = (0x1A, 0x1F, 0x2B)
CODE_LINENO = (0x50, 0x56, 0x68)
CODE_TEXT   = (0xE6, 0xE6, 0xE6)
CODE_KW     = (0x56, 0x9C, 0xD6)
CODE_STR    = (0xCE, 0x91, 0x78)
CODE_CMT    = (0x6A, 0x99, 0x55)
CODE_TYPE   = (0x4E, 0xC9, 0xB0)
CODE_DOTS   = (0x3E, 0x44, 0x58)

CS_KEYWORDS = {
    "using", "namespace", "public", "private", "protected", "static",
    "void", "int", "float", "bool", "string", "var", "new", "return",
    "if", "else", "foreach", "for", "while", "class", "struct",
    "null", "true", "false", "this", "get", "set", "event", "override",
    "break", "continue", "readonly", "const",
}
CS_TYPES = {
    "Quest", "QuestSystem", "QuestUI", "QuestObjectUnlock",
    "SupermarketQuestManager", "ItemPickup", "PickableItem",
    "MonoBehaviour", "GameObject", "Transform", "Vector2", "Vector3",
    "List", "Dictionary", "Action", "AudioClip", "AudioSource",
    "Debug", "Mathf", "Color", "Text", "StringBuilder", "Canvas",
    "RectTransform", "KeyCode", "Collider2D", "LayerMask",
    "Rigidbody2D", "Quaternion", "SceneManager", "Scene",
    "LoadSceneMode", "DontDestroyOnLoad", "Destroy",
}

PHOTOS = {
    "ingame":     ROOT / "фотки" / "фотка квестов в игре.png",
    "activation": ROOT / "фотки" / "фото квестов с активацией.png",
}
SRC = {
    "qs":  ROOT / "Assets" / "Scripts" / "QuestSystem.cs",
    "ui":  ROOT / "Assets" / "Scripts" / "QuestUI.cs",
    "ip":  ROOT / "Assets" / "Scenes" / "script" / "general" / "ItemPickup.cs",
    "qou": ROOT / "Assets" / "Scenes" / "script" / "niveau_supermarché" / "QuestObjectUnlock.cs",
    "sqm": ROOT / "Assets" / "Scenes" / "script" / "niveau_supermarché" / "SupermarketQuestManager.cs",
}

_CYR = re.compile(r"[\u0400-\u04FF]")
ELLIPSIS_MARKER = -99

def _mono(sz):
    for p in [Path(r"C:\Windows\Fonts\consola.ttf"),
              Path(r"C:\Windows\Fonts\CascadiaCode.ttf"),
              Path(r"C:\Windows\Fonts\cour.ttf")]:
        if p.exists(): return ImageFont.truetype(str(p), size=sz)
    return ImageFont.load_default()


def _clean_line(raw):
    raw = raw.rstrip("\n")
    stripped = raw.strip()
    if stripped.startswith("//") and _CYR.search(stripped): return None
    if stripped.startswith("///") and _CYR.search(stripped): return None
    if "//" in raw:
        code, _, cmt = raw.partition("//")
        if _CYR.search(cmt):
            c = code.rstrip()
            return c if c else None
    return raw


def _tokenize_cs(text):
    tokens = []; i = 0
    while i < len(text):
        if text[i:i+2] == "//":
            tokens.append(("comment", text[i:])); break
        if text[i] == '"':
            j = i + 1
            while j < len(text) and text[j] != '"':
                if text[j] == '\\': j += 1
                j += 1
            tokens.append(("string", text[i:j+1])); i = j + 1; continue
        if text[i] == '$' and i+1 < len(text) and text[i+1] == '"':
            j = i + 2; depth = 0
            while j < len(text):
                if text[j] == '{': depth += 1
                elif text[j] == '}': depth -= 1
                elif text[j] == '"' and depth <= 0: break
                j += 1
            tokens.append(("string", text[i:j+1])); i = j + 1; continue
        if text[i].isalpha() or text[i] == '_':
            j = i
            while j < len(text) and (text[j].isalnum() or text[j] == '_'): j += 1
            w = text[i:j]
            tokens.append(("keyword" if w in CS_KEYWORDS else "type" if w in CS_TYPES else "text", w))
            i = j; continue
        tokens.append(("text", text[i])); i += 1
    return tokens


def render_code_png(lines, out, font_size=21, max_chars=78):
    line_h = font_size + 9
    pad_x, pad_y = 20, 18
    gutter_w = 66
    font = _mono(font_size)
    ln_font = _mono(max(13, font_size - 4))
    dots_font = _mono(font_size)

    cleaned = []
    for ln, raw in lines:
        if ln == ELLIPSIS_MARKER:
            cleaned.append((ELLIPSIS_MARKER, "    ..."))
            continue
        r = _clean_line(raw)
        if r is None: continue
        if len(r) > max_chars: r = r[:max_chars-1] + "\u2026"
        cleaned.append((ln, r))
    if not cleaned: cleaned = [(0, " ")]

    max_w = max(font.getbbox(t if t else " ")[2] for _, t in cleaned)
    img_w = max(pad_x*2 + gutter_w + max_w + 30, 900)
    img_h = pad_y*2 + len(cleaned)*line_h + 10

    im = Image.new("RGB", (img_w, img_h), CODE_BG)
    draw = ImageDraw.Draw(im)
    draw.rectangle([0, 0, gutter_w + pad_x, img_h], fill=CODE_GUTTER)

    y = pad_y
    for ln, text in cleaned:
        if ln == ELLIPSIS_MARKER:
            draw.text((pad_x + gutter_w + 10, y), text, fill=CODE_DOTS, font=dots_font)
            y += line_h
            continue
        if ln > 0:
            draw.text((pad_x+6, y+2), f"{ln:4d}", fill=CODE_LINENO, font=ln_font)
        x = pad_x + gutter_w + 10
        for kind, tok in _tokenize_cs(text):
            c = CODE_TEXT
            if kind == "keyword": c = CODE_KW
            elif kind == "string": c = CODE_STR
            elif kind == "comment": c = CODE_CMT
            elif kind == "type": c = CODE_TYPE
            draw.text((x, y), tok, fill=c, font=font)
            x += font.getbbox(tok)[2] - font.getbbox(tok)[0]
        y += line_h

    mask = Image.new("L", (img_w, img_h), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, img_w-1, img_h-1], 10, fill=255)
    result = Image.composite(im, Image.new("RGB", (img_w, img_h), CODE_BG), mask)
    out.parent.mkdir(parents=True, exist_ok=True)
    result.save(out, "PNG")
    return out


def read_src(key):
    return SRC[key].read_text(encoding="utf-8-sig", errors="ignore").splitlines()


def pick(code, ranges):
    """Extract lines with automatic ... between non-contiguous ranges."""
    out = []
    prev_end = -1
    for a, b in ranges:
        if prev_end >= 0 and a > prev_end + 1:
            out.append((ELLIPSIS_MARKER, ""))
        for i in range(a, b+1):
            if 1 <= i <= len(code):
                out.append((i, code[i-1]))
        prev_end = b
    return out


# ─── slide helpers ───────────────────────────────────────────

def set_bg(s):
    f = s.background.fill; f.solid(); f.fore_color.rgb = BG

def title_bar(s, text, sub=""):
    bar = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0), Inches(0),
                             Inches(SLIDE_W), Inches(0.06))
    bar.fill.solid(); bar.fill.fore_color.rgb = ACCENT; bar.line.fill.background()
    box = s.shapes.add_textbox(Inches(MX), Inches(0.3), Inches(SLIDE_W-2*MX), Inches(0.7))
    tf = box.text_frame; tf.word_wrap = True; p = tf.paragraphs[0]
    p.text = text; p.font.size = Pt(34); p.font.bold = True
    p.font.color.rgb = WHITE; p.font.name = "Calibri"
    if sub:
        box2 = s.shapes.add_textbox(Inches(MX), Inches(0.98), Inches(SLIDE_W-2*MX), Inches(0.45))
        tf2 = box2.text_frame; tf2.word_wrap = True; p2 = tf2.paragraphs[0]
        p2.text = sub; p2.font.size = Pt(17); p2.font.color.rgb = GRAY; p2.font.name = "Calibri"

def add_text(s, text, l, t, w, h, size=16, color=WHITE, bold=False, align=PP_ALIGN.LEFT):
    box = s.shapes.add_textbox(Inches(l), Inches(t), Inches(w), Inches(h))
    tf = box.text_frame; tf.word_wrap = True; p = tf.paragraphs[0]
    p.text = text; p.font.size = Pt(size); p.font.color.rgb = color
    p.font.name = "Calibri"; p.font.bold = bold; p.alignment = align

def add_bullets(s, items, l, t, w, h, size=16, color=WHITE, sp=6):
    box = s.shapes.add_textbox(Inches(l), Inches(t), Inches(w), Inches(h))
    tf = box.text_frame; tf.word_wrap = True
    for i, txt in enumerate(items):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.text = txt; p.font.size = Pt(size); p.font.color.rgb = color
        p.font.name = "Calibri"; p.space_after = Pt(sp)

def add_card(s, l, t, w, h, title, body, ac=ACCENT):
    r = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(l), Inches(t), Inches(w), Inches(h))
    r.fill.solid(); r.fill.fore_color.rgb = BG_CARD; r.line.color.rgb = ac; r.line.width = Pt(1.5)
    tf = r.text_frame; tf.word_wrap = True
    tf.margin_left = Inches(0.15); tf.margin_top = Inches(0.1)
    p = tf.paragraphs[0]; p.text = title; p.font.size = Pt(17); p.font.bold = True
    p.font.color.rgb = ac; p.font.name = "Calibri"; p.space_after = Pt(5)
    for ln in body:
        p2 = tf.add_paragraph(); p2.text = ln; p2.font.size = Pt(13)
        p2.font.color.rgb = WHITE; p2.font.name = "Calibri"; p2.space_after = Pt(2)

def add_photo(s, key, l, t, w, h):
    p = PHOTOS.get(key)
    if p and p.exists():
        s.shapes.add_picture(str(p), Inches(l), Inches(t), width=Inches(w), height=Inches(h))


# ─── BUILD ───────────────────────────────────────────────────

def main():
    tmp = ROOT / "_tmp_quests_code_imgs"
    tmp.mkdir(exist_ok=True)

    prs = Presentation()
    prs.slide_width = Inches(SLIDE_W); prs.slide_height = Inches(SLIDE_H)
    blank = prs.slide_layouts[6]

    qs  = read_src("qs")
    ui  = read_src("ui")
    ip  = read_src("ip")
    qou = read_src("qou")
    sqm = read_src("sqm")

    # ══════════════════════════════════════════
    # 1 — TITRE
    # ══════════════════════════════════════════
    s = prs.slides.add_slide(blank); set_bg(s)
    bar = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0), Inches(0),
                             Inches(SLIDE_W), Inches(0.08))
    bar.fill.solid(); bar.fill.fore_color.rgb = ACCENT; bar.line.fill.background()
    add_text(s, "SYSTÈME DE QUÊTES", MX, 2.2, SLIDE_W-2*MX, 1.0,
             size=48, bold=True)
    add_text(s, "Architecture · Singleton + Observer · 5 scripts", MX, 3.3,
             SLIDE_W-2*MX, 0.6, size=20, color=GRAY)
    ln = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(MX), Inches(3.9),
                            Inches(3.5), Inches(0.06))
    ln.fill.solid(); ln.fill.fore_color.rgb = ACCENT; ln.line.fill.background()

    # ══════════════════════════════════════════
    # 2 — OBJECTIF & PROBLÈME
    # ══════════════════════════════════════════
    s = prs.slides.add_slide(blank); set_bg(s)
    title_bar(s, "Objectif & Problème initial")

    add_card(s, MX, 1.55, 5.7, 2.8,
             "Objectif du système", [
                 "Guider les joueurs avec des quêtes",
                 "visibles à l'écran en temps réel.",
                 "",
                 "Étapes booléennes (prendre la planche)",
                 "ET quantifiées (Lait x2 → 0/2).",
                 "",
                 "Réutilisable sur tous les niveaux.",
             ], ACCENT)

    add_card(s, MX + 6.05, 1.55, 5.7, 2.8,
             "Problème avant", [
                 "Pas de système centralisé.",
                 "Chaque niveau avait son propre code.",
                 "Scripts dupliqués → erreurs CS0101.",
                 "Interfaces incompatibles → CS0535.",
                 "Quêtes perdues entre les scènes.",
             ], ORANGE)

    add_card(s, MX, 4.65, 11.75, 2.3,
             "Solution mise en place", [
                 "QuestSystem : singleton persistant (DontDestroyOnLoad) + événement OnQuestsChanged (Observer).",
                 "Quest : modèle avec étapes bool + compteurs. QuestUI : affichage Rich Text auto-refresh.",
                 "ItemPickup : ramassage unifié → événement onItemCollected → managers de niveau réagissent.",
             ], GREEN)

    # ══════════════════════════════════════════
    # 3 — ARCHITECTURE
    # ══════════════════════════════════════════
    s = prs.slides.add_slide(blank); set_bg(s)
    title_bar(s, "Architecture")

    boxes = [
        ("QuestSystem",             "Singleton · DontDestroyOnLoad\nactiveQuests[] · OnQuestsChanged\nAddQuest() · CompleteQuest()",     MX, 1.5, 3.8, 1.65, ACCENT),
        ("Quest (modèle)",          "title · steps[] · completedSteps[]\nstepRequiredCounts[]\nAddStepWithCount · CompleteStep",         MX, 3.4, 3.8, 1.65, ACCENT),
        ("QuestUI",                 "Rich Text : ✓ vert / ○ blanc\nCompteurs (cur/req) auto\nRefresh via OnQuestsChanged",              4.15+MX, 1.5, 3.8, 1.65, GREEN),
        ("ItemPickup",              "Mode inventaire / tenir en main\nonItemCollected event\nHeldItem · DropHeldItem()",                 4.15+MX, 3.4, 3.8, 1.65, ACCENT),
        ("SupermarketQuestManager", "Singleton de niveau\nCrée quêtes obstacle + courses\nProductScanned → IncrementStepCount",         8.3+MX, 1.5, 4.1, 1.65, ACCENT2),
        ("QuestObjectUnlock",       "Porte locale (sans QuestSystem)\nÉcoute onItemCollected\nDébloque objet quand items OK",           8.3+MX, 3.4, 4.1, 1.65, ORANGE),
    ]
    for (t, body, x, y, w, h, c) in boxes:
        r = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(x), Inches(y), Inches(w), Inches(h))
        r.fill.solid(); r.fill.fore_color.rgb = BG_CARD; r.line.color.rgb = c; r.line.width = Pt(1.5)
        tf = r.text_frame; tf.word_wrap = True
        tf.margin_left = Inches(0.12); tf.margin_top = Inches(0.08)
        p0 = tf.paragraphs[0]; p0.text = t; p0.font.size = Pt(15); p0.font.bold = True
        p0.font.color.rgb = c; p0.font.name = "Calibri"; p0.space_after = Pt(4)
        for line in body.split("\n"):
            px = tf.add_paragraph(); px.text = line; px.font.size = Pt(12)
            px.font.color.rgb = WHITE; px.font.name = "Calibri"; px.space_after = Pt(1)

    add_bullets(s, [
        "ItemPickup  ──onItemCollected──→  QuestObjectUnlock  &  SupermarketQuestManager",
        "SupermarketQuestManager  ──AddQuest / CompleteStep──→  QuestSystem  ──OnQuestsChanged──→  QuestUI",
    ], MX, 5.35, SLIDE_W-2*MX, 1.5, size=14, color=GRAY, sp=5)
    add_text(s, "Flux : Joueur → ItemPickup → Managers → QuestSystem → QuestUI",
             MX, 6.3, SLIDE_W-2*MX, 0.4, size=15, color=ACCENT, bold=True)

    # ══════════════════════════════════════════
    # 4 — QUEST + QUESTSYSTEM  (~15 lines, font 21)
    # ══════════════════════════════════════════
    s = prs.slides.add_slide(blank); set_bg(s)
    title_bar(s, "Quest & QuestSystem", "Modèle + Singleton")

    png = render_code_png(pick(qs, [
        (8, 10),         # class Quest
        (17, 18),        # steps, completedSteps
        (21, 22),        # stepRequiredCounts, stepCurrentCounts
        (39, 40), (44, 46),  # AddStepWithCount
        (53, 57),        # IncrementStepCount core
        (73, 76),        # CompleteStep
        (86, 88),        # IsCompleted
        (94, 96), (98, 98), (101, 101),  # QuestSystem
        (108, 111),      # singleton
        (145, 145), (159, 160), (163, 163),  # AddQuest
    ]), tmp / "code_quest.png", font_size=21)
    s.shapes.add_picture(str(png), Inches(MX), Inches(1.4),
                         width=Inches(8.4), height=Inches(5.7))

    add_card(s, 8.7+MX, 1.4, 3.5, 5.7,
             "Points clés", [
                 "Quest : modèle en mémoire.",
                 "steps + completedSteps + counts.",
                 "",
                 "AddStepWithCount :",
                 "étape à quantité (Lait x2).",
                 "",
                 "IncrementStepCount :",
                 "auto-complete si cur >= req.",
                 "",
                 "QuestSystem : Singleton,",
                 "DontDestroyOnLoad.",
                 "",
                 "OnQuestsChanged → UI notifié.",
                 "AddQuest : déduplique par titre.",
             ], ACCENT)

    # ══════════════════════════════════════════
    # 5 — QUESTUI  (~12 lines, font 21)
    # ══════════════════════════════════════════
    s = prs.slides.add_slide(blank); set_bg(s)
    title_bar(s, "QuestUI — Affichage temps réel", "UpdateQuestDisplay()")

    png = render_code_png(pick(ui, [
        (239, 241),      # method sig + null
        (250, 253),      # questSystem, empty state
        (258, 260),      # sb header
        (262, 262), (266, 266),  # foreach + title
        (268, 271),      # step loop
        (273, 277),      # counts
        (284, 286),      # green completed
        (290, 290),      # normal
        (297, 297),      # set text
    ]), tmp / "code_ui.png", font_size=21)
    s.shapes.add_picture(str(png), Inches(MX), Inches(1.4),
                         width=Inches(8.4), height=Inches(5.7))

    add_card(s, 8.7+MX, 1.4, 3.5, 5.7,
             "Rendu", [
                 "Rich Text Unity :",
                 "<b>, <color=green>, ✓ / ○.",
                 "",
                 "Complété → vert + checkmark.",
                 "En cours → blanc + cercle.",
                 "",
                 "Compteurs (cur/req) auto",
                 "si stepRequiredCounts > 0.",
                 "",
                 "Refresh via OnQuestsChanged.",
                 "Fallback : throttle 0.5s.",
             ], GREEN)

    # ══════════════════════════════════════════
    # 6 — ITEMPICKUP  (~13 lines, font 21)
    # ══════════════════════════════════════════
    s = prs.slides.add_slide(blank); set_bg(s)
    title_bar(s, "ItemPickup — Ramassage", "Pont entre joueur et quêtes")

    png = render_code_png(pick(ip, [
        (6, 6),           # class
        (9, 11),          # fields
        (21, 24),         # modes
        (31, 32),         # collectedItemIds
        (35, 35),         # event
        (38, 40),         # HeldItem
        (44, 48),         # Update pickup
        (92, 94), (97, 97),  # collected + event
        (109, 112),       # holdMode
        (121, 126),       # DropHeldItem
    ]), tmp / "code_pickup.png", font_size=21)
    s.shapes.add_picture(str(png), Inches(MX), Inches(1.4),
                         width=Inches(8.4), height=Inches(5.7))

    add_card(s, 8.7+MX, 1.4, 3.5, 5.7,
             "Deux modes", [
                 "Inventaire : collecte →",
                 "collectedItemIds[], disparaît.",
                 "",
                 "Tenir en main : heldItem",
                 "attaché, lâché via E.",
                 "",
                 "onItemCollected : événement",
                 "→ QuestObjectUnlock écoute",
                 "→ SupermarketQuestMgr écoute",
                 "",
                 "HeldItem / HeldItemId :",
                 "API pour la caisse.",
             ], ACCENT)

    # ══════════════════════════════════════════
    # 7 — MANAGERS  (~15 lines combined, font 20)
    # ══════════════════════════════════════════
    s = prs.slides.add_slide(blank); set_bg(s)
    title_bar(s, "Managers de niveau", "QuestObjectUnlock (local) · SupermarketQuestManager (global)")

    combined = (
        pick(qou, [
            (5, 5),           # class
            (9, 9),           # requiredItems
            (15, 15),         # requiredItemCount
            (162, 163), (167, 168),  # subscribe
            (253, 254), (257, 258), (261, 263),  # UnlockObject
        ])
        + [(ELLIPSIS_MARKER, "")]
        + pick(sqm, [
            (9, 11),          # class
            (89, 89), (92, 93), (95, 95),  # CreateObstacleQuest
            (157, 158),       # shoppingQuest
            (165, 166), (168, 168),  # AddStepWithCount + AddQuest
            (193, 194), (200, 200),  # ProductScanned
        ])
    )
    png = render_code_png(combined, tmp / "code_mgr.png", font_size=20)
    s.shapes.add_picture(str(png), Inches(MX), Inches(1.4),
                         width=Inches(8.4), height=Inches(5.7))

    add_card(s, 8.7+MX, 1.4, 3.5, 5.7,
             "Deux patterns", [
                 "QuestObjectUnlock :",
                 "  Porte locale (pas de QuestSystem).",
                 "  Écoute onItemCollected.",
                 "  Vérifie IDs / compteur.",
                 "  → UnlockObject() : visuel + son.",
                 "",
                 "SupermarketQuestManager :",
                 "  Singleton de niveau.",
                 "  Crée obstacle + courses.",
                 "  ProductScanned() à la caisse",
                 "  → IncrementStepCount().",
                 "",
                 "Local vs Global.",
             ], ACCENT2)

    # ══════════════════════════════════════════
    # 8 — RÉSULTATS
    # ══════════════════════════════════════════
    s = prs.slides.add_slide(blank); set_bg(s)
    title_bar(s, "Résultats en jeu")

    pw, ph = 5.8, 3.5
    if PHOTOS["ingame"].exists():
        add_photo(s, "ingame", MX, 1.55, pw, ph)
    if PHOTOS["activation"].exists():
        add_photo(s, "activation", MX+pw+0.3, 1.55, pw, ph)
    add_text(s, "Quêtes affichées — devant la maison", MX, 5.1, pw, 0.4,
             size=13, color=GRAY, align=PP_ALIGN.CENTER)
    add_text(s, "Étape validée ✓ — supermarché", MX+pw+0.3, 5.1, pw, 0.4,
             size=13, color=GRAY, align=PP_ALIGN.CENTER)
    add_bullets(s, [
        "✓  Panneau temps réel : titres, étapes, compteurs (cur/req).",
        "✓  Feedback immédiat : checkmark vert ✓ à chaque complétion.",
        "✓  Nettoyage auto entre scènes · zéro erreur de compilation.",
    ], MX, 5.6, SLIDE_W-2*MX, 1.2, size=16, color=WHITE, sp=5)

    # ══════════════════════════════════════════
    # 9 — CONCLUSION
    # ══════════════════════════════════════════
    s = prs.slides.add_slide(blank); set_bg(s)
    title_bar(s, "Conclusion")

    pts = [
        ("Singleton + Observer", "QuestSystem persiste entre scènes, UI notifiée sans polling.", ACCENT),
        ("Modèle flexible",      "Étapes booléennes + quantifiées, compteurs (cur/req) auto.", GREEN),
        ("Double pattern",       "Quêtes globales (QuestSystem) + déverrouillages locaux (QuestObjectUnlock).", ACCENT2),
        ("Compatibilité",        "API legacy préservée (questName, HeldItem, playerPickupLegacy).", ORANGE),
    ]
    y = 1.6
    for i, (t, desc, clr) in enumerate(pts):
        nb = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE,
                                Inches(MX), Inches(y), Inches(0.55), Inches(0.55))
        nb.fill.solid(); nb.fill.fore_color.rgb = clr; nb.line.fill.background()
        ntf = nb.text_frame
        ntf.paragraphs[0].text = str(i+1); ntf.paragraphs[0].font.size = Pt(20)
        ntf.paragraphs[0].font.bold = True; ntf.paragraphs[0].font.color.rgb = WHITE
        ntf.paragraphs[0].alignment = PP_ALIGN.CENTER; ntf.vertical_anchor = MSO_ANCHOR.MIDDLE
        add_text(s, t, MX+0.75, y-0.02, 4.0, 0.45, size=20, color=WHITE, bold=True)
        add_text(s, desc, MX+4.8, y+0.02, 7.5, 0.45, size=16, color=GRAY)
        y += 1.05

    prs.save(str(OUTPUT))
    print(f"OK -> {OUTPUT}")

if __name__ == "__main__":
    main()
