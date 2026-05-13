import { describe, it, expect, vi, beforeEach } from 'vitest';
import { shallowMount } from '@vue/test-utils';
import NutrientContainer from '@/components/documents/NutrientContainer.vue';

// Mocks
vi.mock('@/components/documents/FileViewer.vue', () => ({
  default: { name: 'FileViewer', template: '<div />', props: ['strategy'] },
}));

vi.mock('@/components/documents/strategies/PDFStrategyFactory', () => {
  const PDFViewerType = {
    FILE: 'FILE',
    KEY_DOCUMENT: 'KEY_DOCUMENT',
    JUDICIAL_BINDER: 'JUDICIAL_BINDER',
  };
  const mockUsePDFStrategy = vi.fn();
  return {
    PDFViewerType,
    usePDFStrategy: mockUsePDFStrategy,
    mockUsePDFStrategy,
  };
});

import {
  PDFViewerType,
  usePDFStrategy,
} from '@/components/documents/strategies/PDFStrategyFactory';

vi.mock('vue-router', () => ({
  useRoute: vi.fn(),
}));

import { useRoute } from 'vue-router';

describe('NutrientContainer.vue', () => {
  beforeEach(() => {
    const mockUsePDFStrategy = usePDFStrategy as unknown as ReturnType<
      typeof vi.fn
    >;
    mockUsePDFStrategy.mockReset();
    (useRoute as any).mockReset();
  });

  it('uses FILE strategy when type is nutrient', () => {
    const mockUsePDFStrategy = usePDFStrategy as unknown as ReturnType<
      typeof vi.fn
    >;
    (useRoute as any).mockReturnValue({ query: { type: 'nutrient' } });
    mockUsePDFStrategy.mockReturnValue('file-strategy');
    const wrapper = shallowMount(NutrientContainer);
    expect(mockUsePDFStrategy).toHaveBeenCalledWith(PDFViewerType.FILE);
    expect(
      wrapper.findComponent({ name: 'FileViewer' }).props('strategy')
    ).toBe('file-strategy');
  });

  it('uses FILE strategy when type is file', () => {
    const mockUsePDFStrategy = usePDFStrategy as unknown as ReturnType<
      typeof vi.fn
    >;
    (useRoute as any).mockReturnValue({ query: { type: 'file' } });
    mockUsePDFStrategy.mockReturnValue('file-strategy');
    const wrapper = shallowMount(NutrientContainer);
    expect(mockUsePDFStrategy).toHaveBeenCalledWith(PDFViewerType.FILE);
  });

  it('uses FILE strategy when type is pdf', () => {
    const mockUsePDFStrategy = usePDFStrategy as unknown as ReturnType<
      typeof vi.fn
    >;
    (useRoute as any).mockReturnValue({ query: { type: 'pdf' } });
    mockUsePDFStrategy.mockReturnValue('file-strategy');
    const wrapper = shallowMount(NutrientContainer);
    expect(mockUsePDFStrategy).toHaveBeenCalledWith(PDFViewerType.FILE);
  });

  it('throws error for unknown type', () => {
    const mockUsePDFStrategy = usePDFStrategy as unknown as ReturnType<
      typeof vi.fn
    >;
    (useRoute as any).mockReturnValue({ query: { type: 'unknown' } });
    mockUsePDFStrategy.mockReturnValue('file-strategy');
    // Error is thrown inside computed, so mounting will throw
    expect(() => shallowMount(NutrientContainer)).toThrow(
      'Unknown PDF viewer type: unknown'
    );
  });

  it('uses FILE strategy and warns when type is missing', () => {
    const mockUsePDFStrategy = usePDFStrategy as unknown as ReturnType<
      typeof vi.fn
    >;
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
    (useRoute as any).mockReturnValue({ query: {} });
    mockUsePDFStrategy.mockReturnValue('file-strategy');
    const wrapper = shallowMount(NutrientContainer);
    expect(mockUsePDFStrategy).toHaveBeenCalledWith(PDFViewerType.FILE);
    expect(warnSpy).toHaveBeenCalledWith('Could not determine PDF viewer type');
    warnSpy.mockRestore();
  });

  it('uses CRIMINAL_BUNDLE strategy when type is criminal-bundle', () => {
    const mockUsePDFStrategy = usePDFStrategy as unknown as ReturnType<
      typeof vi.fn
    >;
    (useRoute as any).mockReturnValue({ query: { type: 'criminal-bundle' } });
    mockUsePDFStrategy.mockReturnValue('criminal-bundle-strategy');
    const wrapper = shallowMount(NutrientContainer);
    expect(mockUsePDFStrategy).toHaveBeenCalledWith(
      PDFViewerType.CRIMINAL_BUNDLE
    );
    expect(
      wrapper.findComponent({ name: 'FileViewer' }).props('strategy')
    ).toBe('criminal-bundle-strategy');
  });

  it('uses JUDICIAL_BINDER strategy when type is judicial-binder', () => {
    const mockUsePDFStrategy = usePDFStrategy as unknown as ReturnType<
      typeof vi.fn
    >;
    (useRoute as any).mockReturnValue({ query: { type: 'judicial-binder' } });
    mockUsePDFStrategy.mockReturnValue('judicial-binder-strategy');
    const wrapper = shallowMount(NutrientContainer);
    expect(mockUsePDFStrategy).toHaveBeenCalledWith(
      PDFViewerType.JUDICIAL_BINDER
    );
    expect(
      wrapper.findComponent({ name: 'FileViewer' }).props('strategy')
    ).toBe('judicial-binder-strategy');
  });
});
