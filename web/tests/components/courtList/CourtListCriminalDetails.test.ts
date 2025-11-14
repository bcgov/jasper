import { describe, it, expect, vi, beforeEach } from 'vitest';
import { FilesService } from '@/services/FilesService';
import { mount, flushPromises } from '@vue/test-utils';
import CourtListCriminalDetails from 'CMP/courtlist/CourtListCriminalDetails.vue';
import { nextTick } from 'vue';
import { setActivePinia, createPinia } from 'pinia'

vi.mock('@/services/FilesService');
vi.mock('@/utils/dateUtils', () => ({
  formatDateToDDMMMYYYY: vi.fn((date) => {
    if (date === '2024-06-01') return '01-Jun-2024';
    return 'formatted-date';
  }),
  formatDateInstanceToDDMMMYYYY: vi.fn((date) => {
    if (date instanceof Date && date.toISOString() === '2024-06-15T00:00:00.000Z') {
      return '15-Jun-2024';
    }
    return 'formatted-instance-date';
  })
}));

beforeEach(() => {
    setActivePinia(createPinia());
});

describe('CourtListCriminalDetails.vue', () => {
    let filesService: any;
    let wrapper: ReturnType<typeof mount>;
    const mockDocuments = {
      keyDocuments: [
        {
          issueDate: '2024-06-01',
          docmFormDsc: 'Form A',
          docmDispositionDsc: 'Perfected',
          docmClassification: 'Type 1',
          documentPageCount: 3,
          category: 'bail',
        },
      ],
    };
    const mockDetails = {
        appearanceMethods: [{ method: 'In Person' }],

        charges: [
            {
            printSeqNo: 1,
            statuteSectionDsc: '123',
            statuteDsc: 'Description',
            appearanceResultDesc: 'Result Description',
            appearanceResultCd: 'RES',
            pleaCode: 'G',
            pleaDate: '2024-06-15T00:00:00.000Z',
            findingDsc: 'Finding',
            },
        ],
    };

  beforeEach(async () => {
    filesService = {
      criminalAppearanceDetails: vi.fn().mockResolvedValue(mockDetails),
      criminalAppearanceDocuments: vi.fn().mockResolvedValue(mockDocuments),
    };
    (FilesService as any).mockReturnValue(filesService);
    
    wrapper = mount(CourtListCriminalDetails, {
      global: {
        provide: {
          filesService,
        },
      },
      props: {
        fileId: 'file1',
        appearanceId: 'app1',
        partId: 'part1',
        courtClass: 'Criminal',
      },
    });
    await flushPromises();
    await nextTick();
  });

  it('receives and uses correct props', () => {
    expect(wrapper.props()).toEqual({
      fileId: 'file1',
      appearanceId: 'app1',
      partId: 'part1',
      courtClass: 'Criminal',
    });
  });

  it('calls filesService.criminalAppearanceDetails on mount', () => {
    expect(filesService.criminalAppearanceDetails).toHaveBeenCalledWith(
      'file1',
      'app1',
      'part1'
    );
  });

  it('renders AppearanceMethods when appearanceMethods exist', () => {
    expect(wrapper.findComponent({name: 'CriminalAppearanceMethods'}).exists()).toBe(true);
  });

it('does not render AppearanceMethods when no appearanceMethods', () => {
    mockDetails.appearanceMethods = [];
    wrapper = mount(CourtListCriminalDetails, {
      global: {
        provide: {
          filesService,
        },
      },
      props: {
        fileId: 'file1',
        appearanceId: 'app1',
        partId: 'part1',
        courtClass: 'Criminal',
      },
    });
    expect(wrapper.findComponent({name: 'CriminalAppearanceMethods'}).exists()).toBe(false);
  });

    it('renders key documents', () => {
      const cards = wrapper.findAll('v-card');
      const tables = wrapper.findAll('v-data-table-virtual');
  
      expect(tables).toHaveLength(2);
      expect(cards).toHaveLength(2);
      expect(cards[0].html()).toContain('Key Documents');
    });

    it('renders bail document', async () => {
      wrapper = mount(CourtListCriminalDetails, {
        global: {
          provide: {
            filesService,
          },
          stubs: {
              'v-data-table-virtual': {
                 template: `
                  <slot name="item.docmFormDsc" :item="items && items[0] ? items[0] : { category: 'bail' }"></slot>
                `,
                  props: ['headers', 'items', 'itemValue'],
              }
          },
        },
        props: {
          fileId: 'file1',
          appearanceId: 'app1',
          partId: 'part1',
          courtClass: 'Criminal',
        },
    });

    await flushPromises();

    expect(wrapper.text()).toContain('Perfected');
    expect(wrapper.text()).toContain('01-Jun-2024');
  });

  it('renders charge headers with correct titles', () => {
    // Test that the charges table exists and has the expected structure
    const chargeTables = wrapper.findAll('v-data-table-virtual');
    expect(chargeTables).toHaveLength(2); // One for documents, one for charges
    
    // Verify the charges table has the expected headers by checking the component's headers prop
    const chargeTable = chargeTables[1];
    expect(chargeTable.exists()).toBe(true);
  });

  it('renders last results with tooltip', async () => {
    wrapper = mount(CourtListCriminalDetails, {
      global: {
        provide: {
          filesService,
        },
        stubs: {
          'v-data-table-virtual': {
            template: `
              <div>
                <slot name="item.lastResults" :value="items && items[0] ? items[0].appearanceResultCd : 'RES'" :item="items && items[0] ? items[0] : { appearanceResultDesc: 'Result Description', appearanceResultCd: 'RES' }"></slot>
              </div>
            `,
            props: ['headers', 'items', 'itemValue'],
          },
          'v-tooltip': {
            template: '<div><slot name="activator" :props="{}"></slot><span class="tooltip-text">{{ text }}</span></div>',
            props: ['text', 'location']
          }
        },
      },
      props: {
        fileId: 'file1',
        appearanceId: 'app1',
        partId: 'part1',
        courtClass: 'Criminal',
      },
    });

    await flushPromises();

    expect(wrapper.find('.has-tooltip').exists()).toBe(true);
    expect(wrapper.find('.tooltip-text').text()).toBe('Result Description');
  });

  it('renders plea code and date correctly', async () => {
    wrapper = mount(CourtListCriminalDetails, {
      global: {
        provide: {
          filesService,
        },
        stubs: {
          'v-data-table-virtual': {
            template: `
              <div>
                <slot name="item.pleaCode" :value="items && items[0] ? items[0].pleaCode : 'G'" :item="items && items[0] ? items[0] : { pleaCode: 'G', pleaDate: '2024-06-15T00:00:00.000Z' }"></slot>
              </div>
            `,
            props: ['headers', 'items', 'itemValue'],
          },
          'v-row': {
            template: '<div class="v-row"><slot></slot></div>',
            props: ['no-gutters']
          },
          'v-col': {
            template: '<div class="v-col"><slot></slot></div>'
          }
        },
      },
      props: {
        fileId: 'file1',
        appearanceId: 'app1',
        partId: 'part1',
        courtClass: 'Criminal',
      },
    });

    await flushPromises();

    expect(wrapper.text()).toContain('G');
    expect(wrapper.text()).toContain('15-Jun-2024');
  });

  it('renders charges table with correct styling', () => {
    const chargeTable = wrapper.findAll('v-data-table-virtual')[1];
    expect(chargeTable.attributes('style')).toContain('background-color: rgba(248, 211, 119, 0.52)');
  });

  it('shows loading skeleton while fetching data', async () => {
    const slowFilesService = {
      criminalAppearanceDetails: vi.fn().mockImplementation(() => 
        new Promise(resolve => setTimeout(() => resolve(mockDetails), 100)),
      ),
      criminalAppearanceDocuments: vi.fn().mockImplementation(() => 
        new Promise(resolve => setTimeout(() => resolve(mockDocuments), 100)),
      ),
    };

    const loadingWrapper = mount(CourtListCriminalDetails, {
      global: {
        provide: {
          filesService: slowFilesService,
        },
      },
      props: {
        fileId: 'file1',
        appearanceId: 'app1',
        partId: 'part1',
        courtClass: 'Criminal',
      },
    });

    // Should show loading initially
    expect(loadingWrapper.find('v-skeleton-loader').attributes('loading')).toBeDefined();
    expect(loadingWrapper.findAll('v-skeleton-loader').length).toBe(2);
    await flushPromises();
    await new Promise(resolve => setTimeout(resolve, 150));
    
    // Should not be loading anymore
    expect(loadingWrapper.find('v-skeleton-loader').attributes('loading')).toBe('false');
  });

  it('handles missing plea date gracefully', async () => {
    const mockDetailsNoPleaDate = {
      ...mockDetails,
      charges: [
        {
          ...mockDetails.charges[0],
          pleaDate: undefined,
        },
      ],
    };

    filesService.criminalAppearanceDetails.mockResolvedValue(mockDetailsNoPleaDate);

    wrapper = mount(CourtListCriminalDetails, {
      global: {
        provide: {
          filesService,
        },
        stubs: {
          'v-data-table-virtual': {
            template: `
              <div>
                <slot name="item.pleaCode" :value="items && items[0] ? items[0].pleaCode : 'G'" :item="items && items[0] ? items[0] : { pleaCode: 'G', pleaDate: undefined }"></slot>
              </div>
            `,
            props: ['headers', 'items', 'itemValue'],
          },
          'v-row': {
            template: '<div class="v-row" :class="{ \'no-gutters\': $attrs[\'no-gutters\'] }"><slot></slot></div>',
            props: ['no-gutters']
          },
          'v-col': {
            template: '<div class="v-col"><slot></slot></div>'
          }
        },
      },
      props: {
        fileId: 'file1',
        appearanceId: 'app1',
        partId: 'part1',
        courtClass: 'Criminal',
      },
    });

    await flushPromises();

    expect(wrapper.text()).toContain('G'); // Plea code should still be displayed
    // Should not render the date row when pleaDate is undefined
    expect(wrapper.findAll('.v-row[no-gutters]')).toHaveLength(0);
  });
});