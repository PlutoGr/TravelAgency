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
  photos: string[];
  amenities: string[];
  included: string[];
  notIncluded: string[];
  category: string;
  isHot: boolean;
  maxTravelers: number;
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
  | 'closed';

export type Booking = {
  id: string;
  clientId: string;
  clientName: string;
  destination: string;
  country: string;
  dateFrom: string;
  dateTo: string;
  travelers: number;
  budget: number;
  status: BookingStatus;
  managerId?: string;
  managerName?: string;
  tourId?: string;
  tour?: Tour;
  notes: string;
  createdAt: string;
  updatedAt: string;
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
  phone: string;
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
  sortBy?: 'popularity' | 'price_asc' | 'price_desc' | 'date' | 'rating';
};

export type PaginatedResponse<T> = {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
};
