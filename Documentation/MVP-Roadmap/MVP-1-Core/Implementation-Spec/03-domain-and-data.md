# MVP-1 Domain and Data

## Основные агрегаты

- `User` (Identity)
- `Tour` (Catalog)
- `Booking` (Booking)
- `Favorite` (Booking)
- `ChatMessage` (если chat включен)

## Ключевые правила

- Статусная модель заявки:
  `New -> InProgress -> ProposalSent -> Confirmed -> Closed/Cancelled`.
- Недопустимо переводить `Closed`/`Cancelled` обратно в активные статусы.
- Для `ProposalSent` нужна минимум 1 запись `Proposal`.
- Для `Confirmed` выбирается финальное предложение.

## Хранение

- Database per service.
- Snapshot тура сохраняется в предложении заявки.
