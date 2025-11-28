import { describe, it, expect, beforeEach, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import CriminalAppearanceDetails from 'CMP/case-details/criminal/appearances/CriminalAppearanceDetails.vue';
import { FilesService } from '@/services/FilesService';

vi.mock('@/services/FilesService');

describe('CriminalAppearanceDetails.vue', () => {
  let wrapper: any;
  let filesService: any;

  const mockDetails = {
    appearanceMethod: [
      { roleTypeDesc: 'Plaintiff', appearanceMethodDesc: 'In Person' },
    ],
    document: [{ id: 1, name: 'Document 1' }],
    party: [{ id: 1, name: 'Party 1' }],
    justinCounsel: { fullName: 'Josh Traill' }
  };

  beforeEach(() => {
    filesService = {
      criminalAppearanceDetails: vi.fn().mockResolvedValue(
        mockDetails
      )
    };
    (FilesService as any).mockReturnValue(filesService);
    wrapper = mount(CriminalAppearanceDetails, {
      global: {
        provide: {
          filesService
        }
      },
      props: {
        fileId: '123',
        appearanceId: '456',
        partId: '789'
      },
    });
  });

  it('renders the tabs correctly', async () => {
    await flushPromises();
    const tabs = wrapper.findAll('v-tab');
    expect(tabs.length).toBe(3);
    expect(tabs[0].text()).toBe('Charges');
    expect(tabs[1].text()).toBe('Appearance Methods');
    expect(tabs[2].text()).toBe('Counsel');
  });

  it('calls appearance details api with correct parameters', async () => {
    expect(filesService.criminalAppearanceDetails).toHaveBeenCalledWith(
      '123',
      '456',
      '789'
    );
  });

  it('renders AppearanceMethods component when "methods" tab is active', async () => {
    wrapper.vm.tab = 'methods';
    expect(wrapper.findComponent({name: 'CriminalAppearanceMethods'}).exists()).toBe(true);
  });

  it('renders AppearanceCounsel component when "counsel" tab is active', async () => {
    wrapper.vm.tab = 'counsel';
    expect(wrapper.findComponent({name: 'AppearanceCounsel'}).exists()).toBe(true);
  });

  it('does not render Counsel tab when no counsel exists', async () => {
    filesService = {
      criminalAppearanceDetails: vi.fn().mockResolvedValue(
        { ...mockDetails, justinCounsel: null }
      )
    };
    (FilesService as any).mockReturnValue(filesService);
    wrapper = mount(CriminalAppearanceDetails, {
      global: {
        provide: {
          filesService
        }
      },
      props: {
        fileId: '123',
        appearanceId: '456',
        partId: '789'
      },
    });
    
    await flushPromises();

    const tabs = wrapper.findAll('v-tab');
    expect(tabs.length).toBe(2);
    expect(tabs[0].text()).toBe('Charges');
    expect(tabs[1].text()).toBe('Appearance Methods');
  });
});