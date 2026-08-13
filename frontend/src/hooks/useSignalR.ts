import { useEffect, useCallback, useRef } from "react";
import {
  HubConnectionBuilder,
  HubConnection,
  HttpTransportType,
  LogLevel,
  type IRetryPolicy,
  type RetryContext,
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

export interface ChecklistUpdatedPayload {
  projectId: string;
}

export interface RegistrationUpdatePayload {
  // Bạn có thể đổi type này thành chính xác hơn nếu backend đã cố định schema.
  [key: string]: unknown;
}

type ReceiveNotificationListener = (notification: unknown) => void;
type RegistrationUpdateListener = (update: RegistrationUpdatePayload) => void;
type UnreadCountListener = (payload: UnreadCountUpdatedPayload) => void;
type ProjectStatusListener = (payload: ProjectStatusUpdatedPayload) => void;
type ChecklistUpdatedListener = (payload: ChecklistUpdatedPayload) => void;

interface UseSignalROptions {
  onReceiveNotification?: ReceiveNotificationListener;
  onRegistrationUpdate?: RegistrationUpdateListener;
  onUnreadCountUpdated?: UnreadCountListener;
  onProjectStatusUpdated?: ProjectStatusListener;
  onChecklistUpdated?: ChecklistUpdatedListener;
}

// Shared connection across all components
let sharedConnection: HubConnection | null = null;
let connectPromise: Promise<void> | null = null;
let refCount = 0;
let reconnectTimer: ReturnType<typeof setTimeout> | null = null;

const MAX_RECONNECT_DELAY_MS = 30_000;

const MAX_JITTER_MS = 1000;

/**
 * Random milliseconds in [0, MAX_JITTER_MS).
 *
 * Uses the Web Crypto API rather than Math.random(), which Sonar flags as security-sensitive
 * (typescript:S2245) and which then blocks the quality gate as an unreviewed hotspot. Nothing here is
 * security-critical — the value is only scheduling jitter — but the crypto call costs nothing at this
 * frequency, so it is not worth carrying a hotspot for.
 */
function jitterMs(): number {
  const buffer = new Uint32Array(1);
  crypto.getRandomValues(buffer);
  return buffer[0] % MAX_JITTER_MS;
}

/** Capped exponential backoff with jitter, starting immediately. */
function backoffDelay(attempt: number): number {
  if (attempt <= 0) return 0;
  const capped = Math.min(2 ** attempt * 1000, MAX_RECONNECT_DELAY_MS);
  // Jitter keeps every open tab from retrying on the same tick after an outage.
  return capped + jitterMs();
}

/**
 * Never stops retrying. The previous policy passed an array of five delays, and SignalR treats an
 * exhausted array as "give up for good" — so any outage longer than ~47s killed realtime until the
 * user happened to reload the page, with nothing in the UI to say so.
 */
const reconnectPolicy: IRetryPolicy = {
  nextRetryDelayInMilliseconds: ({ previousRetryCount }: RetryContext) =>
    backoffDelay(previousRetryCount),
};

const receiveNotificationListeners = new Set<ReceiveNotificationListener>();
const registrationUpdateListeners = new Set<RegistrationUpdateListener>();
const unreadCountListeners = new Set<UnreadCountListener>();
const projectStatusListeners = new Set<ProjectStatusListener>();
const checklistUpdatedListeners = new Set<ChecklistUpdatedListener>();

function buildConnection() {
  const connection = new HubConnectionBuilder()
    .withUrl(`${API_BASE}/hubs/notifications`, {
      accessTokenFactory: async () => (await getToken()) ?? "",
      // WebSocket is excluded on purpose. The CDN in front of tedf.io.vn does not tunnel a WebSocket
      // cleanly: the handshake succeeds, then the frame boundaries drift and the browser aborts with
      // "Unrecognized frame opcode: 11" a few seconds in. SignalR does not recover on its own — it only
      // falls back when a transport fails to *start*, and this one starts fine before dying mid-stream,
      // so withAutomaticReconnect just retries WebSocket forever and no notification is ever delivered.
      // ServerSentEvents and LongPolling are plain HTTP, which the same CDN already serves correctly for
      // /api. Restore WebSocket once the CDN is configured to pass it through.
      transport: HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling,
    })
    .withAutomaticReconnect(reconnectPolicy)
    // Defaults are 30s / 15s. Long-polling behind the CDN adds a hop and can delay a keep-alive past
    // the 30s mark, which the client would read as a dead server and tear down a healthy connection.
    // 60s tolerates one missed ping while still noticing a genuinely dead link reasonably fast.
    .withServerTimeout(60_000)
    .withKeepAliveInterval(15_000)
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

  connection.on(SignalREvents.ChecklistUpdated, (payload: ChecklistUpdatedPayload) => {
    checklistUpdatedListeners.forEach((listener) => listener(payload));
  });

  connection.onreconnecting((err) => console.warn("SignalR reconnecting:", err));
  connection.onreconnected(() => console.info("SignalR reconnected"));

  connection.onclose((err) => {
    console.warn("SignalR connection closed:", err);
    // Drop the dead handle, otherwise ensureConnection's `if (sharedConnection) return` short-circuits
    // on it forever and realtime never comes back for the rest of the session.
    if (sharedConnection === connection) sharedConnection = null;
    scheduleReconnect(0);
  });

  return connection;
}

/**
 * Queues another connect attempt. withAutomaticReconnect only covers a connection that started
 * successfully and later dropped — it does nothing when the very first start() fails, or when no auth
 * token is available yet on a cold page load. Both used to leave the tab with no realtime at all for
 * the rest of the session, so they retry here instead.
 */
function scheduleReconnect(attempt: number): void {
  // Nobody is listening any more (last component unmounted), or a retry is already queued.
  if (refCount <= 0 || reconnectTimer) return;

  reconnectTimer = setTimeout(() => {
    reconnectTimer = null;
    void ensureConnection(attempt + 1);
  }, backoffDelay(attempt));
}

async function ensureConnection(attempt = 0): Promise<void> {
  if (sharedConnection) return;
  if (connectPromise) return connectPromise;

  connectPromise = (async () => {
    // Firebase may not have restored the session yet on first paint; retry rather than give up.
    const token = await getToken();
    if (!token) {
      scheduleReconnect(attempt);
      return;
    }

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
      scheduleReconnect(attempt);
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

  if (reconnectTimer) {
    clearTimeout(reconnectTimer);
    reconnectTimer = null;
  }

  if (connection) {
    try {
      // stop() fires onclose, but sharedConnection is already null and refCount is 0 by now, so
      // scheduleReconnect bails out and an intentional teardown does not resurrect the connection.
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
  onChecklistUpdated,
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

  const checklistUpdatedRef = useRef(onChecklistUpdated);
  checklistUpdatedRef.current = onChecklistUpdated;

  useEffect(() => {
    refCount += 1;

    const connectAndSubscribe = async () => {
      await ensureConnection();
    };

    connectAndSubscribe();

    // Every wrapper is kept in the closure so cleanup can remove the exact function that was added.
    // Removing by comparing against `someRef.current` cannot work: what goes into the Set is the
    // wrapper, not the raw callback, so the comparison never matched and no listener was ever removed.
    // Each mount leaked one, and after N mounts a single notification fired its handler N times.
    let notificationWrapper: ReceiveNotificationListener | null = null;
    if (onReceiveNotification) {
      notificationWrapper = (payload) => receiveNotificationRef.current?.(payload);
      receiveNotificationListeners.add(notificationWrapper);
    }

    let registrationWrapper: RegistrationUpdateListener | null = null;
    if (onRegistrationUpdate) {
      registrationWrapper = (payload) => registrationUpdateRef.current?.(payload);
      registrationUpdateListeners.add(registrationWrapper);
    }

    let unreadCountWrapper: UnreadCountListener | null = null;
    if (onUnreadCountUpdated) {
      unreadCountWrapper = (payload) => unreadCountRef.current?.(payload);
      unreadCountListeners.add(unreadCountWrapper);
    }

    let projectStatusWrapper: ProjectStatusListener | null = null;
    if (onProjectStatusUpdated) {
      projectStatusWrapper = (payload) => projectStatusRef.current?.(payload);
      projectStatusListeners.add(projectStatusWrapper);
    }

    let checklistWrapper: ChecklistUpdatedListener | null = null;
    if (onChecklistUpdated) {
      checklistWrapper = (payload) => checklistUpdatedRef.current?.(payload);
      checklistUpdatedListeners.add(checklistWrapper);
    }

    return () => {
      if (notificationWrapper) receiveNotificationListeners.delete(notificationWrapper);
      if (registrationWrapper) registrationUpdateListeners.delete(registrationWrapper);
      if (unreadCountWrapper) unreadCountListeners.delete(unreadCountWrapper);
      if (projectStatusWrapper) projectStatusListeners.delete(projectStatusWrapper);
      if (checklistWrapper) checklistUpdatedListeners.delete(checklistWrapper);

      refCount -= 1;
      if (refCount <= 0) {
        refCount = 0;
        void teardownConnection();
      }
    };
  }, [onReceiveNotification, onRegistrationUpdate, onUnreadCountUpdated, onProjectStatusUpdated, onChecklistUpdated]);

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
