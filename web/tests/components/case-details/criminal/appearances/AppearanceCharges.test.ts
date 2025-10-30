import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import AppearanceCharges from '@/components/case-details/criminal/appearances/AppearanceCharges.vue';
import { CriminalCharges } from '@/types/criminal/jsonTypes/index';

describe('AppearanceCharges.vue', () => {
  let wrapper: ReturnType<typeof mount>;

  const mockCharges: CriminalCharges[] = [
    {
        printSeqNo: '1',
        statuteSectionDsc: '265(1)(a)',
        statuteDsc: 'Assault',
        appearanceResultCd: 'ADJ',
        appearanceResultDesc: 'Adjournment',
        pleaCode: 'G',
        pleaDate: new Date('2024-06-15T00:00:00.000Z'),
        findingCd: 'PEN',
        appearanceReasonDsc: '',
        findingDsc: '',
        appearanceReasonCd: '',
        additionalProperties: {}
    },
    {
        printSeqNo: '2',
        statuteSectionDsc: '320.14(1)(a)',
        statuteDsc: 'Impaired Driving',
        appearanceResultCd: 'RES',
        appearanceResultDesc: 'Result',
        pleaCode: 'NG',
        pleaDate: null,
        findingCd: 'DIS',
        appearanceReasonDsc: '',
        findingDsc: '',
        appearanceReasonCd: '',
        additionalProperties: {}
    },
    {
        printSeqNo: '3',
        statuteSectionDsc: '145(3)',
        statuteDsc: 'Failure to Comply',
        appearanceResultCd: 'SET',
        appearanceResultDesc: 'Set for Trial',
        pleaCode: 'G',
        pleaDate: new Date('2024-03-10T00:00:00.000Z'),
        findingCd: 'GUI',
        appearanceReasonDsc: '',
        findingDsc: '',
        appearanceReasonCd: '',
        additionalProperties: {}
    },
  ];

  beforeEach(() => {
    wrapper = mount(AppearanceCharges, {
      props: {
        charges: mockCharges,
      },
      global: {
        stubs: {
          'v-data-table-virtual': true,
          'v-tooltip': true,
          'v-row': true,
          'v-col': true,
        },
      },
    });
  });

  it('renders component with charges data', () => {
    expect(wrapper.exists()).toBe(true);
    expect(wrapper.findComponent({ name: 'v-data-table-virtual' }).exists()).toBe(true);
  });

  it('renders tooltip in lastResults slot', () => {
    const dataTable = wrapper.findComponent({ name: 'v-data-table-virtual' });
    
    // Simulate the slot with first charge data
    const slotContent = dataTable.vm.$slots['item.lastResults']({
      value: mockCharges[0].appearanceResultCd,
      item: mockCharges[0]
    });
    
    expect(slotContent).toBeTruthy();
  });

  it('renders plea code slot content', () => {
    const dataTable = wrapper.findComponent({ name: 'v-data-table-virtual' });
    
    // Simulate the slot with first charge data
    const slotContent = dataTable.vm.$slots['item.pleaCode']({
      value: mockCharges[0].pleaCode,
      item: mockCharges[0]
    });
    
    expect(slotContent).toBeTruthy();
  });

  it('handles undefined charges prop', async () => {
    await wrapper.setProps({ charges: undefined });
    
    const dataTable = wrapper.findComponent({ name: 'v-data-table-virtual' });
    expect(dataTable.props('items')).toBeUndefined();
  });

  // Test specific slot functionality with minimal mocking
  describe('slot functionality', () => {
    let wrapperWithSlots: ReturnType<typeof mount>;

    beforeEach(() => {
      wrapperWithSlots = mount(AppearanceCharges, {
        props: {
          charges: [mockCharges[0]],
        },
        global: {
          stubs: {
            'v-data-table-virtual': {
              template: '<div><slot name="item.lastResults" :value="items[0].appearanceResultCd" :item="items[0]"></slot><slot name="item.pleaCode" :value="items[0].pleaCode" :item="items[0]"></slot></div>',
              props: ['headers', 'items'],
            },
            'v-tooltip': {
              template: '<div><slot name="activator"></slot></div>',
              props: ['text', 'location'],
            },
            'v-row': {
              template: '<div class="v-row"><slot></slot></div>',
            },
            'v-col': {
              template: '<div class="v-col"><slot></slot></div>',
            },
          },
        },
      });
    });

    it('renders tooltip with correct appearance result code', () => {
      expect(wrapperWithSlots.text()).toContain('ADJ');
    });

    it('renders plea code', () => {
      expect(wrapperWithSlots.text()).toContain('G');
    });
  });
});
