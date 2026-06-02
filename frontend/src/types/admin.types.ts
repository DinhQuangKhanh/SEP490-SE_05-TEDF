export interface SemesterPhaseDto {
    id: number
    name: string
    type: string
    startDate: string
    endDate: string
    order: number
    status: string
    durationDays: number
}

export interface SemesterDto {
    id: number
    name: string
    code: string
    startDate: string
    endDate: string
    status: string
    academicYear: string
    description: string | null
    createdAt: string
    updatedAt: string | null
    phases: SemesterPhaseDto[]
}
