<template>
  <v-tooltip
    v-model="showTooltip"
    :open-on-hover="false"
    close-on-back
    close-on-content-click
    open-on-click
    location="right"
    close-delay="1000"
  >
    <template v-slot:activator="{ props }">
      <v-btn
        v-bind="props"
        size="small"
        class="ma-2"
        :icon="mdiContentCopy"
        @click="copyToClipBoard(text)"
      >
      </v-btn>
    </template>
    <span>✅Copied!</span>
  </v-tooltip>
</template>

<script setup lang="ts">
  import { mdiContentCopy } from '@mdi/js';
  import { ref, watch } from 'vue';

  defineProps<{
    text: string;
  }>();
  const showTooltip = ref(false);

  watch(showTooltip, (val) => {
    if (val) {
      setTimeout(() => {
        showTooltip.value = false;
      }, 1000);
    }
  });

  const copyToClipBoard = (textToCopy: string) => {
    navigator.clipboard.writeText(textToCopy);
  };
</script>
