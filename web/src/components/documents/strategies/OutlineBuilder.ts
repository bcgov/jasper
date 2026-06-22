import { OutlineItem } from './PDFViewerTypes';

export type GroupedOutlineEntry = {
  groupKeyOne: string;
  groupKeyTwo?: string;
  title: string;
  pageIndex?: number;
};

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
