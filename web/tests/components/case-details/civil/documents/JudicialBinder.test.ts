import JudicialBinder from '@/components/case-details/civil/documents/JudicialBinder.vue';
import { civilDocumentType } from '@/types/civil/jsonTypes';
import { mount } from '@vue/test-utils';
import { describe, expect, it, vi } from 'vitest';

describe('JudicialBinder.vue', () => {
  const mockProps = {
    isBinderLoading: false,
    courtClassCdStyle: '',
    rolesLoading: false,
    roles: [],
    baseHeaders: [],
    binderDocuments: [] as civilDocumentType[],
    openIndividualDocument: vi.fn(),
  };

  it('renders skeleton-loader when binder is loading', () => {
    mockProps.isBinderLoading = true;

    const wrapper = mount(JudicialBinder, {
      props: mockProps,
    });

    const loader = wrapper.find('v-skeleton-loader');
    const jbContainerEl = wrapper.find('[data-testid="jb-container"]');

    expect(loader.exists()).toBe(true);
    expect(jbContainerEl.exists()).toBe(false);
  });

  it('renders judicial binder', () => {
    mockProps.isBinderLoading = false;
    mockProps.binderDocuments = [{} as civilDocumentType];

    const wrapper = mount(JudicialBinder, {
      props: mockProps,
    });

    const loader = wrapper.find('v-skeleton-loader');
    const jbContainerEl = wrapper.find('[data-testid="jb-container"]');
    const alertEl = wrapper.find('v-alert');
    const tableEl = wrapper.find('v-data-table-virtual');

    expect(loader.exists()).toBe(false);
    expect(jbContainerEl.exists()).toBe(true);
    expect(alertEl.exists()).toBe(true);
    expect(tableEl.exists()).toBe(true);
  });
});
