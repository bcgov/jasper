<template>
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
        @click:close="handleCloseError"
      >
        {{ payoutError }}
      </v-alert>

      <div v-if="payoutLoading" class="payout-placeholder">
        <v-skeleton-loader type="table" />
      </div>

      <v-table v-else-if="payoutData" density="compact" class="payout-table">
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
            <td class="text-right">${{ formatMoney(payoutData.rate) }}</td>
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
            <td class="text-right">${{ formatMoney(payoutData.rate) }}</td>
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
              <strong>${{ formatMoney(payoutData.totalPayout) }}</strong>
            </td>
          </tr>
        </tbody>
      </v-table>

      <!-- Extra Duty Summary -->
      <div v-if="payoutData" class="payout-summary mt-3 text-body-2">
        <p class="mb-1">
          At the start of the year there were
          <strong>{{ formatDaysOrHours(payoutData.extraDutyBanked) }}</strong>
          Extra Duties {{ extraDutyHoursDaysLabel }} banked.
        </p>
        <p class="mb-1">
          An entitlement of
          <strong>{{ formatDaysOrHours(payoutData.extraDutyCurrent) }}</strong>
          Extra Duties {{ extraDutyHoursDaysLabel }} were added for the year.
        </p>
        <p class="mb-0">
          As of the effective date (above), the Extra Duties balance is
          <strong>{{ extraDutyBalanceFormatted }}</strong>
          {{ extraDutyHoursDaysLabel }}.
        </p>
      </div>

      <div v-else-if="!payoutLoading" class="text-center text-grey py-4">
        <p>Click "Calculate" to generate payout information.</p>
      </div>
    </v-expansion-panel-text>
  </v-expansion-panel>
</template>

<script setup lang="ts">
  import type { VacationPayout } from '@/types/timebank';

  interface Props {
    payoutLoading: boolean;
    payoutError: string | null;
    payoutData: VacationPayout | null;
    isHours: boolean;
    extraDutyHoursDaysLabel: string;
    extraDutyBalanceFormatted: string;
  }

  const props = defineProps<Props>();

  const emit = defineEmits<{
    'close-error': [];
  }>();

  const handleCloseError = () => {
    emit('close-error');
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
</script>

<style scoped>
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
