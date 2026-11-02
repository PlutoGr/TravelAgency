/** «1 заявка», «2 заявки», «5 заявок», для 11–14 — «заявок». */
export function bookingCountLabel(count: number): string {
  const n = Math.trunc(count);
  const mod10 = Math.abs(n) % 10;
  const mod100 = Math.abs(n) % 100;
  let word = 'заявок';
  if (mod100 < 11 || mod100 > 14) {
    if (mod10 === 1) word = 'заявка';
    else if (mod10 >= 2 && mod10 <= 4) word = 'заявки';
  }
  return `${n} ${word}`;
}
