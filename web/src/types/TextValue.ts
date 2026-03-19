export interface TextValue<T = string> {
  text: string;
  value: T;
}

export interface ItemGroup {
  label: string;
  items: TextValue[];
}
