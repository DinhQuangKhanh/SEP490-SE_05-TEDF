import { createContext, useContext, useState, ReactNode } from 'react'

interface MaintenanceContextType {
    isMaintenanceMode: boolean
    setMaintenanceMode: (value: boolean) => void
}

const MaintenanceContext = createContext<MaintenanceContextType | undefined>(undefined)

export function MaintenanceProvider({ children }: { children: ReactNode }) {
    const [isMaintenanceMode, setIsMaintenanceMode] = useState<boolean>(() => {
        return localStorage.getItem('maintenanceMode') === 'true'
    })

    const setMaintenanceMode = (value: boolean) => {
        setIsMaintenanceMode(value)
        localStorage.setItem('maintenanceMode', String(value))
    }

    return (
        <MaintenanceContext.Provider value={{ isMaintenanceMode, setMaintenanceMode }}>
            {children}
        </MaintenanceContext.Provider>
    )
}

export function useMaintenance() {
    const context = useContext(MaintenanceContext)
    if (context === undefined) {
        throw new Error('useMaintenance must be used within a MaintenanceProvider')
    }
    return context
}
