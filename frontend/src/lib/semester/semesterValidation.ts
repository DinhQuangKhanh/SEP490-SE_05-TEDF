export interface PhaseForValidation {
  type: string; // "Registration" | "Evaluation" | "Implementation" | "Defense"
  startDate: string;
  endDate: string;
  label: string;
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
    const phase = phases[i];
    const pStart = new Date(phase.startDate);
    const pEnd = new Date(phase.endDate);

    if (pEnd < pStart) {
      return `${phase.label}: Ngày kết thúc phải sau ngày bắt đầu.`;
    }

    if (PRE_SEMESTER_PHASES.has(phase.type)) {
      if (current) {
        const cStart = new Date(current.startDate);
        const cEnd = new Date(current.endDate);

        if (pStart < cStart || pEnd > cEnd) {
          return `${phase.label}: Giai đoạn Đăng ký và Thẩm định phải nằm trong kỳ học hiện tại.`;
        }
      } else if (pEnd > semStart) {
        return `${phase.label}: phải kết thúc trước khi kỳ học mới bắt đầu.`;
      }
    } else {
      // Implementation, Defense
      if (pStart < semStart) {
        return `${phase.label}: Ngày bắt đầu không được trước ngày bắt đầu kỳ học.`;
      }
      if (pEnd > semEnd) {
        return `${phase.label}: Ngày kết thúc không được sau ngày kết thúc kỳ học.`;
      }
    }

    if (i > 0) {
      const prevEnd = new Date(phases[i - 1].endDate);
      if (pStart < prevEnd) {
        return `${phase.label}: Ngày bắt đầu phải sau hoặc bằng ngày kết thúc của ${phases[i - 1].label}.`;
      }
    }
  }

  return null;
}

/** Finds the currently-active semester (now ∈ [start, end]) from a list, or undefined. */
export function findCurrentSemester(semesters: SemesterRange[] | undefined, now: Date): SemesterRange | undefined {
  return (semesters ?? []).find((semester) => new Date(semester.startDate) <= now && new Date(semester.endDate) >= now);
}
