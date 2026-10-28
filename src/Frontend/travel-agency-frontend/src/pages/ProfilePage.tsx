import { useCallback, useRef, useState, type ChangeEvent } from 'react';
import { useSearchParams } from 'react-router-dom';
import toast from 'react-hot-toast';
import { PageTransition } from '@/components/common';
import { Breadcrumbs } from '@/components/layout';
import { Avatar, Button, Card, Input, Tabs } from '@/components/ui';
import ManagerShortCard from '@/components/booking/ManagerShortCard';
import { deleteAvatar, updateProfile, uploadAvatar } from '@/api/auth';
import { getManagerCard } from '@/api/managerCard';
import { authFormError } from '@/domain/authFormErrors';
import { ALLOWED_LANGUAGE_CODES } from '@/domain/allowedLanguages';
import {
  aboutCharactersLeft,
  avatarFileError,
  AVATAR_ACCEPT,
  AVATAR_UPLOAD_ERROR,
  PASSWORD_HINT,
  profileCardPayload,
  profileProblemFields,
  type ProfileField,
  type ProfileFieldErrors,
} from '@/domain/profileForm';
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

const LATER = 'Появится позже';

function formFrom(person: User | null) {
  return {
    firstName: person?.firstName ?? '',
    lastName: person?.lastName ?? '',
    phone: person?.phone ?? '',
    email: person?.email ?? '',
    passport: person?.passport ?? '',
    city: person?.city ?? '',
    about: person?.about ?? '',
    tourismSince: person?.tourismSince != null ? String(person.tourismSince) : '',
    languages: [...(person?.languages ?? [])],
    specialization: person?.specialization ?? '',
  };
}

function cardSnapshot(person: User | null): string | null {
  if (!person) return null;
  return [
    person.id,
    person.firstName,
    person.lastName,
    person.phone,
    person.email,
    person.passport ?? '',
    person.city,
    person.about ?? '',
    person.tourismSince ?? '',
    (person.languages ?? []).join('\u0001'),
    person.specialization ?? '',
  ].join('\u0000');
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
  const [clientViewOpen, setClientViewOpen] = useState(false);
  const loadSavedCard = useCallback((id: string) => getManagerCard(id), []);
  // Поколение запросов аватара: устаревший ответ загрузки или удаления не применяется.
  const avatarRevision = useRef(0);

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

  function handleLanguages(event: ChangeEvent<HTMLSelectElement>) {
    const languages = Array.from(event.target.selectedOptions, (option) => option.value);
    setForm((prev) => ({ ...prev, languages }));
    setFieldErrors((prev) => ({ ...prev, languages: undefined }));
  }

  async function handleSave() {
    setFormError(undefined);
    const { payload, errors } = profileCardPayload(form, {
      includeManagerFields: isStaff,
    });
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
      if (serverFields.city) next.city = serverFields.city;
      if (serverFields.about) next.about = serverFields.about;
      if (serverFields.tourismSince) next.tourismSince = serverFields.tourismSince;
      if (serverFields.specialization) next.specialization = serverFields.specialization;
      if (serverFields.languages?.length) next.languages = serverFields.languages;
      if (
        next.firstName ||
        next.lastName ||
        next.phone ||
        next.city ||
        next.about ||
        next.tourismSince ||
        next.specialization ||
        next.languages?.length
      ) {
        setFieldErrors(next);
      } else {
        setFormError(authFormError(err, 'profile').text);
      }
    } finally {
      setIsSaving(false);
    }
  }

  function openFilePicker() {
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
    if (!file) return;

    const problem = avatarFileError(file);
    if (problem) {
      setAvatarError(problem);
      return;
    }

    setAvatarError(undefined);
    setAvatarBusy(true);
    const revision = ++avatarRevision.current;
    try {
      const updated = await uploadAvatar(file);
      applyAvatar(updated, revision);
    } catch (err) {
      setAvatarError(profileProblemFields(err).file ?? AVATAR_UPLOAD_ERROR);
    } finally {
      setAvatarBusy(false);
    }
  }

  async function handleDeleteAvatar() {
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
                        disabled={avatarBusy}
                      >
                        Заменить
                      </Button>
                      <Button
                        type="button"
                        size="sm"
                        variant="ghost"
                        onClick={handleDeleteAvatar}
                        disabled={avatarBusy}
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
                      disabled={avatarBusy}
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
              disabled={avatarBusy}
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

              <Input
                label="Город"
                value={form.city}
                onChange={(event) => handleChange('city', event.target.value)}
                error={fieldErrors.city}
              />

              <Input label="Email" value={form.email} disabled />

              <div>
                <Input
                  label="Паспорт"
                  value={form.passport}
                  disabled
                  aria-describedby="profile-passport-later"
                />
                <p id="profile-passport-later" className="mt-1.5 text-sm text-warm-gray">
                  {LATER}
                </p>
              </div>

              {isStaff && (
                <>
                  <div>
                    <label htmlFor="profile-about" className="mb-1.5 block text-xs font-medium text-primary">
                      О себе
                    </label>
                    <textarea
                      id="profile-about"
                      value={form.about}
                      rows={5}
                      onChange={(event) => handleChange('about', event.target.value)}
                      className="w-full rounded-[12px] border border-sand bg-white px-4 py-3 text-dark outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/10"
                    />
                    <p className="mt-1.5 text-sm text-warm-gray">
                      осталось {aboutCharactersLeft(form.about)} из 1000
                    </p>
                    {fieldErrors.about && (
                      <p className="mt-1.5 text-sm text-red-500">{fieldErrors.about}</p>
                    )}
                  </div>

                  <Input
                    label="В туризме с"
                    value={form.tourismSince}
                    onChange={(event) => handleChange('tourismSince', event.target.value)}
                    error={fieldErrors.tourismSince}
                    inputMode="numeric"
                  />

                  <div>
                    <label
                      htmlFor="profile-languages"
                      className="mb-1.5 block text-xs font-medium text-primary"
                    >
                      Языки
                    </label>
                    <select
                      id="profile-languages"
                      multiple
                      size={8}
                      value={form.languages}
                      onChange={handleLanguages}
                      className="w-full rounded-[12px] border border-sand bg-white px-4 py-2 text-dark outline-none"
                    >
                      {ALLOWED_LANGUAGE_CODES.map((code) => (
                        <option key={code} value={code}>
                          {code}
                        </option>
                      ))}
                    </select>
                    {(fieldErrors.languages ?? []).map((text) => (
                      <p key={text} className="mt-1.5 text-sm text-red-500">
                        {text}
                      </p>
                    ))}
                  </div>

                  <Input
                    label="Специализация"
                    value={form.specialization}
                    onChange={(event) => handleChange('specialization', event.target.value)}
                    error={fieldErrors.specialization}
                  />

                  <div>
                    <Button type="button" onClick={() => setClientViewOpen(true)}>
                      Как меня видит клиент
                    </Button>
                  </div>
                  {clientViewOpen && storeUser && (
                    <ManagerShortCard
                      userId={storeUser.id}
                      onClose={() => setClientViewOpen(false)}
                      loadCard={loadSavedCard}
                    />
                  )}
                </>
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
          </Card>
        ) : (
          <Card className="mx-auto max-w-2xl space-y-8 p-6 sm:p-8">
            <section className="space-y-4" aria-label="Пароль">
              <h2 className="font-heading text-lg font-semibold text-dark">Пароль</h2>
              <Input label="Текущий пароль" type="password" disabled />
              <div>
                <Input
                  label="Новый пароль"
                  type="password"
                  disabled
                  aria-describedby="profile-password-hint"
                />
                <p id="profile-password-hint" className="mt-1.5 text-xs text-warm-gray">
                  {PASSWORD_HINT}
                </p>
              </div>
              <Input label="Повторите пароль" type="password" disabled />
              <p className="text-sm text-warm-gray">{LATER}</p>
              <Button type="button" disabled>
                Сменить пароль
              </Button>
            </section>

            <section className="space-y-3" aria-label="Уведомления">
              <h2 className="font-heading text-lg font-semibold text-dark">Уведомления</h2>
              {isStaff ? (
                <>
                  <label className="flex items-center gap-3 text-sm text-dark">
                    <input type="checkbox" checked={false} disabled onChange={() => undefined} />
                    Письмо о новой заявке
                  </label>
                  <label className="flex items-center gap-3 text-sm text-dark">
                    <input type="checkbox" checked={false} disabled onChange={() => undefined} />
                    Письмо о новом сообщении
                  </label>
                </>
              ) : (
                <label className="flex items-center gap-3 text-sm text-dark">
                  <input type="checkbox" checked={false} disabled onChange={() => undefined} />
                  Уведомления на email о новых сообщениях
                </label>
              )}
              <p className="text-sm text-warm-gray">{LATER}</p>
            </section>

            <section className="space-y-3" aria-label="Видимость" aria-disabled="true">
              <h2 className="font-heading text-lg font-semibold text-dark">Видимость</h2>
              <p className="text-sm text-warm-gray">{LATER}</p>
            </section>
          </Card>
        )}
      </div>
    </PageTransition>
  );
}
