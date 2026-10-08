import { describe, expect, it } from 'vitest';
import { emptyWizardForm, missingPublishCodes, validateWizardStep, type WizardForm } from './tourWizard';

const NOW = new Date('2026-10-08T12:00:00Z');

function readyForm(): WizardForm {
  return {
    ...emptyWizardForm(),
    title: 'Сочи',
    shortDescription: 'Море',
    description: 'Полное описание моря',
    durationDays: '1',
    days: [{ dayNumber: 1, title: 'Приезд', description: 'Пляж' }],
    inclusions: [{ text: 'Завтрак', kind: 'Included' }],
    mealPlan: 'BB',
    accommodationText: 'Отель у моря',
    offers: [
      {
        validFrom: '2026-11-01',
        validTo: '2026-11-08',
        pricePerPerson: '1000',
        currency: 'RUB',
        availableSeats: '4',
      },
    ],
    images: [
      { mediaFileId: 'a', alt: '', isCover: true, sortOrder: 0, widthPx: 1600 },
      { mediaFileId: 'b', alt: '', isCover: false, sortOrder: 1, widthPx: 800 },
      { mediaFileId: 'c', alt: '', isCover: false, sortOrder: 2, widthPx: 800 },
    ],
  };
}

describe('publish checklist', () => {
  it('is empty when the draft matches the publish rules', () => {
    expect(missingPublishCodes(readyForm(), NOW)).toEqual([]);
  });

  it('asks for three photos and a cover at least 1280 px wide', () => {
    const form = readyForm();
    form.images = [
      { mediaFileId: 'a', alt: '', isCover: true, sortOrder: 0, widthPx: 1000 },
      { mediaFileId: 'b', alt: '', isCover: false, sortOrder: 1, widthPx: 1000 },
    ];
    expect(missingPublishCodes(form, NOW)).toEqual(['images.minCount', 'images.coverMinWidth']);
  });

  it('treats a cover of unknown width as too narrow and a past date as not future', () => {
    const form = readyForm();
    form.images[0].widthPx = null;
    form.offers[0].validFrom = '2026-10-08';
    expect(missingPublishCodes(form, NOW)).toEqual(['offers.future', 'images.coverMinWidth']);
  });
});

describe('wizard field validation', () => {
  it('rejects a title longer than 200 characters', () => {
    const form = emptyWizardForm();
    form.title = 'А'.repeat(201);
    expect(validateWizardStep(1, form).title).toMatch(/200/);
  });

  it('rejects a zero price and a date range that does not move forward', () => {
    const form = emptyWizardForm();
    form.offers = [
      {
        validFrom: '2026-11-08',
        validTo: '2026-11-01',
        pricePerPerson: '0',
        currency: 'RU',
        availableSeats: '1',
      },
    ];
    const errors = validateWizardStep(5, form);
    expect(errors['offer-0']).toBeTruthy();
    expect(errors['offer-0-price']).toBeTruthy();
    expect(errors['offer-0-currency']).toBeTruthy();
  });
});
