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
    if (!birthDateRaw) {
      return '';
    }

    const birthDate = new Date(birthDateRaw);
    if (isNaN(birthDate.getTime())) {
      return '';
    }

    const today = new Date();

    let years = today.getFullYear() - birthDate.getFullYear();

    // Check if birthday has happened this year yet
    const birthdayPassed =
      today.getMonth() > birthDate.getMonth() ||
      (today.getMonth() === birthDate.getMonth() &&
        today.getDate() >= birthDate.getDate());

    if (!birthdayPassed) {
      years--;
    }

    if (years >= 1) {
      return years.toString();
    }

    // Calculate months difference for less than 1 year
    let months = today.getMonth() - birthDate.getMonth();
    if (today.getDate() < birthDate.getDate()) {
      months--;
    }

    if (months < 0) {
      months += 12;
    }

    return `${Math.max(0, months)} months`;
  });
</script>
<style scoped>
  .v-chip {
    color: var(--text-blue-800);
  }
</style>
