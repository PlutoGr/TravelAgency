import { apiClient } from './client';

export interface UploadedTourImage {
  id: string;
  width?: number | null;
  height?: number | null;
}

export async function uploadTourImage(file: File): Promise<UploadedTourImage> {
  const body = new FormData();
  body.append('file', file);
  const { data } = await apiClient.postForm<UploadedTourImage>('/media/upload', body, {
    params: { purpose: 'tour-image' },
  });
  return data;
}
