import { ref } from 'vue';

declare global {
  const NutrientViewer: any;
}

// Shared interfaces
export interface PDFViewerState {
  loading: boolean;
  emptyStore: boolean;
}

export interface OutlineItem {
  title: string;
  pageIndex?: number;
  children?: OutlineItem[];
  isExpanded?: boolean;
}

// Shared PDF viewer utilities
export const usePDFViewer = () => {
  const loading = ref(false);
  const emptyStore = ref(false);

  const configuration = { container: '.pdf-container' };

  const createNutrientOutline = (outlineData: OutlineItem[]): any => {
    return NutrientViewer.Immutable.List(
      outlineData.map((item) => createOutlineElement(item))
    );
  };

  const createOutlineElement = (item: OutlineItem): any => {
    if (item.children && item.children.length > 0) {
      // It's a group
      return new NutrientViewer.OutlineElement({
        title: item.title,
        isExpanded: item.isExpanded ?? true,
        children: NutrientViewer.Immutable.List(
          item.children.map((child) => createOutlineElement(child))
        ),
      });
    } else {
      // It's a document
      return new NutrientViewer.OutlineElement({
        title: item.title,
        action: item.pageIndex !== undefined ? 
          new NutrientViewer.Actions.GoToAction({
            pageIndex: item.pageIndex,
          }) : undefined,
      });
    }
  };

  const loadPDFViewer = async (
    base64Pdf: string, 
    outline: OutlineItem[]
  ): Promise<void> => {
    const nutrientOutline = createNutrientOutline(outline);
    
    const instance = await NutrientViewer.load({
      ...configuration,
      document: `data:application/pdf;base64,${base64Pdf}`,
    });

    instance.setDocumentOutline(nutrientOutline);
    instance.setViewState((viewState) =>
      viewState.set('sidebarMode', NutrientViewer.SidebarMode.DOCUMENT_OUTLINE)
    );
  };

  const unloadPDFViewer = (): void => {
    if (NutrientViewer) {
      NutrientViewer.unload('.pdf-container');
    }
  };

  return {
    loading,
    emptyStore,
    configuration,
    createNutrientOutline,
    createOutlineElement,
    loadPDFViewer,
    unloadPDFViewer,
  };
};