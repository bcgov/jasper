import { GeneratePdfResponse } from '@/components/documents/models/GeneratePdf';
import { Binder } from '@/types';

export interface DocumentBundleResponse {
  binders: Binder[];
  pdfResponse: GeneratePdfResponse;
}
