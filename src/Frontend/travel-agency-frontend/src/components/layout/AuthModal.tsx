import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Mail, Lock, User, Phone } from 'lucide-react';
import toast from 'react-hot-toast';
import { useAuthStore } from '@/store/authStore';
import { useUIStore } from '@/store/uiStore';
import { Modal, Tabs, Input, Button } from '@/components/ui';

const AUTH_TABS = [
  { id: 'login', label: 'Вход' },
  { id: 'register', label: 'Регистрация' },
] as const;

export default function AuthModal() {
  const { isAuthModalOpen, authModalTab, closeAuthModal, openAuthModal } =
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
                  } catch {
                    toast.error('Ошибка регистрации. Попробуйте ещё раз.');
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

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!firstName.trim() || !lastName.trim() || !email.trim() || !phone.trim() || !password.trim()) {
      toast.error('Заполните все поля');
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
      <Input
        label="Пароль"
        type="password"
        icon={Lock}
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        autoComplete="new-password"
      />
      <Button type="submit" fullWidth isLoading={isLoading} className="mt-2">
        Зарегистрироваться
      </Button>
    </form>
  );
}
