import { describe, it, expect, vi, beforeEach } from 'vitest';
import { shallowMount } from '@vue/test-utils';
import NutrientContainer from '@/components/documents/NutrientContainer.vue';

// Mocks
vi.mock('@/components/documents/FileViewer.vue', () => ({
  default: { name: 'FileViewer', template: '<div />', props: ['strategy'] },
}));

vi.mock('@/components/documents/strategies/PDFStrategyFactory', () => {
  const PDFViewerType = {
    BUNDLE: 'BUNDLE',
    FILE: 'FILE',
  };
  const mockUsePDFStrategy = vi.fn();
  return {
    PDFViewerType,
    usePDFStrategy: mockUsePDFStrategy,
    mockUsePDFStrategy,
  };
});

// Get PDFViewerType and mockUsePDFStrategy from the mocked module
import { PDFViewerType, usePDFStrategy, mockUsePDFStrategy } from '@/components/documents/strategies/PDFStrategyFactory';
// Use the mock reference for testing
// mockUsePDFStrategy is already a vi.fn() mock function from the mock definition

vi.mock('vue-router', () => ({
  useRoute: vi.fn(),
}));

import { useRoute } from 'vue-router';

describe('NutrientContainer.vue', () => {
  beforeEach(() => {
    mockUsePDFStrategy.mockReset();
    (useRoute as any).mockReset();
  });

  it('uses BUNDLE strategy when type is bundle', () => {
    (useRoute as any).mockReturnValue({ query: { type: 'bundle' } });
    mockUsePDFStrategy.mockReturnValue('bundle-strategy');
    const wrapper = shallowMount(NutrientContainer);
    expect(mockUsePDFStrategy).toHaveBeenCalledWith(PDFViewerType.BUNDLE);
    expect(wrapper.findComponent({ name: 'FileViewer' }).props('strategy')).toBe('bundle-strategy');
  });

  it('uses FILE strategy when type is nutrient', () => {
    (useRoute as any).mockReturnValue({ query: { type: 'nutrient' } });
    mockUsePDFStrategy.mockReturnValue('file-strategy');
    const wrapper = shallowMount(NutrientContainer);
    expect(mockUsePDFStrategy).toHaveBeenCalledWith(PDFViewerType.FILE);
    expect(wrapper.findComponent({ name: 'FileViewer' }).props('strategy')).toBe('file-strategy');
  });

  it('uses FILE strategy when type is file', () => {
    (useRoute as any).mockReturnValue({ query: { type: 'file' } });
    mockUsePDFStrategy.mockReturnValue('file-strategy');
    const wrapper = shallowMount(NutrientContainer);
    expect(mockUsePDFStrategy).toHaveBeenCalledWith(PDFViewerType.FILE);
  });

  it('uses FILE strategy when type is pdf', () => {
    (useRoute as any).mockReturnValue({ query: { type: 'pdf' } });
    mockUsePDFStrategy.mockReturnValue('file-strategy');
    const wrapper = shallowMount(NutrientContainer);
    expect(mockUsePDFStrategy).toHaveBeenCalledWith(PDFViewerType.FILE);
  });

  it('throws error for unknown type', () => {
    (useRoute as any).mockReturnValue({ query: { type: 'unknown' } });
    mockUsePDFStrategy.mockReturnValue('file-strategy');
    // Error is thrown inside computed, so mounting will throw
    expect(() => shallowMount(NutrientContainer)).toThrow('Unknown PDF viewer type: unknown');
  });

  it('uses FILE strategy and warns when type is missing', () => {
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
    (useRoute as any).mockReturnValue({ query: {} });
    mockUsePDFStrategy.mockReturnValue('file-strategy');
    const wrapper = shallowMount(NutrientContainer);
    expect(mockUsePDFStrategy).toHaveBeenCalledWith(PDFViewerType.FILE);
    expect(warnSpy).toHaveBeenCalledWith('Could not determine PDF viewer type');
    warnSpy.mockRestore();
  });
});