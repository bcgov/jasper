<template>
  <Summary v-if="details" :details />
  <accused-panel
    v-if="details"
    :accused="details.participant"
    :activityClass="details.activityClassDesc"
    :appearances
    class="pt-5"
  />
</template>

<script setup lang="ts">
  import { criminalFileDetailsType } from '@/types/criminal/jsonTypes';
  import { ref } from 'vue';
  import AccusedPanel from './Accused/AccusedPanel.vue';
  import Summary from './Summary.vue';

  const props = defineProps<{ details: criminalFileDetailsType }>();

  const details = ref(props.details);
  const appearances = ref(props.details.appearances?.apprDetail);
</script>
