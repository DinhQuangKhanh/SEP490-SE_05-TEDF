import { useEffect, useCallback } from "react";
import { HubConnectionBuilder, HubConnection, LogLevel } from "@microsoft/signalr";
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

interface UseSignalROptions {
  onReceiveNotification?: (notification: unknown) => void;
  /** Real-time registration changes for the mentor "Yêu cầu đăng ký" tab. */
  onRegistrationUpdate?: (update: unknown) => void;
}

export function useSignalR({ onReceiveNotification, onRegistrationUpdate }: UseSignalROptions) {
  const connectionRef = useRef<HubConnection | null>(null);
  const callbackRef = useRef(onReceiveNotification);
  callbackRef.current = onReceiveNotification;
  const registrationCbRef = useRef(onRegistrationUpdate);
  registrationCbRef.current = onRegistrationUpdate;
interface UnreadCountUpdatedPayload {
  count: number;
}

interface ProjectStatusUpdatedPayload {
  projectId: string;
  projectName: string;
  oldStatus: string;
  newStatus: string;
  updatedAt: string;
}

type ReceiveNotificationListener = (notification: unknown) => void;
type UnreadCountListener = (payload: UnreadCountUpdatedPayload) => void;
type ProjectStatusListener = (payload: ProjectStatusUpdatedPayload) => void;

// Single shared connection: every component calling useSignalR attaches/detaches
// listeners to these sets instead of opening its own HubConnection.
let sharedConnection: HubConnection | null = null;
let connectPromise: Promise<void> | null = null;
let refCount = 0;

const receiveNotificationListeners = new Set<ReceiveNotificationListener>();
const unreadCountListeners = new Set<UnreadCountListener>();
const projectStatusListeners = new Set<ProjectStatusListener>();

function ensureConnection(): Promise<void> {
  if (sharedConnection) return Promise.resolve();
  if (connectPromise) return connectPromise;

  connectPromise = (async () => {
    const token = await getToken();
    if (!token) return;

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

    connection.on("ReceiveRegistrationUpdate", (update: unknown) => {
      registrationCbRef.current?.(update);
    });

    connection
      .start()
      .catch((err) => console.warn("SignalR connection failed:", err));
    connection.on(SignalREvents.UnreadCountUpdated, (payload: UnreadCountUpdatedPayload) => {
      unreadCountListeners.forEach((listener) => listener(payload));
    });

    connection.on(SignalREvents.ProjectStatusUpdated, (payload: ProjectStatusUpdatedPayload) => {
      projectStatusListeners.forEach((listener) => listener(payload));
    });

    connection.onreconnecting((err) => console.warn("SignalR reconnecting:", err));
    connection.onreconnected(() => console.info("SignalR reconnected"));
    connection.onclose((err) => console.warn("SignalR connection closed:", err));

    try {
      await connection.start();
      sharedConnection = connection;
      console.info("SignalR connected");
    } catch (err) {
      console.warn("SignalR connection failed:", err);
    }
  })();

  return connectPromise;
}

function teardownConnection() {
  const connection = sharedConnection;
  sharedConnection = null;
  connectPromise = null;
  connection?.stop();
}

interface UseSignalROptions {
  onReceiveNotification?: ReceiveNotificationListener;
  onUnreadCountUpdated?: UnreadCountListener;
  onProjectStatusUpdated?: ProjectStatusListener;
}

export function useSignalR({
  onReceiveNotification,
  onUnreadCountUpdated,
  onProjectStatusUpdated,
}: UseSignalROptions) {
  useEffect(() => {
    refCount += 1;
    ensureConnection();

    if (onReceiveNotification) receiveNotificationListeners.add(onReceiveNotification);
    if (onUnreadCountUpdated) unreadCountListeners.add(onUnreadCountUpdated);
    if (onProjectStatusUpdated) projectStatusListeners.add(onProjectStatusUpdated);

    return () => {
      if (onReceiveNotification) receiveNotificationListeners.delete(onReceiveNotification);
      if (onUnreadCountUpdated) unreadCountListeners.delete(onUnreadCountUpdated);
      if (onProjectStatusUpdated) projectStatusListeners.delete(onProjectStatusUpdated);

      refCount -= 1;
      if (refCount <= 0) {
        refCount = 0;
        teardownConnection();
      }
    };
  }, [onReceiveNotification, onUnreadCountUpdated, onProjectStatusUpdated]);

  const joinProjectChannel = useCallback((projectId: string) => {
    ensureConnection()
      .then(() => sharedConnection?.invoke("JoinProjectChannel", projectId))
      .catch((err) => console.warn("SignalR JoinProjectChannel failed:", err));
  }, []);

  const leaveProjectChannel = useCallback((projectId: string) => {
    sharedConnection?.invoke("LeaveProjectChannel", projectId).catch((err) =>
      console.warn("SignalR LeaveProjectChannel failed:", err),
    );
  }, []);

  return { joinProjectChannel, leaveProjectChannel } as const;
}

export type { UnreadCountUpdatedPayload, ProjectStatusUpdatedPayload };
