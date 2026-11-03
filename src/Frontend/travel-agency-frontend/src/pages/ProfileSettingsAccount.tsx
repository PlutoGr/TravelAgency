import { useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, Input } from '@/components/ui';
import { changePassword, requestEmailChange } from '@/api/account';
import { ACCOUNT_TEXT, accountFailureText, isEmailShape } from '@/domain/accountLinks';
import { PASSWORD_HINT } from '@/domain/profileForm';

type AccountFormProps = {
  onSucceeded: (message: string) => void;
  onBusy: (busy: boolean) => void;
};

function passwordProblem(password: string): string | undefined {
  if (password.length < 8 || password.length > 128) return ACCOUNT_TEXT.passwordRule;
  if (!/[A-Z]/.test(password) || !/[a-z]/.test(password) || !/[0-9]/.test(password)) {
    return ACCOUNT_TEXT.passwordRule;
  }
  return undefined;
}

export function ProfilePasswordForm({ onSucceeded, onBusy }: AccountFormProps) {
  const [current, setCurrent] = useState('');
  const [next, setNext] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState<string | undefined>();
  const [busy, setBusy] = useState(false);
  const busyRef = useRef(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (busyRef.current) return;
    setError(undefined);
    if (!current || !next || !confirm) {
      setError(ACCOUNT_TEXT.fillAll);
      return;
    }
    if (next !== confirm) {
      setError(ACCOUNT_TEXT.passwordMismatch);
      return;
    }
    const rule = passwordProblem(next);
    if (rule) {
      setError(rule);
      return;
    }

    busyRef.current = true;
    setBusy(true);
    onBusy(true);
    let succeeded = false;
    try {
      await changePassword(current, next, confirm);
      succeeded = true;
      onSucceeded(ACCOUNT_TEXT.passwordChanged);
    } catch (err) {
      setError(accountFailureText(err));
    } finally {
      busyRef.current = false;
      onBusy(false);
      if (!succeeded) setBusy(false);
    }
  }

  return (
    <form className="space-y-4" onSubmit={submit}>
      <Input
        id="settings-current-password"
        label="Текущий пароль"
        type="password"
        value={current}
        autoComplete="current-password"
        onChange={(event) => setCurrent(event.target.value)}
      />
      <div>
        <Input
          id="settings-new-password"
          label="Новый пароль"
          type="password"
          value={next}
          autoComplete="new-password"
          aria-describedby="profile-password-hint"
          onChange={(event) => setNext(event.target.value)}
        />
        <p id="profile-password-hint" className="mt-1.5 text-xs text-warm-gray">
          {PASSWORD_HINT}
        </p>
      </div>
      <Input
        id="settings-repeat-password"
        label="Повторите пароль"
        type="password"
        value={confirm}
        autoComplete="new-password"
        onChange={(event) => setConfirm(event.target.value)}
      />
      <Button type="submit" isLoading={busy}>
        Сменить пароль
      </Button>
      {error && (
        <p role="alert" className="text-sm text-red-500">
          {error}
        </p>
      )}
    </form>
  );
}

export function ProfileEmailForm({
  currentEmail,
  onSucceeded,
  onBusy,
}: AccountFormProps & { currentEmail: string }) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | undefined>();
  const [busy, setBusy] = useState(false);
  const busyRef = useRef(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (busyRef.current) return;
    setError(undefined);
    if (!isEmailShape(email)) {
      setError(ACCOUNT_TEXT.emailInvalid);
      return;
    }

    busyRef.current = true;
    setBusy(true);
    onBusy(true);
    let succeeded = false;
    try {
      const data = await requestEmailChange(email, password);
      succeeded = true;
      onSucceeded(data.message || ACCOUNT_TEXT.emailSent);
    } catch (err) {
      setError(accountFailureText(err));
    } finally {
      busyRef.current = false;
      onBusy(false);
      if (!succeeded) setBusy(false);
    }
  }

  return (
    <form className="space-y-4" onSubmit={submit}>
      <p className="text-sm text-dark">{currentEmail}</p>
      <Input
        id="settings-new-email"
        label="Новый email"
        type="email"
        value={email}
        autoComplete="email"
        onChange={(event) => setEmail(event.target.value)}
      />
      <Input
        id="settings-email-password"
        label="Текущий пароль"
        type="password"
        value={password}
        autoComplete="current-password"
        onChange={(event) => setPassword(event.target.value)}
      />
      <Button type="submit" isLoading={busy}>
        Сменить email
      </Button>
      {error && (
        <p role="alert" className="text-sm text-red-500">
          {error}
        </p>
      )}
    </form>
  );
}
