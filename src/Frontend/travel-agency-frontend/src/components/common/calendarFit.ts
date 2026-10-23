import { createContext, useContext } from 'react';

export type CalendarFit = {
  months: 1 | 2;
  compact: boolean;
};

const CalendarFitContext = createContext<CalendarFit | null>(null);

export const CalendarFitProvider = CalendarFitContext.Provider;

export function useCalendarFit(): CalendarFit | null {
  return useContext(CalendarFitContext);
}
