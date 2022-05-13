export class Setting {
  key: string;
  value: boolean | number | null | string;
  displayName: null | string;
  description: null | string;
  type: string;
  settings: Setting[];
  enumValues: { [key: string]: string };
}
