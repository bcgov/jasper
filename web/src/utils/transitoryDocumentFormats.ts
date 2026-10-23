import {
  ApplicationConfigurationKey,
  useCommonStore,
} from '@/stores/CommonStore';

export const isSupportedTransitoryViewerExtension = (
  extension: string | null | undefined
): boolean => {
  const normalizedExtension = normalizeExtension(extension);
  if (!normalizedExtension) {
    return false;
  }

  const commonStore = useCommonStore();
  const enabledExtensions = new Set(
    commonStore
      .getConfigurationValues(
        ApplicationConfigurationKey.TransitoryDocumentConvertibleExtensions
      )
      .map(normalizeExtension)
      .filter((value): value is string => value !== null)
  );

  return enabledExtensions.has(normalizedExtension);
};

export const normalizeExtension = (
  extension: string | null | undefined
): string | null => {
  let normalized = extension?.trim().toLowerCase();
  if (!normalized) return null;
  if (!normalized.startsWith('.')) normalized = `.${normalized}`;
  return /^\.[a-z0-9]{1,15}$/.test(normalized) ? normalized : null;
};
