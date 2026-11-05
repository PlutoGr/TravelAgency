import { useCallback, useEffect, useRef, useState, type ChangeEvent, type FormEvent } from 'react';
import { useLocation } from 'react-router-dom';
import toast from 'react-hot-toast';
import { PageTransition } from '@/components/common';
import { Breadcrumbs } from '@/components/layout';
import { Avatar, Button, Input, Modal } from '@/components/ui';
import AvatarCropDialog from '@/components/profile/AvatarCropDialog';
import ManagerCardWorkspace from '@/components/profile/ManagerCardWorkspace';
import { deleteAvatar, updateProfile, uploadAvatar } from '@/api/auth';
import { getManagerCard, updateManagerCard, type ManagerPublicCard } from '@/api/managerCard';
import { authFormError } from '@/domain/authFormErrors';
import { inspectAvatarFile, loadStraight, type AvatarBitmap } from '@/domain/avatarCrop';
import { managerCardProblemFields, type ManagerCardWrite } from '@/domain/managerCardForm';
import {
  AVATAR_ACCEPT,
  AVATAR_OPEN_ERROR,
  avatarUploadMessage,
  profileCardPayload,
  profileProblemFields,
  type ProfileField,
  type ProfileFieldErrors,
} from '@/domain/profileForm';
import { ProfilePasswordForm } from './ProfileSettingsAccount';
import { useAuthStore } from '@/store/authStore';
import type { User } from '@/types';

const BREADCRUMBS = [
  { label: 'Личный кабинет', path: '/dashboard' },
  { label: 'Профиль' },
];

const PASSWORD_MASK = '••••••••';
const serif = { fontFamily: 'Georgia, "Times New Roman", serif' };
const panel =
  'max-w-full w-full shrink-0 rounded-[28px] border border-[#30281c]/10 bg-[#fffcf8] p-6 shadow-[0_24px_60px_#30241214]';

function formFrom(person: User | null) {
  return {
    firstName: person?.firstName ?? '',
    lastName: person?.lastName ?? '',
    phone: person?.phone ?? '',
  };
}

function initialTab(search: string): 'card' | 'account' {
  const tab = new URLSearchParams(search).get('tab');
  return tab === 'settings' || tab === 'account' ? 'account' : 'card';
}

export default function ProfilePage() {
  const location = useLocation();
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
  const [passwordOpen, setPasswordOpen] = useState(false);
  const [accountNotice, setAccountNotice] = useState<string | undefined>();
  const [accountBusy, setAccountBusyState] = useState(false);
  const [tab, setTab] = useState<'card' | 'account'>(() => initialTab(location.search));
  const [card, setCard] = useState<ManagerPublicCard | null>(null);
  const [cardPhase, setCardPhase] = useState<'idle' | 'loading' | 'error' | 'ready'>('idle');
  const [cardReload, setCardReload] = useState(0);
  const avatarRevision = useRef(0);
  const readGeneration = useRef(0);
  const savingRef = useRef(false);
  const accountBusyRef = useRef(false);
  const cardSeq = useRef(0);
  const userId = storeUser?.id;
  const avatar = storeUser?.avatar;
  const firstName = storeUser?.firstName;
  const lastName = storeUser?.lastName;

  const dialogOpen = personalOpen || passwordOpen;
  const windowBusy = dialogOpen || crop !== null || avatarBusy || isSaving || accountBusy;

  function setAccountBusy(busy: boolean) {
    accountBusyRef.current = busy;
    setAccountBusyState(busy);
  }

  function closePersonal() {
    if (savingRef.current) return;
    setPersonalOpen(false);
    setFieldErrors({});
    setFormError(undefined);
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

  function openPassword() {
    if (windowBusy) return;
    setAccountNotice(undefined);
    setPasswordOpen(true);
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

  const showCard = isStaff && tab === 'card';

  useEffect(() => {
    if (!showCard || !userId) return;
    let cancelled = false;
    const seq = ++cardSeq.current;
    getManagerCard(userId).then(
      (value) => {
        if (cancelled || seq !== cardSeq.current) return;
        setCard(value);
        setCardPhase('ready');
      },
      () => {
        if (cancelled || seq !== cardSeq.current) return;
        setCardPhase((current) => (current === 'ready' ? current : 'error'));
      },
    );
    return () => {
      cancelled = true;
    };
  }, [showCard, userId, avatar, firstName, lastName, cardReload]);

  const saveCard = useCallback(async (payload: ManagerCardWrite) => {
    try {
      const updated = await updateManagerCard(payload);
      setCard(updated);
      setCardPhase('ready');
      toast.success('Визитка сохранена');
    } catch (err) {
      const fields = managerCardProblemFields(err);
      if (fields.position || fields.tourismSince || fields.about || fields.tags) throw fields;
      throw { form: authFormError(err, 'profile').text };
    }
  }, []);

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
  const showAccount = !isStaff || tab === 'account';

  return (
    <PageTransition>
      <div className="space-y-6">
        <Breadcrumbs items={BREADCRUMBS} />

        <div>
          <h1 className="text-4xl font-medium text-[#1c1915]" style={serif}>
            Профиль
          </h1>
          <p className="mt-2 max-w-xl text-sm text-[#5c564e]">
            {isStaff
              ? 'Визитка, которую видят клиенты, и настройки вашего аккаунта.'
              : 'Ваши данные и вход в аккаунт.'}
          </p>
        </div>

        {isStaff ? (
          <div role="tablist" aria-label="Разделы профиля" className="flex flex-wrap gap-2">
            {(
              [
                ['card', 'Визитка для клиентов'],
                ['account', 'Аккаунт'],
              ] as const
            ).map(([id, label]) => {
              const selected = tab === id;
              return (
                <button
                  key={id}
                  type="button"
                  role="tab"
                  id={`profile-tab-${id}`}
                  aria-selected={selected}
                  aria-controls={`profile-panel-${id}`}
                  tabIndex={selected ? 0 : -1}
                  className={
                    selected
                      ? 'rounded-full bg-[#1c1915] px-4 py-2 text-sm font-semibold text-[#f3eee6]'
                      : 'rounded-full border border-[#30281c]/10 bg-[#fffcf8] px-4 py-2 text-sm font-medium text-[#3a342c]'
                  }
                  onClick={() => setTab(id)}
                  onKeyDown={(event) => {
                    if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') return;
                    event.preventDefault();
                    setTab(id === 'card' ? 'account' : 'card');
                  }}
                >
                  {label}
                </button>
              );
            })}
          </div>
        ) : null}

        {showCard ? (
          <div role="tabpanel" id="profile-panel-card" aria-labelledby="profile-tab-card">
            <ManagerCardWorkspace
              personName={fullName}
              avatarUrl={storeUser.avatar}
              card={card}
              phase={cardPhase}
              locked={windowBusy}
              onRetry={() => {
                setCardPhase('loading');
                setCardReload((value) => value + 1);
              }}
              onSave={saveCard}
            />
          </div>
        ) : null}

        {showAccount ? (
          <div
            role={isStaff ? 'tabpanel' : undefined}
            id={isStaff ? 'profile-panel-account' : undefined}
            aria-labelledby={isStaff ? 'profile-tab-account' : undefined}
            className="flex flex-col flex-wrap items-start gap-6"
          >
            <section aria-label="Личные данные" className={panel}>
              <div className="flex items-start justify-between gap-4">
                <h2 className="text-3xl font-medium text-[#1c1915]" style={serif}>
                  Личные данные
                </h2>
                <Button type="button" variant="ghost" size="sm" onClick={openPersonal}>
                  Изменить
                </Button>
              </div>
              <p className="mt-2 text-sm text-[#5c564e]">
                {isStaff
                  ? 'Фото и имя видят клиенты в вашей визитке. Телефон клиентам не показывается.'
                  : 'Так к вам будут обращаться менеджеры.'}
              </p>
              <div className="mt-6 flex min-w-0 flex-col items-start gap-5 sm:flex-row">
                <Avatar src={storeUser.avatar} name={fullName} size="xl" />
                <div className="min-w-0 w-full sm:w-auto sm:flex-1">
                  <p className="text-sm font-medium text-[#1c1915]">Фото профиля</p>
                  <p className="text-sm text-[#5c564e]">
                    {storeUser.avatar ? 'JPG или PNG до 5 МБ' : 'Фото не загружено. JPG или PNG до 5 МБ'}
                  </p>
                  <p className="mt-3 break-words font-heading text-lg font-semibold text-[#1c1915]">{fullName}</p>
                  <div className="mt-3 flex flex-wrap gap-2">
                    {storeUser.avatar ? (
                      <>
                        <Button type="button" size="sm" variant="secondary" className="shrink-0" onClick={openFilePicker} disabled={photoLocked}>
                          Заменить
                        </Button>
                        <Button
                          type="button"
                          size="sm"
                          variant="secondary"
                          className="shrink-0"
                          aria-label="Изменить положение фото"
                          onClick={handleReframe}
                          disabled={photoLocked}
                        >
                          Положение фото
                        </Button>
                        <Button
                          type="button"
                          size="sm"
                          variant="ghost"
                          className="shrink-0 text-[#8e3a34]"
                          aria-label="Удалить фото"
                          onClick={handleDeleteAvatar}
                          disabled={photoLocked}
                        >
                          Удалить
                        </Button>
                      </>
                    ) : (
                      <Button type="button" size="sm" variant="secondary" className="shrink-0" onClick={openFilePicker} disabled={photoLocked}>
                        Загрузить фото
                      </Button>
                    )}
                  </div>
                  {avatarError && (
                    <p role="alert" className="mt-2 text-sm text-red-500">
                      {avatarError}
                    </p>
                  )}
                  <div className="mt-4 border-t border-[#30281c]/10 pt-4">
                    <p className="text-xs font-medium text-[#7a7268]">Телефон</p>
                    {storeUser.phone ? <p className="mt-1 text-sm text-[#1c1915]">{storeUser.phone}</p> : null}
                  </div>
                </div>
              </div>
            </section>

            <section aria-label="Вход в аккаунт" className={panel}>
              <h2 className="text-3xl font-medium text-[#1c1915]" style={serif}>
                Вход в аккаунт
              </h2>
              <p className="mt-2 text-sm text-[#5c564e]">Данные для входа на сайт.</p>
              <div className="mt-6 space-y-5">
                <div className="border-t border-[#30281c]/10 pt-4">
                  <p className="text-xs font-medium text-[#7a7268]">Email</p>
                  <p className="mt-1 text-sm text-[#1c1915]">{storeUser.email}</p>
                  <p className="mt-1 text-xs text-[#5c564e]">
                    Email — ваш логин, сменить его самостоятельно нельзя. Если нужно, напишите в поддержку.
                  </p>
                </div>
                <div className="border-t border-[#30281c]/10 pt-4">
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div>
                      <p className="text-xs font-medium text-[#7a7268]">Пароль</p>
                      <p className="mt-1 text-sm tracking-widest text-[#1c1915]">{PASSWORD_MASK}</p>
                      <p className="mt-1 text-xs text-[#5c564e]">Длина не показана.</p>
                    </div>
                    <Button type="button" size="sm" variant="ghost" onClick={openPassword}>
                      Сменить пароль
                    </Button>
                  </div>
                </div>
                {accountNotice && <p className="text-sm text-[#1c1915]">{accountNotice}</p>}
                <p className="border-t border-[#30281c]/10 pt-4 text-sm text-[#5c564e]">
                  Связанные аккаунты Google, VK и Telegram появятся позже.
                </p>
              </div>
            </section>
          </div>
        ) : null}

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

        <Modal isOpen={personalOpen} onClose={closePersonal} title="Личные данные" dialogLabel="Личные данные" overlay="solid">
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
              <Button type="submit" variant="ink" pill isLoading={isSaving}>
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

        <Modal isOpen={passwordOpen} onClose={closePassword} title="Сменить пароль" dialogLabel="Сменить пароль" overlay="solid">
          <ProfilePasswordForm
            onSucceeded={(message) => {
              setPasswordOpen(false);
              setAccountNotice(message);
            }}
            onBusy={setAccountBusy}
          />
        </Modal>

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
