<template>
  <v-snackbar
    v-model="snackbarStore.isVisible"
    :timeout="snackbarStore.timeout"
    :color="resolvedSnackbarStyle.color"
    :class="resolvedSnackbarClass"
    location="bottom right"
  >
    <div class="snackbar-content">
      <div class="snackbar-header">
        <h3>{{ snackbarStore.title }}</h3>
        <v-icon
          class="snackbar-close-icon"
          :icon="mdiCloseCircle"
          @click="close"
        />
      </div>

      <div class="snackbar-text">
        <span>{{ snackbarStore.message }}</span>
      </div>

      <div class="snackbar-actions">
        <v-btn
          v-if="snackbarStore.actionLabel && snackbarStore.actionHandler"
          variant="outlined"
          size="small"
          class="snackbar-action-button mx-1"
          @click="runAction"
        >
          {{ snackbarStore.actionLabel }}
        </v-btn>
      </div>
    </div>
  </v-snackbar>
</template>

<script setup lang="ts">
  import { useSnackbarStore } from '@/stores/SnackbarStore';
  import { mdiCloseCircle } from '@mdi/js';
  import { computed } from 'vue';

  type SnackbarStyle = 'warning' | 'success' | 'error' | 'light' | 'dark';

  const snackbarStyleMap: Record<
    SnackbarStyle,
    { color?: string; className?: string }
  > = {
    warning: { color: 'warning' },
    success: { color: 'success' },
    error: { color: 'error' },
    light: { className: 'snackbar-light' },
    dark: { className: 'snackbar-dark' },
  };

  const snackbarStore = useSnackbarStore();

  const resolvedSnackbarStyle = computed(() => {
    const configuredStyle = snackbarStore.color as SnackbarStyle;
    return snackbarStyleMap[configuredStyle] ?? { color: snackbarStore.color };
  });

  const resolvedSnackbarClass = computed(() => [
    'snackbar',
    resolvedSnackbarStyle.value.className,
  ]);

  const runAction = () => {
    try {
      snackbarStore.actionHandler?.();
    } finally {
      snackbarStore.hideSnackbar();
    }
  };

  const close = () => {
    snackbarStore.hideSnackbar();
  };
</script>

<style scoped>
  .snackbar-light :deep(.v-snackbar__wrapper) {
    background-color: var(--bg-white-500);
    color: var(--text-black-500);
  }

  .snackbar-dark :deep(.v-snackbar__wrapper) {
    background-color: var(--bg-blue-800);
    color: var(--text-white-500);
  }

  .snackbar-content {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
  }

  .snackbar-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 0.75rem;
  }

  .snackbar-text {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .snackbar-close-icon {
    color: inherit;
    flex-shrink: 0;
    margin-top: 0.125rem;
  }

  .snackbar-actions {
    display: flex;
    justify-content: flex-end;
    align-items: center;
  }

  .snackbar-action-button {
    color: inherit !important;
    border-color: currentColor !important;
  }

  .snackbar-action-button :deep(.v-btn__content) {
    color: inherit;
  }
</style>
