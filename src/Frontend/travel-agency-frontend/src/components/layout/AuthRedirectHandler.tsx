import { useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { useUIStore } from '@/store/uiStore';

/**
 * Handles ?auth=login (guest sent here from a protected page, or a direct link).
 * Opens the login modal and drops only the `auth` flag.
 *
 * `returnTo` stays in the query string. That URL is the only copy of the
 * path: the modal store must not keep a second one, because opening the
 * login tab again would replace it with null.
 */
export default function AuthRedirectHandler() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const openAuthModal = useUIStore((s) => s.openAuthModal);

  useEffect(() => {
    if (searchParams.get('auth') !== 'login') return;

    openAuthModal('login');
    const next = new URLSearchParams(searchParams);
    next.delete('auth');
    const qs = next.toString();
    navigate(qs ? `/?${qs}` : '/', { replace: true });
  }, [searchParams, navigate, openAuthModal]);

  return null;
}
