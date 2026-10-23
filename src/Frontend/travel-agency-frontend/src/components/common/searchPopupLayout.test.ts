import { describe, expect, it } from 'vitest';
import {
  calendarPanelHeight,
  calendarPanelWidth,
  planCalendarPopover,
  planListPopover,
  type CalendarPlanInput,
  type ListPlanInput,
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
    ...partial,
  };
}

function listInput(partial: Partial<ListPlanInput> = {}): ListPlanInput {
  return {
    viewportWidth: 1280,
    viewportHeight: 800,
    anchorTop: 120,
    anchorBottom: 180,
    anchorLeft: 40,
    anchorWidth: 280,
    minWidth: 280,
    contentHeight: 200,
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
    expect(plan.anchorTop).toBe(450);
    expect(plan.height).toBe(calendarPanelHeight(false, 'desktop'));
    expect(plan.width).toBe(calendarPanelWidth(2, false));
  });

  it('flips above the field when the space below is shorter than the panel', () => {
    const plan = planCalendarPopover(input({ anchorTop: 620, anchorBottom: 680 }));

    expect(plan.placeBelow).toBe(false);
    expect(plan.compact).toBe(false);
    expect(plan.months).toBe(2);
    expect(plan.anchorTop).toBe(620);
  });

  it('shrinks day cells when only a compact panel fits in place', () => {
    const plan = planCalendarPopover(
      input({ viewportHeight: 760, anchorTop: 360, anchorBottom: 420 }),
    );

    expect(plan.compact).toBe(true);
    expect(plan.months).toBe(2);
    expect(plan.placeBelow).toBe(false);
    expect(plan.anchorTop).toBe(360);
    expect(plan.height).toBe(calendarPanelHeight(true, 'desktop'));
  });

  it('keeps the anchor in place when even a compact panel does not fit', () => {
    const plan = planCalendarPopover(
      input({ viewportHeight: 640, anchorTop: 300, anchorBottom: 372 }),
    );

    expect(plan.compact).toBe(true);
    expect(plan.months).toBe(2);
    expect(plan.placeBelow).toBe(false);
    expect(plan.anchorTop).toBe(300);
    expect(plan.anchorBottom).toBe(372);
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
    expect(roomy.anchorTop).toBe(100);
    expect(short.compact).toBe(true);
    expect(short.height).toBe(calendarPanelHeight(true, 'sheet'));
    expect(short.height).toBeLessThanOrEqual(400 - 8);
  });
});

describe('planListPopover', () => {
  it('places a short city list below the field', () => {
    const plan = planListPopover(listInput());

    expect(plan.placeBelow).toBe(true);
    expect(plan.maxHeight).toBeGreaterThanOrEqual(200);
    expect(plan.left).toBe(40);
    expect(plan.width).toBe(280);
  });

  it('flips the list above when it does not fit below', () => {
    const plan = planListPopover(
      listInput({ viewportHeight: 700, anchorTop: 640, anchorBottom: 690, contentHeight: 320 }),
    );

    expect(plan.placeBelow).toBe(false);
    expect(plan.maxHeight).toBe(632);
  });

  it('caps a long list to the roomier side so the rest scrolls inside', () => {
    const plan = planListPopover(
      listInput({ viewportHeight: 500, anchorTop: 240, anchorBottom: 300, contentHeight: 480 }),
    );

    expect(plan.placeBelow).toBe(false);
    expect(plan.maxHeight).toBe(232);
    expect(plan.maxHeight).toBeLessThan(480);
  });
});
