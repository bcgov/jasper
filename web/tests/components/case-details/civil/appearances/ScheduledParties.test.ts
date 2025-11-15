import { mount } from '@vue/test-utils';
import ScheduledParties from 'CMP/case-details/civil/appearances/ScheduledParties.vue';
import { FilesService } from '@/services/FilesService';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

vi.mock('@/services/FilesService');

const mockPartyDetails = {
  appearanceId: '123',
  party: [
    {
      fullName: 'John Doe',
      partyId: 'party1',
      lastNm: 'Doe',
      givenNm: 'John',
      courtParticipantId: 'cp1',
      counsel: [
        {
          counselId: 'counsel1',
          counselFullName: 'Attorney Smith',
        },
        {
          counselId: 'counsel2',
          counselFullName: 'Lawyer Jones',
        },
      ],
      representative: [],
      legalRepresentative: [],
      partyRole: [
        {
          roleTypeCd: 'PLT',
          roleTypeDsc: 'Plaintiff',
        },
      ],
    },
    {
      fullName: 'Jane Smith',
      partyId: 'party2',
      lastNm: 'Smith',
      givenNm: 'Jane',
      courtParticipantId: 'cp2',
      counsel: [],
      representative: [],
      legalRepresentative: [],
      partyRole: [
        {
          roleTypeCd: 'DEF',
          roleTypeDsc: 'Defendant',
        },
        {
          roleTypeCd: 'WIT',
          roleTypeDsc: 'Witness',
        },
      ],
    },
  ],
};

describe('ScheduledParties.vue', () => {
  let filesService: any;

  const mountComponent = (fileId = 'file123', appearanceId = 'app456') => {
    return mount(ScheduledParties, {
      props: {
        fileId,
        appearanceId,
      },
      global: {
        provide: {
          filesService,
        },
        stubs: {
          'v-skeleton-loader': {
            template: '<div class="v-skeleton-loader"><slot /></div>',
            props: ['loading', 'type', 'height', 'color', 'class'],
          },
          'v-data-table-virtual': {
            template: `
              <div class="v-data-table">
                <div v-for="item in items" :key="item.partyId" class="table-row">
                  <slot name="item.role" :item="item"></slot>
                  <slot name="item.counsel" :item="item"></slot>
                </div>
              </div>
            `,
            props: ['headers', 'items', 'itemValue'],
          },
          LabelWithTooltip: {
            template: '<div class="label-tooltip">{{ values.join(", ") }}</div>',
            props: ['values', 'location'],
          },
        },
      },
    });
  };

  beforeEach(() => {
    filesService = {
      civilAppearanceParty: vi.fn().mockResolvedValue(mockPartyDetails),
    };
    (FilesService as any).mockReturnValue(filesService);
  });

  it('renders correctly with party data', async () => {
    const wrapper = mountComponent();
    
    await nextTick();

    expect(wrapper.find('.v-data-table').exists()).toBe(true);
    expect(wrapper.vm.parties).toEqual(mockPartyDetails);
  });

  it('calls civilAppearanceParty service with correct parameters', () => {
    mountComponent('testFileId', 'testAppearanceId');

    expect(filesService.civilAppearanceParty).toHaveBeenCalledWith(
      'testFileId',
      'testAppearanceId'
    );
  });

  it('shows loading state initially', () => {
    const wrapper = mountComponent();
    
    expect(wrapper.vm.partiesLoading).toBe(true);
    expect(wrapper.find('.v-skeleton-loader').exists()).toBe(true);
  });

  it('hides loading state after data is loaded', async () => {
    const wrapper = mountComponent();
    
    await nextTick();

    expect(wrapper.vm.partiesLoading).toBe(false);
  });

  it('handles empty party data gracefully', async () => {
    const emptyPartyData = {
      appearanceId: '123',
      party: [],
    };
    filesService.civilAppearanceParty.mockResolvedValue(emptyPartyData);
    
    const wrapper = mountComponent();
    
    await nextTick();

    expect(wrapper.vm.parties).toEqual(emptyPartyData);
    expect(wrapper.find('.v-data-table').exists()).toBe(true);
  });


  it('formats full names correctly using formatFromFullname utility', async () => {
    const wrapper = mountComponent();
    
    await nextTick();

    const headers = wrapper.vm.headers;
    const nameHeader = headers.find(h => h.key === 'fullName');
    expect(nameHeader).toBeDefined();
    expect(typeof nameHeader.value).toBe('function');
    
    const mockItem = { fullName: 'John Doe' };
    expect(nameHeader.value(mockItem)).toBeDefined();
  });

  it('has correct table headers', () => {
    const wrapper = mountComponent();
    
    const headers = wrapper.vm.headers;
    expect(headers).toHaveLength(3);
    expect(headers[0].title).toBe('NAME');
    expect(headers[0].key).toBe('fullName');
    expect(headers[1].title).toBe('ROLE');
    expect(headers[1].key).toBe('role');
    expect(headers[2].title).toBe('CURRENT COUNSEL');
    expect(headers[2].key).toBe('counsel');
  });

  it('handles undefined parties gracefully', async () => {
    filesService.civilAppearanceParty.mockResolvedValue(undefined);
    
    const wrapper = mountComponent();
    
    await nextTick();

    expect(wrapper.vm.parties).toBeUndefined();
    // Component should still render without errors
    expect(wrapper.find('.v-skeleton-loader').exists()).toBe(true);
  });

  it('handles party with undefined counsel gracefully', async () => {
    const partyWithUndefinedCounsel = {
      ...mockPartyDetails,
      party: [
        {
          ...mockPartyDetails.party[0],
          counsel: undefined,
        },
      ],
    };
    filesService.civilAppearanceParty.mockResolvedValue(partyWithUndefinedCounsel);
    
    const wrapper = mountComponent();
    
    await nextTick();

    const tooltips = wrapper.findAllComponents({ name: 'LabelWithTooltip' });
    expect(tooltips.length).toBe(0);
  });

  it('handles party roles with undefined roleTypeDsc gracefully', async () => {
    const partyWithUndefinedRole = {
      ...mockPartyDetails,
      party: [
        {
          ...mockPartyDetails.party[0],
          partyRole: [
            {
              roleTypeCd: 'PLT',
              roleTypeDsc: undefined,
            },
          ],
        },
      ],
    };
    filesService.civilAppearanceParty.mockResolvedValue(partyWithUndefinedRole);
    
    const wrapper = mountComponent();
    
    await nextTick();


    expect(wrapper.vm.parties).toEqual(partyWithUndefinedRole);
  });
});
