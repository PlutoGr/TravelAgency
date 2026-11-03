import { useCallback, useRef, useState, type ChangeEvent, type FormEvent } from 'react';
import toast from 'react-hot-toast';
import { PageTransition } from '@/components/common';
import { Breadcrumbs } from '@/components/layout';
import { Avatar, Button, Input, Modal } from '@/components/ui';
import AvatarCropDialog from '@/components/profile/AvatarCropDialog';
import ManagerShortCard from '@/components/booking/ManagerShortCard';
import { deleteAvatar, updateProfile, uploadAvatar } from '@/api/auth';
import { getManagerCard } from '@/api/managerCard';
import { authFormError } from '@/domain/authFormErrors';
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
import { ProfileEmailForm, ProfilePasswordForm } from './ProfileSettingsAccount';
import { useAuthStore } from '@/store/authStore';
import type { User } from '@/types';

const BREADCRUMBS = [
  { label: 'Личный кабинет', path: '/dashboard' },
  { label: 'Профиль' },
];

const PASSWORD_MASK = '••••••••';

function formFrom(person: User | null) {
  return {
    firstName: person?.firstName ?? '',
    lastName: person?.lastName ?? '',
    phone: person?.phone ?? '',
  };
}

export default function ProfilePage() {
  const storeUser = useAuthStore((state) => state.user);
  const setUser = useAuthStore((state) => state.setUser);
  const isStaff = storeUser?.role === 'manager' || storeUser?.role === 'admin';
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [form, setForm] = useState(() => formFrom(storeUser));
  const [fieldErrors, setFieldErrors] = useState<ProfileFieldErrors>({});
  const [formError, setFormError] = useState<string | undefined>();
  const [avatarError, setAvatarError] = useState<string | undefined>();
  const [isSaving, setIsSaving] = useState(false);
  const [avatarBusy, setAvatarBusy] = useState(false);
  const [crop, setCrop] = useState<{ token: number; image: AvatarBitmap } | null>(null);
  const [cropSaving, setCropSaving] = useState(false);
  const [cropError, setCropError] = useState<string | undefined>();
  const [personalOpen, setPersonalOpen] = useState(false);
  const [emailOpen, setEmailOpen] = useState(false);
  const [passwordOpen, setPasswordOpen] = useState(false);
  const [clientViewOpen, setClientViewOpen] = useState(false);
  const [accountNotice, setAccountNotice] = useState<string | undefined>();
  const loadSavedCard = useCallback((id: string) => getManagerCard(id), []);
  // Поколение запросов аватара: устаревший ответ загрузки или удаления не применяется.
  const avatarRevision = useRef(0);
  const readGeneration = useRef(0);
  const savingRef = useRef(false);
  const accountBusyRef = useRef(false);

  const dialogOpen = personalOpen || emailOpen || passwordOpen || clientViewOpen;
  const windowBusy = dialogOpen || crop !== null || avatarBusy || isSaving || accountBusyRef.current;

  function setAccountBusy(busy: boolean) {
    accountBusyRef.current = busy;
  }

  function closePersonal() {
    if (savingRef.current) return;
    setPersonalOpen(false);
    setFieldErrors({});
    setFormError(undefined);
  }

  function closeEmail() {
    if (accountBusyRef.current) return;
    setEmailOpen(false);
  }

  function closePassword() {
    if (accountBusyRef.current) return;
    setPasswordOpen(false);
  }

  function openPersonal() {
    if (windowBusy) return;
    const person = useAuthStore.getState().user;
    setForm(formFrom(person));
    setFieldErrors({});
    setFormError(undefined);
    setPersonalOpen(true);
  }

  function openEmail() {
    if (windowBusy) return;
    setAccountNotice(undefined);
    setEmailOpen(true);
  }

  function openPassword() {
    if (windowBusy) return;
    setAccountNotice(undefined);
    setPasswordOpen(true);
  }

  function openClientView() {
    if (!isStaff || windowBusy) return;
    setClientViewOpen(true);
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

  async function handleSave(event?: FormEvent) {
    event?.preventDefault();
    if (savingRef.current || crop) return;
    setFormError(undefined);
    const { payload, errors } = profileCardPayload(form);
    setFieldErrors(errors);
    if (!payload) return;

    savingRef.current = true;
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
      setPersonalOpen(false);
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
      savingRef.current = false;
      setIsSaving(false);
    }
  }

  function openFilePicker() {
    if (avatarBusy || crop || dialogOpen || isSaving) return;
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
    if (!file || crop || dialogOpen || isSaving) return;

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
    if (!url || avatarBusy || crop || dialogOpen || isSaving) return;
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
    if (crop || avatarBusy || dialogOpen || isSaving) return;
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
  const photoLocked = avatarBusy || crop !== null || dialogOpen || isSaving;

  return (
    <PageTransition>
      <div className="space-y-6">
        <Breadcrumbs items={BREADCRUMBS} />

        <h1 className="font-heading text-2xl font-bold text-dark">Профиль</h1>

        <div className="flex flex-wrap items-start justify-start gap-6">
          <section aria-label="Личные данные" className="max-w-full shrink-0 rounded-[16px] bg-white p-6 shadow-card">
            <h2 className="font-heading text-lg font-semibold text-dark">Личные данные</h2>
            <div className="mt-6 flex items-start gap-5">
              <Avatar src={storeUser.avatar} name={fullName} size="xl" />
              <div>
                <p className="font-heading text-lg font-semibold text-dark">{fullName}</p>
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
                <div className="mt-3">
                  <p className="text-xs font-medium text-warm-gray">Телефон</p>
                  {storeUser.phone ? <p className="mt-1 text-sm text-dark">{storeUser.phone}</p> : null}
                </div>
                <div className="mt-4 flex flex-col items-start gap-2">
                  <Button type="button" onClick={openPersonal}>
                    Редактировать
                  </Button>
                  {isStaff && (
                    <Button type="button" onClick={openClientView}>
                      Как меня видит клиент
                    </Button>
                  )}
                </div>
              </div>
            </div>
          </section>

          <section aria-label="Вход в аккаунт" className="max-w-full shrink-0 rounded-[16px] bg-white p-6 shadow-card">
            <h2 className="font-heading text-lg font-semibold text-dark">Вход в аккаунт</h2>
            <div className="mt-6 space-y-5">
              <div>
                <p className="text-xs font-medium text-warm-gray">Email</p>
                <div className="mt-1 flex flex-wrap items-center gap-3">
                  <p className="text-sm text-dark">{storeUser.email}</p>
                  <Button type="button" size="sm" variant="secondary" onClick={openEmail}>
                    Сменить email
                  </Button>
                </div>
              </div>
              <div>
                <p className="text-xs font-medium text-warm-gray">Пароль</p>
                <p className="mt-1 text-sm tracking-widest text-dark">{PASSWORD_MASK}</p>
                <p className="mt-1 text-xs text-warm-gray">Длина не показана.</p>
                <div className="mt-3">
                  <Button type="button" size="sm" variant="secondary" onClick={openPassword}>
                    Сменить пароль
                  </Button>
                </div>
              </div>
              {accountNotice && <p className="text-sm text-dark">{accountNotice}</p>}
            </div>
          </section>
        </div>

        <input
          ref={fileInputRef}
          type="file"
          accept={AVATAR_ACCEPT}
          className="hidden"
          tabIndex={-1}
          aria-hidden="true"
          disabled={crop !== null || dialogOpen || isSaving}
          onChange={handleFile}
        />

        <Modal isOpen={personalOpen} onClose={closePersonal} title="Личные данные" dialogLabel="Личные данные">
          <form className="space-y-4" onSubmit={handleSave}>
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
            <Input
              label="Телефон"
              value={form.phone}
              onChange={(event) => handleChange('phone', event.target.value)}
              error={fieldErrors.phone}
            />
            <div className="flex justify-end gap-2">
              <Button type="button" variant="ghost" onClick={closePersonal}>
                Отмена
              </Button>
              <Button type="submit" isLoading={isSaving}>
                Сохранить
              </Button>
            </div>
            {formError && (
              <p role="alert" className="text-sm text-red-500">
                {formError}
              </p>
            )}
          </form>
        </Modal>

        <Modal isOpen={emailOpen} onClose={closeEmail} title="Сменить email" dialogLabel="Сменить email">
          <ProfileEmailForm
            currentEmail={storeUser.email}
            onSucceeded={(message) => {
              setEmailOpen(false);
              setAccountNotice(message);
            }}
            onBusy={setAccountBusy}
          />
        </Modal>

        <Modal isOpen={passwordOpen} onClose={closePassword} title="Сменить пароль" dialogLabel="Сменить пароль">
          <ProfilePasswordForm
            onSucceeded={(message) => {
              setPasswordOpen(false);
              setAccountNotice(message);
            }}
            onBusy={setAccountBusy}
          />
        </Modal>

        {clientViewOpen && isStaff && (
          <ManagerShortCard
            userId={storeUser.id}
            onClose={() => setClientViewOpen(false)}
            loadCard={loadSavedCard}
          />
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
      </div>
    </PageTransition>
  );
}
