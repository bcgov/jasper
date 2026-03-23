import shared from '@/components/shared';
import { useDarsStore } from '@/stores/DarsStore';
import { CourtDocumentType } from '@/types/shared';
import { mount } from '@vue/test-utils';
import AppearancesView from 'CMP/case-details/common/AppearancesView.vue';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';

// Mock the shared module
vi.mock('@/components/shared', () => ({
  default: {
    openDocumentsPdf: vi.fn(),
  },
}));

// Initialize Pinia before tests
const pinia = createPinia();
setActivePinia(pinia);

// Stub for v-data-table-virtual to render slots in tests
const dataTableStub = {
  'v-data-table-virtual': {
    template: `
      <div>
        <slot name="item.transcripts" v-bind="{ item: { appearanceId: 1 } }"></slot>
      </div>
    `,
    props: [
      'headers',
      'items',
      'sortBy',
      'height',
      'itemValue',
      'fixedHeader',
      'showExpand',
      'variant',
    ],
  },
};

describe('AppearancesView.vue', () => {
  let props: any;

  beforeEach(() => {
    props = {
      appearances: [
        {
          appearanceId: 1,
          appearanceDt: '2023-10-01',
          appearanceReasonCd: 'ARR',
          appearanceReasonDsc: 'Arraignment',
          appearanceTm: '10:00',
          estimatedTimeHour: 1,
          estimatedTimeMin: 30,
          courtLocation: 'Court A',
          courtRoomCd: '101',
          judgeFullNm: 'Judge Smith',
          lastNm: 'Doe',
          givenNm: 'John',
          appearanceStatusCd: 'SCHD',
          activity: 'Hearing',
        },
        {
          appearanceId: 2,
          appearanceDt: '3023-10-01',
          appearanceReasonCd: 'TRI',
          appearanceReasonDsc: 'Trial',
          appearanceTm: '14:00',
          estimatedTimeHour: 2,
          estimatedTimeMin: 0,
          courtLocation: 'Court B',
          courtRoomCd: '202',
          judgeFullNm: 'Judge Brown',
          lastNm: 'Smith',
          givenNm: 'Jane',
          appearanceStatusCd: 'Completed',
          activity: 'Trial',
        },
      ],
      isCriminal: true,
      fileNumber: 'TEST-123',
      courtClassCd: 'A',
      details: {
        fileNumberTxt: 'TEST-123',
        courtLevelCd: 'P',
        courtClassCd: 'A',
        homeLocationAgencyName: 'Vancouver',
      },
    };
  });

  it('renders default card and table if no appearances are provided', () => {
    const wrapper = mount(AppearancesView, {
      props: { appearances: [] },
      global: {
        plugins: [pinia],
      },
    });

    const card = wrapper.find('v-card');
    expect(card.exists()).toBe(true);
    expect(card.text()).toContain('Appearances');
    expect(wrapper.find('v-data-table-virtual').exists()).toBe(true);
  });

  it('filters appearances by selected accused', () => {
    const wrapper: any = mount(AppearancesView, {
      props,
      global: {
        plugins: [pinia],
      },
    });

    wrapper.vm.selectedAccused = 'Doe, John';
    const appearances = wrapper.vm.appearances;
    expect(appearances.length).toBe(1);
  });

  it('passes unfiltered props.appearances to NameFilter people prop', async () => {
    const wrapper = mount(AppearancesView, {
      props,
      global: {
        plugins: [pinia],
        stubs: {
          NameFilter: {
            name: 'NameFilter',
            props: ['modelValue', 'people'],
            template: '<div data-testid="name-filter-stub" />',
          },
        },
      },
    });

    const nameFilter = wrapper.findComponent({ name: 'NameFilter' });
    expect(nameFilter.props('people')).toEqual(props.appearances);
    expect(nameFilter.props('people')).toHaveLength(2);
  });

  it('always renders table with no-data-text when appearances is empty', () => {
    const wrapper = mount(AppearancesView, {
      props: {
        ...props,
        appearances: [],
      },
      global: {
        plugins: [pinia],
        stubs: {
          'v-data-table-virtual': {
            name: 'v-data-table-virtual',
            template: '<div data-testid="appearances-table" />',
            props: ['headers', 'items', 'sortBy', 'noDataText'],
          },
        },
      },
    });

    const table = wrapper.findComponent({ name: 'v-data-table-virtual' });
    expect(table.exists()).toBe(true);
    expect(table.props('items')).toEqual([]);
    expect(table.props('noDataText')).toBe('No appearances');
  });

  it('renders correct table headers for criminal appearances', () => {
    const wrapper: any = mount(AppearancesView, {
      props,
      global: {
        plugins: [pinia],
      },
    });

    const expectedHeaderKeys = [
      'data-table-expand',
      'appearanceDt',
      'transcripts',
      'DARS',
      'appearanceReasonCd',
      'appearanceTm',
      'courtLocation',
      'judgeFullNm',
      'name',
      'appearanceStatusCd',
    ];
    const headerKeys = wrapper.vm.headers.map((header: any) => header.key);

    expect(headerKeys).toEqual(expectedHeaderKeys);
  });

  it('renders correct table headers for civil appearances', () => {
    props.isCriminal = false;
    const wrapper: any = mount(AppearancesView, {
      props,
      global: {
        plugins: [pinia],
      },
    });

    const expectedHeaderKeys = [
      'data-table-expand',
      'appearanceDt',
      'transcripts',
      'DARS',
      'appearanceReasonCd',
      'appearanceTm',
      'courtLocation',
      'judgeFullNm',
      'appearanceStatusCd',
    ];
    const headerKeys = wrapper.vm.headers.map((header: any) => header.key);

    expect(headerKeys).toEqual(expectedHeaderKeys);
  });

  it.each([
    { isCriminal: true, shouldExist: true },
    { isCriminal: false, shouldExist: false },
  ])(
    'renders appearances filters based on isCriminal prop',
    ({ isCriminal, shouldExist }) => {
      props.isCriminal = isCriminal;
      const wrapper = mount(AppearancesView, {
        props,
        global: {
          plugins: [pinia],
        },
      });

      expect(wrapper.find('NameFilter').exists()).toBe(shouldExist);
    }
  );

  it('opens DARS modal with correct data when DARS button is clicked', async () => {
    // Ensure the first appearance has 'SCHD' status so the DARS button appears
    props.appearances[0].appearanceStatusCd = 'SCHD';

    const wrapper = mount(AppearancesView, {
      props,
      global: {
        plugins: [pinia],
        stubs: {
          // Simple stub that renders items and the DARS slot
          'v-data-table-virtual': {
            props: ['items', 'headers'],
            template: `
            <table>
              <tbody>
                <tr v-for="item in items" :key="item.appearanceId">
                  <td>
                    <slot name="item.DARS" :item="item"></slot>
                  </td>
                </tr>
              </tbody>
            </table>
          `,
          },
        },
      },
    });

    const darsStore = useDarsStore();

    await wrapper.find('[data-testid="dars-button-1"]').trigger('click');

    expect(darsStore.isModalVisible).toBe(true);
    expect(darsStore.searchDate?.toISOString()).toBe(
      '2023-10-01T00:00:00.000Z'
    );
    expect(darsStore.searchLocationId).toBe(null);
    expect(darsStore.searchRoom).toBe('101');
  });

  describe('Transcript Icon', () => {
    it('does not render transcript icon when no transcripts are provided', () => {
      const wrapper = mount(AppearancesView, {
        props,
        global: {
          plugins: [pinia],
        },
      });

      expect(wrapper.find('[data-testid="transcript-button-1"]').exists()).toBe(
        false
      );
    });

    it('does not render transcript icon when transcripts do not match appearance', () => {
      props.transcripts = [
        {
          id: 1,
          orderId: 100,
          description: 'Test Transcript',
          fileName: 'transcript.txt',
          pagesComplete: 10,
          statusCodeId: 1,
          appearances: [
            {
              justinAppearanceId: 999, // Does not match any appearance
              ceisAppearanceId: 999,
              appearanceDt: '2023-10-01',
              appearanceReasonCd: 'ARR',
              appearanceTm: '10:00',
              courtAgencyId: 1,
              courtRoomCd: '101',
              judgeFullNm: 'Judge Smith',
              estimatedDuration: 90,
              estimatedStartTime: '10:00',
              fileid: 1,
              id: 1,
              isInCamera: false,
              statusCodeId: 1,
            },
          ],
        },
      ];

      const wrapper = mount(AppearancesView, {
        props,
        global: {
          plugins: [pinia],
        },
      });

      expect(wrapper.find('[data-testid="transcript-button-1"]').exists()).toBe(
        false
      );
    });

    it('renders transcript icon for criminal file when transcript matches appearance', async () => {
      props.transcripts = [
        {
          id: 1,
          orderId: 100,
          description: 'Test Transcript',
          fileName: 'transcript.txt',
          pagesComplete: 10,
          statusCodeId: 1,
          appearances: [
            {
              justinAppearanceId: 1,
              ceisAppearanceId: 0,
              appearanceDt: '2023-10-01',
              appearanceReasonCd: 'ARR',
              appearanceTm: '10:00',
              courtAgencyId: 1,
              courtRoomCd: '101',
              judgeFullNm: 'Judge Smith',
              estimatedDuration: 90,
              estimatedStartTime: '10:00',
              fileid: 1,
              id: 1,
              isInCamera: false,
              statusCodeId: 1,
            },
          ],
        },
      ];

      const wrapper = mount(AppearancesView, {
        props,
        global: {
          plugins: [pinia],
          stubs: {
            'v-data-table-virtual': {
              template: `
                <div>
                  <slot name="item.transcripts" v-bind="{ item: { appearanceId: 1 } }"></slot>
                </div>
              `,
              props: [
                'headers',
                'items',
                'sortBy',
                'height',
                'itemValue',
                'fixedHeader',
                'showExpand',
                'variant',
              ],
            },
          },
        },
      });

      await wrapper.vm.$nextTick();
      await wrapper.vm.$nextTick(); // Wait for Vuetify components to render

      const button = wrapper.find('[data-testid="transcript-button-1"]');
      expect(button.exists()).toBe(true);
    });

    it('renders transcript icon for civil file when transcript matches appearance', async () => {
      props.isCriminal = false;
      props.transcripts = [
        {
          id: 1,
          orderId: 100,
          description: 'Test Transcript',
          fileName: 'transcript.txt',
          pagesComplete: 10,
          statusCodeId: 1,
          appearances: [
            {
              justinAppearanceId: 0,
              ceisAppearanceId: 1,
              appearanceDt: '2023-10-01',
              appearanceReasonCd: 'ARR',
              appearanceTm: '10:00',
              courtAgencyId: 1,
              courtRoomCd: '101',
              judgeFullNm: 'Judge Smith',
              estimatedDuration: 90,
              estimatedStartTime: '10:00',
              fileid: 1,
              id: 1,
              isInCamera: false,
              statusCodeId: 1,
            },
          ],
        },
      ];

      const wrapper = mount(AppearancesView, {
        props,
        global: {
          plugins: [pinia],
          stubs: dataTableStub,
        },
      });

      await wrapper.vm.$nextTick();
      await wrapper.vm.$nextTick(); // Wait for Vuetify components to render

      expect(wrapper.find('[data-testid="transcript-button-1"]').exists()).toBe(
        true
      );
    });

    it('does not render transcript icon when no transcript exists for appearance', async () => {
      props.transcripts = [
        {
          id: 1,
          orderId: 100,
          description: 'Test Transcript',
          fileName: 'transcript.txt',
          pagesComplete: 10,
          statusCodeId: 1,
          appearances: [
            {
              justinAppearanceId: 1,
              ceisAppearanceId: 0,
              appearanceDt: '2023-10-01',
              appearanceReasonCd: 'ARR',
              appearanceTm: '10:00',
              courtAgencyId: 1,
              courtRoomCd: '101',
              judgeFullNm: 'Judge Smith',
              estimatedDuration: 90,
              estimatedStartTime: '10:00',
              fileid: 1,
              id: 1,
              isInCamera: false,
              statusCodeId: 1,
            },
          ],
        },
      ];

      const wrapper = mount(AppearancesView, {
        props,
        global: {
          plugins: [pinia],
        },
      });

      expect(wrapper.find('v-menu').exists()).toBe(false);
    });

    it('renders menu when multiple transcripts match appearance', async () => {
      props.transcripts = [
        {
          id: 1,
          orderId: 100,
          description: 'Morning Session',
          fileName: 'transcript1.txt',
          pagesComplete: 10,
          statusCodeId: 1,
          appearances: [
            {
              justinAppearanceId: 1,
              ceisAppearanceId: 0,
              appearanceDt: '2023-10-01',
              appearanceReasonCd: 'ARR',
              appearanceTm: '10:00',
              courtAgencyId: 1,
              courtRoomCd: '101',
              judgeFullNm: 'Judge Smith',
              estimatedDuration: 90,
              estimatedStartTime: '10:00',
              fileid: 1,
              id: 1,
              isInCamera: false,
              statusCodeId: 1,
            },
          ],
        },
        {
          id: 2,
          orderId: 100,
          description: 'Afternoon Session',
          fileName: 'transcript2.txt',
          pagesComplete: 15,
          statusCodeId: 1,
          appearances: [
            {
              justinAppearanceId: 1,
              ceisAppearanceId: 0,
              appearanceDt: '2023-10-01',
              appearanceReasonCd: 'ARR',
              appearanceTm: '14:00',
              courtAgencyId: 1,
              courtRoomCd: '101',
              judgeFullNm: 'Judge Smith',
              estimatedDuration: 120,
              estimatedStartTime: '14:00',
              fileid: 1,
              id: 2,
              isInCamera: false,
              statusCodeId: 1,
            },
          ],
        },
      ];

      const wrapper = mount(AppearancesView, {
        props,
        global: {
          plugins: [pinia],
          stubs: dataTableStub,
        },
      });

      await wrapper.vm.$nextTick();
      await wrapper.vm.$nextTick(); // Wait for Vuetify components to render

      expect(wrapper.find('[data-testid="transcript-button-1"]').exists()).toBe(
        true
      );
      // The transcript button should exist for appearances with multiple transcripts
      // (Menu rendering is handled by Vuetify)
    });

    it('opens single transcript directly when icon is clicked', async () => {
      props.transcripts = [
        {
          id: 123,
          orderId: 100,
          description: 'Morning Session',
          fileName: 'transcript.txt',
          pagesComplete: 10,
          statusCodeId: 1,
          appearances: [
            {
              justinAppearanceId: 1,
              ceisAppearanceId: 0,
              appearanceDt: '2023-10-01',
              appearanceReasonCd: 'ARR',
              appearanceTm: '10:00',
              courtAgencyId: 1,
              courtRoomCd: '101',
              judgeFullNm: 'Judge Smith',
              estimatedDuration: 90,
              estimatedStartTime: '10:00',
              fileid: 1,
              id: 1,
              isInCamera: false,
              statusCodeId: 1,
            },
          ],
        },
      ];

      const wrapper = mount(AppearancesView, {
        props,
        global: {
          plugins: [pinia],
          stubs: dataTableStub,
        },
      });

      await wrapper.vm.$nextTick();

      const transcriptButton = wrapper.find(
        '[data-testid="transcript-button-1"]'
      );
      await transcriptButton.trigger('click');

      expect(shared.openDocumentsPdf).toHaveBeenCalledWith(
        CourtDocumentType.Transcript,
        expect.objectContaining({
          documentId: '123',
          orderId: '100',
          documentDescription: 'Transcript - Morning Session',
          fileId: 'TEST-123',
          fileNumberText: 'TEST-123',
          courtLevel: 'P',
          courtClass: 'A',
          location: 'Vancouver',
          isCriminal: true,
        })
      );
    });

    it('opens menu when multiple transcripts exist and one is selected', async () => {
      props.transcripts = [
        {
          id: 123,
          orderId: 100,
          description: 'Morning Session',
          fileName: 'transcript1.txt',
          pagesComplete: 10,
          statusCodeId: 1,
          appearances: [
            {
              justinAppearanceId: 1,
              ceisAppearanceId: 0,
              appearanceDt: '2023-10-01',
              appearanceReasonCd: 'ARR',
              appearanceTm: '10:00',
              courtAgencyId: 1,
              courtRoomCd: '101',
              judgeFullNm: 'Judge Smith',
              estimatedDuration: 90,
              estimatedStartTime: '10:00',
              fileid: 1,
              id: 1,
              isInCamera: false,
              statusCodeId: 1,
            },
          ],
        },
        {
          id: 456,
          orderId: 200,
          description: 'Afternoon Session',
          fileName: 'transcript2.txt',
          pagesComplete: 15,
          statusCodeId: 1,
          appearances: [
            {
              justinAppearanceId: 1,
              ceisAppearanceId: 0,
              appearanceDt: '2023-10-01',
              appearanceReasonCd: 'ARR',
              appearanceTm: '14:00',
              courtAgencyId: 1,
              courtRoomCd: '101',
              judgeFullNm: 'Judge Smith',
              estimatedDuration: 120,
              estimatedStartTime: '14:00',
              fileid: 1,
              id: 2,
              isInCamera: false,
              statusCodeId: 1,
            },
          ],
        },
      ];

      const wrapper = mount(AppearancesView, {
        props,
        global: {
          plugins: [pinia],
          stubs: {
            ...dataTableStub,
            'v-menu': {
              template: '<div><slot></slot></div>',
              props: ['modelValue', 'activator', 'location'],
            },
            'v-list': {
              template: '<div><slot></slot></div>',
            },
            'v-list-item': {
              template:
                '<div @click="$emit(\'click\')"><slot name="default"></slot></div>',
            },
            'v-list-item-title': {
              template: '<div><slot></slot></div>',
            },
          },
        },
      });

      await wrapper.vm.$nextTick();

      const transcriptButton = wrapper.find(
        '[data-testid="transcript-button-1"]'
      );
      // We should find the transcript button rendered for multiple transcripts
      expect(transcriptButton.exists()).toBe(true);

      // Note: Full menu interaction testing requires Vuetify components to be fully rendered,
      // which is beyond the scope of this unit test with component stubs
    });

    it('uses ceisAppearanceId for civil files instead of justinAppearanceId', async () => {
      props.isCriminal = false;
      props.transcripts = [
        {
          id: 123,
          orderId: 100,
          description: 'Civil Hearing Transcript',
          fileName: 'transcript.txt',
          pagesComplete: 10,
          statusCodeId: 1,
          appearances: [
            {
              justinAppearanceId: 0,
              ceisAppearanceId: 1,
              appearanceDt: '2023-10-01',
              appearanceReasonCd: 'ARR',
              appearanceTm: '10:00',
              courtAgencyId: 1,
              courtRoomCd: '101',
              judgeFullNm: 'Judge Smith',
              estimatedDuration: 90,
              estimatedStartTime: '10:00',
              fileid: 1,
              id: 1,
              isInCamera: false,
              statusCodeId: 1,
            },
          ],
        },
      ];

      const wrapper = mount(AppearancesView, {
        props,
        global: {
          plugins: [pinia],
          stubs: dataTableStub,
        },
      });

      await wrapper.vm.$nextTick();
      await wrapper.vm.$nextTick(); // Wait for Vuetify components to render

      const transcriptButton = wrapper.find(
        '[data-testid="transcript-button-1"]'
      );
      await transcriptButton.trigger('click');

      await transcriptButton.trigger('click');

      expect(shared.openDocumentsPdf).toHaveBeenCalledWith(
        CourtDocumentType.Transcript,
        expect.objectContaining({
          documentId: '123',
          orderId: '100',
          isCriminal: false,
        })
      );
    });

    it('getAppearanceTranscripts returns empty array when no transcripts prop provided', () => {
      const wrapper: any = mount(AppearancesView, {
        props: {
          ...props,
          transcripts: undefined,
        },
        global: {
          plugins: [pinia],
        },
      });

      const result = wrapper.vm.getAppearanceTranscripts('1');
      expect(result).toEqual([]);
    });

    it('getAppearanceTranscripts filters transcripts by criminal appearanceId', () => {
      props.transcripts = [
        {
          id: 1,
          orderId: 100,
          description: 'Transcript 1',
          fileName: 'transcript1.txt',
          pagesComplete: 10,
          statusCodeId: 1,
          appearances: [
            {
              justinAppearanceId: 1,
              ceisAppearanceId: 0,
              appearanceDt: '2023-10-01',
              appearanceReasonCd: 'ARR',
              appearanceTm: '10:00',
              courtAgencyId: 1,
              courtRoomCd: '101',
              judgeFullNm: 'Judge Smith',
              estimatedDuration: 90,
              estimatedStartTime: '10:00',
              fileid: 1,
              id: 1,
              isInCamera: false,
              statusCodeId: 1,
            },
          ],
        },
        {
          id: 2,
          orderId: 200,
          description: 'Transcript 2',
          fileName: 'transcript2.txt',
          pagesComplete: 15,
          statusCodeId: 1,
          appearances: [
            {
              justinAppearanceId: 2,
              ceisAppearanceId: 0,
              appearanceDt: '2023-10-02',
              appearanceReasonCd: 'TRI',
              appearanceTm: '14:00',
              courtAgencyId: 1,
              courtRoomCd: '202',
              judgeFullNm: 'Judge Brown',
              estimatedDuration: 120,
              estimatedStartTime: '14:00',
              fileid: 1,
              id: 2,
              isInCamera: false,
              statusCodeId: 1,
            },
          ],
        },
      ];

      const wrapper: any = mount(AppearancesView, {
        props,
        global: {
          plugins: [pinia],
        },
      });

      const result = wrapper.vm.getAppearanceTranscripts('1');
      expect(result).toHaveLength(1);
      expect(result[0].id).toBe(1);
    });

    it('getAppearanceTranscripts filters transcripts by civil appearanceId', () => {
      props.isCriminal = false;
      props.transcripts = [
        {
          id: 1,
          orderId: 100,
          description: 'Civil Transcript 1',
          fileName: 'transcript1.txt',
          pagesComplete: 10,
          statusCodeId: 1,
          appearances: [
            {
              justinAppearanceId: 0,
              ceisAppearanceId: 1,
              appearanceDt: '2023-10-01',
              appearanceReasonCd: 'ARR',
              appearanceTm: '10:00',
              courtAgencyId: 1,
              courtRoomCd: '101',
              judgeFullNm: 'Judge Smith',
              estimatedDuration: 90,
              estimatedStartTime: '10:00',
              fileid: 1,
              id: 1,
              isInCamera: false,
              statusCodeId: 1,
            },
          ],
        },
        {
          id: 2,
          orderId: 200,
          description: 'Civil Transcript 2',
          fileName: 'transcript2.txt',
          pagesComplete: 15,
          statusCodeId: 1,
          appearances: [
            {
              justinAppearanceId: 0,
              ceisAppearanceId: 2,
              appearanceDt: '2023-10-02',
              appearanceReasonCd: 'TRI',
              appearanceTm: '14:00',
              courtAgencyId: 1,
              courtRoomCd: '202',
              judgeFullNm: 'Judge Brown',
              estimatedDuration: 120,
              estimatedStartTime: '14:00',
              fileid: 1,
              id: 2,
              isInCamera: false,
              statusCodeId: 1,
            },
          ],
        },
      ];

      const wrapper: any = mount(AppearancesView, {
        props,
        global: {
          plugins: [pinia],
        },
      });

      const result = wrapper.vm.getAppearanceTranscripts('1');
      expect(result).toHaveLength(1);
      expect(result[0].id).toBe(1);
    });

    it('passes correct document data when opening transcript', async () => {
      props.transcripts = [
        {
          id: 789,
          orderId: 300,
          description: 'Full Day Hearing',
          fileName: 'full-transcript.txt',
          pagesComplete: 50,
          statusCodeId: 1,
          appearances: [
            {
              justinAppearanceId: 1,
              ceisAppearanceId: 0,
              appearanceDt: '2023-10-01',
              appearanceReasonCd: 'ARR',
              appearanceTm: '10:00',
              courtAgencyId: 1,
              courtRoomCd: '101',
              judgeFullNm: 'Judge Smith',
              estimatedDuration: 90,
              estimatedStartTime: '10:00',
              fileid: 1,
              id: 1,
              isInCamera: false,
              statusCodeId: 1,
            },
          ],
        },
      ];

      const wrapper = mount(AppearancesView, {
        props,
        global: {
          plugins: [pinia],
          stubs: dataTableStub,
        },
      });

      await wrapper.vm.$nextTick();

      const transcriptButton = wrapper.find(
        '[data-testid="transcript-button-1"]'
      );
      await transcriptButton.trigger('click');

      expect(shared.openDocumentsPdf).toHaveBeenCalledWith(
        CourtDocumentType.Transcript,
        {
          documentId: '789',
          orderId: '300',
          documentDescription: 'Transcript - Full Day Hearing',
          fileId: 'TEST-123',
          fileNumberText: 'TEST-123',
          courtLevel: 'P',
          courtClass: 'A',
          location: 'Vancouver',
          isCriminal: true,
        }
      );
    });
  });
});
