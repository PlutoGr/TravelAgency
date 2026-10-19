/// <reference types="node" />
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { describe, expect, it } from 'vitest';
import { formatDate } from './format';

describe('formatDate', () => {
  it('returns an em dash for undefined', () => {
    expect(formatDate(undefined)).toBe('—');
  });

  it('returns an em dash for null and empty string', () => {
    expect(formatDate(null)).toBe('—');
    expect(formatDate('')).toBe('—');
  });

  it('returns an em dash for garbage', () => {
    expect(formatDate('garbage')).toBe('—');
    expect(formatDate('not-a-date')).toBe('—');
    expect(formatDate('32.13.2026')).toBe('—');
  });

  it('formats a real ISO date', () => {
    expect(formatDate('2026-04-15T10:30:00Z')).toMatch(/15/);
    expect(formatDate('2026-04-15T10:30:00Z')).not.toBe('—');
  });

  it('formats a calendar day without shifting it', () => {
    expect(formatDate('2026-11-02', 'd MMM')).toMatch(/^2 /);
    expect(formatDate('2026-11-02', 'd MMM')).toMatch(/ноя/);
    expect(formatDate('2026-02-31', 'd MMM')).toBe('—');
  });

  it('keeps 2 ноя in America/New_York and Asia/Novosibirsk', () => {
    const script = path.resolve(process.cwd(), 'scripts/format-zone-check.ts');
    for (const tz of ['America/New_York', 'Asia/Novosibirsk']) {
      const result = spawnSync(
        process.execPath,
        ['--experimental-strip-types', '--disable-warning=ExperimentalWarning', script],
        { env: { ...process.env, TZ: tz }, encoding: 'utf8' },
      );
      expect(result.status, result.stderr).toBe(0);
      expect(result.stdout).toMatch(/^2 /);
      expect(result.stdout).toMatch(/ноя/);
    }
  }, 20000);
});
