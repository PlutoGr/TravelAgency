/** Фраза стажа от года `tourismSince`. Год берётся по UTC, сервер фразу не присылает. */
export function tourismTenurePhrase(
  tourismSince: unknown,
  now: Date = new Date(),
): string | null {
  if (typeof tourismSince !== 'number' || !Number.isInteger(tourismSince)) return null;

  const years = now.getUTCFullYear() - tourismSince;
  if (years < 0) return null;
  if (years === 0) return 'В туризме меньше года';

  return `В туризме ${years} ${yearsWord(years)}`;
}

function yearsWord(years: number): string {
  const mod100 = years % 100;
  const mod10 = years % 10;
  if (mod10 === 1 && mod100 !== 11) return 'год';
  if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return 'года';
  return 'лет';
}
