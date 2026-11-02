import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { confirmEmailChange } from '@/api/account';
import { getMe } from '@/api/auth';
import { Button } from '@/components/ui';
import { ACCOUNT_TEXT, accountFailureText } from '@/domain/accountLinks';
import { useAuthStore } from '@/store/authStore';
import { useUIStore } from '@/store/uiStore';
import { AccountColumn, AccountStatus, useLinkToken } from './accountPage';

type Phase = 'loading' | 'changed' | 'changed-other' | 'taken' | 'invalid';

export default function ConfirmEmailPage() {
  const token = useLinkToken();
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const setUser = useAuthStore((state) => state.setUser);
  const openAuthModal = useUIStore((state) => state.openAuthModal);
  const [phase, setPhase] = useState<Phase>('loading');
  const [error, setError] = useState<string | undefined>();

  useEffect(() => {
    let cancelled = false;
    confirmEmailChange(token)
      .then(async (data) => {
        if (cancelled) return;
        if (data.outcome === 'changed' && data.session === 'other') {
          setPhase('changed-other');
          return;
        }
        if (data.outcome === 'changed') {
          if (data.session === 'current') {
            try {
              const profile = await getMe();
              if (!cancelled) setUser(profile);
            } catch {
              // Адрес уже сменён. Сбой чтения профиля не делает ссылку мёртвой.
            }
          }
          if (!cancelled) setPhase('changed');
          return;
        }
        if (data.outcome === 'taken') {
          setPhase('taken');
          return;
        }
        setPhase('invalid');
      })
      .catch((err) => {
        if (!cancelled) {
          setError(accountFailureText(err));
          setPhase('invalid');
        }
      });
    return () => {
      cancelled = true;
    };
  }, [setUser, token]);

  return (
    <AccountColumn>
      <h1 className="sr-only">Подтверждение адреса</h1>
      {phase === 'loading' && <AccountStatus>Проверяем ссылку...</AccountStatus>}
      {phase === 'changed' && (
        <div className="space-y-4">
          <AccountStatus>{ACCOUNT_TEXT.emailChanged}</AccountStatus>
          {user ? (
            <Button type="button" fullWidth onClick={() => navigate('/dashboard/profile')}>
              В профиль
            </Button>
          ) : (
            <Button
              type="button"
              fullWidth
              onClick={() => {
                openAuthModal('login');
                navigate('/?auth=login');
              }}
            >
              Войти
            </Button>
          )}
        </div>
      )}
      {phase === 'changed-other' && <AccountStatus>{ACCOUNT_TEXT.emailChangedOther}</AccountStatus>}
      {phase === 'taken' && <AccountStatus>{ACCOUNT_TEXT.emailTaken}</AccountStatus>}
      {phase === 'invalid' && (
        <div className="space-y-4">
          <AccountStatus>{error ?? ACCOUNT_TEXT.linkInvalid}</AccountStatus>
        </div>
      )}
    </AccountColumn>
  );
}
