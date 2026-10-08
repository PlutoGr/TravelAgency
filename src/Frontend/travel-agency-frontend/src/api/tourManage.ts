import { apiClient } from './client';

export interface ManagedTourDay {
  id: string;
  dayNumber: number;
  title: string;
  description: string;
}

export interface ManagedTourInclusion {
  id: string;
  text: string;
  kind: string;
  sortOrder: number;
}

export interface ManagedTourOffer {
  id: string;
  validFrom: string;
  validTo: string;
  pricePerPerson: number;
  currency: string;
  availableSeats: number;
}

export interface ManagedTourImage {
  id: string;
  mediaFileId: string;
  sortOrder: number;
  isCover: boolean;
  alt: string | null;
  widthPx: number | null;
}

export interface ManagedTour {
  id: string;
  title: string;
  shortDescription: string | null;
  description: string;
  departureCity: string | null;
  country: string;
  tourType: string;
  durationDays: number;
  directionId: string | null;
  mealPlan: string | null;
  accommodationText: string | null;
  status: string;
  source: string;
  ownerId: string | null;
  version: number;
  etag: string;
  days: ManagedTourDay[];
  inclusions: ManagedTourInclusion[];
  offers: ManagedTourOffer[];
  images: ManagedTourImage[];
}

export interface TourDraftInput {
  title?: string | null;
  shortDescription?: string | null;
  description?: string | null;
  departureCity?: string | null;
  country?: string | null;
  tourType?: string | null;
  durationDays?: number | null;
  directionId?: string | null;
}

export interface TourBasicsInput {
  title?: string | null;
  shortDescription?: string | null;
  departureCity?: string | null;
  country?: string | null;
  tourType?: string | null;
  durationDays?: number | null;
  directionId?: string | null;
}

export interface TourDayInput {
  dayNumber: number;
  title: string;
  description: string;
}

export interface TourInclusionInput {
  text: string;
  kind: string;
  sortOrder: number;
}

export interface TourOfferInput {
  validFrom: string;
  validTo: string;
  pricePerPerson: number;
  currency: string;
  availableSeats: number;
}

export interface TourImageInput {
  mediaFileId: string;
  sortOrder: number;
  isCover: boolean;
  alt: string | null;
}

function readHeaderEtag(headers: unknown): string | null {
  if (!headers || typeof headers !== 'object') return null;
  const bag = headers as Record<string, unknown>;
  const value = bag.etag ?? bag.ETag;
  return typeof value === 'string' && value.trim() ? value : null;
}

export function normalizeManagedTour(tour: ManagedTour, headers?: unknown): ManagedTour {
  const etag = tour.etag || readHeaderEtag(headers) || `"${tour.version}"`;
  return {
    ...tour,
    etag,
    days: tour.days ?? [],
    inclusions: tour.inclusions ?? [],
    offers: tour.offers ?? [],
    images: tour.images ?? [],
  };
}

function ifMatch(etag: string) {
  return { headers: { 'If-Match': etag } };
}

export async function listManagedTours(): Promise<ManagedTour[]> {
  const { data, headers } = await apiClient.get<ManagedTour[]>('/catalog/manage/tours');
  return (data ?? []).map((tour) => normalizeManagedTour(tour, headers));
}

export async function getManagedTour(id: string): Promise<ManagedTour> {
  const { data, headers } = await apiClient.get<ManagedTour>(`/catalog/manage/tours/${id}`);
  return normalizeManagedTour(data, headers);
}

export async function createTourDraft(input: TourDraftInput): Promise<ManagedTour> {
  const { data, headers } = await apiClient.post<ManagedTour>('/catalog/manage/tours', input);
  return normalizeManagedTour(data, headers);
}

export async function saveBasics(id: string, etag: string, input: TourBasicsInput): Promise<ManagedTour> {
  const { data, headers } = await apiClient.put<ManagedTour>(
    `/catalog/manage/tours/${id}/basics`,
    input,
    ifMatch(etag),
  );
  return normalizeManagedTour(data, headers);
}

export async function saveDescription(id: string, etag: string, description: string): Promise<ManagedTour> {
  const { data, headers } = await apiClient.put<ManagedTour>(
    `/catalog/manage/tours/${id}/description`,
    { description },
    ifMatch(etag),
  );
  return normalizeManagedTour(data, headers);
}

export async function saveProgram(id: string, etag: string, days: TourDayInput[]): Promise<ManagedTour> {
  const { data, headers } = await apiClient.put<ManagedTour>(
    `/catalog/manage/tours/${id}/program`,
    { days },
    ifMatch(etag),
  );
  return normalizeManagedTour(data, headers);
}

export async function saveConditions(
  id: string,
  etag: string,
  input: { inclusions: TourInclusionInput[]; mealPlan: string | null; accommodationText: string | null },
): Promise<ManagedTour> {
  const { data, headers } = await apiClient.put<ManagedTour>(
    `/catalog/manage/tours/${id}/conditions`,
    input,
    ifMatch(etag),
  );
  return normalizeManagedTour(data, headers);
}

export async function saveOffers(id: string, etag: string, offers: TourOfferInput[]): Promise<ManagedTour> {
  const { data, headers } = await apiClient.put<ManagedTour>(
    `/catalog/manage/tours/${id}/prices`,
    { offers },
    ifMatch(etag),
  );
  return normalizeManagedTour(data, headers);
}

export async function saveImages(id: string, etag: string, images: TourImageInput[]): Promise<ManagedTour> {
  const { data, headers } = await apiClient.put<ManagedTour>(
    `/catalog/manage/tours/${id}/images`,
    { images },
    ifMatch(etag),
  );
  return normalizeManagedTour(data, headers);
}

export async function publishManagedTour(id: string, etag: string): Promise<ManagedTour> {
  const { data, headers } = await apiClient.post<ManagedTour>(
    `/catalog/manage/tours/${id}/publish`,
    null,
    ifMatch(etag),
  );
  return normalizeManagedTour(data, headers);
}

export async function unpublishManagedTour(id: string, etag: string): Promise<ManagedTour> {
  const { data, headers } = await apiClient.post<ManagedTour>(
    `/catalog/manage/tours/${id}/unpublish`,
    null,
    ifMatch(etag),
  );
  return normalizeManagedTour(data, headers);
}

export async function deleteManagedTour(id: string, etag: string): Promise<void> {
  await apiClient.delete(`/catalog/manage/tours/${id}`, ifMatch(etag));
}
