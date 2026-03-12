import { useState } from 'react';
import { Camera } from 'lucide-react';
import toast from 'react-hot-toast';
import type { User } from '@/types';
import { PageTransition } from '@/components/common';
import { Breadcrumbs } from '@/components/layout';
import { Card, Avatar, Button, Input } from '@/components/ui';
import { useAuthStore } from '@/store/authStore';
import { updateProfile } from '@/api/auth';
import { mockUser } from '@/mocks/users';

const BREADCRUMBS = [
  { label: 'Личный кабинет', path: '/dashboard' },
  { label: 'Профиль' },
];

export default function ProfilePage() {
  const storeUser = useAuthStore((s) => s.user);
  const setUser = useAuthStore((s) => s.setUser);
  const user = storeUser ?? mockUser;

  const [form, setForm] = useState({
    firstName: user.firstName,
    lastName: user.lastName,
    phone: user.phone,
    email: user.email,
    passport: user.passport ?? '',
  });
  const [isSaving, setIsSaving] = useState(false);

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
        passport: form.passport || undefined,
      });
      setUser(updated);
      toast.success('Профиль успешно обновлён');
    } catch {
      toast.error('Не удалось сохранить изменения');
    } finally {
      setIsSaving(false);
    }
  }

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
              onChange={(e) => handleChange('passport', e.target.value)}
              placeholder="Серия и номер"
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
