import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import CivilAppearanceDetails from 'CMP/civil/CivilAppearanceDetails.vue';
import { FilesService } from '@/services/FilesService';
import { setActivePinia, createPinia } from 'pinia'

vi.mock('@/services/FilesService');

beforeEach(() => {
    setActivePinia(createPinia());
});

describe('CivilAppearanceDetails.vue', () => {
  let wrapper: any;
  let filesService: any;

  const mockDetails = {
    appearanceMethod: [
      { roleTypeDesc: 'Plaintiff', appearanceMethodDesc: 'In Person' },
    ],
    document: [{ id: 1, name: 'Document 1' }],
    party: [{ id: 1, name: 'Party 1' }],
  };

  beforeEach(() => {
    filesService = {
      civilAppearanceDetails: vi.fn().mockResolvedValue([
        mockDetails
      ])
    };
    (FilesService as any).mockReturnValue(filesService);
    wrapper = mount(CivilAppearanceDetails, {
      global: {
        provide: {
          filesService
        }
      },
      props: {
        fileId: '123',
        appearanceId: '456',
      },
    });
  });

  it('renders the tabs correctly', () => {
    const tabs = wrapper.findAll('v-tab');
    expect(tabs.length).toBe(3);
    expect(tabs[0].text()).toBe('Scheduled Documents');
    expect(tabs[1].text()).toBe('Judicial Binder');
    expect(tabs[2].text()).toBe('Scheduled Parties');
  });

  it('calls appearance details api with correct parameters', async () => {
    expect(filesService.civilAppearanceDetails).toHaveBeenCalledWith(
      '123',
      '456'
    );
  });

  it('renders ScheduledDocuments component when "documents" tab is active', async () => {
    wrapper.vm.tab = 'documents';
    expect(wrapper.findComponent({name: 'ScheduledDocuments'}).exists()).toBe(true);
  });

  it('renders ScheduledParties component when "documents" tab is active', async () => {
    wrapper.vm.tab = 'parties';
    expect(wrapper.findComponent({name: 'ScheduledParties'}).exists()).toBe(true);
    expect(wrapper.findComponent({name: 'CivilAppearanceMethods'}).exists()).toBe(false);
  });
});