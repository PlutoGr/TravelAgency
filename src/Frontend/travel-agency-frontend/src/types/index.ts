export type TourImage = {
  mediaFileId: string;
  alt: string | null;
  isCover: boolean;
  sortOrder: number;
};

export type TourDay = {
  dayNumber: number;
  title: string;
  description: string;
};

export type TourOffer = {
  id: string;
  start: string;
  end: string;
  price: number;
  currency: string;
  seats: number;
};

export type Tour = {
  id: string;
  title: string;
  description: string;
  shortDescription: string;
  country: string;
  city: string;
  hotel: string;
  price: number;
  originalPrice?: number;
  rating: number;
  reviewCount: number;
  dates: { start: string; end: string }[];
  duration: number;
  /** Id обложки в Media. Картинка собирается через mediaImageUrl. */
  coverMediaFileId?: string | null;
  photos: string[];
  amenities: string[];
  included: string[];
  notIncluded: string[];
  category: string;
  isHot: boolean;
  maxTravelers: number;
  /** false — тур снят с публикации и остаётся только в избранном. */
  available?: boolean;
  departureCity?: string;
  mealPlan?: string | null;
  accommodation?: string;
  currency?: string | null;
  nearestDate?: string | null;
  priceFrom?: number | null;
  images?: TourImage[];
  days?: TourDay[];
  offers?: TourOffer[];
};

export type Destination = {
  id: string;
  name: string;
  country: string;
  photo: string;
  tourCount: number;
  description: string;
};

export type Review = {
  id: string;
  tourId: string;
  userName: string;
  userAvatar: string;
  rating: number;
  text: string;
  date: string;
  tourTitle: string;
};

export type User = {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phone: string;
  /** Optional. Not populated by backend UserProfileDto; reserved for future use. */
  avatar?: string;
  passport?: string;
  role: 'client' | 'manager' | 'admin';
  createdAt: string;
};

export type BookingStatus =
  | 'new'
  | 'in_progress'
  | 'proposal_sent'
  | 'confirmed'
  | 'closed'
  | 'cancelled';

/** Снимок тура внутри предложения. Поля совпадают с BookingTourSnapshotDto. */
export type BookingTourSnapshot = {
  tourId: string;
  title: string;
  description: string;
  price: number;
  currency: string;
  durationDays: number;
  snapshotTakenAt: string;
};

/** Предложение менеджера. Поля совпадают с ProposalDto. */
export type BookingProposal = {
  id: string;
  bookingId: string;
  managerId: string;
  tourSnapshot: BookingTourSnapshot;
  notes: string | null;
  isConfirmed: boolean;
  createdAt: string;
};

/**
 * Бронь как её отдаёт BookingDto.
 * Название тура в ответе нет: карточка каталога подставляется отдельно, по tourId.
 */
export type Booking = {
  id: string;
  clientId: string;
  tourId: string;
  comment: string | null;
  status: BookingStatus;
  createdAt: string;
  updatedAt: string | null;
  proposals: BookingProposal[];
  clientName: string | null;
  clientEmail: string | null;
  clientPhone: string | null;
  /** Карточка публичного каталога. В BookingDto её нет. */
  tour?: Tour;
};

export type ChatMessage = {
  id: string;
  bookingId: string;
  senderId: string;
  senderName: string;
  senderRole: 'client' | 'manager';
  text: string;
  attachments?: string[];
  createdAt: string;
};

export type AuthTokens = {
  accessToken: string;
  refreshToken: string;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterRequest = {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone?: string;
};

export type UpdateProfileRequest = {
  firstName?: string;
  lastName?: string;
  phone?: string;
};

export type TourFilters = {
  search?: string;
  country?: string[];
  priceMin?: number;
  priceMax?: number;
  dateFrom?: string;
  dateTo?: string;
  rating?: number;
  amenities?: string[];
  category?: string;
  directionId?: string;
  sortBy?:
    | 'popularity'
    | 'price_asc'
    | 'price_desc'
    | 'date'
    | 'rating'
    | 'title'
    | 'duration_asc'
    | 'duration_desc';
};

export type PaginatedResponse<T> = {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
};
