export interface UserBriefDto {
    id: string
    fullName: string
    email: string
    role: string
}

export interface TicketMessageDto {
    id: string
    senderId: string
    sender?: UserBriefDto
    content: string
    createdAt: string
}

export interface TicketDto {
    id: string
    code: string
    title: string
    description: string
    reporter: UserBriefDto
    assignee?: UserBriefDto
    category: string
    priority: string
    status: string
    createdAt: string
    updatedAt?: string
    resolvedAt?: string
    closedAt?: string
    messages: TicketMessageDto[]
}

export interface TicketListDto {
    id: string
    code: string
    title: string
    reporter: UserBriefDto
    category: string
    priority: string
    status: string
    createdAt: string
}

export interface TicketStatsDto {
    totalTickets: number
    unread: number
    inProgress: number
    resolved: number
}
