export interface TextValue<T = string> {
  text: string;
  value: T;
  color?: string;
}

export interface ItemGroup {
  label: string;
  items: TextValue[];
}
