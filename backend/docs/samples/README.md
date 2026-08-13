# Register form fixtures

Test files for `IRegisterFormParser`, which reads the student roster off the "Capstone Project
Register" form a mentor attaches when proposing a topic.

## Files derived from the real template — prefer these

`official-template.docx` is the school's blank form. `fill-official-register-form.py` writes a
roster into it and leaves everything else byte-identical, so these fixtures exercise the exact
structure, styles and wording a mentor actually uploads.

| File | Contents | Expected result |
|------|----------|-----------------|
| `official-template.docx` | Blank form, no students | Empty roster — the proposal still succeeds |
| `official-register-filled.docx` | 5 students, Student 1 marked Leader | Roster of 5, leader on row 1 |
| `official-register-filled-no-leader.docx` | 5 students, all marked Member | Roster of 5, row 1 promoted to leader |

Regenerate after editing the `ROSTER` list in the script:

```bash
python backend/docs/samples/fill-official-register-form.py backend/docs/samples/official-template.docx -o backend/docs/samples/official-register-filled.docx
```

## Synthetic files

`make-register-form-sample.py` builds minimal documents from scratch instead of editing the
template. They are smaller and quicker to reason about, and they cover one case the real template
cannot — a roster larger than the form has rows.

| File | Contents | Expected result |
|------|----------|-----------------|
| `capstone-project-register-sample.docx` | 5 students, one Leader | Roster of 5 |
| `capstone-project-register-empty.docx` | Header row only | Empty roster |
| `capstone-project-register-no-leader.docx` | 5 students, no Leader | Row 1 promoted to leader |
| `capstone-project-register-too-many.docx` | 7 students | Roster dropped — over `MaxStudents` |

## Notes

Both generators use the standard library only, so no `pip install` is needed.

The student codes printed on the form (`LT000001`) do not match what the load-test seeder stores
(`LT-000001`), so the code lookup misses and the server falls back to matching on e-mail. Real FPT
codes such as `HE160123` carry no hyphen and match directly. Keep the e-mail column filled in.

Group formation after approval needs at least `Group.MinMembers` (4) students, and every one of them
must be free of another group in that semester — otherwise the whole roster is skipped.
