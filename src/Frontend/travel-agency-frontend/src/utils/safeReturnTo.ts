/** Where to send the user when `returnTo` is missing or not an internal path. */
export const DEFAULT_RETURN_PATH = '/';

/**
 * Accept an internal return path only.
 *
 * The value must start with `/`, and the second character must be neither
 * `/` nor `\`. Browsers treat `\` as `/`, so `/\evil.com` is the same
 * protocol-relative URL as `//evil.com` and would leave the site.
 * Absolute URLs and schemes (`https://…`, `javascript:…`) do not start
 * with `/` and are rejected too.
 */
export function isSafeReturnTo(returnTo: string | null | undefined): returnTo is string {
  if (typeof returnTo !== 'string' || returnTo.length === 0) return false;
  if (returnTo[0] !== '/') return false;
  const second = returnTo[1];
  if (second === '/' || second === '\\') return false;
  return true;
}

/** Valid internal path, or `fallback` (home by default). */
export function resolveReturnTo(
  returnTo: string | null | undefined,
  fallback: string = DEFAULT_RETURN_PATH,
): string {
  return isSafeReturnTo(returnTo) ? returnTo : fallback;
}

/**
 * Read `returnTo` from the current query.
 * `URLSearchParams` already decodes once; a second decode accepts a value
 * that was encoded twice. A broken escape sequence is not a path.
 */
export function readReturnToParam(raw: string | null): string | null {
  if (raw == null) return null;
  try {
    return decodeURIComponent(raw);
  } catch {
    return null;
  }
}
