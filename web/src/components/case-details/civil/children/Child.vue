<template>
  <v-card variant="text" class="mb-3">
    <v-row>
      <v-col class="pb-0">
        <v-chip
          variant="flat"
          rounded="lg"
          color="var(--bg-pale-blue)"
          class="w-100 justify-center align-center text-uppercase"
        >
          {{ getName }}
        </v-chip>
      </v-col>
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
  import { formatToFullName } from '@/utils/utils';
  import { computed } from 'vue';

  const props = defineProps<{
    child: partyType;
  }>();

  const counselNames = props.child.counsel?.map((c) => c.fullNm) ?? [];
  const getName = computed(() => {
    const { lastNm, givenNm, orgNm } = props.child;
    return lastNm ? formatToFullName(lastNm, givenNm) : orgNm;
  });
</script>
<style scoped>
  .v-chip {
    color: var(--text-deep-blue);
  }
</style>
