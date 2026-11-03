import { useCallback, useRef, useState, type ChangeEvent } from 'react';
import { useSearchParams } from 'react-router-dom';
import toast from 'react-hot-toast';
import { PageTransition } from '@/components/common';
import { Breadcrumbs } from '@/components/layout';
import { Avatar, Button, Card, Input, Tabs } from '@/components/ui';
import AvatarCropDialog from '@/components/profile/AvatarCropDialog';
import ManagerShortCard from '@/components/booking/ManagerShortCard';
import { deleteAvatar, updateProfile, uploadAvatar } from '@/api/auth';
import { getManagerCard } from '@/api/managerCard';
import { authFormError } from '@/domain/authFormErrors';
import { ACCOUNT_TEXT } from '@/domain/accountLinks';
import { inspectAvatarFile, loadStraight, type AvatarBitmap } from '@/domain/avatarCrop';
import {
  AVATAR_ACCEPT,
  AVATAR_OPEN_ERROR,
  avatarUploadMessage,
  profileCardPayload,
  profileProblemFields,
  type ProfileField,
  type ProfileFieldErrors,
} from '@/domain/profileForm';
import { ProfileEmailBlock, ProfilePasswordBlock } from './ProfileSettingsAccount';
import { useAuthStore } from '@/store/authStore';
import type { User } from '@/types';

const BREADCRUMBS = [
  { label: 'Личный кабинет', path: '/dashboard' },
  { label: 'Профиль' },
];

const TABS = [
  { id: 'profile', label: 'Профиль' },
  { id: 'settings', label: 'Настройки' },
];

function formFrom(person: User | null) {
  return {
    firstName: person?.firstName ?? '',
    lastName: person?.lastName ?? '',
    phone: person?.phone ?? '',
    email: person?.email ?? '',
  };
}

function cardSnapshot(person: User | null): string | null {
  if (!person) return null;
  return [person.id, person.firstName, person.lastName, person.phone, person.email].join('\u0000');
}

export default function ProfilePage() {
  const storeUser = useAuthStore((state) => state.user);
  const setUser = useAuthStore((state) => state.setUser);
  const isStaff = storeUser?.role === 'manager' || storeUser?.role === 'admin';
  const [searchParams, setSearchParams] = useSearchParams();
  const tab = searchParams.get('tab') === 'settings' ? 'settings' : 'profile';
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [formSource, setFormSource] = useState(storeUser);
  const [form, setForm] = useState(() => formFrom(storeUser));
  const [fieldErrors, setFieldErrors] = useState<ProfileFieldErrors>({});
  const [formError, setFormError] = useState<string | undefined>();
  const [avatarError, setAvatarError] = useState<string | undefined>();
  const [isSaving, setIsSaving] = useState(false);
  const [avatarBusy, setAvatarBusy] = useState(false);
  const [crop, setCrop] = useState<{ token: number; image: AvatarBitmap } | null>(null);
  const [cropSaving, setCropSaving] = useState(false);
  const [cropError, setCropError] = useState<string | undefined>();
  const [clientViewOpen, setClientViewOpen] = useState(false);
  const loadSavedCard = useCallback((id: string) => getManagerCard(id), []);
  // Поколение запросов аватара: устаревший ответ загрузки или удаления не применяется.
  const avatarRevision = useRef(0);
  const readGeneration = useRef(0);

  if (storeUser !== formSource) {
    const cardChanged = cardSnapshot(storeUser) !== cardSnapshot(formSource);
    setFormSource(storeUser);
    if (cardChanged) setForm(formFrom(storeUser));
  }

  function selectTab(id: string) {
    setSearchParams(id === 'settings' ? { tab: 'settings' } : {}, { replace: true });
  }

  function handleChange(field: ProfileField, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }));
    setFieldErrors((prev) => ({ ...prev, [field]: undefined }));
  }

  function closeCrop() {
    crop?.image.close?.();
    setCrop(null);
    setCropError(undefined);
  }

  async function handleSave() {
    if (crop) return;
    setFormError(undefined);
    const { payload, errors } = profileCardPayload(form);
    setFieldErrors(errors);
    if (!payload) return;

    setIsSaving(true);
    const avatarAtSave = useAuthStore.getState().user?.avatar;
    try {
      const updated = await updateProfile(payload);
      const current = useAuthStore.getState().user;
      setUser(
        current != null && current.avatar !== avatarAtSave
          ? { ...updated, avatar: current.avatar }
          : updated,
      );
      toast.success('Профиль успешно обновлён');
    } catch (err) {
      const serverFields = profileProblemFields(err);
      const next: ProfileFieldErrors = {};
      if (serverFields.firstName) next.firstName = serverFields.firstName;
      if (serverFields.lastName) next.lastName = serverFields.lastName;
      if (serverFields.phone) next.phone = serverFields.phone;
      if (next.firstName || next.lastName || next.phone) {
        setFieldErrors(next);
      } else {
        setFormError(authFormError(err, 'profile').text);
      }
    } finally {
      setIsSaving(false);
    }
  }

  function openFilePicker() {
    if (avatarBusy || crop) return;
    fileInputRef.current?.click();
  }

  function applyAvatar(updated: User, revision: number) {
    if (revision !== avatarRevision.current) return;
    const current = useAuthStore.getState().user;
    setUser(current ? { ...current, avatar: updated.avatar } : updated);
  }

  async function handleFile(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file || crop) return;

    const generation = ++readGeneration.current;
    setAvatarBusy(true);
    try {
      const result = await inspectAvatarFile(file);
      if (generation !== readGeneration.current) {
        if (result.ok) result.image.close?.();
        return;
      }
      if (!result.ok) {
        setAvatarError(result.message);
        return;
      }
      setAvatarError(undefined);
      setCropError(undefined);
      setCrop({ token: generation, image: result.image });
    } finally {
      if (generation === readGeneration.current) setAvatarBusy(false);
    }
  }

  async function handleCropSave(file: File) {
    setCropSaving(true);
    setAvatarBusy(true);
    setCropError(undefined);
    const revision = ++avatarRevision.current;
    try {
      const updated = await uploadAvatar(file);
      if (revision !== avatarRevision.current) return;
      applyAvatar(updated, revision);
      crop?.image.close?.();
      setCrop(null);
    } catch (err) {
      if (revision !== avatarRevision.current) return;
      setCropError(avatarUploadMessage(err));
    } finally {
      if (revision === avatarRevision.current) {
        setCropSaving(false);
        setAvatarBusy(false);
      }
    }
  }

  async function handleReframe() {
    const url = useAuthStore.getState().user?.avatar;
    if (!url || avatarBusy || crop) return;
    const generation = ++readGeneration.current;
    setAvatarBusy(true);
    try {
      const response = await fetch(url, { credentials: 'same-origin' });
      if (!response.ok) throw new Error('status');
      const blob = await response.blob();
      if (blob.size === 0) throw new Error('empty');
      const image = await loadStraight(blob);
      if (generation !== readGeneration.current) {
        image.close?.();
        return;
      }
      setAvatarError(undefined);
      setCropError(undefined);
      setCrop({ token: generation, image });
    } catch {
      if (generation !== readGeneration.current) return;
      setAvatarError(AVATAR_OPEN_ERROR);
    } finally {
      if (generation === readGeneration.current) setAvatarBusy(false);
    }
  }

  async function handleDeleteAvatar() {
    if (crop || avatarBusy) return;
    setAvatarError(undefined);
    setAvatarBusy(true);
    const revision = ++avatarRevision.current;
    try {
      const updated = await deleteAvatar();
      applyAvatar(updated, revision);
    } catch (err) {
      setAvatarError(authFormError(err, 'profile').text);
    } finally {
      setAvatarBusy(false);
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

  const fullName = `${storeUser.firstName} ${storeUser.lastName}`;
  const photoLocked = avatarBusy || crop !== null;

  return (
    <PageTransition>
      <div className="space-y-6">
        <Breadcrumbs items={BREADCRUMBS} />

        <h1 className="font-heading text-2xl font-bold text-dark">Профиль</h1>

        <Tabs tabs={TABS} activeTab={tab} onChange={selectTab} />

        {tab === 'profile' ? (
          <Card className="mx-auto max-w-2xl p-6 sm:p-8">
            <div className="mb-8 flex items-center gap-5">
              <Avatar src={storeUser.avatar} name={fullName} size="xl" />
              <div>
                <h2 className="font-heading text-lg font-semibold text-dark">{fullName}</h2>
                <p className="text-sm text-warm-gray">{storeUser.email}</p>
                <div className="mt-3 flex flex-wrap gap-2">
                  {storeUser.avatar ? (
                    <>
                      <Button
                        type="button"
                        size="sm"
                        variant="secondary"
                        onClick={openFilePicker}
                        disabled={photoLocked}
                      >
                        Заменить
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant="secondary"
                        onClick={handleReframe}
                        disabled={photoLocked}
                      >
                        Изменить положение фото
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant="ghost"
                        onClick={handleDeleteAvatar}
                        disabled={photoLocked}
                      >
                        Удалить фото
                      </Button>
                    </>
                  ) : (
                    <Button
                      type="button"
                      size="sm"
                      variant="secondary"
                      onClick={openFilePicker}
                      disabled={photoLocked}
                    >
                      Загрузить фото
                    </Button>
                  )}
                </div>
                {avatarError && (
                  <p role="alert" className="mt-2 text-sm text-red-500">
                    {avatarError}
                  </p>
                )}
              </div>
            </div>

            <input
              ref={fileInputRef}
              type="file"
              accept={AVATAR_ACCEPT}
              className="hidden"
              tabIndex={-1}
              aria-hidden="true"
              disabled={crop !== null}
              onChange={handleFile}
            />

            <div className="space-y-4">
              <div className="grid gap-4 sm:grid-cols-2">
                <Input
                  label="Имя"
                  value={form.firstName}
                  onChange={(event) => handleChange('firstName', event.target.value)}
                  error={fieldErrors.firstName}
                />
                <Input
                  label="Фамилия"
                  value={form.lastName}
                  onChange={(event) => handleChange('lastName', event.target.value)}
                  error={fieldErrors.lastName}
                />
              </div>

              <Input
                label="Телефон"
                value={form.phone}
                onChange={(event) => handleChange('phone', event.target.value)}
                error={fieldErrors.phone}
              />

              <div>
                <Input label="Email" value={form.email} disabled />
                <p className="mt-1.5 text-xs text-warm-gray">{ACCOUNT_TEXT.emailHint}</p>
              </div>

              {isStaff && (
                <div>
                  <Button type="button" onClick={() => setClientViewOpen(true)}>
                    Как меня видит клиент
                  </Button>
                </div>
              )}
              {clientViewOpen && isStaff && (
                <ManagerShortCard
                  userId={storeUser.id}
                  onClose={() => setClientViewOpen(false)}
                  loadCard={loadSavedCard}
                />
              )}
            </div>

            <div className="mt-8 flex justify-end">
              <Button onClick={handleSave} isLoading={isSaving}>
                Сохранить
              </Button>
            </div>
            {formError && (
              <p role="alert" className="mt-4 text-sm text-red-500">
                {formError}
              </p>
            )}
            {crop && (
              <AvatarCropDialog
                key={crop.token}
                image={crop.image}
                saving={cropSaving}
                error={cropError}
                onCancel={closeCrop}
                onSave={handleCropSave}
              />
            )}
          </Card>
        ) : (
          <Card className="mx-auto max-w-2xl space-y-8 p-6 sm:p-8">
            <ProfilePasswordBlock />
            <ProfileEmailBlock currentEmail={storeUser.email} />
          </Card>
        )}
      </div>
    </PageTransition>
  );
}
