import { format, parseISO } from 'date-fns';
import { ru } from 'date-fns/locale';

/**
 * Formats an ISO date string using parseISO for consistent parsing.
 * @param dateStr - ISO 8601 date string
 * @param formatStr - date-fns format pattern (default: 'd MMM yyyy')
 */
export function formatDate(
  dateStr: string,
  formatStr: string = 'd MMM yyyy',
): string {
  return format(parseISO(dateStr), formatStr, { locale: ru });
}

/**
 * Formats a date for full display (e.g. "15 марта 2025").
 */
export function formatDateFull(dateStr: string): string {
  return formatDate(dateStr, 'd MMMM yyyy');
}

/**
 * Formats a date range.
 */
export function formatDateRange(from: string, to: string): string {
  return `${formatDate(from, 'd MMM')} — ${formatDate(to, 'd MMM yyyy')}`;
}

/**
 * Formats a budget/price amount with currency (e.g. "50 000 ₽").
 */
export function formatBudget(amount: number): string {
  return amount.toLocaleString('ru-RU') + ' ₽';
}

/**
 * Formats a budget with "от" prefix for client-facing display (e.g. "от 50 000 ₽").
 */
export function formatBudgetFrom(amount: number): string {
  return `от ${amount.toLocaleString('ru-RU')} ₽`;
}

/**
 * Formats a price with currency (e.g. "50 000 ₽").
 */
export function formatPrice(price: number): string {
  return price.toLocaleString('ru-RU') + ' ₽';
}

/**
 * Formats a booking ID for display (e.g. "#BK-001").
 */
export function formatBookingId(id: string): string {
  return '#BK-' + (id.split('-')[1]?.padStart(3, '0') ?? id);
}
