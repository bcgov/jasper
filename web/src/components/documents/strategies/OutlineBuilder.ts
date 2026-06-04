import { CourtDocumentType, DocumentData } from '@/types/shared';
import { OutlineItem } from './PDFViewerTypes';
import shared from '@/components/shared';

export type OutlineDocumentInput = {
  documentId?: string | null;
  pageIndex?: number;
  documentType: CourtDocumentType;
  documentData: DocumentData;
};

export type OutlineBinderInput = {
  fileNumber?: string | null;
  courtClassCd?: string | null;
  physicalFileId?: string | null;
  documents: OutlineDocumentInput[];
};

export type BuildGroupedPdfOutlineOptions = {
  binders: OutlineBinderInput[];
};

export type GroupedOutlineEntry = {
  groupKeyOne: string;
  groupKeyTwo?: string;
  title: string;
  pageIndex?: number;
};

export function buildGroupedPdfOutline(
  options: BuildGroupedPdfOutlineOptions
): OutlineItem[] {
  return options.binders
    .map((binder): OutlineItem | undefined => {
      const children = binder.documents.map(
        (document): OutlineItem => ({
          title: shared.getDocumentDisplayName(
            document.documentType,
            document.documentData
          ),
          pageIndex: document.pageIndex,
        })
      );

      if (children.length === 0) {
        return undefined;
      }

      return {
        title: buildBinderTitle(binder),
        pageIndex: children[0].pageIndex,
        children,
      };
    })
    .filter((item): item is OutlineItem => item !== undefined);
}

export function buildGroupedEntriesOutline(
  entries: GroupedOutlineEntry[]
): OutlineItem[] {
  const groups = new Map<
    string,
    {
      pageIndex?: number;
      children: OutlineItem[];
      subgroups: Map<string, OutlineItem>;
    }
  >();

  entries.forEach((entry) => {
    let group = groups.get(entry.groupKeyOne);

    if (!group) {
      group = {
        pageIndex: undefined,
        children: [],
        subgroups: new Map<string, OutlineItem>(),
      };
      groups.set(entry.groupKeyOne, group);
    }

    group.pageIndex ??= entry.pageIndex;

    const outlineItem: OutlineItem = {
      title: entry.title,
      pageIndex: entry.pageIndex,
    };

    if (entry.groupKeyTwo) {
      let subgroup = group.subgroups.get(entry.groupKeyTwo);

      if (!subgroup) {
        subgroup = {
          title: entry.groupKeyTwo,
          pageIndex: entry.pageIndex,
          children: [],
        };
        group.subgroups.set(entry.groupKeyTwo, subgroup);
        group.children.push(subgroup);
      }

      subgroup.pageIndex ??= entry.pageIndex;

      subgroup.children ??= [];
      subgroup.children.push(outlineItem);
      return;
    }

    group.pageIndex ??= entry.pageIndex;

    group.children.push(outlineItem);
  });

  return [...groups.entries()]
    .map(([title, group]): OutlineItem | undefined => {
      if (group.children.length === 0) {
        return undefined;
      }

      return {
        title,
        pageIndex: group.pageIndex,
        children: group.children,
      };
    })
    .filter((item): item is OutlineItem => item !== undefined);
}

export function buildGroupedOutline<T>(
  groupedDocuments: Record<string, Record<string, T[]>>,
  buildDocument: (document: T) => OutlineItem
): OutlineItem[] {
  return Object.entries(groupedDocuments).map(([groupKeyOne, groupedItems]) => {
    const children = Object.entries(groupedItems).map(
      ([groupKeyTwo, documents]) => {
        const documentChildren = documents.map(buildDocument);

        if (!groupKeyTwo) {
          return documentChildren;
        }

        return {
          title: groupKeyTwo,
          children: documentChildren,
        };
      }
    );

    return {
      title: groupKeyOne,
      children: children.flat(),
    };
  });
}

function buildBinderTitle(binder: OutlineBinderInput): string {
  if (binder.courtClassCd && binder.physicalFileId) {
    return `${binder.courtClassCd}-${binder.physicalFileId}`;
  }

  if (binder.fileNumber) {
    return binder.fileNumber;
  }

  return binder.physicalFileId ?? 'Binder';
}
