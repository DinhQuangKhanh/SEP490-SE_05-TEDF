import { useEffect, useRef, useCallback } from "react";
import { HubConnectionBuilder, HubConnection, LogLevel, HubConnectionState } from "@microsoft/signalr";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "";

function getToken(): string | null {
  try {
    const stored = localStorage.getItem("user");
    if (!stored) return null;
    const user = JSON.parse(stored);
    return user?.firebaseToken ?? null;
  } catch {
    return null;
  }
}

interface UseSignalROptions {
  onReceiveNotification?: (notification: unknown) => void;
}

export function useSignalR({ onReceiveNotification }: UseSignalROptions) {
  const connectionRef = useRef<HubConnection | null>(null);
  const callbackRef = useRef(onReceiveNotification);
  callbackRef.current = onReceiveNotification;

  const connect = useCallback(() => {
    const token = getToken();
    if (!token) return;

    if (connectionRef.current?.state === HubConnectionState.Connected) return;

    const connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE}/hubs/notifications`, {
        accessTokenFactory: () => getToken() ?? "",
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on("ReceiveNotification", (notification: unknown) => {
      callbackRef.current?.(notification);
    });

    connection
      .start()
      .catch((err) => console.warn("SignalR connection failed:", err));

    connectionRef.current = connection;
  }, []);

  useEffect(() => {
    connect();

    return () => {
      connectionRef.current?.stop();
      connectionRef.current = null;
    };
  }, [connect]);

  return connectionRef;
}
