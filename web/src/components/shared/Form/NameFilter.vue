<template>
  <v-select
    v-if="namesOnFile.length > 1"
    v-model="selectedName"
    :label
    :placeholder="'All ' + label"
    hide-details
    :items="namesOnFile"
  />
</template>

<script setup lang="ts">
  import { PersonType } from '@/types/criminal/jsonTypes';
  import { formatToFullName } from '@/utils/utils';
  import { computed } from 'vue';

  const props = defineProps({
    people: {
      type: Array<PersonType>,
      required: true,
    },
    label: {
      type: String,
      default: 'Accused',
    },
  });

  const selectedName = defineModel<string>();
  const namesOnFile = computed<string[]>(() => {
    const nameList = props.people?.map((app) =>
      formatToFullName(app.lastNm.trim(), app.givenNm.trim())
    );
    return [...new Set(nameList)];
  });
</script>
