<template>
  <div class="pt-4">
    <h5>{{ titleText }}</h5>
    <Accused
      v-for="accused in props.accused"
      :key="accused.partId"
      :accused="accused"
      :courtClassCd="props.courtClassCd"
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
  import { getEnumName } from '@/utils/utils';
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
      case getEnumName(CourtClassEnum, CourtClassEnum.A): // Adult
        title = 'Accused';
        break;
      case getEnumName(CourtClassEnum, CourtClassEnum.Y): // Youth
        title = 'Youth';
        break;
      default:
        title = 'Participants';
        break;
    }
    return `${title} (${count})`;
  });
</script>
