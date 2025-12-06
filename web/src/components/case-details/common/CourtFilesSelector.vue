<template>
  <v-card :class="getBannerStyle" variant="flat">
    <v-container class="pb-0">
      <v-tabs v-model="activeTab" center-active>
        <template v-for="file in props.files" :key="file.key">
          <v-tab
            class="text-body-1 mb-0"
            selected-class="active-tab"
            :rounded="fileNumber == file.key ? 't-lg' : false"
            :ripple="false"
            :to="file.key"
            hide-slider
            base-color="white"
            color="black"
            border="e-md"
            @click="fileNumber = file.key"
            >{{ file.value }}
          </v-tab>
        </template>
      </v-tabs>
    </v-container>
  </v-card>
</template>
<script setup lang="ts">
  import { KeyValueInfo } from '@/types/common';
  import { getCourtClassStyle } from '@/utils/utils';
  import { computed, PropType, ref } from 'vue';

  const fileNumber = defineModel<string>();
  const props = defineProps({
    files: { type: Array as PropType<KeyValueInfo[]>, default: () => [] },
    courtClass: { type: String, default: null },
  });
  const activeTab = ref(() => fileNumber.value);
  const getBannerStyle = computed(() => getCourtClassStyle(props.courtClass));
</script>

<style scoped>
  .active-tab {
    background-color: var(--bg-white-500) !important;
    color: var(--text-black-500) !important;
  }
  .active-tab:hover {
    color: var(--text-black-500) !important;
  }
  .v-tab {
    text-decoration: none !important;
  }
  /* Dark mode styling overrides */
  .v-theme--dark .active-tab {
    background-color: var(--bg-black) !important;
    color: var(--text-white-500) !important;
  }
  .v-theme--dark .active-tab:hover {
    color: var(--text-white-500) !important;
  }

  .criminal {
    background-color: var(--bg-blue-300);
  }

  .small-claims {
    background-color: var(--bg-purple-300);
  }

  .family {
    background-color: var(--bg-green-300);
  }
</style>
