<template>
  <h5 class="my-1">Case Details</h5>
  <v-card class="py-3" color="var(--bg-gray)" flat>
    <div class="ml-3 d-flex align-center">
      <DivisionBadge :division :activityClassDesc />
      <v-icon
        v-if="sheriffComments"
        :icon="mdiPoliceBadgeOutline"
        color="var(--text-red)"
        @click="showSheriffCommentsModal = true"
      />
    </div>
    <v-card-title style="text-wrap: wrap">{{ names }}</v-card-title>
    <v-card-subtitle>{{ location }}</v-card-subtitle>
  </v-card>
  <SheriffCommentsDialog
    v-if="sheriffComments"
    :comments="sheriffComments"
    v-model="showSheriffCommentsModal"
  />
</template>
<script setup lang="ts">
  import { civilFileDetailsType } from '@/types/civil/jsonTypes';
  import { mdiPoliceBadgeOutline } from '@mdi/js';
  import { ref } from 'vue';
  import DivisionBadge from '../common/DivisionBadge.vue';
  import SheriffCommentsDialog from './SheriffCommentsDialog.vue';

  const props = defineProps<{ details: civilFileDetailsType }>();
  const showSheriffCommentsModal = ref(false);

  const division = props.details.courtClassDescription;
  const activityClassDesc = props.details.activityClassDesc;
  const sheriffComments = props.details.sheriffCommentText;
  const names = props.details.socTxt;
  const location = props.details.homeLocationAgencyName;
</script>
<style scoped>
  .v-card {
    border-radius: 0.5rem !important;
  }
</style>
