export const DEFAULT_SECTION_TITLE = 'All Documents';
export const MULTI_SECTION_TITLE = 'Selected Categories';
export const OTHER_CATEGORY_LABEL = 'Other';
export const SCHEDULED_CATEGORY_FILTER = '__scheduled__';
export const UNCATEGORIZED_CATEGORY_FILTER = '__uncategorized__';
export const UNCATEGORIZED_CATEGORY_LABEL = 'Uncategorized';

type CategoryValue = string | null | undefined;

export const normalizeCategory = (value: CategoryValue): string =>
  (value ?? '').trim().toLowerCase();

export const isAllOptionsSelected = (
  selectedValues: string[],
  validValues: string[]
): boolean =>
  validValues.length > 0 &&
  validValues.every((value) => selectedValues.includes(value));

export const getActiveSelections = (
  selectedValues: string[],
  allSelected: boolean
): string[] => (allSelected ? [] : selectedValues);

export const pruneInvalidSelections = (
  selectedValues: string[],
  validValues: string[]
): string[] => {
  const validValuesByIdentity = new Map(
    validValues.map((value) => [normalizeCategory(value), value])
  );

  return [
    ...new Set(
      selectedValues
        .map((value) => validValuesByIdentity.get(normalizeCategory(value)))
        .filter((value): value is string => value !== undefined)
    ),
  ];
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
    uncategorizedValue?: string;
    specialPredicates?: Record<string, (entry: T) => boolean>;
  }
): boolean => {
  if (activeValues.length === 0) {
    return true;
  }

  const normalizedCategory = normalizeCategory(getCategory(item));
  const normalizedUncategorizedValue = normalizeCategory(
    options?.uncategorizedValue ?? UNCATEGORIZED_CATEGORY_FILTER
  );

  return activeValues.some((value) => {
    const normalizedValue = normalizeCategory(value);
    const specialPredicate = options?.specialPredicates?.[normalizedValue];

    if (specialPredicate) {
      return specialPredicate(item);
    }

    if (normalizedValue === normalizedUncategorizedValue) {
      return !normalizedCategory;
    }

    return normalizedCategory === normalizedValue;
  });
};
