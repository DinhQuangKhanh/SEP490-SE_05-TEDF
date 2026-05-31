import { createContext, useContext, useState, useEffect, ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import {
    signInWithEmailAndPassword,
    signInWithPopup,
    GoogleAuthProvider,
    signOut,
    onAuthStateChanged,
    type User as FirebaseUser,
} from 'firebase/auth'
import { auth } from '@/config/firebase'

type UserRole = 'admin' | 'mentor' | 'evaluator' | 'student' | 'departmenthead'

interface User {
    id: string
    name: string
    email: string
    role: UserRole
    roles: UserRole[]
    avatar?: string
    firebaseToken?: string
}

interface AuthContextType {
    user: User | null
    activeRole: UserRole | null
    isAuthenticated: boolean
    login: (username: string, password: string) => Promise<boolean>
    loginWithGoogle: () => Promise<boolean>
    loginWithEmailPassword: (email: string, password: string) => Promise<boolean>
    switchRole: (role: UserRole) => void
    logout: () => void
    isLoading: boolean
}

const useFirebase = import.meta.env.VITE_USE_FIREBASE_EMULATOR === 'true' ||
    (import.meta.env.VITE_FIREBASE_API_KEY && import.meta.env.VITE_FIREBASE_API_KEY !== 'fake-api-key')

const AuthContext = createContext<AuthContextType | undefined>(undefined)

function parseRolesFromToken(token: string): UserRole[] {
    try {
        const payload = token.split('.')[1]
        const decoded = JSON.parse(atob(payload))
        // Backend may store role as a single string or an array
        const roleClaim =
            decoded.role ||
            decoded.roles ||
            decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
        if (!roleClaim) return ['student']
        const rawRoles: string[] = Array.isArray(roleClaim) ? roleClaim : [roleClaim]
        const validRoles: UserRole[] = rawRoles
            .map((r: string) => r.toLowerCase() as UserRole)
            .filter((r): r is UserRole =>
                ['admin', 'mentor', 'evaluator', 'student', 'departmenthead'].includes(r),
            )
        return validRoles.length > 0 ? validRoles : ['student']
    } catch (err) {
        console.error('Failed to parse roles from JWT:', err)
        return ['student']
    }
}

function firebaseUserToUser(fbUser: FirebaseUser, token: string): User {
    const email = fbUser.email || ''
    const roles = parseRolesFromToken(token)
    return {
        id: fbUser.uid,
        name: fbUser.displayName || email.split('@')[0],
        email,
        role: roles[0],
        roles,
        avatar: fbUser.photoURL || undefined,
        firebaseToken: token,
    }
}

export function AuthProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<User | null>(() => {
        const stored = localStorage.getItem('user')
        return stored ? JSON.parse(stored) : null
    })
    const [activeRole, setActiveRole] = useState<UserRole | null>(() => {
        const stored = localStorage.getItem('activeRole')
        if (stored) return stored as UserRole
        const userStored = localStorage.getItem('user')
        if (userStored) {
            const parsed = JSON.parse(userStored)
            return parsed.role || null
        }
        return null
    })
    const [isLoading, setIsLoading] = useState(!!useFirebase)
    const navigate = useNavigate()

    // Listen for Firebase auth state changes
    useEffect(() => {
        if (!useFirebase) return

        const unsubscribe = onAuthStateChanged(auth, async (fbUser) => {
            if (fbUser) {
                const token = await fbUser.getIdToken()
                const appUser = firebaseUserToUser(fbUser, token)
                setUser(appUser)
                localStorage.setItem('user', JSON.stringify(appUser))
                if (!localStorage.getItem('activeRole')) {
                    setActiveRole(appUser.role)
                    localStorage.setItem('activeRole', appUser.role)
                }
            } else {
                setUser(null)
                setActiveRole(null)
                localStorage.removeItem('user')
                localStorage.removeItem('activeRole')
            }
            setIsLoading(false)
        })

        return () => unsubscribe()
    }, [])

    const loginWithEmailPassword = async (email: string, password: string): Promise<boolean> => {
        try {
            const result = await signInWithEmailAndPassword(auth, email, password)
            const token = await result.user.getIdToken()
            const appUser = firebaseUserToUser(result.user, token)
            setUser(appUser)
            setActiveRole(appUser.role)
            localStorage.setItem('user', JSON.stringify(appUser))
            localStorage.setItem('activeRole', appUser.role)
            return true
        } catch (err) {
            console.error('Email/password login failed:', err)
            return false
        }
    }

    const loginWithGoogle = async (): Promise<boolean> => {
        try {
            const provider = new GoogleAuthProvider()
            provider.setCustomParameters({ hd: 'fpt.edu.vn' })
            const result = await signInWithPopup(auth, provider)
            const token = await result.user.getIdToken()
            const appUser = firebaseUserToUser(result.user, token)
            setUser(appUser)
            setActiveRole(appUser.role)
            localStorage.setItem('user', JSON.stringify(appUser))
            localStorage.setItem('activeRole', appUser.role)
            return true
        } catch (err) {
            console.error('Google login failed:', err)
            return false
        }
    }

    // Legacy mock login (kept for backward compatibility when Firebase is not configured)
    const login = async (username: string, _password: string): Promise<boolean> => {
        if (useFirebase) {
            return loginWithGoogle()
        }

        const lowerUsername = username.toLowerCase()
        const isEvaluator = lowerUsername.includes('evaluator') || lowerUsername.includes('professor')
        const isMentor = lowerUsername.includes('mentor') || lowerUsername.includes('gvhd') || lowerUsername.includes('huongdan')
        const isStudent = lowerUsername.includes('student') || lowerUsername.includes('sinhvien') || lowerUsername.includes('sv')

        let mockUser: User

        if (isStudent) {
            mockUser = { id: 'SV001', name: 'Nguyen Van An', email: 'annv@student.uni.edu.vn', role: 'student', roles: ['student'] }
        } else if (isMentor) {
            mockUser = { id: 'MT001', name: 'TS. Tran Minh Tuan', email: 'tuantm@uni.edu.vn', role: 'mentor', roles: ['mentor', 'evaluator'] }
        } else if (isEvaluator) {
            mockUser = { id: 'EV001', name: 'Prof. Smith', email: 'professor@uni.edu.vn', role: 'evaluator', roles: ['evaluator', 'mentor'] }
        } else {
            mockUser = { id: 'AD001', name: 'Admin System', email: 'admin@uni.edu.vn', role: 'admin', roles: ['admin'] }
        }

        await new Promise(resolve => setTimeout(resolve, 500))

        if (username) {
            setUser(mockUser)
            setActiveRole(mockUser.role)
            localStorage.setItem('user', JSON.stringify(mockUser))
            localStorage.setItem('activeRole', mockUser.role)
            return true
        }
        return false
    }

    const switchRole = (role: UserRole) => {
        if (user && user.roles.includes(role)) {
            setActiveRole(role)
            localStorage.setItem('activeRole', role)
            const roleHomeMap: Record<string, string> = {
                admin: '/admin',
                mentor: '/mentor',
                evaluator: '/evaluator',
                student: '/student',
            }
            navigate(roleHomeMap[role] || '/')
        }
    }

    const logout = async () => {
        if (useFirebase) {
            await signOut(auth)
        }
        setUser(null)
        setActiveRole(null)
        localStorage.removeItem('user')
        localStorage.removeItem('activeRole')
        navigate('/login')
    }

    return (
        <AuthContext.Provider value={{
            user,
            activeRole,
            isAuthenticated: !!user,
            login,
            loginWithGoogle,
            loginWithEmailPassword,
            switchRole,
            logout,
            isLoading,
        }}>
            {children}
        </AuthContext.Provider>
    )
}

export function useAuth() {
    const context = useContext(AuthContext)
    if (context === undefined) {
        throw new Error('useAuth must be used within an AuthProvider')
    }
    return context
}
