import { describe, expect, it } from 'vitest';
import { coverMediaFileId, manageMediaFileUrl, mediaImageUrl, mediaSrcSet, resolveTourMedia } from './media';

const FILE = '11111111-1111-1111-1111-111111111111';

describe('media urls', () => {
  it('builds public size urls and a srcset that is not wider than the variant', () => {
    expect(mediaImageUrl(FILE, 'w800')).toBe(`/api/v1/media/files/${FILE}/w800`);
    expect(mediaSrcSet(FILE, 'w800')).toContain('w200');
    expect(mediaSrcSet(FILE, 'w800')).toContain('w800');
    expect(mediaSrcSet(FILE, 'w800')).not.toContain('w1600');
  });

  it('uses w800 for catalog cards and renders nothing without a file id', () => {
    const card = resolveTourMedia(FILE, 'card');
    expect(card?.src).toBe(mediaImageUrl(FILE, 'w800'));
    expect(card?.srcSet).toContain('w1600');
    expect(resolveTourMedia(null, 'gallery')).toBeNull();
    expect(resolveTourMedia(undefined, 'thumb')).toBeNull();
  });

  it('builds a manage preview from the file id', () => {
    const src = manageMediaFileUrl(FILE, 'w200');
    expect(src).toBe(`/api/v1/media/manage/files/${FILE}/w200`);
    expect(src).not.toContain('/media/files/');
  });

  it('reads the cover id and ignores a missing photo list', () => {
    expect(coverMediaFileId({ coverMediaFileId: FILE, images: [] })).toBe(FILE);
    expect(coverMediaFileId({ images: [{ mediaFileId: FILE, isCover: true }] })).toBe(FILE);
    expect(coverMediaFileId({})).toBeNull();
  });
});
