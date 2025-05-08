<template>
  <div>
    <v-chip
      v-for="{ key, description, value } in data"
      :key
      rounded="lg"
      variant="outlined"
      size="small"
      :class="[classOverride, { selected: value }]"
      selected-class="selected"
    >
      {{ key }}
      <v-tooltip activator="parent" location="bottom">
        {{ description }}
      </v-tooltip>
    </v-chip>
  </div>
</template>

<script setup lang="ts">
  import { FileMarkerEnum } from '@/types/common';

  const props = defineProps<{
    classOverride: string;
    markers: { marker: FileMarkerEnum; value: string }[];
  }>();

  const allMarkers = [
    { marker: FileMarkerEnum.CNT, description: 'Continuation' },
    { marker: FileMarkerEnum.CPA, description: 'Child Protection Act' },
    { marker: FileMarkerEnum.DO, description: 'Detained Order' },
    { marker: FileMarkerEnum.IC, description: 'In Custody' },
    { marker: FileMarkerEnum.INT, description: 'Interpreter Required' },
    { marker: FileMarkerEnum.LOCT, description: 'Lack of Court Time' },
    { marker: FileMarkerEnum.W, description: 'Warrant' },
  ];

  const data = props.markers.map((m) => {
    const match = allMarkers.find((am) => am.marker === m.marker);
    return {
      ...m,
      ...match,
      key: Object.entries(FileMarkerEnum).find(
        ([, val]) => val === m.marker
      )?.[0],
      value: m.value === 'Y',
    };
  });
</script>
<style scoped>
  .v-chip {
    cursor: default;
  }
  .selected {
    background-color: #183a4a !important;
    color: white !important;
  }
</style>
