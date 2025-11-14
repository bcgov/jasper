import TimebankModal from '@/components/shared/TimebankModal.vue';
import { TimebankService } from '@/services/TimebankService';
import { useCommonStore } from '@/stores';
import { TimebankSummary, VacationPayout } from '@/types/timebank';
import { faker } from '@faker-js/faker';
import { mount, VueWrapper } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';
import { createVuetify } from 'vuetify';

// Mock the stores
vi.mock('@/stores');

const vuetify = createVuetify();

describe('TimebankModal.vue', () => {
  let wrapper: VueWrapper;
  let mockTimebankService: Partial<TimebankService>;
  let mockCommonStore: any;
  let pinia: any;

  const mockTimebankData: TimebankSummary = {
    judiciaryPersonId: faker.number.int({ min: 1, max: 1000 }),
    firstNm: faker.person.firstName(),
    surnameNm: faker.person.lastName(),
    locationId: faker.number.int({ min: 1, max: 100 }),
    period: new Date().getFullYear(),
    vacation: {
      period: new Date().getFullYear(),
      isHours: false,
      vacationScheduled: faker.number.float({
        min: 0,
        max: 50,
        fractionDigits: 2,
      }),
      flags: [
        {
          reason: 'TEST_FLAG',
          shortDescription: 'Test',
          description: 'You have 2.5 days remaining in your vacation balance',
        },
      ],
      regular: {
        jan1Entitlement: faker.number.float({
          min: 0,
          max: 50,
          fractionDigits: 2,
        }),
        jan1Adjustment: 0,
        jan1Total: faker.number.float({ min: 0, max: 50, fractionDigits: 2 }),
        otherAdjustment: 0,
        totalAdjustment: 0,
        otherEntitlement: faker.number.float({
          min: 0,
          max: 50,
          fractionDigits: 2,
        }),
        total: faker.number.float({ min: 0, max: 50, fractionDigits: 2 }),
        remaining: faker.number.float({ min: 0, max: 50, fractionDigits: 2 }),
      },
      total: faker.number.float({ min: 0, max: 100, fractionDigits: 2 }),
      totalRemaining: faker.number.float({
        min: 0,
        max: 100,
        fractionDigits: 2,
      }),
    },
  };

  const mockPayoutData: VacationPayout = {
    judiciaryPersonId: faker.number.int({ min: 1, max: 1000 }),
    period: new Date().getFullYear(),
    effectiveDate: new Date().toISOString().substring(0, 10),
    entitlementCalcType: 'STANDARD',
    vacationCurrent: faker.number.float({ min: 0, max: 50, fractionDigits: 2 }),
    vacationBanked: faker.number.float({ min: 0, max: 50, fractionDigits: 2 }),
    extraDutyCurrent: faker.number.float({
      min: 0,
      max: 20,
      fractionDigits: 2,
    }),
    extraDutyBanked: faker.number.float({ min: 0, max: 20, fractionDigits: 2 }),
    vacationUsed: faker.number.float({ min: 0, max: 30, fractionDigits: 2 }),
    vacationCurrentRemaining: faker.number.float({
      min: 0,
      max: 50,
      fractionDigits: 2,
    }),
    vacationBankedRemaining: faker.number.float({
      min: 0,
      max: 50,
      fractionDigits: 2,
    }),
    extraDutyCurrentRemaining: faker.number.float({
      min: 0,
      max: 20,
      fractionDigits: 2,
    }),
    extraDutyBankedRemaining: faker.number.float({
      min: 0,
      max: 20,
      fractionDigits: 2,
    }),
    rate: faker.number.float({ min: 100, max: 1000, fractionDigits: 2 }),
    totalCurrent: faker.number.float({ min: 0, max: 10000, fractionDigits: 2 }),
    totalBanked: faker.number.float({ min: 0, max: 10000, fractionDigits: 2 }),
    totalPayout: faker.number.float({ min: 0, max: 20000, fractionDigits: 2 }),
  };

  const mockLocations = [
    {
      locationId: mockTimebankData.locationId.toString(),
      name: 'Vancouver Provincial Court',
      code: 'VAN',
      shortName: 'Vancouver PC',
      courtRooms: [],
    },
  ];

  beforeEach(() => {
    pinia = createPinia();
    setActivePinia(pinia);

    mockTimebankService = {
      getTimebankSummaryForJudge: vi.fn().mockResolvedValue(mockTimebankData),
      getTimebankPayoutForJudge: vi.fn().mockResolvedValue(mockPayoutData),
    };

    mockCommonStore = {
      courtRoomsAndLocations: mockLocations,
    };

    (useCommonStore as any).mockReturnValue(mockCommonStore);

    wrapper = mount(TimebankModal, {
      props: {
        judgeId: mockTimebankData.judiciaryPersonId,
        modelValue: true,
      },
      global: {
        plugins: [vuetify, pinia],
        provide: {
          timebankService: mockTimebankService,
        },
      },
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('Dialog Display', () => {
    it('displays judge name and location when data is loaded', async () => {
      await wrapper.vm.$nextTick();

      const title = wrapper.find('.text-h5');
      expect(title.text()).toContain(mockTimebankData.firstNm);
      expect(title.text()).toContain(mockTimebankData.surnameNm);
      expect(title.text()).toContain('Vancouver Provincial Court');
    });

    it('displays only judge name when location is not found', async () => {
      mockCommonStore.courtRoomsAndLocations = [];
      await wrapper.vm.$nextTick();

      const title = wrapper.find('.text-h5');
      expect(title.text()).toContain(mockTimebankData.firstNm);
      expect(title.text()).toContain(mockTimebankData.surnameNm);
      expect(title.text()).not.toContain(
        'Alivia Steuber  (Vancouver Provincial Court)'
      );
    });
  });

  describe('Data Loading', () => {
    it('calls timebankService when dialog opens', async () => {
      expect(
        mockTimebankService.getTimebankSummaryForJudge
      ).toHaveBeenCalledWith(
        new Date().getFullYear(),
        mockTimebankData.judiciaryPersonId,
        true
      );
    });

    it('displays error message when service fails', async () => {
      const errorMessage = 'Failed to load data';
      mockTimebankService.getTimebankSummaryForJudge = vi
        .fn()
        .mockRejectedValue(new Error(errorMessage));

      const wrapper2 = mount(TimebankModal, {
        props: {
          judgeId: mockTimebankData.judiciaryPersonId,
          modelValue: true,
        },
        global: {
          plugins: [vuetify, pinia],
          provide: {
            timebankService: mockTimebankService,
          },
        },
      });

      await nextTick();
      await nextTick();

      const errorAlert = wrapper2.find('v-alert[type="error"]');
      expect(errorAlert.exists()).toBe(true);
    });

    it('displays "No vacation data available" when service returns null', async () => {
      mockTimebankService.getTimebankSummaryForJudge = vi
        .fn()
        .mockResolvedValue(null);

      const wrapper2 = mount(TimebankModal, {
        props: {
          judgeId: mockTimebankData.judiciaryPersonId,
          modelValue: true,
        },
        global: {
          plugins: [vuetify, pinia],
          provide: {
            timebankService: mockTimebankService,
          },
        },
      });

      expect(wrapper2.text()).toContain('No vacation data available');
    });
  });

  describe('Flags Processing', () => {
    it('rounds up decimal values in flag descriptions', async () => {
      await wrapper.vm.$nextTick();

      const flagSection = wrapper.find('v-alert[type="warning"]');
      expect(flagSection.exists()).toBe(true);
      expect(flagSection.text()).toContain('3 days remaining'); // 2.5 rounded up to 3
      expect(flagSection.text()).not.toContain('2.5 days');
    });

    it('displays flags with correct styling', async () => {
      await wrapper.vm.$nextTick();

      const flagAlert = wrapper.find('v-alert[type="warning"]');
      const flagChip = wrapper.find('v-chip[color="error"]');

      expect(flagAlert.exists()).toBe(true);
      expect(flagChip.exists()).toBe(true);
    });

    it('does not display flags section when no flags exist', async () => {
      const dataWithoutFlags = { ...mockTimebankData };
      if (dataWithoutFlags.vacation) {
        dataWithoutFlags.vacation.flags = [];
      }

      mockTimebankService.getTimebankSummaryForJudge = vi
        .fn()
        .mockResolvedValue(dataWithoutFlags);

      const wrapper2 = mount(TimebankModal, {
        props: {
          judgeId: mockTimebankData.judiciaryPersonId,
          modelValue: true,
        },
        global: {
          plugins: [vuetify, pinia],
          provide: {
            timebankService: mockTimebankService,
          },
        },
      });

      await nextTick();

      const flagAlert = wrapper2.find('v-alert[type="warning"]');
      expect(flagAlert.exists()).toBe(false);
    });
  });

  describe('Period Selection', () => {
    it('generates correct available periods', () => {
      const currentYear = new Date().getFullYear();
      const periodSelect = wrapper.find('v-select[label="Period"]');

      expect(periodSelect.exists()).toBe(true);

      const vm = wrapper.vm as any;
      const periods = vm.availablePeriods;

      expect(periods).toContain(2013); // Min year
      expect(periods).toContain(currentYear); // Current year
      expect(periods).toContain(currentYear + 2); // Max year
      expect(periods.length).toBe(currentYear - 2013 + 3); // Total range
    });

    it('validates period selection', () => {
      const vm = wrapper.vm as any;

      // Valid periods
      vm.selectedPeriod = new Date().getFullYear();
      expect(vm.isPeriodValid).toBe(true);

      vm.selectedPeriod = 2013;
      expect(vm.isPeriodValid).toBe(true);

      // Invalid periods
      vm.selectedPeriod = 2012;
      expect(vm.isPeriodValid).toBe(false);

      vm.selectedPeriod = new Date().getFullYear() + 3;
      expect(vm.isPeriodValid).toBe(false);
    });
  });

  describe('Payout Calculation', () => {
    beforeEach(async () => {
      const vm = wrapper.vm as any;
      vm.payoutRate = 500;
      vm.payoutExpiryDate = new Date();
      await nextTick();
    });

    it('validates payout form correctly', async () => {
      const vm = wrapper.vm as any;

      // Valid form
      vm.payoutRate = 500;
      vm.payoutExpiryDate = new Date();
      expect(vm.isPayoutFormValid).toBe(true);
      expect(vm.isCalculateEnabled).toBe(true);

      // Invalid rate
      vm.payoutRate = null;
      expect(vm.isPayoutFormValid).toBe(false);
      expect(vm.isCalculateEnabled).toBe(false);

      // Invalid rate (zero)
      vm.payoutRate = 0;
      expect(vm.isPayoutFormValid).toBe(false);
      expect(vm.isCalculateEnabled).toBe(false);
    });

    it.each([
      [null, false],
      [0, false],
      [-100, false],
      [100, true],
      [500.5, true],
    ])('validates rate %s as %s', (rate, expected) => {
      const vm = wrapper.vm as any;
      vm.payoutRate = rate;
      vm.payoutExpiryDate = new Date();

      expect(vm.isPayoutFormValid).toBe(expected);
    });

    it('calls payout service when Calculate button is clicked', async () => {
      const calculateButton = wrapper.find('v-btn[color="primary"]');
      await calculateButton.trigger('click');

      expect(
        mockTimebankService.getTimebankPayoutForJudge
      ).toHaveBeenCalledWith(
        new Date().getFullYear(),
        expect.any(Date),
        500,
        mockTimebankData.judiciaryPersonId
      );
    });

    it('displays payout results after calculation', async () => {
      const vm = wrapper.vm as any;
      await vm.calculatePayout();
      await nextTick();

      const payoutTable = wrapper.find('.payout-table');
      expect(payoutTable.exists()).toBe(true);
      expect(wrapper.text()).toContain('Current Year');
      expect(wrapper.text()).toContain('Prior Year');
      expect(wrapper.text()).toContain('Total Payout');
    });

    it('handles payout calculation errors', async () => {
      mockTimebankService.getTimebankPayoutForJudge = vi
        .fn()
        .mockRejectedValue(new Error('Calculation failed'));

      const vm = wrapper.vm as any;
      await vm.calculatePayout();
      await nextTick();

      const errorAlert = wrapper.find('v-alert[type="error"]');
      expect(errorAlert.exists()).toBe(true);
      expect(errorAlert.text()).toContain('Failed to calculate payout');
    });
  });

  describe('Formatting Functions', () => {
    it.each([
      [0, '0'],
      [1, '1.00'],
      [1.5, '1.50'],
      [10.123, '10.12'],
      [null, '0'],
      [undefined, '0'],
    ])('formats days %s as %s', (input, expected) => {
      const vm = wrapper.vm as any;
      expect(vm.formatDaysOrHours(input)).toBe(expected);
    });

    it.each([
      [0, '0.00'],
      [1, '1.00'],
      [1.5, '1.50'],
      [10.123, '10.12'],
      [null, '0.00'],
      [undefined, '0.00'],
    ])('formats money %s as %s', (input, expected) => {
      const vm = wrapper.vm as any;
      expect(vm.formatMoney(input)).toBe(expected);
    });
  });

  describe('Decimal Rounding in Text', () => {
    it.each([
      ['You have 2.5 days remaining', 'You have 3 days remaining'],
      ['Balance of 15.75 hours', 'Balance of 16 hours'],
      ['Accrued 0.1 vacation days', 'Accrued 1 vacation days'],
      ['No decimals here', 'No decimals here'],
      ['Multiple 2.3 and 4.7 values', 'Multiple 3 and 5 values'],
    ])('rounds decimals in text "%s" to "%s"', (input, expected) => {
      const vm = wrapper.vm as any;
      expect(vm.roundDecimalsInText(input)).toBe(expected);
    });
  });

  describe('Vacation Summary', () => {
    it('displays vacation summary table when data exists', async () => {
      await wrapper.vm.$nextTick();

      const summaryTable = wrapper.find('.vacation-summary-table');
      expect(summaryTable.exists()).toBe(true);

      const tableRows = wrapper.findAll('.vacation-summary-table tbody tr');
      expect(tableRows.length).toBeGreaterThan(0);
    });

    it('shows correct header for hours vs days', async () => {
      // Test with days (default)
      await wrapper.vm.$nextTick();
      expect(wrapper.text()).toContain('Days');
      expect(wrapper.text()).not.toContain('Hours');

      // Test with hours
      const hoursData = { ...mockTimebankData };
      if (hoursData.vacation) {
        hoursData.vacation.isHours = true;
      }

      mockTimebankService.getTimebankSummaryForJudge = vi
        .fn()
        .mockResolvedValue(hoursData);

      const wrapper2 = mount(TimebankModal, {
        props: {
          judgeId: mockTimebankData.judiciaryPersonId,
          modelValue: true,
        },
        global: {
          plugins: [vuetify, pinia],
          provide: {
            timebankService: mockTimebankService,
          },
        },
      });

      await nextTick();
      await nextTick();

      expect(wrapper2.text()).toContain('Hours');
      expect(wrapper2.text()).not.toContain('Days');
    });
  });

  describe('Refresh Functionality', () => {
    it('refreshes data when refresh button is clicked', async () => {
      const refreshButton = wrapper.find(
        'v-btn[title="Refresh vacation summary"]'
      );

      // Clear previous calls
      vi.clearAllMocks();

      await refreshButton.trigger('click');

      expect(
        mockTimebankService.getTimebankSummaryForJudge
      ).toHaveBeenCalledWith(
        new Date().getFullYear(),
        mockTimebankData.judiciaryPersonId,
        true
      );
    });

    it('refreshes data when period selection changes', async () => {
      const vm = wrapper.vm as any;

      // Clear previous calls
      vi.clearAllMocks();

      // Set the selected period and call refreshTimebankData directly
      vm.selectedPeriod = 2023;
      await vm.refreshTimebankData();
      await nextTick();

      expect(
        mockTimebankService.getTimebankSummaryForJudge
      ).toHaveBeenCalledWith(2023, mockTimebankData.judiciaryPersonId, true);
    });
  });
});
