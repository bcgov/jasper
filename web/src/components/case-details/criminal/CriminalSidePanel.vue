<template>
  <div class="scrollable">
    <!-- Summary panel -->
    <v-skeleton-loader
    v-if="loadingStates.summary"
      type="card"
     />
    <CriminalSummary
      v-else
      :details="summaryData"
      :hasBans="hasBans"
      :hasBansLoading="loadingStates.participants"
    />
    <!-- Adjudicator Restrictions panel -->
    <v-skeleton-loader
      v-if="loadingStates.restrictions"
      type="card"
      class="mt-4"
    />
    <adjudicator-restrictions-panel
      v-else-if="adjudicatorRestrictions?.length > 0"
      :adjudicatorRestrictions
    />
    <!-- Participants panel -->
    <v-skeleton-loader
      v-if="loadingStates.participants"
      type="list-item-three-line@3"
      class="mt-4"
    />
    <accused-panel
      v-else-if="details"
      :accused="details.participant ?? []"
      :courtClassCd="details.courtClassCd"
      :appearances
    />
  </div>
</template>

<script setup lang="ts">
  import { AdjudicatorRestrictionsInfoType } from '@/types/common';
  import { criminalFileDetailsType } from '@/types/criminal/jsonTypes';
  import { computed } from 'vue';
  import AccusedPanel from '../common/accused/AccusedPanel.vue';
  import AdjudicatorRestrictionsPanel from '../common/adjudicator-restrictions/AdjudicatorRestrictionsPanel.vue';
  import CriminalSummary from './CriminalSummary.vue';

  const props = defineProps<{
    details: criminalFileDetailsType;
    summaryDetails?: criminalFileDetailsType;
    adjudicatorRestrictions: AdjudicatorRestrictionsInfoType[];
    loadingStates: {
      summary: boolean;
      participants: boolean;
      restrictions: boolean;
    };
  }>();

  const summaryData = computed(() => props.summaryDetails ?? props.details);
  const details = computed(() => props.details);
  const appearances = computed(() => props.details.appearances?.apprDetail ?? []);
  const hasBans = computed(() =>
    (props.details.participant ?? []).some((participant) =>
      (participant.ban ?? []).length > 0
    )
  );
</script>
