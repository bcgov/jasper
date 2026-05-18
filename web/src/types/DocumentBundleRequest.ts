import { AppearanceDocumentRequest } from './AppearanceDocumentRequest';
import { BinderDocumentRequest } from './BinderDocumentRequest';

export interface CriminalDocumentBundleRequest {
  appearances: AppearanceDocumentRequest[];
}

export interface BinderDocumentBundleRequest {
  binders: BinderDocumentRequest[];
}
