import { CourtListAppearance } from '@/types/courtlist';
import { mount } from '@vue/test-utils';
import CourtListTableActionBarGroup from 'CMP/courtlist/CourtListTableActionBarGroup.vue';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { nextTick } from 'vue';

vi.mock('@/services/BinderService');
vi.mock('@/stores/CommonStore', () => ({
  useCommonStore: () => ({
    userInfo: { userTitle: 'Judge Josh', userId: '123', judgeId: 123 },
    state: () => ({
      userInfo: { userTitle: 'Judge Josh', userId: '123', judgeId: 123 },
    }),
  }),
}));

let pinia: any;
let binderService: any;

const createWrapper = (props = {}, options: any = {}) => {
  return mount(CourtListTableActionBarGroup, {
    global: {
      plugins: [pinia],
      provide: {
        binderService,
      },
      ...options.global,
    },
    props,
  });
};

beforeEach(() => {
  pinia = createPinia();
  setActivePinia(pinia);
  binderService = {
    getBinders: vi.fn().mockResolvedValue({
      succeeded: true,
      payload: [],
    }),
  };
});

const mockAppearances: CourtListAppearance[] = [
  {
    id: 1,
    courtClassCd: 'A',
    physicalFileId: 'file-001',
    participantId: 'part-001',
    courtFileNumber: 'A-001',
  } as unknown as CourtListAppearance,
  {
    id: 2,
    courtClassCd: 'Y',
    physicalFileId: 'file-002',
    participantId: 'part-002',
    courtFileNumber: 'Y-002',
  } as unknown as CourtListAppearance,
  {
    id: 3,
    courtClassCd: 'C',
    physicalFileId: 'file-003',
    participantId: 'part-003',
    courtFileNumber: 'C-003',
  } as unknown as CourtListAppearance,
  {
    id: 4,
    courtClassCd: 'F',
    physicalFileId: 'file-004',
    participantId: 'part-004',
    courtFileNumber: 'F-004',
  } as unknown as CourtListAppearance,
];

describe('CourtListTableActionBarGroup.vue', () => {
  it('renders grouped ActionBars for each court class', () => {
    const wrapper = createWrapper({ selected: mockAppearances });

    expect(wrapper.text()).toContain('Criminal - Adult file/s');
    expect(wrapper.text()).toContain('Youth file/s');
    expect(wrapper.text()).toContain('Small Claims file/s');
    expect(wrapper.text()).toContain('Family file/s');
  });

  it('groups appearances by correct court class', () => {
    const wrapper = createWrapper({ selected: mockAppearances });

    const actionBars = wrapper.findAllComponents({ name: 'action-bar' });
    expect(actionBars.length).toBe(4);
  });

  it('renders nothing if selected is empty', () => {
    const wrapper = createWrapper(
      { selected: [] },
      {
        global: {
          stubs: {
            ActionBar: true,
            'v-btn': true,
          },
        },
      }
    );
    expect(wrapper.html()).not.toContain('file/s');
  });

  it('clicking the first View Case Details button should emit an event and payload from the first mock data', () => {
    const wrapper = createWrapper({ selected: mockAppearances });

    const firstViewButton = wrapper.find('[data-testid="view-case-details"]');
    firstViewButton.trigger('click');

    expect(wrapper.emitted('view-case-details')).toBeTruthy();
    expect(wrapper.emitted('view-case-details')![0][0]).toEqual([
      mockAppearances[0],
    ]);
  });

  it('renders "View judicial binder(s)" button for civil/family court classes', () => {
    const wrapper = createWrapper({ selected: mockAppearances });

    const judicialBinderButtons = wrapper.findAll(
      '[data-testid="view-judicial-binders"]'
    );
    expect(judicialBinderButtons.length).toBeGreaterThan(0);
  });

  it('does not render "View judicial binder(s)" button for criminal court classes', () => {
    const criminalAppearances = mockAppearances.filter((a) =>
      ['A', 'Y'].includes(a.courtClassCd)
    );
    const wrapper = createWrapper({ selected: criminalAppearances });

    const judicialBinderButtons = wrapper.findAll(
      '[data-testid="view-judicial-binders"]'
    );
    expect(judicialBinderButtons.length).toBe(0);
  });

  it('fetches binder counts when civil/family appearances are selected', async () => {
    binderService.getBinders.mockResolvedValue({
      succeeded: true,
      payload: [{ id: '1' }, { id: '2' }],
    });

    const civilAppearances = mockAppearances.filter((a) =>
      ['C', 'F'].includes(a.courtClassCd)
    );
    createWrapper({ selected: civilAppearances });

    await nextTick();
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(binderService.getBinders).toHaveBeenCalled();
  });

  it('displays binder count in judicial binder button', async () => {
    binderService.getBinders.mockResolvedValue({
      succeeded: true,
      payload: [{ id: '1' }],
    });

    const civilAppearances = mockAppearances.filter(
      (a) => a.courtClassCd === 'C'
    );
    const wrapper = createWrapper({ selected: civilAppearances });

    await nextTick();
    await new Promise((resolve) => setTimeout(resolve, 50));

    const judicialBinderButton = wrapper.find(
      '[data-testid="view-judicial-binders"]'
    );
    expect(judicialBinderButton.text()).toContain('1 / 1');
  });

  it('disables judicial binder button when count is 0', async () => {
    binderService.getBinders.mockResolvedValue({
      succeeded: true,
      payload: [],
    });

    const civilAppearances = mockAppearances.filter(
      (a) => a.courtClassCd === 'C'
    );
    const wrapper = createWrapper({ selected: civilAppearances });

    await nextTick();
    await new Promise((resolve) => setTimeout(resolve, 50));

    const judicialBinderButton = wrapper.find(
      '[data-testid="view-judicial-binders"]'
    );
    expect(judicialBinderButton.attributes('disabled')).toBeDefined();
  });

  it('emits view-judicial-binders event when button is clicked', async () => {
    binderService.getBinders.mockResolvedValue({
      succeeded: true,
      payload: [
        {
          id: '1',
          labels: { physicalFileId: 'file-003' },
          documents: [],
        },
      ],
    });

    const civilAppearances = mockAppearances.filter(
      (a) => a.courtClassCd === 'C'
    );
    const wrapper = createWrapper({ selected: civilAppearances });

    await nextTick();
    await new Promise((resolve) => setTimeout(resolve, 50));

    const judicialBinderButton = wrapper.find(
      '[data-testid="view-judicial-binders"]'
    );
    await judicialBinderButton.trigger('click');

    expect(wrapper.emitted('view-judicial-binders')).toBeTruthy();
    expect(wrapper.emitted('view-judicial-binders')![0][0]).toEqual([
      {
        documents: [],
        id: '1',
        labels: {
          physicalFileId: 'file-003',
        },
        fileNumber: 'C-003',
      },
    ]);
  });

  it('emits view-key-documents event when button is clicked', async () => {
    binderService.getBinders.mockResolvedValue({
      succeeded: true,
      payload: [{ id: '1' }],
    });

    const criminalAppearances = mockAppearances.filter(
      (a) => a.courtClassCd === 'A'
    );
    const wrapper = createWrapper({ selected: criminalAppearances });

    await nextTick();
    await new Promise((resolve) => setTimeout(resolve, 50));

    const keyDocsBinderButton = wrapper.find(
      '[data-testid="view-key-documents"]'
    );
    await keyDocsBinderButton.trigger('click');

    expect(wrapper.emitted('view-key-documents')).toBeTruthy();
    expect(wrapper.emitted('view-key-documents')![0][0]).toEqual(
      criminalAppearances
    );
    expect(wrapper.emitted('view-key-documents')![0][1]).toEqual([]);
  });

  it('emits view-informations event when button is clicked', async () => {
    binderService.getBinders.mockResolvedValue({
      succeeded: true,
      payload: [{ id: '1' }],
    });

    const criminalAppearances = mockAppearances.filter(
      (a) => a.courtClassCd === 'A'
    );
    const wrapper = createWrapper({ selected: criminalAppearances });

    await nextTick();
    await new Promise((resolve) => setTimeout(resolve, 50));

    const keyDocsBinderButton = wrapper.find(
      '[data-testid="view-informations"]'
    );
    await keyDocsBinderButton.trigger('click');

    expect(wrapper.emitted('view-informations')).toBeTruthy();
    expect(wrapper.emitted('view-informations')![0][0]).toEqual(
      criminalAppearances
    );
    expect(wrapper.emitted('view-informations')![0][1]).toEqual(['INITIATING']);
  });

  it('emits unique-civil-file-selected when a new civil file is selected', async () => {
    binderService.getBinders.mockResolvedValue({
      succeeded: true,
      payload: [{ id: '1' }],
    });

    const wrapper = createWrapper({ selected: [mockAppearances[2]] });
    await nextTick();

    expect(wrapper.emitted('unique-civil-file-selected')).toBeTruthy();
    expect(wrapper.emitted('unique-civil-file-selected')![0][0]).toEqual(
      mockAppearances[2]
    );
  });

  it('does not emit unique-civil-file-selected for criminal files', async () => {
    binderService.getBinders.mockResolvedValue({
      succeeded: true,
      payload: [],
    });

    const wrapper = createWrapper({ selected: [mockAppearances[0]] });
    await nextTick();

    expect(wrapper.emitted('unique-civil-file-selected')).toBeFalsy();
  });
});
