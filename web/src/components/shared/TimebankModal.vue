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
          <v-expansion-panel value="vacation-summary" class="mb-2">
            <v-expansion-panel-title class="py-3">
              <h3 class="text-h6">
                Vacation Summary
                <span v-if="processedFlags.length > 0" class="mb-4">
                  <span v-for="flag in processedFlags" :key="flag.reason">
                    <v-chip
                      v-if="
                        flag.shortDescription &&
                        flag.shortDescription?.length > 0
                      "
                      size="small"
                      color="error"
                      class="mr-2"
                      :title="flag.shortDescription"
                      >{{ flag.shortDescription }}</v-chip
                    ></span
                  >
                </span>
              </h3>
            </v-expansion-panel-title>
            <v-expansion-panel-text class="pt-3">
              <v-alert v-if="error" type="error" class="mb-4" closable>
                {{ error }}
              </v-alert>

              <div v-if="processedFlags.length > 0" class="mb-4">
                <div
                  v-for="flag in processedFlags"
                  :key="flag.reason"
                  class="mb-1"
                >
                  <v-alert type="warning" density="compact"
                    ><span>{{ flag.description }}</span></v-alert
                  >
                </div>
              </div>

              <v-skeleton-loader
                v-if="loading"
                type="table"
                class="vacation-summary-skeleton"
              />

              <div v-else-if="vacationSummaryList.length > 0">
                <v-table density="compact" class="vacation-summary-table pb-5">
                  <thead>
                    <tr>
                      <th scope="col" class="text-left">&nbsp;</th>
                      <th v-if="!isHours" scope="col" class="text-right">
                        Days
                      </th>
                      <th v-if="isHours" scope="col" class="text-right">
                        Hours
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in vacationSummaryList" :key="item.desc">
                      <td class="text-left">{{ item.desc }}</td>
                      <td class="text-right">
                        {{ formatDaysOrHours(item.amount) }}
                      </td>
                    </tr>
                  </tbody>
                </v-table>
              </div>

              <div
                v-else-if="!loading && !error"
                class="text-center text-grey py-4"
              >
                <p>No vacation data available.</p>
              </div>
            </v-expansion-panel-text>
          </v-expansion-panel>

          <v-expansion-panel value="vacation-payout">
            <v-expansion-panel-title class="py-3">
              <h3 class="text-h6">Vacation Payout</h3>
            </v-expansion-panel-title>
            <v-expansion-panel-text class="pt-3">
              <v-alert
                v-if="payoutError"
                type="error"
                class="mb-4"
                closable
                @click:close="payoutError = null"
              >
                {{ payoutError }}
              </v-alert>

              <div v-if="payoutLoading" class="payout-placeholder">
                <v-skeleton-loader type="table" />
              </div>

              <v-table
                v-else-if="payoutData"
                density="compact"
                class="payout-table"
              >
                <thead>
                  <tr>
                    <th scope="col">&nbsp;</th>
                    <th scope="col" class="text-right">Days</th>
                    <th scope="col" class="text-right">Rate</th>
                    <th scope="col" class="text-right">Total</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td><strong>Current Year</strong></td>
                    <td class="text-right">
                      {{
                        formatDaysOrHours(
                          payoutData.vacationCurrentRemaining +
                            payoutData.extraDutyCurrentRemaining
                        )
                      }}
                    </td>
                    <td class="text-right">
                      ${{ formatMoney(payoutData.rate) }}
                    </td>
                    <td class="text-right">
                      ${{ formatMoney(payoutData.totalCurrent) }}
                    </td>
                  </tr>
                  <tr>
                    <td><strong>Prior Year</strong></td>
                    <td class="text-right">
                      {{
                        formatDaysOrHours(
                          payoutData.vacationBankedRemaining +
                            payoutData.extraDutyBankedRemaining
                        )
                      }}
                    </td>
                    <td class="text-right">
                      ${{ formatMoney(payoutData.rate) }}
                    </td>
                    <td class="text-right">
                      ${{ formatMoney(payoutData.totalBanked) }}
                    </td>
                  </tr>
                  <tr class="total-row">
                    <td><strong>Total Payout</strong></td>
                    <td class="text-right">
                      <strong>{{
                        formatDaysOrHours(
                          payoutData.vacationCurrentRemaining +
                            payoutData.extraDutyCurrentRemaining +
                            payoutData.vacationBankedRemaining +
                            payoutData.extraDutyBankedRemaining
                        )
                      }}</strong>
                    </td>
                    <td class="text-right">
                      <strong>${{ formatMoney(payoutData.rate) }}</strong>
                    </td>
                    <td class="text-right">
                      <strong
                        >${{ formatMoney(payoutData.totalPayout) }}</strong
                      >
                    </td>
                  </tr>
                </tbody>
              </v-table>

              <!-- Extra Duty Summary -->
              <div v-if="payoutData" class="payout-summary mt-3 text-body-2">
                <p class="mb-1">
                  At the start of the year there were
                  <strong>{{
                    formatDaysOrHours(payoutData.extraDutyBanked)
                  }}</strong>
                  Extra Duties {{ extraDutyHoursDaysLabel }} banked.
                </p>
                <p class="mb-1">
                  An entitlement of
                  <strong>{{
                    formatDaysOrHours(payoutData.extraDutyCurrent)
                  }}</strong>
                  Extra Duties {{ extraDutyHoursDaysLabel }} were added for the
                  year.
                </p>
                <p class="mb-0">
                  As of the effective date (above), the Extra Duties balance is
                  <strong>{{ extraDutyBalanceFormatted }}</strong>
                  {{ extraDutyHoursDaysLabel }}.
                </p>
              </div>

              <div
                v-else-if="!payoutLoading"
                class="text-center text-grey py-4"
              >
                <p>Click "Calculate" to generate payout information.</p>
              </div>
            </v-expansion-panel-text>
          </v-expansion-panel>
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
    if (value === null || value === undefined || value === '') {
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
  .vacation-summary-table {
    border: 1px solid rgba(0, 0, 0, 0.12);
    border-radius: 4px;
  }

  .vacation-summary-table th {
    font-weight: 600;
    background-color: rgba(0, 0, 0, 0.04);
  }

  .vacation-summary-table tbody tr:hover {
    background-color: rgba(0, 0, 0, 0.04);
  }

  .vacation-summary-skeleton {
    border: 1px solid rgba(0, 0, 0, 0.12);
    border-radius: 4px;
  }

  .payout-table {
    border: 1px solid rgba(0, 0, 0, 0.12);
    border-radius: 4px;
    margin-top: 16px;
  }

  .payout-table th {
    font-weight: 600;
    background-color: rgba(0, 0, 0, 0.04);
  }

  .payout-table .total-row {
    background-color: rgba(33, 150, 243, 0.08);
    border-top: 2px solid #2196f3;
  }

  .payout-placeholder {
    margin-top: 16px;
  }

  .payout-summary {
    line-height: 1.6;
  }

  .payout-summary p {
    margin-bottom: 8px;
  }

  .payout-summary p:last-child {
    margin-bottom: 0;
  }
</style>
