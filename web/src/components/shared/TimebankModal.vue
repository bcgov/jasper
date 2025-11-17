<template>
  <v-dialog v-model="show" max-width="900px">
    <v-card>
      <v-card-title class="pa-4 pb-2">
        <div class="d-flex justify-space-between align-center w-100">
          <span class="text-h5">
            {{ fullName }}
            <span v-if="locationName" class="text-body-2 text-grey">
              ({{ locationName }})
            </span>
          </span>
          <v-btn icon @click="show = false">
            <v-icon :icon="mdiClose" size="32" />
          </v-btn>
        </div>
      </v-card-title>

      <v-container class="pa-4">
        <v-card class="mx-auto mb-4" elevation="1">
          <v-container>
            <v-form @submit.prevent>
              <v-row class="py-1">
                <v-col cols="3">
                  <v-select
                    v-model="selectedPeriod"
                    :items="availablePeriods"
                    label="Period"
                    density="compact"
                    hide-details
                    @update:model-value="refreshTimebankData"
                  />
                </v-col>
                <v-col cols="1">
                  <v-btn
                    :icon="mdiRefresh"
                    size="small"
                    variant="text"
                    :loading="loading"
                    @click="refreshTimebankData"
                    title="Refresh vacation summary"
                  />
                </v-col>

                <v-spacer />

                <v-col cols="2">
                  <v-text-field
                    v-model.number="payoutRate"
                    label="Rate (Day)"
                    density="compact"
                    :rules="[rateRequired]"
                    prefix="$"
                    variant="outlined"
                    rounded
                    hide-details
                  />
                </v-col>
                <v-col cols="3">
                  <v-date-input
                    v-model="payoutExpiryDate"
                    label="Expiry Date"
                    density="compact"
                    :rules="[dateRequired]"
                    prepend-icon=""
                    prepend-inner-icon="$calendar"
                    hide-details
                  />
                </v-col>
                <v-col cols="2">
                  <v-btn
                    color="primary"
                    :loading="payoutLoading"
                    :disabled="!isCalculateEnabled"
                    @click="calculatePayout"
                    size="large"
                    block
                  >
                    Calculate
                  </v-btn>
                </v-col>
              </v-row>
            </v-form>
          </v-container>
        </v-card>

        <v-expansion-panels
          v-model="expandedPanels"
          multiple
          class="mb-4"
          elevation="1"
        >
          <TimebankSummaryComponent
            :loading="loading"
            :error="error"
            :processed-flags="processedFlags"
            :vacation-summary-list="vacationSummaryList"
            :is-hours="isHours"
          />

          <TimebankPayoutComponent
            :payout-loading="payoutLoading"
            :payout-error="payoutError"
            :payout-data="payoutData"
            :is-hours="isHours"
            :extra-duty-hours-days-label="extraDutyHoursDaysLabel"
            :extra-duty-balance-formatted="extraDutyBalanceFormatted"
            @close-error="payoutError = null"
          />
        </v-expansion-panels>
      </v-container>

      <v-card-actions class="justify-end px-4 pb-4">
        <v-btn variant="outlined" size="large" @click="show = false"
          >Close</v-btn
        >
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
  import { TimebankService } from '@/services/TimebankService';
  import { useCommonStore } from '@/stores';
  import {
    TimebankSummary,
    VacationPayout,
    VacationSummaryItem,
  } from '@/types/timebank';
  import { mdiClose, mdiRefresh } from '@mdi/js';
  import { computed, inject, onMounted, ref, watch } from 'vue';
  import {
    TimebankSummary as TimebankSummaryComponent,
    TimebankPayout as TimebankPayoutComponent,
  } from './timebank';

  interface Props {
    judgeId: number;
  }

  const props = defineProps<Props>();
  const show = defineModel<boolean>({ type: Boolean, required: true });

  const timebankService = inject<TimebankService>('timebankService');
  const commonStore = useCommonStore();

  const loading = ref(false);
  const error = ref<string | null>(null);
  const timebankData = ref<TimebankSummary | null>(null);
  const selectedPeriod = ref<number>(new Date().getFullYear());

  const expandedPanels = ref(['vacation-summary']); // Start with vacation summary expanded

  const payoutLoading = ref(false);
  const payoutError = ref<string | null>(null);
  const payoutData = ref<VacationPayout | null>(null);
  const payoutRate = ref<number | null>(null);
  const payoutExpiryDate = ref<Date>(new Date()); // Default to today

  // Generate available periods (2013 to current year + 2), matches PCSS
  const availablePeriods = computed(() => {
    const currentYear = new Date().getFullYear();
    const periods: number[] = [];
    for (let year = 2013; year <= currentYear + 2; year++) {
      periods.push(year);
    }
    return periods;
  });

  const fullName = computed(() => {
    if (!timebankData.value) return '';
    return `${timebankData.value.firstNm || ''} ${timebankData.value.surnameNm || ''}`.trim();
  });

  const isHours = computed(() => {
    return timebankData.value?.vacation?.isHours ?? false;
  });

  const flags = computed(() => {
    return timebankData.value?.vacation?.flags ?? [];
  });

  const processedFlags = computed(() => {
    return flags.value.map((flag) => ({
      ...flag,
      description: roundDecimalsInText(flag.description || ''), // PCSS returns unrounded decimal places from backend request - frontend responsible for rounding display.
    }));
  });

  const locationName = computed(() => {
    const locationId = timebankData.value?.locationId;
    if (!locationId) return '';

    const location = commonStore.courtRoomsAndLocations.find(
      (loc) => loc.locationId === locationId.toString()
    );

    return location?.name || '';
  });

  const isPeriodValid = computed(() => {
    const currentYear = new Date().getFullYear();
    const minValidYear = 2013;
    const maxValidYear = currentYear + 2;

    return (
      selectedPeriod.value &&
      selectedPeriod.value >= minValidYear &&
      selectedPeriod.value <= maxValidYear
    );
  });

  // Validation rules
  const rateRequired = (value: number | null) => {
    return value !== null && value > 0
      ? true
      : 'Rate is required and must be greater than 0';
  };

  const dateRequired = (value: Date | null | undefined | string) => {
    // If value is null, undefined, or empty string, require a date
    if (!value) {
      return 'Expiry date is required';
    }
    // Check if it's a valid Date object
    if (value instanceof Date && !Number.isNaN(value.getTime())) {
      return true;
    }
    // If it's a string, try to parse it
    if (typeof value === 'string') {
      const parsed = new Date(value);
      if (!Number.isNaN(parsed.getTime())) {
        return true;
      }
    }
    return 'Please enter a valid date';
  };

  const isPayoutFormValid = computed(() => {
    const hasValidRate = payoutRate.value !== null && payoutRate.value > 0;
    const hasValidDate =
      payoutExpiryDate.value instanceof Date &&
      !Number.isNaN(payoutExpiryDate.value.getTime());

    return hasValidRate && hasValidDate;
  });

  const hasFormErrors = computed(() => {
    const rateError = rateRequired(payoutRate.value) !== true;
    const dateError = dateRequired(payoutExpiryDate.value) !== true;
    return rateError || dateError;
  });

  // Calculate button should be enabled when form is valid and has no errors
  const isCalculateEnabled = computed(() => {
    return (
      isPayoutFormValid.value && !hasFormErrors.value && !payoutLoading.value
    );
  });

  // Extra duty computed properties
  const extraDutyHoursDaysLabel = computed(() => {
    return isHours.value ? 'hours' : 'days';
  });

  const extraDutyBalanceFormatted = computed(() => {
    if (!payoutData.value) return '0';
    const balance =
      payoutData.value.extraDutyCurrentRemaining +
      payoutData.value.extraDutyBankedRemaining;
    return formatDaysOrHours(balance);
  });

  // Adapted from PCSS source.
  const vacationSummaryList = computed((): VacationSummaryItem[] => {
    const vacation = timebankData.value?.vacation;
    if (!vacation) return [];

    const list: VacationSummaryItem[] = [];

    // A: Prior Year(s) Regular Vacation Carry Over
    if (vacation.regularCarryOver) {
      list.push({
        desc: 'Prior Year(s) Regular Vacation Carry Over',
        amount: vacation.regularCarryOver.total,
      });
    }

    // B: Prior Year(s) Extra Duties Vacation Carry Over
    if (vacation.extraDutiesCarryOver) {
      list.push({
        desc: 'Prior Year(s) Extra Duties Vacation Carry Over',
        amount: vacation.extraDutiesCarryOver.total,
      });
    }

    // C: Current Year Regular Vacation Entitlement
    if (vacation.regular) {
      list.push({
        desc: 'Current Year Regular Vacation Entitlement',
        amount: vacation.regular.total,
      });
    }

    // D: Current Year Extra Duties Vacation Entitlement
    if (vacation.extraDuties) {
      list.push({
        desc: 'Current Year Extra Duties Vacation Entitlement',
        amount: vacation.extraDuties.total,
      });
    }

    // E: Total Combined Vacation Available for the Year
    // F: Vacation Scheduled
    // G: Current Year Regular Vacation Balance
    // H: Current Year Extra Duties Vacation Balance
    // I: Prior Year(s) Regular Vacation Balance
    // J: Prior Year(s) Extra Duties Vacation Balance
    // K: Total Vacation Available
    const additionalItems: VacationSummaryItem[] = [
      {
        desc: 'Total Combined Vacation Available for the Year',
        amount: vacation.total,
      },
      {
        desc: 'Vacation Scheduled',
        amount: vacation.vacationScheduled,
      },
    ];

    if (vacation.regular) {
      additionalItems.push({
        desc: 'Current Year Regular Vacation Balance',
        amount: vacation.regular.remaining,
      });
    }

    if (vacation.extraDuties) {
      additionalItems.push({
        desc: 'Current Year Extra Duties Vacation Balance',
        amount: vacation.extraDuties.remaining,
      });
    }

    if (vacation.regularCarryOver) {
      additionalItems.push({
        desc: 'Prior Year(s) Regular Vacation Balance',
        amount: vacation.regularCarryOver.remaining,
      });
    }

    if (vacation.extraDutiesCarryOver) {
      additionalItems.push({
        desc: 'Prior Year(s) Extra Duties Vacation Balance',
        amount: vacation.extraDutiesCarryOver.remaining,
      });
    }

    additionalItems.push({
      desc: 'Total Vacation Available',
      amount: vacation.totalRemaining,
    });

    return [...list, ...additionalItems];
  });

  const roundDecimalsInText = (text: string): string => {
    // Match decimal numbers (including whole numbers with .0, .00, etc.)
    return text.replaceAll(/\b\d+\.\d+\b/g, (match) => {
      const num = Number.parseFloat(match);
      return Math.ceil(num).toString();
    });
  };

  const formatDaysOrHours = (value: number): string => {
    if (value === null || value === undefined) {
      return '0';
    }
    if (value === 0) return '0';
    return value.toFixed(2);
  };

  const formatMoney = (value: number): string => {
    if (value === null || value === undefined) {
      return '0.00';
    }
    if (value === 0) return '0.00';
    return value.toFixed(2);
  };

  const fetchTimebankData = async () => {
    if (!timebankService) {
      error.value = 'Timebank service not available';
      return;
    }

    // Validate period is a valid year
    if (!isPeriodValid.value) {
      error.value = 'Invalid period selected. Please select a valid year.';
      timebankData.value = null;
      return;
    }

    loading.value = true;
    error.value = null;

    try {
      const response = await timebankService.getTimebankSummaryForJudge(
        selectedPeriod.value,
        props.judgeId,
        true
      );

      if (response) {
        timebankData.value = response;
      } else {
        timebankData.value = null;
        error.value = 'No timebank data available for the selected period';
      }
    } catch (err) {
      if (
        err &&
        typeof err === 'object' &&
        'status' in err &&
        err.status === 404
      ) {
        error.value = 'Timebank data not found for this period';
      } else {
        error.value = 'Failed to load timebank data. Please try again.';
      }
    } finally {
      loading.value = false;
    }
  };

  const refreshTimebankData = async () => {
    await fetchTimebankData();
    // Expand vacation summary and collapse vacation payout on refresh
    expandedPanels.value = ['vacation-summary'];
  };

  const calculatePayout = async () => {
    if (!timebankService) {
      payoutError.value = 'Timebank service not available';
      return;
    }

    // Validate period is a valid year
    if (!isPeriodValid.value) {
      payoutError.value =
        'Invalid period selected. Please select a valid year.';
      return;
    }

    if (!isCalculateEnabled.value) {
      payoutError.value = 'Please fill in all required fields correctly';
      return;
    }

    payoutLoading.value = true;
    payoutError.value = null;

    try {
      const response = await timebankService.getTimebankPayoutForJudge(
        selectedPeriod.value,
        payoutExpiryDate.value,
        payoutRate.value!,
        props.judgeId
      );

      payoutData.value = response;

      // Collapse vacation summary and expand vacation payout when calculation is complete
      expandedPanels.value = ['vacation-payout'];
    } catch (err) {
      console.error('Error calculating payout:', err);
      payoutError.value = 'Failed to calculate payout. Please try again.';
      payoutData.value = null;
    } finally {
      payoutLoading.value = false;
    }
  };

  watch(show, (newValue) => {
    if (newValue) {
      fetchTimebankData();
      // Reset payout data when dialog opens
      payoutData.value = null;
      payoutRate.value = null;
      payoutExpiryDate.value = new Date(); // Reset to today
      payoutError.value = null;
      // Reset panels to show vacation summary by default
      expandedPanels.value = ['vacation-summary'];
    }
  });

  // Fetch data on mount if dialog is already open
  onMounted(() => {
    if (show.value) {
      fetchTimebankData();
    }
  });
</script>

<style scoped>
  /* Styles for the main modal container */
</style>
