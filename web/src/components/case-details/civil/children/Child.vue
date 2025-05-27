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
    <v-row>
      <v-col cols="6" class="data-label">Age</v-col>
      <v-col>{{ age }}</v-col>
    </v-row>
    <v-row>
      <v-col cols="6" class="data-label">DOB</v-col>
      <v-col>{{ formatDateToDDMMMYYYY(child.birthDate) }}</v-col>
    </v-row>
    <v-row>
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
    if (!props.child.birthDate) {
      return '';
    }

    const currentDate = new Date();
    const birthDate = new Date(props.child.birthDate);

    if (isNaN(birthDate.getTime())) {
      return '';
    }

    const yearsDiff = currentDate.getFullYear() - birthDate.getFullYear();

    // Check if birthdate had passed this year
    const hadBirthday =
      currentDate.getMonth() > birthDate.getMonth() ||
      (currentDate.getMonth() === birthDate.getMonth() &&
        currentDate.getDate() >= birthDate.getDate());

    if (yearsDiff > 1 || (yearsDiff === 1 && hadBirthday)) {
      // Age has been at least 1 year old
      return yearsDiff - (hadBirthday ? 0 : 1);
    } else {
      // Age is less than 1 year
      let monthsDiff = currentDate.getMonth() - birthDate.getMonth();

      if (monthsDiff < 0) {
        monthsDiff += 12;
      }

      if (currentDate.getDate() < birthDate.getDate()) {
        monthsDiff = Math.max(0, monthsDiff - 1);
      }

      return `${monthsDiff} months`;
    }
  });
</script>
<style scoped>
  .v-chip {
    color: var(--text-blue-800);
  }
</style>
