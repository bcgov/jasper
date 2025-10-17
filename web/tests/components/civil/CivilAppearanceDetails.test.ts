import { FilesService } from '@/services/FilesService';
import { mount } from '@vue/test-utils';
import CivilAppearanceDetails from 'CMP/civil/CivilAppearanceDetails.vue';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/services/FilesService');

beforeEach(() => {
  setActivePinia(createPinia());
});

describe('CivilAppearanceDetails.vue', () => {
  let filesService: any;

  const mockDetails = {
    appearanceMethod: [
      { roleTypeDesc: 'Plaintiff', appearanceMethodDesc: 'In Person' },
    ],
    document: [{ id: 1, name: 'Document 1' }],
    party: [{ id: 1, name: 'Party 1' }],
  };

  const mountComponent = (showBinder = true) => {
    return mount(CivilAppearanceDetails, {
      global: {
        provide: {
          filesService,
        },
      },
      props: {
        fileId: '123',
        appearanceId: '456',
        showBinder,
      },
    });
  };

  beforeEach(() => {
    filesService = {
      civilAppearanceDetails: vi.fn().mockResolvedValue([mockDetails]),
    };
    (FilesService as any).mockReturnValue(filesService);
  });

  it('renders the tabs correctly', () => {
    const wrapper = mountComponent();
    const tabs = wrapper.findAll('v-tab');
    expect(tabs.length).toBe(3);
    expect(tabs[0].text()).toBe('Scheduled Documents');
    expect(tabs[1].text()).toBe('Judicial Binder');
    expect(tabs[2].text()).toBe('Scheduled Parties');
  });

  it('calls appearance details api with correct parameters', async () => {
    mountComponent();
    expect(filesService.civilAppearanceDetails).toHaveBeenCalledWith(
      '123',
      '456',
      true
    );
  });

  it('renders ScheduledDocuments component when "documents" tab is active', async () => {
    const wrapper: any = mountComponent();
    wrapper.vm.tab = 'documents';
    expect(wrapper.findComponent({ name: 'ScheduledDocuments' }).exists()).toBe(
      true
    );
  });

  it('renders ScheduledParties component when "documents" tab is active', async () => {
    const wrapper: any = mountComponent();
    wrapper.vm.tab = 'parties';
    expect(wrapper.findComponent({ name: 'ScheduledParties' }).exists()).toBe(
      true
    );
    expect(
      wrapper.findComponent({ name: 'CivilAppearanceMethods' }).exists()
    ).toBe(false);
  });

  it('renders JudicialBinder component when "binder" tab active and showBinder is true', async () => {
    const wrapper: any = mountComponent();
    wrapper.vm.tab = 'binder';
    expect(wrapper.findComponent({ name: 'JudicialBinder' }).exists()).toBe(
      true
    );
    expect(wrapper.find('[data-testid="binder-tab"]').exists()).toBe(true);
    expect(
      wrapper.findComponent({ name: 'CivilAppearanceMethods' }).exists()
    ).toBe(false);
  });

  it('hides JudicialBinder tab when showBinder is false', async () => {
    const wrapper: any = mountComponent(false);

    expect(wrapper.find('[data-testid="binder-tab"]').exists()).toBe(false);
  });
});
