<template>
  <div class="pt-4">
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
  import { CourtClassEnum } from '@/types/common';
  import {
    criminalApprDetailType,
    criminalParticipantType,
  } from '@/types/criminal/jsonTypes';
  import { computed, defineProps } from 'vue';
  import Accused from './Accused.vue';

  const props = defineProps<{
    accused: criminalParticipantType[];
    courtClassCd: string;
    appearances: criminalApprDetailType[];
  }>();
  const count = props.accused.length;

  const titleText = computed(() => {
    let title = '';
    switch (props.courtClassCd) {
      case CourtClassEnum[CourtClassEnum.A]:
        title = 'Accused';
        break;
      case CourtClassEnum[CourtClassEnum.Y]:
        title = 'Youth';
        break;
      default:
        title = 'Participants';
        break;
    }
    return `${title} (${count})`;
  });
</script>
