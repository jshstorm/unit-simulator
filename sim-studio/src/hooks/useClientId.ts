import { useState, useEffect } from 'react';

const CLIENT_ID_KEY = 'unit-simulator-client-id';

/**
 * Hook to manage a persistent client ID stored in localStorage.
 * This ID is used to identify the client for session ownership.
 */
export function useClientId(): string {
  const [clientId] = useState<string>(() => {
    // Try to get existing client ID from localStorage
    const stored = localStorage.getItem(CLIENT_ID_KEY);
    if (stored) {
      return stored;
    }

    // Generate a new UUID
    const newId = crypto.randomUUID();
    localStorage.setItem(CLIENT_ID_KEY, newId);
    return newId;
  });

  // Ensure it's saved (in case localStorage was cleared externally)
  useEffect(() => {
    const stored = localStorage.getItem(CLIENT_ID_KEY);
    if (stored !== clientId) {
      localStorage.setItem(CLIENT_ID_KEY, clientId);
    }
  }, [clientId]);

  return clientId;
}

/**
 * Get or create client ID synchronously (for use outside React components).
 */
export function getClientId(): string {
  const stored = localStorage.getItem(CLIENT_ID_KEY);
  if (stored) {
    return stored;
  }

  const newId = crypto.randomUUID();
  localStorage.setItem(CLIENT_ID_KEY, newId);
  return newId;
}
