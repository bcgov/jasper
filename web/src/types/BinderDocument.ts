import { DocumentRequestType } from '@/types/shared';

export interface BinderDocument {
  documentId: string;
  order: number;
  documentType: DocumentRequestType;
  fileName: string;
}