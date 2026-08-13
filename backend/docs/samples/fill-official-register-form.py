"""Fills the official "Capstone Project Register" template with a student roster, for testing.

Unlike make-register-form-sample.py — which builds a minimal document from scratch — this edits the
real template in place, so the fixture keeps the school's exact structure, styles and wording. Only
the four empty cells of each "Student N" row are written to; everything else is byte-identical.

    python fill-official-register-form.py <official-template.docx> [-o OUTPUT] [--no-leader]

Stdlib only.
"""

from __future__ import annotations

import argparse
import re
import shutil
import zipfile
from pathlib import Path
from xml.sax.saxutils import escape

# Column order of section 2, read off the real template:
# 0 = "Student N" | 1 = Full name | 2 = Student code | 3 = Phone | 4 = E-mail | 5 = Role in Group
COL_FULL_NAME, COL_CODE, COL_PHONE, COL_EMAIL = 1, 2, 3, 4

STUDENT_SECTION = "Register information for students"
CONTENT_SECTION = "Register content"

# Load-test accounts that are free of a group. The register form prints codes without the hyphen the
# database stores, which is fine: the server falls back to matching on e-mail.
ROSTER = [
    ("Student LoadTest 1", "LT000001", "0987000001", "student1@fpt.edu.vn"),
    ("Student LoadTest 2", "LT000002", "0987000002", "student2@fpt.edu.vn"),
    ("Student LoadTest 3", "LT000003", "0987000003", "student3@fpt.edu.vn"),
    ("Student LoadTest 4", "LT000004", "0987000004", "student4@fpt.edu.vn"),
    ("Student LoadTest 5", "LT000005", "0987000005", "student5@fpt.edu.vn"),
]

TR_RE = re.compile(r"<w:tr[ >].*?</w:tr>", re.S)
TC_RE = re.compile(r"<w:tc>.*?</w:tc>", re.S)
TEXT_RE = re.compile(r"<w:t[^>]*>(.*?)</w:t>", re.S)
STUDENT_ROW_RE = re.compile(r"\bStudent\s*\d\b", re.I)


def cell_text(fragment: str) -> str:
    return " ".join("".join(TEXT_RE.findall(fragment)).split())


def student_table_span(body: str) -> tuple[int, int]:
    """Byte range of section 2, between its heading and the "Register content" heading."""
    start = body.find(STUDENT_SECTION)
    if start < 0:
        raise SystemExit(f'Could not find the "{STUDENT_SECTION}" heading — is this the right template?')

    end = body.find(CONTENT_SECTION, start + len(STUDENT_SECTION))
    if end < 0:
        end = len(body)
    return start, end


def write_cell(cell: str, value: str) -> str:
    """Puts `value` into a cell by appending a run to its last paragraph."""
    marker = "</w:p>"
    at = cell.rfind(marker)
    if at < 0:
        raise SystemExit("A student cell has no paragraph to write into.")

    run = f'<w:r><w:rPr><w:sz w:val="22"/></w:rPr><w:t xml:space="preserve">{escape(value)}</w:t></w:r>'
    return cell[:at] + run + cell[at:]


def fill(document_xml: str, roster: list[tuple[str, str, str, str]], demote_leader: bool) -> str:
    start, end = student_table_span(document_xml)
    section = document_xml[start:end]

    rows = list(TR_RE.finditer(section))
    data_rows = [m for m in rows if STUDENT_ROW_RE.search(cell_text(m.group(0)))]
    if not data_rows:
        raise SystemExit('No "Student N" rows found in section 2.')

    print(f"found {len(data_rows)} student rows; filling {min(len(data_rows), len(roster))}")

    # Rebuild back-to-front so earlier offsets stay valid.
    out = section
    for row_match, person in reversed(list(zip(data_rows, roster))):
        row = row_match.group(0)
        cells = list(TC_RE.finditer(row))
        if len(cells) <= COL_EMAIL:
            raise SystemExit(f"Unexpected column count ({len(cells)}) in a student row.")

        values = {
            COL_FULL_NAME: person[0],
            COL_CODE: person[1],
            COL_PHONE: person[2],
            COL_EMAIL: person[3],
        }

        new_row = row
        for index in sorted(values, reverse=True):
            cell = cells[index]
            new_row = new_row[:cell.start()] + write_cell(cell.group(0), values[index]) + new_row[cell.end():]

        if demote_leader:
            # Blank out the "Leader" text so the server's fallback (first row becomes leader) is exercised.
            new_row = new_row.replace(">Leader<", ">Member<")

        out = out[:row_match.start()] + new_row + out[row_match.end():]

    return document_xml[:start] + out + document_xml[end:]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("template", type=Path)
    parser.add_argument("-o", "--output", type=Path)
    parser.add_argument("--no-leader", action="store_true",
                        help='replace the "Leader" role with "Member" on every row')
    args = parser.parse_args()

    output = args.output or args.template.with_name("capstone-project-register-filled.docx")
    shutil.copyfile(args.template, output)

    with zipfile.ZipFile(args.template) as source:
        parts = {name: source.read(name) for name in source.namelist()}

    document = parts["word/document.xml"].decode("utf-8")
    parts["word/document.xml"] = fill(document, ROSTER, args.no_leader).encode("utf-8")

    with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as target:
        for name, data in parts.items():
            target.writestr(name, data)

    print(f"wrote {output} ({output.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
