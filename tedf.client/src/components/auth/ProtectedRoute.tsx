import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import { useMaintenance } from '@/contexts/MaintenanceContext'
import { ReactNode } from 'react'

type UserRole = 'admin' | 'mentor' | 'evaluator' | 'student'

interface ProtectedRouteProps {
    children: ReactNode
    /** Roles allowed to access this route. If omitted, any authenticated user can access. */
    allowedRoles?: UserRole[]
}

export function ProtectedRoute({ children, allowedRoles }: ProtectedRouteProps) {
    const { isAuthenticated, user, isLoading } = useAuth()
    const { isMaintenanceMode } = useMaintenance()
    const location = useLocation()

    // Wait for Firebase auth state to resolve before deciding
    if (isLoading) {
        return null
    }

    if (!isAuthenticated) {
        return <Navigate to="/login" state={{ from: location }} replace />
    }

    // If maintenance mode is on and user is not admin, redirect to maintenance page
    if (isMaintenanceMode && !user?.roles?.includes('admin')) {
        return <Navigate to="/maintenance" replace />
    }

    // Role-based access control: if allowedRoles specified, check user roles (supports multi-role)
    if (allowedRoles && user && !allowedRoles.some(r => user.roles?.includes(r))) {
        return <Navigate to="/403" replace />
    }

    return <>{children}</>
}
