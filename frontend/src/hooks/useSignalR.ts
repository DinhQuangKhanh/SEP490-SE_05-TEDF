import { useEffect, useCallback, useRef } from "react";
import {
  HubConnectionBuilder,
  HubConnection,
  LogLevel,
} from "@microsoft/signalr";
import { auth } from "@/config/firebase";
import { SignalREvents } from "@/hooks/signalREvents";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "";

async function getToken(): Promise<string | null> {
  try {
    const currentUser = auth.currentUser;
    if (currentUser) return await currentUser.getIdToken();

    const stored = localStorage.getItem("user");
    if (!stored) return null;

    const user = JSON.parse(stored);
    return user?.firebaseToken ?? null;
  } catch (err) {
    console.warn("SignalR getToken failed:", err);
    return null;
  }
}

export interface UnreadCountUpdatedPayload {
  count: number;
}

export interface ProjectStatusUpdatedPayload {
  projectId: string;
  projectName: string;
  oldStatus: string;
  newStatus: string;
  updatedAt: string;
}

export interface RegistrationUpdatePayload {
  // Bạn có thể đổi type này thành chính xác hơn nếu backend đã cố định schema.
  [key: string]: unknown;
}

type ReceiveNotificationListener = (notification: unknown) => void;
type RegistrationUpdateListener = (update: RegistrationUpdatePayload) => void;
type UnreadCountListener = (payload: UnreadCountUpdatedPayload) => void;
type ProjectStatusListener = (payload: ProjectStatusUpdatedPayload) => void;

interface UseSignalROptions {
  onReceiveNotification?: ReceiveNotificationListener;
  onRegistrationUpdate?: RegistrationUpdateListener;
  onUnreadCountUpdated?: UnreadCountListener;
  onProjectStatusUpdated?: ProjectStatusListener;
}

// Shared connection across all components
let sharedConnection: HubConnection | null = null;
let connectPromise: Promise<void> | null = null;
let refCount = 0;

const receiveNotificationListeners = new Set<ReceiveNotificationListener>();
const registrationUpdateListeners = new Set<RegistrationUpdateListener>();
const unreadCountListeners = new Set<UnreadCountListener>();
const projectStatusListeners = new Set<ProjectStatusListener>();

function buildConnection() {
  const connection = new HubConnectionBuilder()
    .withUrl(`${API_BASE}/hubs/notifications`, {
      accessTokenFactory: async () => (await getToken()) ?? "",
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build();

  connection.on(SignalREvents.ReceiveNotification, (notification: unknown) => {
    receiveNotificationListeners.forEach((listener) => listener(notification));
  });

  connection.on(SignalREvents.ReceiveRegistrationUpdate, (update: RegistrationUpdatePayload) => {
    registrationUpdateListeners.forEach((listener) => listener(update));
  });

  connection.on(SignalREvents.UnreadCountUpdated, (payload: UnreadCountUpdatedPayload) => {
    unreadCountListeners.forEach((listener) => listener(payload));
  });

  connection.on(SignalREvents.ProjectStatusUpdated, (payload: ProjectStatusUpdatedPayload) => {
    projectStatusListeners.forEach((listener) => listener(payload));
  });

  connection.onreconnecting((err) => console.warn("SignalR reconnecting:", err));
  connection.onreconnected(() => console.info("SignalR reconnected"));
  connection.onclose((err) => console.warn("SignalR connection closed:", err));

  return connection;
}

async function ensureConnection(): Promise<void> {
  if (sharedConnection) return;
  if (connectPromise) return connectPromise;

  connectPromise = (async () => {
    const token = await getToken();
    if (!token) return;

    const connection = buildConnection();

    try {
      await connection.start();
      sharedConnection = connection;
      console.info("SignalR connected");
    } catch (err) {
      console.warn("SignalR connection failed:", err);
      try {
        await connection.stop();
      } catch {
        // ignore
      }
    } finally {
      connectPromise = null;
    }
  })();

  return connectPromise;
}

async function teardownConnection() {
  const connection = sharedConnection;
  sharedConnection = null;
  connectPromise = null;

  if (connection) {
    try {
      await connection.stop();
    } catch (err) {
      console.warn("SignalR stop failed:", err);
    }
  }
}

export function useSignalR({
  onReceiveNotification,
  onRegistrationUpdate,
  onUnreadCountUpdated,
  onProjectStatusUpdated,
}: UseSignalROptions) {
  // Keep latest callbacks without re-subscribing the SignalR handlers
  const receiveNotificationRef = useRef(onReceiveNotification);
  receiveNotificationRef.current = onReceiveNotification;

  const registrationUpdateRef = useRef(onRegistrationUpdate);
  registrationUpdateRef.current = onRegistrationUpdate;

  const unreadCountRef = useRef(onUnreadCountUpdated);
  unreadCountRef.current = onUnreadCountUpdated;

  const projectStatusRef = useRef(onProjectStatusUpdated);
  projectStatusRef.current = onProjectStatusUpdated;

  useEffect(() => {
    refCount += 1;

    const connectAndSubscribe = async () => {
      await ensureConnection();
    };

    connectAndSubscribe();

    if (onReceiveNotification) {
      receiveNotificationListeners.add((payload) => receiveNotificationRef.current?.(payload));
    }

    if (onRegistrationUpdate) {
      registrationUpdateListeners.add((payload) => registrationUpdateRef.current?.(payload));
    }

    if (onUnreadCountUpdated) {
      unreadCountListeners.add((payload) => unreadCountRef.current?.(payload));
    }

    if (onProjectStatusUpdated) {
      projectStatusListeners.add((payload) => projectStatusRef.current?.(payload));
    }

    return () => {
      if (onReceiveNotification) {
        receiveNotificationListeners.forEach((listener) => {
          if (listener === receiveNotificationRef.current) {
            receiveNotificationListeners.delete(listener);
          }
        });
      }

      if (onRegistrationUpdate) {
        registrationUpdateListeners.forEach((listener) => {
          if (listener === registrationUpdateRef.current) {
            registrationUpdateListeners.delete(listener);
          }
        });
      }

      if (onUnreadCountUpdated) {
        unreadCountListeners.forEach((listener) => {
          if (listener === unreadCountRef.current) {
            unreadCountListeners.delete(listener);
          }
        });
      }

      if (onProjectStatusUpdated) {
        projectStatusListeners.forEach((listener) => {
          if (listener === projectStatusRef.current) {
            projectStatusListeners.delete(listener);
          }
        });
      }

      refCount -= 1;
      if (refCount <= 0) {
        refCount = 0;
        void teardownConnection();
      }
    };
  }, [onReceiveNotification, onRegistrationUpdate, onUnreadCountUpdated, onProjectStatusUpdated]);

  const joinProjectChannel = useCallback((projectId: string) => {
    void ensureConnection()
      .then(() => sharedConnection?.invoke("JoinProjectChannel", projectId))
      .catch((err) => console.warn("SignalR JoinProjectChannel failed:", err));
  }, []);

  const leaveProjectChannel = useCallback((projectId: string) => {
    sharedConnection?.invoke("LeaveProjectChannel", projectId).catch((err) =>
      console.warn("SignalR LeaveProjectChannel failed:", err),
    );
  }, []);

  return {
    connectionRef: { current: sharedConnection },
    joinProjectChannel,
    leaveProjectChannel,
  } as const;
}

export type {
  ReceiveNotificationListener,
  RegistrationUpdateListener,
  UnreadCountListener,
  ProjectStatusListener,
};
