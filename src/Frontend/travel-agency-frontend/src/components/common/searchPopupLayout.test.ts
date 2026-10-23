import { describe, expect, it } from 'vitest';
import {
  calendarPanelHeight,
  calendarPanelWidth,
  planCalendarPopover,
  type CalendarPlanInput,
} from './searchPopupLayout';

function input(partial: Partial<CalendarPlanInput> = {}): CalendarPlanInput {
  return {
    viewportWidth: 1280,
    viewportHeight: 800,
    anchorTop: 100,
    anchorBottom: 160,
    anchorLeft: 40,
    anchorWidth: 280,
    wide: true,
    allowTwoMonths: true,
    scrollY: 0,
    maxScrollDown: 5000,
    ...partial,
  };
}

describe('planCalendarPopover', () => {
  it('prefers two regular months below the field when both sides fit', () => {
    const plan = planCalendarPopover(
      input({ viewportHeight: 1000, anchorTop: 450, anchorBottom: 510 }),
    );

    expect(plan.months).toBe(2);
    expect(plan.compact).toBe(false);
    expect(plan.placeBelow).toBe(true);
    expect(plan.scrollDelta).toBe(0);
    expect(plan.height).toBe(calendarPanelHeight(false, 'desktop'));
    expect(plan.width).toBe(calendarPanelWidth(2, false));
  });

  it('flips above the field when the space below is shorter than the panel', () => {
    const plan = planCalendarPopover(input({ anchorTop: 620, anchorBottom: 680 }));

    expect(plan.placeBelow).toBe(false);
    expect(plan.compact).toBe(false);
    expect(plan.months).toBe(2);
    expect(plan.scrollDelta).toBe(0);
  });

  it('shrinks day cells before scrolling when only a compact panel fits', () => {
    const plan = planCalendarPopover(
      input({ viewportHeight: 760, anchorTop: 360, anchorBottom: 420 }),
    );

    expect(plan.compact).toBe(true);
    expect(plan.months).toBe(2);
    expect(plan.placeBelow).toBe(false);
    expect(plan.scrollDelta).toBe(0);
    expect(plan.height).toBe(calendarPanelHeight(true, 'desktop'));
  });

  it('scrolls the anchor toward the upper third when the panel does not fit in place', () => {
    const plan = planCalendarPopover(
      input({ viewportHeight: 640, anchorTop: 300, anchorBottom: 372 }),
    );

    expect(plan.compact).toBe(true);
    expect(plan.months).toBe(2);
    expect(plan.placeBelow).toBe(true);
    expect(plan.scrollDelta).toBeCloseTo(300 - 640 / 3, 5);
    expect(plan.anchorTop).toBeCloseTo(640 / 3, 5);
    expect(plan.height).toBeLessThanOrEqual(640 - plan.anchorBottom - 8);
  });

  it('uses one month on a wide layout when two months are wider than the viewport', () => {
    const plan = planCalendarPopover(
      input({ viewportWidth: 520, viewportHeight: 900, anchorTop: 80, anchorBottom: 140 }),
    );

    expect(plan.months).toBe(1);
    expect(plan.compact).toBe(false);
    expect(plan.placeBelow).toBe(true);
    expect(calendarPanelWidth(2, true) + 16).toBeGreaterThan(520);
  });

  it('keeps two compact months when that width still fits', () => {
    const plan = planCalendarPopover(
      input({ viewportWidth: 600, viewportHeight: 900, anchorTop: 80, anchorBottom: 140 }),
    );

    expect(plan.months).toBe(2);
    expect(plan.compact).toBe(true);
  });

  it('does not scroll when the page cannot move', () => {
    const plan = planCalendarPopover(
      input({
        viewportHeight: 640,
        anchorTop: 300,
        anchorBottom: 372,
        scrollY: 0,
        maxScrollDown: 0,
      }),
    );

    expect(plan.scrollDelta).toBe(0);
    expect(plan.compact).toBe(true);
    expect(plan.months).toBe(2);
    expect(plan.placeBelow).toBe(false);
  });

  it('keeps a single date on one month', () => {
    const plan = planCalendarPopover(input({ allowTwoMonths: false }));

    expect(plan.months).toBe(1);
    expect(plan.compact).toBe(false);
  });

  it('compacts the bottom sheet only when the regular panel is taller than the screen', () => {
    const roomy = planCalendarPopover(input({ wide: false, viewportHeight: 800 }));
    const short = planCalendarPopover(input({ wide: false, viewportHeight: 400 }));

    expect(roomy.months).toBe(1);
    expect(roomy.compact).toBe(false);
    expect(roomy.scrollDelta).toBe(0);
    expect(short.compact).toBe(true);
    expect(short.height).toBe(calendarPanelHeight(true, 'sheet'));
    expect(short.height).toBeLessThanOrEqual(400 - 8);
  });
});
