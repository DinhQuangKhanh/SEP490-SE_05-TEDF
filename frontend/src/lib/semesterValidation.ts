// Shared timeline-phase validation for the Create/Edit semester modals, so both stay identical.
//
// Domain rule:
// - Registration & Evaluation phases happen during the CURRENT (active) semester — topics for the
//   new semester are registered and vetted while the current one is still running — so they must
//   fall within the current semester's dates (or, if there is no current semester, simply end
//   before the new semester begins).
// - Implementation & Defense phases must fall within the NEW semester's dates.
// - Phases must be sequential and each must end after it starts.

export interface PhaseForValidation {
  type: string; // "Registration" | "Evaluation" | "Implementation" | "Defense"
  startDate: string;
  endDate: string;
  label: string; // display label used in error messages
}

export interface SemesterRange {
  startDate: string;
  endDate: string;
}

const PRE_SEMESTER_PHASES = new Set(["Registration", "Evaluation"]);

/**
 * Returns a Vietnamese error message if any phase is invalid, or `null` if all phases are valid.
 */
export function validatePhases(
  phases: PhaseForValidation[],
  semStart: Date,
  semEnd: Date,
  current: SemesterRange | undefined,
): string | null {
  for (let i = 0; i < phases.length; i++) {
    const p = phases[i];
    const pStart = new Date(p.startDate);
    const pEnd = new Date(p.endDate);

    if (pEnd < pStart) {
      return `${p.label}: Ngày kết thúc phải sau ngày bắt đầu.`;
    }

    if (PRE_SEMESTER_PHASES.has(p.type)) {
      if (current) {
        const cStart = new Date(current.startDate);
        const cEnd = new Date(current.endDate);
        if (pStart < cStart || pEnd > cEnd) {
          return `${p.label}: Giai đoạn Đăng ký và Thẩm định phải nằm trong kỳ học hiện tại.`;
        }
      } else if (pEnd > semStart) {
        return `${p.label}: phải kết thúc trước khi kỳ học mới bắt đầu.`;
      }
    } else {
      // Implementation, Defense
      if (pStart < semStart) {
        return `${p.label}: Ngày bắt đầu không được trước ngày bắt đầu kỳ học.`;
      }
      if (pEnd > semEnd) {
        return `${p.label}: Ngày kết thúc không được sau ngày kết thúc kỳ học.`;
      }
    }

    if (i > 0) {
      const prevEnd = new Date(phases[i - 1].endDate);
      if (pStart < prevEnd) {
        return `${p.label}: Ngày bắt đầu phải sau hoặc bằng ngày kết thúc của ${phases[i - 1].label}.`;
      }
    }
  }
  return null;
}

/** Finds the currently-active semester (now ∈ [start, end]) from a list, or undefined. */
export function findCurrentSemester(semesters: SemesterRange[] | undefined, now: Date): SemesterRange | undefined {
  return (semesters ?? []).find((s) => new Date(s.startDate) <= now && new Date(s.endDate) >= now);
}
