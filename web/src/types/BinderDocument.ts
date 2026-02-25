import { DocumentRequestType } from '@/types/shared';

export interface BinderDocument {
  // Common fields
  documentId: string;
  category: string;
  imageId: string;
  fileName: string;
  order: number; // For ordering documents within a binder
  documentType: DocumentRequestType;

  // Criminal-specific fields
  documentPageCount?: number | null;

  // Civil-specific fields
  fileSeqNo: string;
  swornByNm: string;
  filedDt: string;
  dateGranted: string;

  issues: Issue[];
  filedBy: FiledBy[];
  documentSupport: DocumentSupport[];
  orderId?: string; // For transcript documents
}

export interface DocumentSupport {
  actCd: string;
  actDsc: string;
}

export interface Issue {
  issueNumber: string;
  issueDsc: string;
}

export interface FiledBy {
  filedByName: string;
  roleTypeCode: string;
}
