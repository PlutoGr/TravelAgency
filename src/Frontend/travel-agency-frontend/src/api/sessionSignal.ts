const storageKey = 'ta.hasSession';

function storage(): Storage | null {
  try {
    return window.localStorage;
  } catch {
    return null;
  }
}

/** Признак сессии без токена: refresh вызывается только после входа. */
export function hasSessionSignal(): boolean {
  try {
    return storage()?.getItem(storageKey) === '1';
  } catch {
    return false;
  }
}

export function markSessionPresent(): void {
  try {
    storage()?.setItem(storageKey, '1');
  } catch {
    // Приватный режим может запретить localStorage.
  }
}

export function clearSessionSignal(): void {
  try {
    storage()?.removeItem(storageKey);
  } catch {
    // Нет доступа к хранилищу: признак и так не прочитается.
  }
}
