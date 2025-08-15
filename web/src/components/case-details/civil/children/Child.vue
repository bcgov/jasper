<template>
  <v-card variant="text" class="mb-3">
    <v-row>
      <v-col class="pb-0">
        <v-chip
          variant="flat"
          rounded="lg"
          color="var(--bg-blue-100)"
          class="w-100 justify-center align-center text-uppercase"
        >
          {{ name }}
        </v-chip>
      </v-col>
    </v-row>
    <v-row class="mx-1">
      <v-col cols="6" class="data-label">Age</v-col>
      <v-col>{{ age }}</v-col>
    </v-row>
    <v-row class="mx-1 mt-0">
      <v-col cols="6" class="data-label">DOB</v-col>
      <v-col>{{ formatDateToDDMMMYYYY(child.birthDate) }}</v-col>
    </v-row>
    <v-row class="mx-1 mt-0">
      <v-col cols="6" class="data-label">Counsel</v-col>
      <v-col>
        <LabelWithTooltip :values="counselNames" />
      </v-col>
    </v-row>
  </v-card>
</template>
<script setup lang="ts">
  import LabelWithTooltip from '@/components/shared/LabelWithTooltip.vue';
  import { partyType } from '@/types/civil/jsonTypes';
  import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';
  import { formatToFullName } from '@/utils/utils';
  import { DateTime } from 'luxon';
  import { computed } from 'vue';

  const props = defineProps<{
    child: partyType;
  }>();

  const counselNames = props.child.counsel?.map((c) => c.counselFullName) ?? [];
  const name = computed(() => {
    const { lastNm, givenNm, orgNm } = props.child;
    return lastNm ? formatToFullName(lastNm, givenNm) : orgNm;
  });
  const age = computed(() => {
    const birthDateRaw = props.child.birthDate;
    if (!birthDateRaw) return '';

    const birthDate = DateTime.fromFormat(
      birthDateRaw,
      'yyyy-MM-dd HH:mm:ss.S'
    );
    if (!birthDate.isValid) return '';

    const today = DateTime.now().startOf('day');
    const diff = today.diff(birthDate, ['years', 'months']).toObject();

    if ((diff.years ?? 0) >= 1) {
      return Math.floor(diff.years!); // return the whole number of years as the age if years is non-zero
    } else {
      return `${Math.floor(diff.months ?? 0)} months`; // if the age is less then 1, return the whole number of months.
    }
  });
</script>
<style scoped>
  .v-chip {
    color: var(--text-blue-800);
  }
</style>
