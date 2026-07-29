import { initializeApp } from 'firebase/app'
import { getAuth, connectAuthEmulator } from 'firebase/auth'

const firebaseConfig = {
    apiKey: import.meta.env.VITE_FIREBASE_API_KEY || 'fake-api-key',
    authDomain: `${import.meta.env.VITE_FIREBASE_PROJECT_ID || 'TEDF-dev'}.firebaseapp.com`,
    projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID || 'TEDF-dev',
    storageBucket: `${import.meta.env.VITE_FIREBASE_PROJECT_ID || 'TEDF-dev'}.firebasestorage.app`,
    messagingSenderId: import.meta.env.VITE_FIREBASE_MESSAGING_SENDER_ID || 'fake-messaging-sender-id'
}

const app = initializeApp(firebaseConfig)
export const auth = getAuth(app)

// Connect to Firebase Auth Emulator in development
if (import.meta.env.VITE_USE_FIREBASE_EMULATOR === 'true') {
    const emulatorHost = import.meta.env.VITE_FIREBASE_EMULATOR_HOST || 'http://localhost:9099'
    connectAuthEmulator(auth, emulatorHost, { disableWarnings: true })
}
