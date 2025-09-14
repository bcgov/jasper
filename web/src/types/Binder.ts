import { BinderDocument } from './BinderDocument';

export interface Binder {
  id: string | null;
  labels: { [key: string]: string };
  documents: BinderDocument[];
  updatedDate: Date;
}
