import { formatDate } from '../src/utils/format.ts';

const text = formatDate('2026-11-02', 'd MMM');
if (!/^2 /.test(text) || !text.includes('ноя')) {
  console.error(text);
  process.exit(1);
}
console.log(text);
