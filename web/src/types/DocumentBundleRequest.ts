import { AppearanceDocumentRequest } from './AppearanceDocumentRequest';
import { BinderDocumentRequest } from './BinderDocumentRequest';

export interface KeyDocumentBundleRequest {
  appearances: AppearanceDocumentRequest[];
}

export interface BinderDocumentBundleRequest {
  binders: BinderDocumentRequest[];
}
