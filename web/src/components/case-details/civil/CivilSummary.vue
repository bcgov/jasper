<template>
  <h5 class="my-1">
    Case Details
    <span
      class="pl-3"
      :class="
        labelClasses[getCourtClassLabel(details.courtClassCd)] ||
        'criminal-label'
      "
    >
      {{ details.fileNumberTxt }}
    </span>
    <CopyToClipboard :text="details.fileNumberTxt" />
  </h5>
  <v-card class="py-3" color="var(--bg-gray-500)" flat>
    <div class="ml-3 d-flex align-center">
      <DivisionBadge :division :activityClassDesc :courtClassCd />
      <DivisionBadge
        v-if="isCPA"
        division="CPA"
        :activityClassDesc
        :courtClassCd
      />
      <v-icon
        v-if="sheriffComments"
        :icon="mdiPoliceBadgeOutline"
        color="var(--text-red-500)"
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
  import CopyToClipboard from '@/components/shared/CopyToClipboard.vue';
  import { labelClasses } from '@/constants/labelClasses';
  import { civilFileDetailsType } from '@/types/civil/jsonTypes';
  import { getCourtClassLabel } from '@/utils/utils';
  import { mdiPoliceBadgeOutline } from '@mdi/js';
  import { ref } from 'vue';
  import DivisionBadge from '../common/DivisionBadge.vue';
  import SheriffCommentsDialog from './SheriffCommentsDialog.vue';

  const props = defineProps<{ details: civilFileDetailsType }>();
  const showSheriffCommentsModal = ref(false);

  const division = props.details.courtClassDescription;
  const activityClassDesc = props.details.activityClassDesc;
  const courtClassCd = props.details.courtClassCd;
  const sheriffComments = props.details.sheriffCommentText;
  const names = props.details.socTxt;
  const location = props.details.homeLocationAgencyName;
  const isCPA = props.details.cfcsaFileYN === 'Y';
</script>
<style scoped>
  .v-card {
    border-radius: 0.5rem !important;
  }
</style>
