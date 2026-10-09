/**
 * JSON exactly as ASP.NET Core System.Text.Json writes BookingDto:
 * camelCase, BookingStatus as a number, nulls included, proposals as ProposalDto.
 */
export const BOOKING_WITH_PROPOSAL_JSON = `{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientId": "11111111-1111-1111-1111-111111111111",
  "tourId": "22222222-2222-2222-2222-222222222222",
  "comment": "Хочу поехать в мае, двое взрослых",
  "status": 2,
  "createdAt": "2026-04-15T10:30:00Z",
  "updatedAt": "2026-04-16T08:00:00Z",
  "proposals": [
    {
      "id": "33333333-3333-3333-3333-333333333333",
      "bookingId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "managerId": "44444444-4444-4444-4444-444444444444",
      "tourSnapshot": {
        "tourId": "22222222-2222-2222-2222-222222222222",
        "title": "Санторини",
        "description": "Неделя на кальдере",
        "price": 148000.5,
        "currency": "RUB",
        "durationDays": 7,
        "snapshotTakenAt": "2026-04-16T08:00:00Z"
      },
      "notes": "Отель с видом на кальдеру",
      "isConfirmed": false,
      "createdAt": "2026-04-16T08:00:00Z"
    }
  ],
  "clientName": "Анна Смирнова",
  "clientEmail": "anna@example.com",
  "clientPhone": "+79990001122"
}`;

/** Fresh booking: no proposal, nullable strings are null. */
export const BOOKING_MINIMAL_JSON = `{
  "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "clientId": "11111111-1111-1111-1111-111111111111",
  "tourId": "99999999-9999-9999-9999-999999999999",
  "comment": null,
  "status": 0,
  "createdAt": "2026-05-01T00:00:00Z",
  "updatedAt": null,
  "proposals": [],
  "clientName": null,
  "clientEmail": null,
  "clientPhone": null
}`;

export const TOUR_CARD_JSON = `{
  "id": "22222222-2222-2222-2222-222222222222",
  "title": "Солнечная Греция — Санторини",
  "available": true,
  "priceFrom": 148000,
  "currency": "RUB",
  "country": "Греция",
  "durationDays": 7,
  "shortDescription": "Кальдера и белые дома",
  "nearestDate": null,
  "coverMediaFileId": "55555555-5555-5555-5555-555555555555",
  "cover": {
    "mediaFileId": "55555555-5555-5555-5555-555555555555",
    "alt": "Санторини",
    "isCover": true,
    "sortOrder": 0
  }
}`;

export function parseBookingJson(json: string): unknown {
  return JSON.parse(json);
}
