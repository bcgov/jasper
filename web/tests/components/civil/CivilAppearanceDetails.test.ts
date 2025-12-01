import { FilesService } from '@/services/FilesService';
import { flushPromises, mount } from '@vue/test-utils';
import CivilAppearanceDetails from 'CMP/civil/CivilAppearanceDetails.vue';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/services/FilesService');

describe('CivilAppearanceDetails.vue', () => {
  let filesService: any;

  const mockDocumentDetails = {
    agencyId: 'AGENCY123',
    fileNumberTxt: 'FILE123',
    courtLevelCd: 'P',
    document: [{ civilDocumentId: '1', name: 'Document 1' }],
  };

  const mockMethods = {
    appearanceMethod: [
      { roleTypeDesc: 'Plaintiff', appearanceMethodDesc: 'In Person' },
    ],
  };

  const mockBinderDocuments = [
    { civilDocumentId: '1', name: 'Document 1' },
    { civilDocumentId: '2', name: 'Document 2' },
  ];

  const mountComponent = (showBinder = true, courtClassCd = 'C') => {
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
        courtClassCd,
      },
    });
  };

  beforeEach(() => {
    setActivePinia(createPinia());
    filesService = {
      civilAppearanceDocuments: vi.fn().mockResolvedValue(mockDocumentDetails),
      civilAppearanceMethods: vi.fn().mockResolvedValue(mockMethods),
      civilBinderDocuments: vi.fn().mockResolvedValue(mockBinderDocuments),
      civilAppearanceParty: vi.fn().mockResolvedValue({}),
    };
    (FilesService as any).mockReturnValue(filesService);
  });

  it('renders the tabs correctly', async () => {
    const wrapper = mountComponent();
    await flushPromises();

    const tabs = wrapper.findAll('v-tab');
    expect(tabs.length).toBe(4);
    expect(tabs[0].text()).toContain('Scheduled Documents');
    expect(tabs[1].text()).toContain('Judicial Binder');
    expect(tabs[2].text()).toContain('Scheduled Parties');
    expect(tabs[3].text()).toContain('Appearance Methods');
  });

  it('calls appearance documents, methods, and binder documents apis with correct parameters', async () => {
    mountComponent();
    await flushPromises();
    expect(filesService.civilAppearanceDocuments).toHaveBeenCalledWith(
      '123',
      '456'
    );
    expect(filesService.civilAppearanceMethods).toHaveBeenCalledWith(
      '123',
      '456'
    );
    expect(filesService.civilBinderDocuments).toHaveBeenCalledWith('123');
  });

  it('calls binder service with correct parameters when showBinder is true', async () => {
    mountComponent(true, 'C');
    await flushPromises();
    expect(filesService.civilBinderDocuments).toHaveBeenCalled();
  });

  it('renders ScheduledDocuments component when "documents" tab is active', async () => {
    const wrapper: any = mountComponent();
    wrapper.vm.tab = 'documents';
    expect(wrapper.findComponent({ name: 'ScheduledDocuments' }).exists()).toBe(
      true
    );
  });

  it('renders ScheduledParties component when "parties" tab is active', async () => {
    const wrapper: any = mountComponent();
    wrapper.vm.tab = 'parties';
    expect(wrapper.findComponent({ name: 'ScheduledParties' }).exists()).toBe(
      true
    );
    expect(
      wrapper.findComponent({ name: 'CivilAppearanceMethods' }).exists()
    ).toBe(false);
  });

  it('passes correct props to ScheduledParties component', async () => {
    const wrapper: any = mountComponent();
    wrapper.vm.tab = 'parties';
    const scheduledPartiesComponent = wrapper.findComponent({
      name: 'ScheduledParties',
    });
    expect(scheduledPartiesComponent.props('fileId')).toBe('123');
    expect(scheduledPartiesComponent.props('appearanceId')).toBe('456');
  });

  it('renders JudicialBinder component when "binder" tab active and showBinder is true', async () => {
    const wrapper: any = mountComponent();
    await flushPromises();
    wrapper.vm.tab = 'binder';
    expect(wrapper.findComponent({ name: 'JudicialBinder' }).exists()).toBe(
      true
    );
    expect(wrapper.find('[data-testid="binder-tab"]').exists()).toBe(true);
    expect(
      wrapper.findComponent({ name: 'CivilAppearanceMethods' }).exists()
    ).toBe(true);
  });

  it('hides JudicialBinder tab when showBinder is false', async () => {
    const wrapper: any = mountComponent(false);
    expect(wrapper.find('[data-testid="binder-tab"]').exists()).toBe(false);
  });

  it('disables binder tab when binder is loading or has no documents', async () => {
    filesService.civilBinderDocuments = vi.fn().mockResolvedValue([]);

    const wrapper: any = mountComponent();
    await flushPromises();

    const binderTab = wrapper.find('[data-testid="binder-tab"]');
    expect(binderTab.attributes('disabled')).toBeDefined();
  });

  it('enables binder tab when binder documents exist', async () => {
    const wrapper: any = mountComponent();
    await flushPromises();

    const binderTab = wrapper.find('[data-testid="binder-tab"]');

    expect(binderTab.attributes('disabled')).not.toBe('true');
  });

  it('shows methods tab when appearance methods exist', async () => {
    const wrapper: any = mountComponent();
    await new Promise((resolve) => setTimeout(resolve, 0)); // Wait for async operations
    const tabs = wrapper.findAll('v-tab');
    expect(
      tabs.some((tab: any) => tab.text().includes('Appearance Methods'))
    ).toBe(true);
  });

  it('handles error in binder loading gracefully', async () => {
    filesService.civilBinderDocuments.mockRejectedValue(
      new Error('Binder error')
    );
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

    mountComponent();
    await flushPromises();

    expect(consoleSpy).toHaveBeenCalledWith(
      expect.stringContaining("Error occured while retrieving user's binders")
    );
    consoleSpy.mockRestore();
  });

  it('handles error in methods loading gracefully', async () => {
    filesService.civilAppearanceMethods.mockRejectedValue(
      new Error('Methods error')
    );
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

    mountComponent();
    await flushPromises();

    expect(consoleSpy).toHaveBeenCalledWith(
      'Error occurred while retrieving appearance methods:',
      expect.any(Error)
    );
    consoleSpy.mockRestore();
  });
});
