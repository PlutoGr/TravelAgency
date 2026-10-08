import { describe, expect, it } from 'vitest';
import { manageMediaFileUrl, mediaFileUrl, mediaSrcSet, resolveTourMedia } from './media';

const FILE = '11111111-1111-1111-1111-111111111111';

describe('media urls', () => {
  it('builds public size urls and a srcset that is not wider than the variant', () => {
    expect(mediaFileUrl(FILE, 'w800')).toBe(`/api/v1/media/files/${FILE}/w800`);
    expect(mediaSrcSet(FILE, 'w800')).toContain('w200');
    expect(mediaSrcSet(FILE, 'w800')).toContain('w800');
    expect(mediaSrcSet(FILE, 'w800')).not.toContain('w1600');
  });

  it('uses w800 for catalog cards and ignores minio fallbacks', () => {
    const card = resolveTourMedia({ mediaFileId: FILE }, 'card');
    expect(card?.src).toContain('/w800');
    expect(card?.srcSet).toContain('w1600');

    const blocked = resolveTourMedia(
      { fallbackUrl: 'http://minio:9000/bucket/a.jpg' },
      'gallery',
    );
    expect(blocked).toBeNull();
  });

  it('builds a manage preview from the file id', () => {
    const src = manageMediaFileUrl(FILE, 'w200');
    expect(src).toBe(`/api/v1/media/manage/files/${FILE}/w200`);
    expect(src).not.toContain('minio');
    expect(src).not.toContain('/media/files/');
  });
});
