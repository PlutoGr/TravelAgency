import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { Mail, Lock, User, Phone } from 'lucide-react';
import toast from 'react-hot-toast';
import { useAuthStore } from '@/store/authStore';
import { useUIStore } from '@/store/uiStore';
import { Modal, Tabs, Input, Button } from '@/components/ui';
import { resolveReturnTo } from '@/utils/safeReturnTo';

const AUTH_TABS = [
  { id: 'login', label: 'Вход' },
  { id: 'register', label: 'Регистрация' },
] as const;

export default function AuthModal() {
  const navigate = useNavigate();
  const { isAuthModalOpen, authModalTab, authReturnTo, closeAuthModal, openAuthModal } =
    useUIStore();
  const { login, register, isLoading } = useAuthStore();

  return (
    <Modal isOpen={isAuthModalOpen} onClose={closeAuthModal} size="sm">
      <Tabs
        tabs={[...AUTH_TABS]}
        activeTab={authModalTab}
        onChange={(id) => openAuthModal(id as 'login' | 'register')}
      />
      <div className="mt-6">
        <AnimatePresence mode="wait">
          {authModalTab === 'login' ? (
            <motion.div
              key="login"
              initial={{ opacity: 0, x: -20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: 20 }}
              transition={{ duration: 0.2 }}
            >
              <LoginForm
                isLoading={isLoading}
                onSubmit={async (email, password) => {
                  try {
                    await login(email, password);
                    toast.success('Добро пожаловать!');
                    closeAuthModal();
                    if (authReturnTo) {
                      navigate(resolveReturnTo(authReturnTo));
                    }
                  } catch {
                    toast.error('Неверный email или пароль');
                  }
                }}
              />
            </motion.div>
          ) : (
            <motion.div
              key="register"
              initial={{ opacity: 0, x: 20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: -20 }}
              transition={{ duration: 0.2 }}
            >
              <RegisterForm
                isLoading={isLoading}
                onSubmit={async (data) => {
                  try {
                    await register(data);
                    toast.success('Регистрация прошла успешно!');
                    closeAuthModal();
                    if (authReturnTo) {
                      navigate(resolveReturnTo(authReturnTo));
                    }
                  } catch (err) {
                    const messages = extractValidationErrors(err);
                    if (messages.length > 0) {
                      toast.error(messages.join('. '));
                    } else {
                      const detail = (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail;
                      toast.error(detail || 'Ошибка регистрации. Попробуйте ещё раз.');
                    }
                  }
                }}
              />
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </Modal>
  );
}

function LoginForm({
  isLoading,
  onSubmit,
}: {
  isLoading: boolean;
  onSubmit: (email: string, password: string) => void;
}) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!email.trim() || !password.trim()) {
      toast.error('Заполните все поля');
      return;
    }
    onSubmit(email, password);
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <Input
        label="Email"
        type="email"
        icon={Mail}
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        autoComplete="email"
      />
      <Input
        label="Пароль"
        type="password"
        icon={Lock}
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        autoComplete="current-password"
      />
      <Button type="submit" fullWidth isLoading={isLoading} className="mt-2">
        Войти
      </Button>
    </form>
  );
}

const PASSWORD_MIN_LENGTH = 8;
const PASSWORD_MAX_LENGTH = 128;
const PASSWORD_RULES = {
  length: (p: string) => p.length >= PASSWORD_MIN_LENGTH && p.length <= PASSWORD_MAX_LENGTH,
  uppercase: (p: string) => /[A-Z]/.test(p),
  lowercase: (p: string) => /[a-z]/.test(p),
  digit: (p: string) => /[0-9]/.test(p),
};
const PHONE_REGEX = /^\+?[\d\s\-()]+$/;

function validatePassword(password: string): string[] {
  const errors: string[] = [];
  if (password.length < PASSWORD_MIN_LENGTH) {
    errors.push(`Минимум ${PASSWORD_MIN_LENGTH} символов`);
  }
  if (password.length > PASSWORD_MAX_LENGTH) {
    errors.push(`Максимум ${PASSWORD_MAX_LENGTH} символов`);
  }
  if (!PASSWORD_RULES.uppercase(password)) errors.push('Хотя бы одна заглавная буква');
  if (!PASSWORD_RULES.lowercase(password)) errors.push('Хотя бы одна строчная буква');
  if (!PASSWORD_RULES.digit(password)) errors.push('Хотя бы одна цифра');
  return errors;
}

function extractValidationErrors(error: unknown): string[] {
  const data = (error as { response?: { data?: { errors?: Record<string, string[]> } } })?.response?.data;
  const errors = data?.errors;
  if (!errors || typeof errors !== 'object') return [];
  return Object.values(errors).flat().filter((m): m is string => typeof m === 'string');
}

function RegisterForm({
  isLoading,
  onSubmit,
}: {
  isLoading: boolean;
  onSubmit: (data: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    phone: string;
  }) => void;
}) {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [passwordError, setPasswordError] = useState<string | undefined>();

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setPasswordError(undefined);

    if (!firstName.trim() || !lastName.trim() || !email.trim() || !phone.trim() || !password.trim()) {
      toast.error('Заполните все поля');
      return;
    }

    if (phone.trim() && !PHONE_REGEX.test(phone.trim())) {
      toast.error('Неверный формат телефона. Допустимы цифры, пробелы, дефисы, скобки и + в начале.');
      return;
    }

    const pwdErrors = validatePassword(password);
    if (pwdErrors.length > 0) {
      setPasswordError(pwdErrors.join('. '));
      toast.error(`Пароль: ${pwdErrors.join(', ')}`);
      return;
    }

    onSubmit({ email, password, firstName, lastName, phone });
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <div className="grid grid-cols-2 gap-3">
        <Input
          label="Имя"
          icon={User}
          value={firstName}
          onChange={(e) => setFirstName(e.target.value)}
          autoComplete="given-name"
        />
        <Input
          label="Фамилия"
          icon={User}
          value={lastName}
          onChange={(e) => setLastName(e.target.value)}
          autoComplete="family-name"
        />
      </div>
      <Input
        label="Email"
        type="email"
        icon={Mail}
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        autoComplete="email"
      />
      <Input
        label="Телефон"
        type="tel"
        icon={Phone}
        value={phone}
        onChange={(e) => setPhone(e.target.value)}
        autoComplete="tel"
      />
      <div>
        <Input
          label="Пароль"
          type="password"
          icon={Lock}
          value={password}
          onChange={(e) => {
            setPassword(e.target.value);
            setPasswordError(undefined);
          }}
          autoComplete="new-password"
          error={passwordError}
        />
        <p className="mt-1.5 text-xs text-warm-gray">
          Минимум {PASSWORD_MIN_LENGTH} символов, заглавная, строчная буква и цифра
        </p>
      </div>
      <Button type="submit" fullWidth isLoading={isLoading} className="mt-2">
        Зарегистрироваться
      </Button>
    </form>
  );
}
