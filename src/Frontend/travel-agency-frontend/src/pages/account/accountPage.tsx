import { useEffect, useRef, type ReactNode } from 'react';
import { useSearchParams } from 'react-router-dom';

export function useNoReferrer() {
  useEffect(() => {
    const meta = document.createElement('meta');
    meta.name = 'referrer';
    meta.content = 'no-referrer';
    document.head.appendChild(meta);
    return () => meta.remove();
  }, []);
}

/** Секрет читается один раз и сразу убирается из адреса. */
export function useLinkToken(): string {
  const [params, setParams] = useSearchParams();
  const token = useRef<string | null>(null);
  if (token.current === null) token.current = params.get('token') ?? '';

  useEffect(() => {
    if (!params.has('token')) return;
    const next = new URLSearchParams(params);
    next.delete('token');
    setParams(next, { replace: true });
  }, [params, setParams]);

  return token.current;
}

export function AccountColumn({ children }: { children: ReactNode }) {
  useNoReferrer();
  return <div className="mx-auto w-full max-w-md px-4 py-16">{children}</div>;
}

export function AccountStatus({ children }: { children: ReactNode }) {
  return <p className="text-sm text-dark">{children}</p>;
}

export function AccountAlert({ children }: { children: ReactNode }) {
  return (
    <p role="alert" className="text-sm text-red-500">
      {children}
    </p>
  );
}
