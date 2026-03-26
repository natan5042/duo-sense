# -*- coding: utf-8 -*-
"""
Génère une présentation FR (8 slides) sur le mini-jeu de rythme
en s'appuyant sur CookingGame.cs.

Objectifs:
- Design "jury-ready" : structure Problème/Solution/Conclusion.
- Code lisible : gros blocs, pas de grandes zones vides.
- Aucun texte en cyrillique dans les captures de code (on "sanitise" les lignes).
- Fond de slide simple (pas de "gros carrés" autour du code).
"""

from __future__ import annotations

import math
import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont
from pptx import Presentation
from pptx.enum.shapes import MSO_SHAPE
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor


ROOT = Path(__file__).resolve().parent.parent
CODE_PATH = ROOT / "Assets" / "Scenes" / "script" / "maison" / "CookingGame.cs"
OUTPUT_PPTX = ROOT / "Presentation_MiniJeu_Rythme_FR_final.pptx"

if not CODE_PATH.exists():
    raise SystemExit(f"Fichier introuvable: {CODE_PATH}")


# Theme
SLIDE_BG = RGBColor(0xF6, 0xF7, 0xFB)
CARD_LINE = RGBColor(0xE3, 0xE6, 0xEE)
CARD_FILL = RGBColor(0xFF, 0xFF, 0xFF)

TITLE_COLOR = RGBColor(0x1E, 0x1E, 0x2A)
SUBTITLE_COLOR = RGBColor(0x4B, 0x4F, 0x5F)
BODY_COLOR = RGBColor(0x2C, 0x2C, 0x38)
ACCENT = RGBColor(0x49, 0x7B, 0xFF)

CODE_BG = (0x1E, 0x24, 0x2F)
CODE_GUTTER = (0x2A, 0x31, 0x44)
CODE_LINENO = (0x9CA3AF)
CODE_TEXT = (0xE5E7EB)


def _find_mono_font(size: int) -> ImageFont.FreeTypeFont:
    candidates = [
        Path(r"C:\Windows\Fonts\consola.ttf"),
        Path(r"C:\Windows\Fonts\cour.ttf"),
    ]
    for p in candidates:
        if p.exists():
            return ImageFont.truetype(str(p), size=size)
    return ImageFont.load_default()  # fallback


def _sanitize_cyrillic(line: str) -> str:
    """
    Supprime l'information en cyrillique pour satisfaire la contrainte.
    On garde la structure (guillemets, /, etc) en remplaçant juste les caractères.
    """
    if not line:
        return line
    # Remplace tout caractère dans la plage cyrillique par un espace
    line = re.sub(r"[\u0400-\u04FF]", " ", line)
    # Compacte des espaces si besoin
    line = re.sub(r"[ \t]+", " ", line)
    return line.rstrip("\n")


def _wrap_code_line(s: str, max_chars: int = 92) -> list[str]:
    """
    Wrap "simple" par caractères (monospace), pour que les lignes longues
    restent lisibles dans l'image.
    """
    s = s.rstrip()
    if len(s) <= max_chars:
        return [s]
    parts = []
    cur = s
    while len(cur) > max_chars:
        parts.append(cur[: max_chars - 3] + "...")
        cur = cur[max_chars - 3 :]
    if cur:
        parts.append(cur)
    return parts


def render_code_png(
    lines_1indexed: list[tuple[int, str]],
    caption: str,
    out_png: Path,
    font_size: int = 16,
    line_h: int = 21,
    pad_x: int = 14,
    pad_y: int = 10,
    max_chars: int = 92,
) -> None:
    """
    lines_1indexed: [(num_line, text_line_original)]
    On sanitise le texte (pas de cyrillique), puis on wrap.
    """
    font = _find_mono_font(font_size)
    lineno_font = _find_mono_font(max(12, font_size - 2))

    # Pré-wrap pour compter la hauteur
    wrapped: list[tuple[int, str]] = []
    for ln, text in lines_1indexed:
        text_s = _sanitize_cyrillic(text)
        wrapped_parts = _wrap_code_line(text_s, max_chars=max_chars)
        # Le numéro de ligne reste sur la première ligne du wrap
        for i, part in enumerate(wrapped_parts):
            wrapped.append((ln if i == 0 else -1, part))

    # Largeur estimation (grosse marge => code toujours large)
    # On fixe la largeur à partir du texte le plus long (après wrap)
    max_w = 0
    for _, t in wrapped:
        w = font.getbbox(t if t else " ")[2]
        max_w = max(max_w, w)

    gutter_w = 62
    img_w = pad_x * 2 + gutter_w + max_w + 6
    img_h = pad_y * 2 + len(wrapped) * line_h + 30

    im = Image.new("RGB", (img_w, img_h), CODE_BG)
    draw = ImageDraw.Draw(im)

    y = pad_y
    for ln, text in wrapped:
        # gutter background (petit "tag")
        draw.rectangle(
            [pad_x + gutter_w - 10, y + 2, pad_x + gutter_w - 2, y + line_h - 6],
            fill=CODE_GUTTER,
        )
        if ln != -1:
            draw.text((pad_x + 6, y), f"{ln:4d}", fill=CODE_LINENO, font=lineno_font)
        else:
            # on garde le même "tracé" sans numéro
            draw.text((pad_x + 6, y), "    ", fill=CODE_LINENO, font=lineno_font)

        draw.text((pad_x + gutter_w + 6, y), text, fill=CODE_TEXT, font=font)
        y += line_h

    # Caption (sans cyrillique)
    cap = _sanitize_cyrillic(caption)
    cap_font = lineno_font
    draw.text((pad_x, img_h - 22), cap, fill=CODE_LINENO, font=cap_font)
    im.save(out_png, "PNG")


def add_slide_bg(slide) -> None:
    fill = slide.background.fill
    fill.solid()
    fill.fore_color.rgb = SLIDE_BG


def add_card(slide, left, top, width, height) -> None:
    shape = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height)
    shape.fill.solid()
    shape.fill.fore_color.rgb = CARD_FILL
    shape.line.color.rgb = CARD_LINE
    shape.line.width = Pt(1)
    try:
        shape.adjustments[0] = 0.12  # radius
    except Exception:
        pass
    return shape


def add_title(slide, text: str, left: float, top: float, width: float, font_size: int = 30) -> None:
    box = slide.shapes.add_textbox(left, top, width, Inches(0.6))
    tf = box.text_frame
    tf.clear()
    p = tf.paragraphs[0]
    p.text = text
    p.font.size = Pt(font_size)
    p.font.name = "Calibri"
    p.font.bold = True
    p.font.color.rgb = TITLE_COLOR
    return box


def add_bullets(
    slide,
    lines: list[str],
    left,
    top,
    width,
    height,
    font_size: int = 16,
) -> None:
    box = slide.shapes.add_textbox(left, top, width, height)
    tf = box.text_frame
    tf.clear()
    for i, t in enumerate(lines):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.text = t
        p.level = 0
        p.font.size = Pt(font_size)
        p.font.name = "Calibri"
        p.font.color.rgb = BODY_COLOR
        p.space_after = Pt(6)


def add_picture_fit(
    slide,
    img_path: Path,
    left,
    top,
    max_w_in: float,
    max_h_in: float,
):
    """
    Ajoute l'image sans l'écraser: on choisit le scale qui fait rentrer
    l'image dans la "box" max_w_in x max_h_in.
    """
    im = Image.open(img_path)
    w_px, h_px = im.size
    # 96 dpi => approx px par pouce côté PowerPoint
    max_w_px = max_w_in * 96.0
    max_h_px = max_h_in * 96.0
    scale = min(max_w_px / max(w_px, 1), max_h_px / max(h_px, 1))
    out_w_in = (w_px * scale) / 96.0
    out_h_in = (h_px * scale) / 96.0
    slide.shapes.add_picture(str(img_path), left, top, width=Inches(out_w_in), height=Inches(out_h_in))


def read_code_lines() -> list[str]:
    txt = CODE_PATH.read_text(encoding="utf-8-sig", errors="ignore")
    return txt.splitlines()


def extract_lines(start: int, end: int, code_lines: list[str]) -> list[tuple[int, str]]:
    out = []
    for ln in range(start, end + 1):
        if 1 <= ln <= len(code_lines):
            out.append((ln, code_lines[ln - 1]))
    return out


def extract_specific(nums: list[int], code_lines: list[str]) -> list[tuple[int, str]]:
    out = []
    for ln in nums:
        if 1 <= ln <= len(code_lines):
            out.append((ln, code_lines[ln - 1]))
    return out


def main() -> None:
    code_lines = read_code_lines()
    prs = Presentation()
    prs.slide_width = Inches(13.333)
    prs.slide_height = Inches(7.5)
    blank = prs.slide_layouts[6]

    # Create temp dir for code images
    tmp_dir = ROOT / "_tmp_ppt_code_imgs"
    tmp_dir.mkdir(exist_ok=True)

    # 1) Title
    s1 = prs.slides.add_slide(blank)
    add_slide_bg(s1)
    add_card(s1, Inches(0.45), Inches(0.9), Inches(12.4), Inches(5.9))
    add_title(s1, "DUO SENSE", Inches(1.0), Inches(1.25), Inches(11.0), 40)
    add_title(s1, "Mini-jeu de rythme (cuisine) — Iris + Achille", Inches(1.0), Inches(2.15), Inches(11.0), 22)
    add_bullets(
        s1,
        [
            "But: synchroniser son + entrée + génération/validation des notes.",
            "Code central: `CookingGame.cs` (Unity).",
        ],
        Inches(1.0),
        Inches(3.4),
        Inches(11.0),
        Inches(2.0),
        18,
    )

    # 2) Problème initial
    s2 = prs.slides.add_slide(blank)
    add_slide_bg(s2)
    add_card(s2, Inches(0.45), Inches(0.85), Inches(12.4), Inches(6.15))
    add_title(s2, "Problème initial", Inches(1.0), Inches(1.0), Inches(11.6), 30)
    add_bullets(
        s2,
        [
            "Un rythme doit être lisible: le joueur doit savoir QUAND appuyer.",
            "En parallèle, les notes doivent apparaître et être validées par fenêtre temporelle.",
            "Résultat attendu: feedback immédiat (hit / miss) + fin de partie nette.",
        ],
        Inches(1.0),
        Inches(1.8),
        Inches(11.6),
        Inches(3.0),
        18,
    )
    # Accent line
    bar = s2.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(1.0), Inches(5.1), Inches(4.2), Inches(0.14))
    bar.fill.solid()
    bar.fill.fore_color.rgb = ACCENT
    bar.line.width = Pt(0)
    add_bullets(
        s2,
        ["Objectif: un gameplay déterministe et stable (sans “désync”)."],
        Inches(1.15),
        Inches(5.25),
        Inches(10.0),
        Inches(1.0),
        16,
    )

    # 3) Solution globale (diagramme textuel)
    s3 = prs.slides.add_slide(blank)
    add_slide_bg(s3)
    add_card(s3, Inches(0.45), Inches(0.85), Inches(12.4), Inches(6.15))
    add_title(s3, "Solution: découpage en 2 boucles + fin unique", Inches(1.0), Inches(1.0), Inches(11.6), 26)

    # Boxes
    box_w = Inches(3.9)
    box_h = Inches(1.1)
    xL = Inches(1.0)
    yT = Inches(2.0)
    b1 = s3.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, xL, yT, box_w, box_h)
    b1.fill.solid(); b1.fill.fore_color.rgb = RGBColor(255, 255, 255)
    b1.line.color.rgb = CARD_LINE
    b1.line.width = Pt(1)
    b1.text_frame.text = "Update()\n(temps + appel sous-systèmes)"

    b2 = s3.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, xL + Inches(4.5), yT, box_w, box_h)
    b2.fill.solid(); b2.fill.fore_color.rgb = RGBColor(255, 255, 255)
    b2.line.color.rgb = CARD_LINE
    b2.line.width = Pt(1)
    b2.text_frame.text = "Iris\nfenêtre de hit + feedback son/visuel"

    b3 = s3.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, xL, yT + Inches(1.6), box_w, box_h)
    b3.fill.solid(); b3.fill.fore_color.rgb = RGBColor(255, 255, 255)
    b3.line.color.rgb = CARD_LINE
    b3.line.width = Pt(1)
    b3.text_frame.text = "Achille\nspawn + validation notes"

    b4 = s3.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, xL + Inches(4.5), yT + Inches(1.6), box_w, box_h)
    b4.fill.solid(); b4.fill.fore_color.rgb = RGBColor(255, 255, 255)
    b4.line.color.rgb = CARD_LINE
    b4.line.width = Pt(1)
    b4.text_frame.text = "EndGame()\n+ quête cuisine"

    # Arrows (simple lines)
    def arrow(x1, y1, x2, y2):
        ln = s3.shapes.add_connector(1, x1, y1, x2, y2)  # 1=straight
        ln.line.color.rgb = RGBColor(0xA1, 0xA7, 0xB3)
        ln.line.width = Pt(2)

    arrow(Inches(4.1), Inches(2.0), Inches(5.0), Inches(2.0))
    arrow(Inches(2.95), Inches(3.1), Inches(5.0), Inches(3.1))
    arrow(Inches(6.45), Inches(2.0), Inches(6.45), Inches(3.1))

    add_bullets(
        s3,
        [
            "Séparation claire: rythme (Iris) vs notes (Achille).",
            "Fin unique: conditions de fin + affichage + progression quête.",
        ],
        Inches(1.0),
        Inches(4.6),
        Inches(11.6),
        Inches(2.0),
        18,
    )

    # Code slides
    # 4) Iris snippet (select key lines)
    # Ces numéros doivent coller au fichier actuel.
    iris_line_nums = [
        146,
        148,
        151,
        153,
        154,
        157,
        158,
        160,
        162,
        166,
        170,
        171,
        173,
        175,
        179,
        180,
        182,
        186,
        189,
        190,
        194,
        196,
        197,
        205,
        206,
        207,
        208,
        214,
        216,
        217,
        219,
        220,
    ]
    iris_lines = extract_specific(iris_line_nums, code_lines)
    code_png_iris = tmp_dir / "code_slide_iris.png"
    render_code_png(iris_lines, "CookingGame.cs · Iris (UpdateWomanRhythm + StartRhythmBeat)", code_png_iris)

    s4 = prs.slides.add_slide(blank)
    add_slide_bg(s4)
    add_card(s4, Inches(0.45), Inches(0.85), Inches(12.4), Inches(6.15))
    add_title(s4, "Iris: fenêtre de frappe (son -> hit)", Inches(1.0), Inches(1.0), Inches(11.6), 26)
    add_picture_fit(s4, code_png_iris, Inches(0.9), Inches(1.65), 11.7, 4.35)
    add_bullets(
        s4,
        [
            "Déclenchement du beat: `beatTimer >= beatInterval` -> `StartRhythmBeat()`.",
            "Fenêtre de réponse: `canWomanPress` + décrément `womanPressWindow`.",
            "Feedback: pulsation UI via `rhythmIndicator`.",
        ],
        Inches(1.0),
        Inches(6.2),
        Inches(11.6),
        Inches(1.0),
        14,
    )

    # 5) Achille snippet
    ach_line_nums = [
        322,
        327,
        328,
        332,
        353,
        356,
        359,
        363,
        365,
        366,
        378,
        383,
        384,
    ]
    ach_lines = extract_specific(ach_line_nums, code_lines)
    code_png_ach = tmp_dir / "code_slide_achille.png"
    render_code_png(ach_lines, "CookingGame.cs · Achille (SpawnNote + TryHitNote)", code_png_ach)

    s5 = prs.slides.add_slide(blank)
    add_slide_bg(s5)
    add_card(s5, Inches(0.45), Inches(0.85), Inches(12.4), Inches(6.15))
    add_title(s5, "Achille: spawn + validation des notes", Inches(1.0), Inches(1.0), Inches(11.6), 26)
    add_picture_fit(s5, code_png_ach, Inches(0.9), Inches(1.65), 11.7, 4.35)
    add_bullets(
        s5,
        [
            "Spawn 2 colonnes: gauche/droit aléatoires -> `activeNotes`.",
            "Validation: seulement les notes `canBeHit` sont considérées.",
            "Action: `ManHit()` / `ManMiss()` + destruction de la note validée.",
        ],
        Inches(1.0),
        Inches(6.2),
        Inches(11.6),
        Inches(1.0),
        14,
    )

    # 6) Fin de partie
    end_line_nums = [
        656,
        658,
        661,
        663,
        665,
        669,
        673,
        679,
        685,
        687,
        688,
        691,
    ]
    end_lines = extract_specific(end_line_nums, code_lines)
    code_png_end = tmp_dir / "code_slide_end.png"
    render_code_png(end_lines, "CookingGame.cs · Fin de partie (EndGame)", code_png_end)

    s6 = prs.slides.add_slide(blank)
    add_slide_bg(s6)
    add_card(s6, Inches(0.45), Inches(0.85), Inches(12.4), Inches(6.15))
    add_title(s6, "Fin de partie: conditions + UI + feedback", Inches(1.0), Inches(1.0), Inches(11.6), 26)
    add_picture_fit(s6, code_png_end, Inches(0.9), Inches(1.65), 11.7, 4.35)
    add_bullets(
        s6,
        [
            "Arrêt unique: temps dépassé ou vies à 0 -> `EndGame()`.",
            "Succès: affiche `Bravo!` et déclenche la progression quête.",
            "Échec: affiche `Perdu!` et active le panneau Game Over.",
        ],
        Inches(1.0),
        Inches(6.2),
        Inches(11.6),
        Inches(1.0),
        14,
    )

    # 7) Lien avec la quête cuisine
    quest_line_nums = [
        782,
        784,
        791,
        795,
        797,
        802,
        809,
        815,
        816,
        820,
        822,
        823,
    ]
    quest_lines = extract_specific(quest_line_nums, code_lines)
    code_png_quest = tmp_dir / "code_slide_quest.png"
    render_code_png(quest_lines, "CookingGame.cs · Lien avec la quête cuisine", code_png_quest)

    s7 = prs.slides.add_slide(blank)
    add_slide_bg(s7)
    add_card(s7, Inches(0.45), Inches(0.85), Inches(12.4), Inches(6.15))
    add_title(s7, "Lien avec la quête: étape complétée", Inches(1.0), Inches(1.0), Inches(11.6), 26)
    add_picture_fit(s7, code_png_quest, Inches(0.9), Inches(1.65), 11.7, 4.35)
    add_bullets(
        s7,
        [
            "Le mini-jeu localise la quête active par son nom.",
            "Quand le joueur réussit: `CompleteStep(1)` puis retrait si la quête est finie.",
            "Résultat: le gameplay “remonte” au système de quête sans boucles de polling.",
        ],
        Inches(1.0),
        Inches(6.2),
        Inches(11.6),
        Inches(1.0),
        14,
    )

    # 8) Conclusion
    s8 = prs.slides.add_slide(blank)
    add_slide_bg(s8)
    add_card(s8, Inches(0.45), Inches(0.85), Inches(12.4), Inches(6.15))
    add_title(s8, "Conclusion", Inches(1.0), Inches(1.0), Inches(11.6), 34)
    add_bullets(
        s8,
        [
            "Découpage clair: Iris gère le rythme et la fenêtre; Achille gère spawn + validation.",
            "Feedback immédiat: UI + audio + destruction des notes validées.",
            "Intégration quête: succès du mini-jeu -> progression (étape 2).",
        ],
        Inches(1.0),
        Inches(2.2),
        Inches(11.6),
        Inches(4.5),
        20,
    )
    # Final accent
    dot = s8.shapes.add_shape(MSO_SHAPE.OVAL, Inches(1.0), Inches(6.2), Inches(0.2), Inches(0.2))
    dot.fill.solid()
    dot.fill.fore_color.rgb = ACCENT
    dot.line.width = Pt(0)
    add_bullets(
        s8,
        ["Résultat global: un mini-jeu paramétrable, stable et lisible pour le joueur."],
        Inches(1.25),
        Inches(6.15),
        Inches(10.8),
        Inches(0.9),
        16,
    )

    prs.save(str(OUTPUT_PPTX))
    print(f"OK -> {OUTPUT_PPTX}")


if __name__ == "__main__":
    main()

