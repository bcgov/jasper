<template>
  <v-container>
    <v-row>
      <v-col>
        <v-expansion-panels
          rounded="lg"
          bg-color="#efedf5"
          :flat="true"
          v-model="expanded"
        >
          <v-expansion-panel rounded="lg" :value="RESERVED_JUDGEMENT">
            <v-expansion-panel-title>
              <h4 class="px-2 py-2">
                Reserved judgments & decisions
                {{
                  reservedJudgementCount > 0
                    ? `(${reservedJudgementCount})`
                    : ''
                }}
              </h4>
            </v-expansion-panel-title>
            <v-expansion-panel-text class="reserved-judgements-table">
              <v-skeleton-loader
                class="my-1"
                type="table"
                :loading="judgementsLoading"
              >
                <ReservedJudgementTable :data="reservedJudgements" />
              </v-skeleton-loader>
            </v-expansion-panel-text>
          </v-expansion-panel>
        </v-expansion-panels>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
  import ReservedJudgementTable from '@/components/dashboard/panels/reserved-judgements/ReservedJudgementTable.vue';
  import { ReservedJudgementService } from '@/services/ReservedJudgementService';
  import { ReservedJudgement } from '@/types/ReservedJudgement';
  import { computed, inject, ref, watch } from 'vue';

  const expanded = ref('');
  const judgementsLoading = ref(false);
  const reservedJudgements = ref<ReservedJudgement[]>([]);
  const RESERVED_JUDGEMENT = 'reserved-judgement';
  const reservedJudgementCount = computed(
    () => reservedJudgements.value.length
  );
  const props = defineProps({
    judgeId: { type: Number, default: null },
  });
  const reservedJudgementService = inject<ReservedJudgementService>(
    'reservedJudgementService'
  );
  if (!reservedJudgementService) {
    throw new Error('Service(s) is undefined.');
  }

  watch([expanded, () => props.judgeId], ([newVal, newJudgeId]) => {
    if (newVal == RESERVED_JUDGEMENT) {
      judgementsLoading.value = true;
      reservedJudgementService
        .get({
          judgeId: newJudgeId ?? '',
        })
        .then((data: ReservedJudgement[]) => {
          reservedJudgements.value = data;
        })
        .catch((err) => {
          console.error('Failed to load reserved judgements:', err);
        })
        .finally(() => {
          judgementsLoading.value = false;
        });
    } else {
      reservedJudgements.value = [];
    }
  });
</script>

<style scoped>
  .reserved-judgements-table {
    background-color: white !important;
  }
</style>
