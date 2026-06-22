export interface PdfSourceStore<TItem> {
  hasPdfData(sessionId?: string): boolean;
  getPdfItems(sessionId?: string): TItem[];
  clearPdfItems(sessionId?: string): void;
}
