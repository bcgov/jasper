<template>
  <div class="py-3">
    <h5>{{ titleText }}</h5>
    <Accused
      v-for="accused in props.accused"
      :key="accused.partId"
      :accused="accused"
      :appearances="
        props.appearances.filter(
          (appearance) => appearance.lastNm === accused.lastNm
        )
      "
    />
  </div>
</template>

<script setup lang="ts">
  import {
    criminalApprDetailType,
    criminalParticipantType,
  } from '@/types/criminal/jsonTypes';
  import { computed, defineProps } from 'vue';
  import Accused from './Accused.vue';

  const props = defineProps<{
    accused: criminalParticipantType[];
    activityClass: string;
    appearances: criminalApprDetailType[];
  }>();
  const count = props.accused.length;
  const titleText = computed(() =>
    props.activityClass === 'Adult' ? `Accused (${count})` : `Youth (${count})`
  );
</script>
