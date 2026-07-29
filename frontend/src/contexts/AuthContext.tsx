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
import { authService } from '@/lib/auth/authService'
import { ApiError } from '@/lib/common/apiClient'
import type { SessionAccess } from '@/types'

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
    /** Server access gate (account status + student eligibility); null until resolved or signed out. */
    access: SessionAccess | null
}

const useFirebase = import.meta.env.VITE_USE_FIREBASE_EMULATOR === 'true' ||
    (import.meta.env.VITE_FIREBASE_API_KEY && import.meta.env.VITE_FIREBASE_API_KEY !== 'fake-api-key')

const AuthContext = createContext<AuthContextType | undefined>(undefined)

/**
 * Most-privileged first. roles[0] decides the landing page, and the database returns roles in
 * insertion order — an account that was auto-provisioned as Student before being granted Admin
 * would otherwise keep landing on the student UI.
 */
const ROLE_PRIORITY: UserRole[] = ['admin', 'departmenthead', 'mentor', 'evaluator', 'student']

/** Keeps only roles the SPA knows how to route, lower-cased ("Admin" -> "admin"), most-privileged first. */
function normalizeRoles(raw: readonly string[] | undefined): UserRole[] {
    if (!raw) return []
    const seen = new Set(
        raw
            .map((r) => r.toLowerCase() as UserRole)
            .filter((r): r is UserRole => ROLE_PRIORITY.includes(r)),
    )
    return ROLE_PRIORITY.filter((r) => seen.has(r))
}

/**
 * Fallback only — used before the server session arrives, and when it cannot be reached.
 *
 * Firebase custom claims are a cache that lags one token refresh behind the database: a role
 * granted (or revoked) server-side is not in the token the browser is already holding. The DB,
 * via GET /api/auth/session, is the source of truth for roles.
 */
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
    const account1 = {
        id: fbUser.uid,
        name: fbUser.displayName || email.split('@')[0],
        email,
        role: roles[0],
        roles,
        avatar: fbUser.photoURL || undefined,
        firebaseToken: token,
    }
    console.log('firebaseUserToUser', account1)
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
    const [access, setAccess] = useState<SessionAccess | null>(null)

    /**
     * Reconciles the signed-in Firebase user against the TEDF database.
     *
     * Signing in with Google only proves who the person is; it says nothing about what they are
     * in this system. GET /api/auth/session is what resolves that — it returns the roles held in
     * the database, plus the access gate. Roles from the Firebase token are only a stale cache,
     * so whatever the server says wins.
     *
     * Returns the user with server roles applied; on a network failure it returns the input
     * unchanged rather than locking the person out.
     */
    const syncWithServer = async (appUser: User): Promise<User> => {
        try {
            const session = await authService.getSession()
            setAccess(session.access)

            const roles = normalizeRoles(session.roles)
            if (roles.length === 0) return appUser

            return {
                ...appUser,
                roles,
                role: roles[0],
                name: session.fullName || appUser.name,
                email: session.email || appUser.email,
            }
        } catch (err) {
            // Never swallow this silently: when the session lookup fails the SPA falls back to the
            // Firebase token, whose default is 'student' — so a failure here looks exactly like a
            // successful login with the wrong role, which is impossible to diagnose from the UI.
            const status = err instanceof ApiError ? err.status : null
            if (status === 401 || status === 403) {
                console.error(
                    `[auth] GET /api/auth/session -> ${status}. The Firebase sign-in worked, but the ` +
                    `backend did not accept it: no TEDF account is linked to this user, or the account ` +
                    `is locked. Roles fall back to the Firebase token. Server said: ${(err as ApiError).message}`,
                )
            } else if (status !== null) {
                console.error(`[auth] GET /api/auth/session -> ${status}: ${(err as ApiError).message}`)
            } else {
                console.error(
                    '[auth] GET /api/auth/session could not be reached at all ' +
                    `(${import.meta.env.VITE_API_BASE_URL || 'same origin'}). Typical causes: the API is ` +
                    'not running, CORS, or an untrusted https dev certificate. Original error:',
                    err,
                )
            }

            // Fail-open on the access gate; the server middleware still enforces it on every request.
            setAccess({ allowed: true, kind: null, reason: null })
            return appUser
        }
    }

    /**
     * Stores the bearer token before any authenticated call is made.
     *
     * apiClient reads the token out of localStorage["user"], so the session lookup in
     * syncWithServer only authenticates if the freshly minted token is already there. Writing
     * React state here as well would briefly publish the fallback role and bounce the user
     * through the wrong dashboard, so this deliberately touches storage only.
     */
    const persistToken = (appUser: User) => {
        localStorage.setItem('user', JSON.stringify(appUser))
    }

    /** Persists the user and keeps activeRole valid for the roles they actually hold. */
    const commitUser = (appUser: User) => {
        setUser(appUser)
        localStorage.setItem('user', JSON.stringify(appUser))

        // A role stored from an earlier session may no longer be granted — fall back instead of
        // leaving the SPA on a tab the account cannot use.
        console.log('commitUser', appUser)
        const stored = localStorage.getItem('activeRole') as UserRole | null
        const nextRole = stored && appUser.roles.includes(stored) ? stored : appUser.role
        setActiveRole(nextRole)
        localStorage.setItem('activeRole', nextRole)
    }

    // Listen for Firebase auth state changes
    useEffect(() => {
        if (!useFirebase) return

        const unsubscribe = onAuthStateChanged(auth, async (fbUser) => {
            if (fbUser) {
                const token = await fbUser.getIdToken()
                const appUser = firebaseUserToUser(fbUser, token)
                persistToken(appUser)
                commitUser(await syncWithServer(appUser))
            } else {
                setUser(null)
                setActiveRole(null)
                setAccess(null)
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
            persistToken(appUser)
            commitUser(await syncWithServer(appUser))
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
            persistToken(appUser)
            commitUser(await syncWithServer(appUser))
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
            setAccess({ allowed: true, kind: null, reason: null })
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
                mentor: '/lecturer',
                evaluator: '/lecturer',
                student: '/student',
                departmenthead: '/lecturer',
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
        setAccess(null)
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
            access,
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
