import { useCallback, useEffect, useState, useSyncExternalStore } from "react";
import type { TopicInPoolItem } from "@/lib/topicPoolService";

const STORAGE_KEY = "unithesis_topic_wishlist";
const SUMMARIES_KEY = "unithesis_topic_wishlist_summaries";

// ── Lightweight store (persists to localStorage) ─────────────────────────────

type WishlistSummary = Pick<
  TopicInPoolItem,
  "id" | "nameVi" | "majorCode" | "mentorName" | "poolStatus" | "poolStatusName"
>;

function readIds(): string[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch {
    return [];
  }
}

function readSummaries(): Record<string, WishlistSummary> {
  try {
    const raw = localStorage.getItem(SUMMARIES_KEY);
    return raw ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
}

function writeIds(ids: string[]) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(ids));
}

function writeSummaries(summaries: Record<string, WishlistSummary>) {
  localStorage.setItem(SUMMARIES_KEY, JSON.stringify(summaries));
}

// ── External store for cross-component reactivity ────────────────────────────

let listeners: Array<() => void> = [];
let snapshot = readIds();

function emitChange() {
  snapshot = readIds();
  listeners.forEach((l) => l());
}

function subscribe(listener: () => void) {
  listeners = [...listeners, listener];
  return () => {
    listeners = listeners.filter((l) => l !== listener);
  };
}

function getSnapshot() {
  return snapshot;
}

// ── Hook ─────────────────────────────────────────────────────────────────────

export function useWishlist() {
  const ids = useSyncExternalStore(subscribe, getSnapshot);
  const [summaries, setSummaries] = useState<Record<string, WishlistSummary>>(
    readSummaries
  );

  // Sync across browser tabs
  useEffect(() => {
    const handler = (e: StorageEvent) => {
      if (e.key === STORAGE_KEY) emitChange();
      if (e.key === SUMMARIES_KEY) setSummaries(readSummaries());
    };
    window.addEventListener("storage", handler);
    return () => window.removeEventListener("storage", handler);
  }, []);

  const has = useCallback((id: string) => ids.includes(id), [ids]);

  const toggle = useCallback(
    (id: string, topic?: TopicInPoolItem) => {
      const current = readIds();
      const currentSummaries = readSummaries();

      if (current.includes(id)) {
        // Remove
        writeIds(current.filter((x) => x !== id));
        delete currentSummaries[id];
      } else {
        // Add
        writeIds([...current, id]);
        if (topic) {
          currentSummaries[id] = {
            id: topic.id,
            nameVi: topic.nameVi,
            majorCode: topic.majorCode,
            mentorName: topic.mentorName,
            poolStatus: topic.poolStatus,
            poolStatusName: topic.poolStatusName,
          };
        }
      }

      writeSummaries(currentSummaries);
      setSummaries({ ...currentSummaries });
      emitChange();
    },
    []
  );

  const clear = useCallback(() => {
    writeIds([]);
    writeSummaries({});
    setSummaries({});
    emitChange();
  }, []);

  return {
    ids,
    count: ids.length,
    has,
    toggle,
    clear,
    summaries,
  };
}
