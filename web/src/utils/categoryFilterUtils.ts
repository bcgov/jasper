export const DEFAULT_OTHER_LABEL = 'Other';
export const DEFAULT_SECTION_TITLE = 'All Documents';
export const MULTI_SECTION_TITLE = 'Selected Categories';

type CategoryValue = string | null | undefined;

const normalizeCategory = (value: CategoryValue): string =>
  (value ?? '').trim().toLowerCase();

export const isAllOptionsSelected = (
  selectedValues: string[],
  totalOptions: number
): boolean => totalOptions > 0 && selectedValues.length === totalOptions;

export const getActiveSelections = (
  selectedValues: string[],
  allSelected: boolean
): string[] => (allSelected ? [] : selectedValues);

export const pruneInvalidSelections = (
  selectedValues: string[],
  validValues: string[]
): string[] => {
  const validSet = new Set(validValues);
  return selectedValues.filter((value) => validSet.has(value));
};

export const getSectionTitle = (
  activeValues: string[],
  mapDisplayTitle?: (value: string) => string
): string => {
  if (activeValues.length === 0) {
    return DEFAULT_SECTION_TITLE;
  }

  if (activeValues.length === 1) {
    return mapDisplayTitle ? mapDisplayTitle(activeValues[0]) : activeValues[0];
  }

  return MULTI_SECTION_TITLE;
};

export const getUncategorizedCount = <T>(
  items: T[],
  getCategory: (item: T) => CategoryValue
): number =>
  items.filter((item) => !normalizeCategory(getCategory(item))).length;

export const matchesCategorySelection = <T>(
  item: T,
  activeValues: string[],
  getCategory: (entry: T) => CategoryValue,
  options?: {
    otherLabel?: string;
    specialPredicates?: Record<string, (entry: T) => boolean>;
  }
): boolean => {
  if (activeValues.length === 0) {
    return true;
  }

  const normalizedCategory = normalizeCategory(getCategory(item));
  const normalizedOtherLabel = normalizeCategory(
    options?.otherLabel ?? DEFAULT_OTHER_LABEL
  );

  return activeValues.some((value) => {
    const normalizedValue = normalizeCategory(value);
    const specialPredicate = options?.specialPredicates?.[normalizedValue];

    if (specialPredicate) {
      return specialPredicate(item);
    }

    if (normalizedValue === normalizedOtherLabel) {
      return !normalizedCategory;
    }

    return normalizedCategory === normalizedValue;
  });
};
