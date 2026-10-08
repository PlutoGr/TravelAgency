import { describe, expect, it } from 'vitest';
import {
  TOUR_MANAGE_API_CODES,
  TOUR_MANAGE_ERROR_TEXT,
  TOUR_MANAGE_STATUS_TEXT,
  readManageFailure,
} from './tourManageErrors';

describe('tour manage error texts', () => {
  it('has a text for every code the manage and publish API can return', () => {
    expect(TOUR_MANAGE_API_CODES).toEqual([
      'title',
      'shortDescription',
      'description',
      'program.dayCount',
      'inclusions.included',
      'mealPlan',
      'accommodation',
      'offers.future',
      'images.cover',
      'images.minCount',
      'images.coverMinWidth',
      'images.owner',
      'images.notFound',
    ]);

    for (const code of TOUR_MANAGE_API_CODES) {
      expect(TOUR_MANAGE_ERROR_TEXT[code].trim().length).toBeGreaterThan(0);
    }

    expect(Object.keys(TOUR_MANAGE_ERROR_TEXT).sort()).toEqual([...TOUR_MANAGE_API_CODES].sort());
  });

  it('explains 422 missing codes, including a narrow cover, too few photos and OwnerMismatch', () => {
    const failure = readManageFailure({
      response: {
        status: 422,
        data: {
          type: 'https://travelagency/errors/tour-not-publishable',
          missing: ['images.coverMinWidth', 'images.minCount', 'images.owner'],
        },
      },
    });

    expect(failure.missing).toEqual(['images.coverMinWidth', 'images.minCount', 'images.owner']);
    expect(TOUR_MANAGE_ERROR_TEXT[failure.missing[0] as 'images.coverMinWidth']).toMatch(/1280/);
    expect(TOUR_MANAGE_ERROR_TEXT[failure.missing[1] as 'images.minCount']).toMatch(/3 фото/);
    expect(TOUR_MANAGE_ERROR_TEXT[failure.missing[2] as 'images.owner']).toMatch(/другому пользователю/);
  });

  it('explains 413 as a file larger than 10 MB', () => {
    const failure = readManageFailure({
      response: { status: 413, data: '<html>too large</html>' },
    });
    expect(failure.message).toBe(TOUR_MANAGE_STATUS_TEXT.fileTooLarge);
    expect(failure.message).toMatch(/Файл больше 10 МБ/);
  });

  it('explains 412 as a stale version and 428 as a missing If-Match', () => {
    expect(readManageFailure({ response: { status: 412, data: { detail: 'precondition' } } }).message).toMatch(
      /актуальную версию/,
    );
    expect(readManageFailure({ response: { status: 428, data: { detail: 'If-Match header is required.' } } }).message).toMatch(
      /If-Match/,
    );
  });

  it('explains 409 when the tour is not a draft', () => {
    const failure = readManageFailure({
      response: { status: 409, data: { detail: 'Only a draft tour can be deleted.' } },
    });
    expect(failure.message).toBe(TOUR_MANAGE_STATUS_TEXT.draftOnly);
  });
});
