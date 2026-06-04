// DocumentBundleRequest.ts

import { AppearanceDocumentRequest } from './AppearanceDocumentRequest';
import { BinderDocument } from './BinderDocument';

export interface CriminalDocumentBundleRequest {
  appearances: AppearanceDocumentRequest[];
}

export interface BinderRequest {
  id?: string | null;
  labels: Partial<{
    courtClassCd: string;
    isCriminal: string;
    judgeId: string;
    physicalFileId: string;
    participantId: string;
  }>;
  fileNumber: string;
  documents?: BinderDocument[];
}

export interface BinderDocumentBundleRequest {
  binderRequests: BinderRequest[];
}
