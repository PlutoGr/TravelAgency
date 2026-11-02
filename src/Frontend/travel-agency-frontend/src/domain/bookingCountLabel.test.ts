import { describe, expect, it } from 'vitest';
import { bookingCountLabel } from './bookingCountLabel';

describe('bookingCountLabel', () => {
  it.each([
    [1, '1 заявка'],
    [2, '2 заявки'],
    [5, '5 заявок'],
    [11, '11 заявок'],
    [14, '14 заявок'],
    [21, '21 заявка'],
  ])('count %s', (count, label) => {
    expect(bookingCountLabel(count)).toBe(label);
  });
});
