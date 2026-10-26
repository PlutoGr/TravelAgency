import { useEffect, useState } from 'react';
import { format, parseISO } from 'date-fns';
import { ru } from 'date-fns/locale';
import toast from 'react-hot-toast';
import { PageTransition } from '@/components/common';
import { Button, Input } from '@/components/ui';
import {
  getAdminSiteSettings,
  getSiteSettingsAudit,
  siteSettingsFieldErrors,
  updateSiteSettings,
} from '@/api/siteSettings';
import {
  AUDIT_FIELD_LABELS,
  SITE_SETTINGS_SAVED_MESSAGE,
  type SiteSettingsAuditItem,
  type SocialLink,
} from '@/domain/siteSettings';
import { useSiteSettingsStore } from '@/store/siteSettingsStore';

interface FormState {
  siteName: string;
  supportPhone: string;
  contactEmail: string;
  officeAddress: string;
  workingHours: string;
  socialLinks: SocialLink[];
  bannerText: string;
  bannerEnabled: boolean;
}

const EMPTY_FORM: FormState = {
  siteName: '',
  supportPhone: '',
  contactEmail: '',
  officeAddress: '',
  workingHours: '',
  socialLinks: [],
  bannerText: '',
  bannerEnabled: false,
};

function emptyToNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
}

export default function AdminSiteSettingsPage() {
  const setPublicSettings = useSiteSettingsStore((state) => state.setSettings);
  const [form, setForm] = useState<FormState>(EMPTY_FORM);
  const [audit, setAudit] = useState<SiteSettingsAuditItem[]>([]);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [loadError, setLoadError] = useState<string | null>(null);
  const [ready, setReady] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let cancelled = false;
    Promise.all([getAdminSiteSettings(), getSiteSettingsAudit()])
      .then(([settings, journal]) => {
        if (cancelled) return;
        setForm({
          siteName: settings.siteName ?? '',
          supportPhone: settings.supportPhone ?? '',
          contactEmail: settings.contactEmail ?? '',
          officeAddress: settings.officeAddress ?? '',
          workingHours: settings.workingHours ?? '',
          socialLinks: settings.socialLinks ?? [],
          bannerText: settings.bannerText ?? '',
          bannerEnabled: settings.bannerEnabled,
        });
        setAudit(journal);
        setReady(true);
      })
      .catch(() => {
        if (!cancelled) setLoadError('Не удалось загрузить настройки');
      });
    return () => {
      cancelled = true;
    };
  }, []);

  function patch(partial: Partial<FormState>) {
    setForm((current) => ({ ...current, ...partial }));
  }

  function patchLink(index: number, partial: Partial<SocialLink>) {
    setForm((current) => ({
      ...current,
      socialLinks: current.socialLinks.map((link, linkIndex) =>
        linkIndex === index ? { ...link, ...partial } : link,
      ),
    }));
  }

  async function handleSave() {
    setFieldErrors({});
    setSaving(true);
    try {
      const saved = await updateSiteSettings({
        siteName: form.siteName.trim(),
        supportPhone: emptyToNull(form.supportPhone),
        contactEmail: emptyToNull(form.contactEmail),
        officeAddress: emptyToNull(form.officeAddress),
        workingHours: emptyToNull(form.workingHours),
        socialLinks: form.socialLinks.map((link) => ({
          label: link.label.trim(),
          url: link.url.trim(),
        })),
        bannerText: emptyToNull(form.bannerText),
        bannerEnabled: form.bannerEnabled,
      });
      setPublicSettings(saved);
      toast.success(saved.message?.trim() || SITE_SETTINGS_SAVED_MESSAGE);
      const journal = await getSiteSettingsAudit();
      setAudit(journal);
    } catch (error) {
      const errors = siteSettingsFieldErrors(error);
      if (Object.keys(errors).length > 0) {
        setFieldErrors(errors);
      } else {
        toast.error('Не удалось сохранить настройки');
      }
    } finally {
      setSaving(false);
    }
  }

  const linkError = Object.entries(fieldErrors).find(([key]) => key.startsWith('socialLinks'))?.[1];

  return (
    <PageTransition>
      <h1 className="font-heading text-2xl font-bold text-dark sm:text-3xl">Настройки сайта</h1>
      <p className="mt-3 max-w-2xl text-sm text-warm-gray">
        Публичные контакты и объявление. Секреты и пароли здесь не хранятся.
      </p>
      {loadError && (
        <p role="alert" className="mt-4 text-sm text-red-500">
          {loadError}
        </p>
      )}

      <div className="mt-8 max-w-2xl space-y-4 rounded-2xl border border-sand bg-white p-5 sm:p-6">
        <Input
          label="Название сайта"
          value={form.siteName}
          error={fieldErrors.siteName}
          onChange={(event) => patch({ siteName: event.target.value })}
        />
        <Input
          label="Телефон поддержки"
          value={form.supportPhone}
          error={fieldErrors.supportPhone}
          onChange={(event) => patch({ supportPhone: event.target.value })}
        />
        <Input
          label="Email для связи"
          value={form.contactEmail}
          error={fieldErrors.contactEmail}
          onChange={(event) => patch({ contactEmail: event.target.value })}
        />
        <Input
          label="Адрес офиса"
          value={form.officeAddress}
          error={fieldErrors.officeAddress}
          onChange={(event) => patch({ officeAddress: event.target.value })}
        />
        <Input
          label="Часы работы"
          value={form.workingHours}
          error={fieldErrors.workingHours}
          onChange={(event) => patch({ workingHours: event.target.value })}
        />

        <fieldset className="space-y-3">
          <legend className="text-sm font-medium text-dark">Ссылки на соцсети и мессенджеры</legend>
          {form.socialLinks.map((link, index) => (
            <div key={index} className="grid gap-3 sm:grid-cols-[1fr_1.4fr_auto]">
              <Input
                label={`Подпись ${index + 1}`}
                value={link.label}
                onChange={(event) => patchLink(index, { label: event.target.value })}
              />
              <Input
                label={`Ссылка ${index + 1}`}
                value={link.url}
                onChange={(event) => patchLink(index, { url: event.target.value })}
              />
              <button
                type="button"
                className="text-sm text-red-500 hover:underline sm:mt-4"
                onClick={() =>
                  patch({ socialLinks: form.socialLinks.filter((_, linkIndex) => linkIndex !== index) })
                }
              >
                Удалить
              </button>
            </div>
          ))}
          <button
            type="button"
            className="text-sm font-medium text-primary hover:underline disabled:opacity-50"
            disabled={form.socialLinks.length >= 10}
            onClick={() => patch({ socialLinks: [...form.socialLinks, { label: '', url: '' }] })}
          >
            Добавить ссылку
          </button>
          {linkError && <p className="text-sm text-red-500">{linkError}</p>}
        </fieldset>

        <label className="block text-sm font-medium text-dark" htmlFor="banner-text">
          Текст баннера
          <textarea
            id="banner-text"
            value={form.bannerText}
            onChange={(event) => patch({ bannerText: event.target.value })}
            rows={3}
            className="mt-1.5 w-full rounded-[12px] border border-sand px-4 py-3 text-base font-normal text-dark outline-none focus:border-primary focus:ring-2 focus:ring-primary/10"
          />
        </label>
        {fieldErrors.bannerText && <p className="text-sm text-red-500">{fieldErrors.bannerText}</p>}

        <label className="flex items-center gap-3 text-sm text-dark">
          <input
            type="checkbox"
            checked={form.bannerEnabled}
            onChange={(event) => patch({ bannerEnabled: event.target.checked })}
          />
          Показывать баннер
        </label>

        <div className="flex justify-end">
          <Button onClick={handleSave} isLoading={saving} disabled={!ready || Boolean(loadError)}>
            Сохранить
          </Button>
        </div>
      </div>

      <section className="mt-10 max-w-4xl" aria-label="Журнал изменений">
        <h2 className="font-heading text-xl font-semibold text-dark">Журнал изменений</h2>
        {audit.length === 0 ? (
          <p className="mt-3 text-sm text-warm-gray">Изменений пока нет.</p>
        ) : (
          <div className="mt-4 overflow-x-auto rounded-2xl border border-sand bg-white">
            <table className="min-w-full text-left text-sm">
              <thead className="border-b border-sand text-warm-gray">
                <tr>
                  <th className="px-4 py-3 font-medium">Поле</th>
                  <th className="px-4 py-3 font-medium">Было</th>
                  <th className="px-4 py-3 font-medium">Стало</th>
                  <th className="px-4 py-3 font-medium">Кто</th>
                  <th className="px-4 py-3 font-medium">Когда</th>
                </tr>
              </thead>
              <tbody>
                {audit.map((item, index) => (
                  <tr key={`${item.changedAt}-${item.fieldName}-${index}`} className="border-b border-sand last:border-0">
                    <td className="px-4 py-3">{AUDIT_FIELD_LABELS[item.fieldName] ?? item.fieldName}</td>
                    <td className="px-4 py-3">{item.oldValue ?? 'пусто'}</td>
                    <td className="px-4 py-3">{item.newValue ?? 'пусто'}</td>
                    <td className="px-4 py-3 font-mono text-xs" title={item.changedByUserId}>
                      {item.changedByUserId}
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap">
                      {format(parseISO(item.changedAt), 'd MMM yyyy, HH:mm', { locale: ru })}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </PageTransition>
  );
}
