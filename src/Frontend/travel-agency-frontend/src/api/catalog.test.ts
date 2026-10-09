import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as catalog from './catalog';

const { mockApiClient } = vi.hoisted(() => ({
  mockApiClient: {
    get: vi.fn(),
  },
}));

vi.mock('./client', () => ({
  apiClient: mockApiClient,
}));

describe('catalog API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('buildTourFromDtoFields', () => {
    it('maps required params to Tour with defaults for optional fields', () => {
      const result = catalog.buildTourFromDtoFields({
        id: 't1',
        title: 'Alpine Trek',
        country: 'Switzerland',
        tourType: 'hiking',
        durationDays: 7,
      });

      expect(result).toEqual({
        id: 't1',
        title: 'Alpine Trek',
        description: '',
        shortDescription: '',
        country: 'Switzerland',
        city: '',
        hotel: '',
        price: 0,
        rating: 0,
        reviewCount: 0,
        dates: [],
        duration: 7,
        coverMediaFileId: null,
        photos: [],
        amenities: [],
        included: [],
        notIncluded: [],
        category: 'hiking',
        isHot: false,
        maxTravelers: 0,
        available: true,
        departureCity: '',
        mealPlan: null,
        accommodation: '',
        currency: null,
        nearestDate: null,
        priceFrom: null,
        images: [],
        days: [],
        offers: [],
      });
    });

    it('maps optional description, shortDescription, price, dates, maxTravelers', () => {
      const result = catalog.buildTourFromDtoFields({
        id: 't2',
        title: 'Greek Islands',
        country: 'Greece',
        tourType: 'cruise',
        durationDays: 10,
        description: 'Sail the Aegean.',
        shortDescription: 'Sail the Aegean.',
        price: 2500,
        dates: [{ start: '2025-06-01', end: '2025-06-10' }],
        maxTravelers: 20,
      });

      expect(result.description).toBe('Sail the Aegean.');
      expect(result.shortDescription).toBe('Sail the Aegean.');
      expect(result.price).toBe(2500);
      expect(result.dates).toEqual([{ start: '2025-06-01', end: '2025-06-10' }]);
      expect(result.maxTravelers).toBe(20);
    });

    it('maps numeric tourType (0-6) to category label', () => {
      expect(catalog.buildTourFromDtoFields({
        id: 'a',
        title: 'T',
        country: 'X',
        tourType: 0,
        durationDays: 1,
      }).category).toBe('Beach');

      expect(catalog.buildTourFromDtoFields({
        id: 'a',
        title: 'T',
        country: 'X',
        tourType: 4,
        durationDays: 1,
      }).category).toBe('Adventure');

      expect(catalog.buildTourFromDtoFields({
        id: 'a',
        title: 'T',
        country: 'X',
        tourType: 6,
        durationDays: 1,
      }).category).toBe('Safari');
    });

    it('passes through string tourType as category', () => {
      const result = catalog.buildTourFromDtoFields({
        id: 'a',
        title: 'T',
        country: 'X',
        tourType: 'custom-type',
        durationDays: 1,
      });
      expect(result.category).toBe('custom-type');
    });

    it('returns no photos when the tour has no media file id', () => {
      const result = catalog.buildTourFromDtoFields({
        id: 'a',
        title: 'T',
        country: 'X',
        tourType: 'x',
        durationDays: 1,
      });
      expect(result.photos).toEqual([]);
      expect(result.coverMediaFileId).toBeNull();
    });

    it('builds photo addresses only from media file ids', () => {
      const fileId = '11111111-1111-1111-1111-111111111111';
      const result = catalog.buildTourFromDtoFields({
        id: 'a',
        title: 'T',
        country: 'X',
        tourType: 'x',
        durationDays: 1,
        coverMediaFileId: fileId,
        images: [{ mediaFileId: fileId, alt: null, isCover: true, sortOrder: 0 }],
      });
      expect(result.coverMediaFileId).toBe(fileId);
      expect(result.photos).toEqual([`/api/v1/media/files/${fileId}/w800`]);
    });

    it('defaults price to 0 when omitted', () => {
      const result = catalog.buildTourFromDtoFields({
        id: 'a',
        title: 'T',
        country: 'X',
        tourType: 'x',
        durationDays: 1,
      });
      expect(result.price).toBe(0);
    });

    it('defaults dates to empty array when omitted', () => {
      const result = catalog.buildTourFromDtoFields({
        id: 'a',
        title: 'T',
        country: 'X',
        tourType: 'x',
        durationDays: 1,
      });
      expect(result.dates).toEqual([]);
    });
  });

  describe('getTours', () => {
    it('fetches /catalog/tours with Page and PageSize when no filters', async () => {
      mockApiClient.get.mockResolvedValue({
        data: {
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 6,
          totalPages: 0,
        },
      });

      await catalog.getTours();

      expect(mockApiClient.get).toHaveBeenCalledWith('/catalog/tours', {
        params: {
          Page: 1,
          PageSize: 6,
        },
      });
    });

    it('fetches with page parameter when provided', async () => {
      mockApiClient.get.mockResolvedValue({
        data: {
          items: [],
          totalCount: 12,
          page: 2,
          pageSize: 6,
          totalPages: 2,
        },
      });

      await catalog.getTours(undefined, 2);

      expect(mockApiClient.get).toHaveBeenCalledWith('/catalog/tours', {
        params: {
          Page: 2,
          PageSize: 6,
        },
      });
    });

    it('builds query params from filters: country, priceMin, priceMax, dateFrom, dateTo, category', async () => {
      mockApiClient.get.mockResolvedValue({
        data: {
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 6,
          totalPages: 0,
        },
      });

      await catalog.getTours(
        {
          country: ['Italy'],
          priceMin: 100,
          priceMax: 500,
          dateFrom: '2025-06-01',
          dateTo: '2025-08-31',
          category: 'adventure',
        },
        1,
      );

      expect(mockApiClient.get).toHaveBeenCalledWith('/catalog/tours', {
        params: {
          Page: 1,
          PageSize: 6,
          Country: 'Italy',
          MinPrice: 100,
          MaxPrice: 500,
          DateFrom: '2025-06-01',
          DateTo: '2025-08-31',
          TourType: 'adventure',
        },
      });
    });

    it('builds sort params for price_asc', async () => {
      mockApiClient.get.mockResolvedValue({
        data: {
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 6,
          totalPages: 0,
        },
      });

      await catalog.getTours({ sortBy: 'price_asc' }, 1);

      expect(mockApiClient.get).toHaveBeenCalledWith('/catalog/tours', {
        params: {
          Page: 1,
          PageSize: 6,
          SortBy: 'Price',
          SortDirection: 'Asc',
        },
      });
    });

    it('builds sort params for price_desc', async () => {
      mockApiClient.get.mockResolvedValue({
        data: {
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 6,
          totalPages: 0,
        },
      });

      await catalog.getTours({ sortBy: 'price_desc' }, 1);

      expect(mockApiClient.get).toHaveBeenCalledWith('/catalog/tours', {
        params: {
          Page: 1,
          PageSize: 6,
          SortBy: 'Price',
          SortDirection: 'Desc',
        },
      });
    });

    it('builds sort params for date', async () => {
      mockApiClient.get.mockResolvedValue({
        data: {
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 6,
          totalPages: 0,
        },
      });

      await catalog.getTours({ sortBy: 'date' }, 1);

      expect(mockApiClient.get).toHaveBeenCalledWith('/catalog/tours', {
        params: {
          Page: 1,
          PageSize: 6,
          SortBy: 'CreatedAt',
          SortDirection: 'Desc',
        },
      });
    });

    it('maps TourSummaryDto to Tour and PagedResult to PaginatedResponse', async () => {
      const tourSummaryDto = {
        id: 't1',
        title: 'Alpine Adventure',
        country: 'Switzerland',
        tourType: 'hiking',
        durationDays: 7,
        imageUrl: 'https://images.unsplash.com/photo-1?w=800',
        coverMediaFileId: null,
        minPrice: 1200,
        currency: 'EUR',
        isActive: true,
      };
      mockApiClient.get.mockResolvedValue({
        data: {
          items: [tourSummaryDto],
          totalCount: 1,
          page: 1,
          pageSize: 6,
          totalPages: 1,
        },
      });

      const result = await catalog.getTours();

      expect(result).toEqual({
        items: [
          {
            id: 't1',
            title: 'Alpine Adventure',
            description: '',
            shortDescription: '',
            country: 'Switzerland',
            city: '',
            hotel: '',
            price: 1200,
            rating: 0,
            reviewCount: 0,
            dates: [],
            duration: 7,
            coverMediaFileId: null,
            photos: [],
            amenities: [],
            included: [],
            notIncluded: [],
            category: 'hiking',
            isHot: false,
            maxTravelers: 0,
            available: true,
            departureCity: '',
            mealPlan: null,
            accommodation: '',
            currency: 'EUR',
            nearestDate: null,
            priceFrom: 1200,
            images: [],
            days: [],
            offers: [],
          },
        ],
        total: 1,
        page: 1,
        pageSize: 6,
        totalPages: 1,
      });
    });

    it('maps TourSummaryDto with null cover and minPrice to Tour', async () => {
      mockApiClient.get.mockResolvedValue({
        data: {
          items: [
            {
              id: 't2',
              title: 'Mystery Tour',
              country: 'France',
              tourType: 'cultural',
              durationDays: 3,
              coverMediaFileId: null,
              minPrice: null,
              currency: null,
              isActive: true,
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 6,
          totalPages: 1,
        },
      });

      const result = await catalog.getTours();

      expect(result.items[0]).toMatchObject({
        id: 't2',
        title: 'Mystery Tour',
        country: 'France',
        duration: 3,
        price: 0,
        coverMediaFileId: null,
        photos: [],
        category: 'cultural',
      });
    });

    it('computes totalPages from totalCount and pageSize', async () => {
      mockApiClient.get.mockResolvedValue({
        data: {
          items: [],
          totalCount: 25,
          page: 1,
          pageSize: 6,
          totalPages: 5,
        },
      });

      const result = await catalog.getTours();

      expect(result.totalPages).toBe(5);
      expect(result.total).toBe(25);
    });
  });

  describe('getTourById', () => {
    it('fetches /catalog/tours/{id} and maps TourDto to Tour', async () => {
      const tourDto = {
        id: 'tour-123',
        title: 'Greek Islands',
        description: 'Sail the Aegean.',
        country: 'Greece',
        tourType: 'cruise',
        durationDays: 10,
        imageUrl: 'https://images.unsplash.com/photo-greece',
        coverMediaFileId: null,
        directionId: null,
        isActive: true,
        createdAt: '2025-01-01T00:00:00Z',
        updatedAt: null,
        prices: [
          {
            id: 'p1',
            validFrom: '2025-06-01',
            validTo: '2025-06-10',
            pricePerPerson: 2500,
            currency: 'EUR',
            availableSeats: 20,
          },
        ],
      };
      mockApiClient.get.mockResolvedValue({ data: tourDto });

      const result = await catalog.getTourById('tour-123');

      expect(mockApiClient.get).toHaveBeenCalledWith('/catalog/tours/tour-123');
      expect(result).toMatchObject({
        id: 'tour-123',
        title: 'Greek Islands',
        description: 'Sail the Aegean.',
        country: 'Greece',
        duration: 10,
        price: 2500,
        maxTravelers: 20,
        category: 'cruise',
        coverMediaFileId: null,
        photos: [],
      });
      expect(result.dates).toEqual([
        { start: '2025-06-01', end: '2025-06-10' },
      ]);
      expect(result.shortDescription).toBe('Sail the Aegean.');
    });

    it('maps TourDto with long description to shortDescription (first 150 chars + ...)', async () => {
      const longDesc = 'A'.repeat(200);
      mockApiClient.get.mockResolvedValue({
        data: {
          id: 't3',
          title: 'Long Tour',
          description: longDesc,
          country: 'Spain',
          tourType: 'cultural',
          durationDays: 5,
          coverMediaFileId: null,
          directionId: null,
          isActive: true,
          createdAt: '2025-01-01T00:00:00Z',
          updatedAt: null,
          prices: [],
        },
      });

      const result = await catalog.getTourById('t3');

      expect(result.shortDescription).toBe('A'.repeat(150) + '...');
    });

    it('maps TourDto with empty prices array to price 0 and maxTravelers 0', async () => {
      mockApiClient.get.mockResolvedValue({
        data: {
          id: 't4',
          title: 'No Prices',
          description: 'Desc',
          country: 'Italy',
          tourType: 'adventure',
          durationDays: 3,
          coverMediaFileId: null,
          directionId: null,
          isActive: true,
          createdAt: '2025-01-01T00:00:00Z',
          updatedAt: null,
          prices: [],
        },
      });

      const result = await catalog.getTourById('t4');

      expect(result.price).toBe(0);
      expect(result.maxTravelers).toBe(0);
      expect(result.dates).toEqual([]);
    });

    it('throws "Тур с ID "{id}" не найден" when API returns 404', async () => {
      const axiosErr = {
        response: { status: 404 },
      };
      mockApiClient.get.mockRejectedValue(axiosErr);

      await expect(catalog.getTourById('nonexistent')).rejects.toThrow(
        'Тур с ID "nonexistent" не найден',
      );
    });

    it('rethrows non-404 errors', async () => {
      mockApiClient.get.mockRejectedValue(new Error('Network error'));

      await expect(catalog.getTourById('t5')).rejects.toThrow('Network error');
    });

    it('rethrows when error has no response', async () => {
      mockApiClient.get.mockRejectedValue(new Error('Unknown'));

      await expect(catalog.getTourById('t6')).rejects.toThrow('Unknown');
    });
  });

  describe('getDestinations', () => {
    it('fetches /catalog/directions and maps DirectionDto to Destination', async () => {
      mockApiClient.get.mockResolvedValue({
        data: [
          {
            id: 'd1',
            name: 'Greek Islands',
            country: 'Greece',
            description: 'Beautiful islands',
          },
        ],
      });

      const result = await catalog.getDestinations();

      expect(mockApiClient.get).toHaveBeenCalledWith('/catalog/directions');
      expect(result).toEqual([
        {
          id: 'd1',
          name: 'Greek Islands',
          country: 'Greece',
          photo: '',
          tourCount: 0,
          description: 'Beautiful islands',
        },
      ]);
    });

    it('returns empty array when API returns empty', async () => {
      mockApiClient.get.mockResolvedValue({ data: [] });

      const result = await catalog.getDestinations();

      expect(result).toEqual([]);
    });

    it('returns empty array when API returns null or undefined', async () => {
      mockApiClient.get.mockResolvedValue({ data: null });

      const result = await catalog.getDestinations();

      expect(result).toEqual([]);
    });

    it('maps DirectionDto with null description to empty string', async () => {
      mockApiClient.get.mockResolvedValue({
        data: [{ id: 'd2', name: 'Amalfi', country: 'Italy', description: null }],
      });

      const result = await catalog.getDestinations();

      expect(result[0].description).toBe('');
    });

    it('returns empty array on 404 or 500', async () => {
      mockApiClient.get.mockRejectedValue({ response: { status: 404 } });
      const r404 = await catalog.getDestinations();
      expect(r404).toEqual([]);

      mockApiClient.get.mockRejectedValue({ response: { status: 500 } });
      const r500 = await catalog.getDestinations();
      expect(r500).toEqual([]);
    });

    it('rethrows on other errors', async () => {
      mockApiClient.get.mockRejectedValue(new Error('Network error'));

      await expect(catalog.getDestinations()).rejects.toThrow('Network error');
    });
  });

  describe('searchTours', () => {
    it('returns empty array (stub until API available)', async () => {
      const result = await catalog.searchTours('beach');
      expect(result).toEqual([]);
      expect(mockApiClient.get).not.toHaveBeenCalled();
    });
  });

  describe('public tour page', () => {
    it('maps days, inclusions, stay, offers and media ids', async () => {
      mockApiClient.get.mockResolvedValue({
        data: {
          id: 'tour-1',
          title: 'Мальдивы',
          description: 'Описание',
          shortDescription: 'Кратко',
          departureCity: 'Москва',
          country: 'Мальдивы',
          tourType: 0,
          durationDays: 7,
          imageUrl: 'http://minio:9000/bucket/photo.jpg?X-Amz-Signature=abc',
          coverMediaFileId: '11111111-1111-1111-1111-111111111111',
          directionId: null,
          isActive: true,
          createdAt: '2026-01-01T00:00:00Z',
          updatedAt: null,
          mealPlan: 'AI',
          accommodationText: 'Водная вилла',
          priceFrom: 289000,
          currency: 'RUB',
          nearestDate: '2026-11-01T00:00:00Z',
          prices: [
            {
              id: 'offer-1',
              validFrom: '2026-11-01',
              validTo: '2026-11-08',
              pricePerPerson: 289000,
              currency: 'RUB',
              availableSeats: 8,
            },
          ],
          days: [{ dayNumber: 1, title: 'Прилёт', description: 'Трансфер' }],
          inclusions: [
            { text: 'Проживание', kind: 'Included', sortOrder: 0 },
            { text: 'Виза', kind: 1, sortOrder: 1 },
          ],
          images: [
            {
              mediaFileId: '11111111-1111-1111-1111-111111111111',
              url: 'http://minio:9000/bucket/photo.jpg?X-Amz-Signature=abc',
              alt: 'Вилла',
              isCover: true,
              sortOrder: 0,
            },
          ],
        },
      });

      const result = await catalog.getTourById('tour-1');

      expect(result.shortDescription).toBe('Кратко');
      expect(result.departureCity).toBe('Москва');
      expect(result.mealPlan).toBe('Всё включено');
      expect(result.accommodation).toBe('Водная вилла');
      expect(result.priceFrom).toBe(289000);
      expect(result.days).toEqual([{ dayNumber: 1, title: 'Прилёт', description: 'Трансфер' }]);
      expect(result.included).toEqual(['Проживание']);
      expect(result.notIncluded).toEqual(['Виза']);
      expect(result.offers?.[0]?.price).toBe(289000);
      expect(result.coverMediaFileId).toBe('11111111-1111-1111-1111-111111111111');
      expect(result.photos).toEqual([
        '/api/v1/media/files/11111111-1111-1111-1111-111111111111/w800',
      ]);
      expect(JSON.stringify(result)).not.toContain('minio');
      expect(JSON.stringify(result)).not.toContain('X-Amz-');
      expect(JSON.stringify(result)).not.toContain('unsplash');
    });
  });

  describe('getTourCards', () => {
    it('asks cards by repeated ids and keeps an unavailable tour', async () => {
      mockApiClient.get.mockResolvedValue({
        data: [
          {
            id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
            title: 'Снятый тур',
            coverMediaFileId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
            cover: {
              mediaFileId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
              url: 'https://images.unsplash.com/photo-x',
              alt: 'Обложка',
              isCover: true,
              sortOrder: 0,
            },
            available: false,
          },
        ],
      });

      const result = await catalog.getTourCards([
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      ]);

      expect(mockApiClient.get).toHaveBeenCalledWith(
        '/catalog/tours/cards?ids=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      );
      expect(result[0]?.available).toBe(false);
      expect(result[0]?.title).toBe('Снятый тур');
      expect(result[0]?.priceFrom).toBeNull();
      expect(result[0]?.coverMediaFileId).toBe('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');
      expect(result[0]?.images?.[0]?.mediaFileId).toBe('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');
      expect(JSON.stringify(result)).not.toContain('unsplash');
      expect(JSON.stringify(result)).not.toContain('http');
    });

    it('does not call the API for an empty id list', async () => {
      const result = await catalog.getTourCards([]);
      expect(result).toEqual([]);
      expect(mockApiClient.get).not.toHaveBeenCalled();
    });
  });
});
