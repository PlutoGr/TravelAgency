import { useState, useEffect } from 'react';
import { Camera } from 'lucide-react';
import toast from 'react-hot-toast';
import { PageTransition } from '@/components/common';
import { Breadcrumbs } from '@/components/layout';
import { Card, Avatar, Button, Input } from '@/components/ui';
import { useAuthStore } from '@/store/authStore';
import { updateProfile } from '@/api/auth';

const BREADCRUMBS = [
  { label: 'Личный кабинет', path: '/dashboard' },
  { label: 'Профиль' },
];

export default function ProfilePage() {
  const storeUser = useAuthStore((s) => s.user);
  const setUser = useAuthStore((s) => s.setUser);

  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    phone: '',
    email: '',
    passport: '',
  });
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    if (storeUser) {
      setForm({
        firstName: storeUser.firstName,
        lastName: storeUser.lastName,
        phone: storeUser.phone,
        email: storeUser.email,
        passport: storeUser.passport ?? '',
      });
    }
  }, [storeUser]);

  function handleChange(field: string, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  async function handleSave() {
    setIsSaving(true);
    try {
      const updated = await updateProfile({
        firstName: form.firstName,
        lastName: form.lastName,
        phone: form.phone,
      });
      setUser(updated);
      toast.success('Профиль успешно обновлён');
    } catch {
      toast.error('Не удалось сохранить изменения');
    } finally {
      setIsSaving(false);
    }
  }

  if (!storeUser) {
    return (
      <PageTransition>
        <div className="flex items-center justify-center p-12">
          <p className="text-warm-gray">Загрузка профиля...</p>
        </div>
      </PageTransition>
    );
  }

  const user = storeUser;

  return (
    <PageTransition>
      <div className="space-y-6">
        <Breadcrumbs items={BREADCRUMBS} />

        <h1 className="font-heading text-2xl font-bold text-dark">Профиль</h1>

        <Card className="mx-auto max-w-2xl p-6 sm:p-8">
          {/* Avatar section */}
          <div className="mb-8 flex items-center gap-5">
            <div className="relative">
              <Avatar
                src={user.avatar}
                name={`${user.firstName} ${user.lastName}`}
                size="xl"
              />
              <button className="absolute -bottom-1 -right-1 flex h-8 w-8 items-center justify-center rounded-full bg-primary text-white shadow-button transition-colors hover:bg-primary-light">
                <Camera size={14} />
              </button>
            </div>
            <div>
              <h2 className="font-heading text-lg font-semibold text-dark">
                {user.firstName} {user.lastName}
              </h2>
              <p className="text-sm text-warm-gray">{user.email}</p>
            </div>
          </div>

          {/* Form */}
          <div className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-2">
              <Input
                label="Имя"
                value={form.firstName}
                onChange={(e) => handleChange('firstName', e.target.value)}
              />
              <Input
                label="Фамилия"
                value={form.lastName}
                onChange={(e) => handleChange('lastName', e.target.value)}
              />
            </div>

            <Input
              label="Телефон"
              value={form.phone}
              onChange={(e) => handleChange('phone', e.target.value)}
            />

            <Input
              label="Email"
              value={form.email}
              disabled
            />

            <Input
              label="Паспорт"
              value={form.passport}
              disabled
              placeholder="Пока не поддерживается"
              title="Редактирование паспортных данных пока недоступно"
            />
          </div>

          <div className="mt-8 flex justify-end">
            <Button onClick={handleSave} isLoading={isSaving}>
              Сохранить
            </Button>
          </div>
        </Card>
      </div>
    </PageTransition>
  );
}
