export interface QuickLink {
  id: string;
  name: string;
  parentName: string;
  isMenu?: boolean;
  url: string;
  order: number;
  judgeId: string;
  children?: QuickLink[];
}
