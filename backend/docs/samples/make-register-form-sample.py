"""Generates the "Capstone Project Register" DOCX fixtures used to test RegisterFormParser.

The layout mirrors the real form (verified against SP26._BusDN-HanhNT54_filled-test.pdf, whose
PdfPig text dump is quoted in the docstring of DocxRegisterFormReader): a numbered heading
"2. Register information for students", a table whose columns are
No. | Full name | Student code | Phone | E-mail | Role in Group, then the closing heading
"3. Register content of Capstone Project".

Stdlib only -- python-docx is deliberately not required so anyone can regenerate the fixtures.

    python make-register-form-sample.py
"""

from __future__ import annotations

import zipfile
from pathlib import Path
from xml.sax.saxutils import escape

W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"

OUT_DIR = Path(__file__).parent

# Mirrors the load-test students in the real filled form, so a proposal made with these fixtures
# resolves against the same seeded users as the PDF does.
STUDENTS = [
    ("Student LoadTest 33", "LT000033", "0987000033", "student33@fpt.edu.vn", "Leader"),
    ("Student LoadTest 34", "LT000034", "0987000034", "student34@fpt.edu.vn", "Member"),
    ("Student LoadTest 36", "LT000036", "0987000036", "student36@fpt.edu.vn", "Member"),
    ("Student LoadTest 37", "LT000037", "0987000037", "student37@fpt.edu.vn", "Member"),
    ("Student LoadTest 41", "LT000041", "0987000041", "student41@fpt.edu.vn", "Member"),
]

EXTRA_STUDENTS = [
    ("Student LoadTest 42", "LT000042", "0987000042", "student42@fpt.edu.vn", "Member"),
    ("Student LoadTest 43", "LT000043", "0987000043", "student43@fpt.edu.vn", "Member"),
]

CONTENT_TYPES = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
</Types>"""

ROOT_RELS = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>"""

DOC_RELS = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"/>"""


def para(text: str) -> str:
    return f'<w:p><w:r><w:t xml:space="preserve">{escape(text)}</w:t></w:r></w:p>'


def cell(text: str) -> str:
    return f"<w:tc><w:tcPr><w:tcW w:w=\"2000\" w:type=\"dxa\"/></w:tcPr>{para(text)}</w:tc>"


def row(cells: list[str]) -> str:
    return "<w:tr>" + "".join(cell(c) for c in cells) + "</w:tr>"


def student_table(students: list[tuple[str, str, str, str, str]]) -> str:
    header = row(["No.", "Full name", "Student code", "Phone", "E-mail", "Role in Group"])
    body = "".join(
        row([f"Student {i}", name, code, phone, mail, role])
        for i, (name, code, phone, mail, role) in enumerate(students, start=1)
    )
    borders = (
        "<w:tblBorders>"
        + "".join(
            f'<w:{side} w:val="single" w:sz="4" w:space="0" w:color="000000"/>'
            for side in ("top", "left", "bottom", "right", "insideH", "insideV")
        )
        + "</w:tblBorders>"
    )
    return f'<w:tbl><w:tblPr><w:tblW w:w="0" w:type="auto"/>{borders}</w:tblPr>{header}{body}</w:tbl>'


def document(students: list[tuple[str, str, str, str, str]]) -> str:
    body = [
        para("CAPSTONE PROJECT REGISTER"),
        para("Class: CP_SEP490    Duration time: From 05/01/2026 To 30/04/2026"),
        para("Profession: Information Technology"),
        para("Kinds of person make registers: Specialty: Software Engineering"),
        para("1. Register information for supervisor"),
        student_table_supervisor(),
        para("2. Register information for students"),
        student_table(students),
        para("3. Register content of Capstone Project"),
        para("3.1. Capstone Project name:"),
        para(
            "English: Develop BusDN - a real-time bus management and tracking system in Da Nang "
            "City using React, Nodejs, Mongodb."
        ),
        para("Abbreviation: BusDN"),
        para("3.2. Context: (brief introduction)"),
    ]
    return (
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        f'<w:document xmlns:w="{W}"><w:body>' + "".join(body) + "<w:sectPr/></w:body></w:document>"
    )


def student_table_supervisor() -> str:
    """The supervisor table carries an e-mail too; it must stay outside the student section."""
    header = row(["No.", "Full name", "Phone", "E-Mail", "Title"])
    body = row(["Supervisor 1", "Nguyen Thi Hanh", "0935688515", "Hanhnt54@fe.edu.vn", "MSc"])
    return f'<w:tbl><w:tblPr><w:tblW w:w="0" w:type="auto"/></w:tblPr>{header}{body}</w:tbl>'


def write_docx(name: str, students: list[tuple[str, str, str, str, str]]) -> Path:
    path = OUT_DIR / name
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as z:
        z.writestr("[Content_Types].xml", CONTENT_TYPES)
        z.writestr("_rels/.rels", ROOT_RELS)
        z.writestr("word/_rels/document.xml.rels", DOC_RELS)
        z.writestr("word/document.xml", document(students))
    return path


def main() -> None:
    written = [
        # Happy path: 5 students, an explicit Leader.
        write_docx("capstone-project-register-sample.docx", STUDENTS),
        # Blank template: the proposal must still succeed, just without a roster.
        write_docx("capstone-project-register-empty.docx", []),
        # Leader cell edited away: the first row should be promoted to leader.
        write_docx(
            "capstone-project-register-no-leader.docx",
            [(n, c, p, m, "Member") for n, c, p, m, _ in STUDENTS],
        ),
        # Seven students against MaxStudents=5: the whole roster is dropped.
        write_docx("capstone-project-register-too-many.docx", STUDENTS + EXTRA_STUDENTS),
    ]
    for path in written:
        print(f"wrote {path.name} ({path.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
