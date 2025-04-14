<template>
  <v-card :class="getBannerStyle" variant="flat">
    <v-container class="pb-0">
      <v-row class="pb-3">
        <v-col cols="10" />
        <v-col>
          <v-btn base-color="white">View all documents</v-btn>
        </v-col>
      </v-row>
      <v-tabs v-model="activeTab">
        <template v-for="file in props.files" :key="file.key">
          <v-tab
            class="text-body-1 mb-0"
            selected-class="active-tab"
            rounded="t-lg"
            :ripple="false"
            :to="file.key"
            hide-slider
            base-color="white"
            color="black"
            @click="fileNumber = file.key"
            >{{ file.value }}</v-tab
          >
          <v-divider class="ms-2" inset vertical thickness="2"></v-divider>
        </template>
      </v-tabs>
    </v-container>
  </v-card>
</template>
<script setup lang="ts">
  import { KeyValueInfo } from '@/types/common';
  import { CourtClassEnum } from '@/types/courtFileSearch';
  import { PropType, ref, computed } from 'vue';

  const fileNumber = defineModel<string>();
  const props = defineProps({
    files: { type: Array as PropType<KeyValueInfo[]>, default: () => [] },
    courtClass: { type: String, default: null },
  });
  const activeTab = ref(() => fileNumber.value);

  const getBannerStyle = computed(() => {
    switch (props.courtClass) {
      case CourtClassEnum[CourtClassEnum.A]:
      case CourtClassEnum[CourtClassEnum.Y]:
      case CourtClassEnum[CourtClassEnum.T]:
        return 'criminal';
      case CourtClassEnum[CourtClassEnum.C]:
        return 'small-claims';
      case CourtClassEnum[CourtClassEnum.F]:
        return 'family';
      default:
        return 'criminal';
    }
  });
</script>

<style scoped>
  .active-tab {
    background-color: var(--bg-white) !important;
  }
  .active-tab:hover {
    color: var(--text-black) !important;
  }
  .v-tab {
    text-decoration: none !important;
  }
  /* Dark mode styling overrides */
  .v-theme--dark .active-tab {
    background-color: var(--bg-black) !important;
    color: var(--text-white) !important;
  }
  .v-theme--dark .active-tab:hover {
    color: var(--text-white) !important;
  }

  .criminal {
    background-color: var(--bg-blue);
  }

  .small-claims {
    background-color: var(--bg-purple);
  }

  .family {
    background-color: var(--bg-green);
  }
</style>
