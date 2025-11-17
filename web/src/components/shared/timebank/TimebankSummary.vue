<template>
  <v-expansion-panel value="vacation-summary" class="mb-2">
    <v-expansion-panel-title class="py-3">
      <h3 class="text-h6">
        Vacation Summary
        <span v-if="processedFlags.length > 0" class="mb-4">
          <span v-for="flag in processedFlags" :key="flag.reason">
            <v-chip
              v-if="flag.shortDescription && flag.shortDescription?.length > 0"
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
        <div v-for="flag in processedFlags" :key="flag.reason" class="mb-1">
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
              <th v-if="!isHours" scope="col" class="text-right">Days</th>
              <th v-if="isHours" scope="col" class="text-right">Hours</th>
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

      <div v-else-if="!loading && !error" class="text-center text-grey py-4">
        <p>No vacation data available.</p>
      </div>
    </v-expansion-panel-text>
  </v-expansion-panel>
</template>

<script setup lang="ts">
  import type { VacationSummaryItem, TbSummaryFlag } from '@/types/timebank';

  interface Props {
    loading: boolean;
    error: string | null;
    processedFlags: TbSummaryFlag[];
    vacationSummaryList: VacationSummaryItem[];
    isHours: boolean;
  }

  defineProps<Props>();

  const formatDaysOrHours = (value: number): string => {
    if (value === null || value === undefined) {
      return '0';
    }
    if (value === 0) return '0';
    return value.toFixed(2);
  };
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
</style>
