<template>
  <v-tab :value="value" :to="`/${value}`">
    <v-badge
      v-if="priorityCount > 0 && regularCount > 0"
      data-testid="order-combo-badge"
      :class="{
        'badge-pulse': pulseActive,
        'order-combo-badge': true,
      }"
      offset-x="10"
      offset-y="-12"
    >
      <template #badge>
        <span
          class="capsule-half priority"
          :title="`${priorityCount} priority ${label} pending`"
        >
          {{ priorityCount }}
        </span>
        <span
          class="capsule-half regular"
          :title="`${regularCount} regular ${label} pending`"
        >
          {{ regularCount }}
        </span>
      </template>
      {{ title }}
    </v-badge>
    <v-badge
      v-else-if="priorityCount > 0"
      data-testid="priority-badge"
      :content="priorityCount"
      :class="{ 'badge-pulse': pulseActive, 'priority-badge': true }"
      color="var(--bg-red-500)"
      :title="`${priorityCount} priority ${label} pending`"
      offset-x="-10"
      offset-y="-10"
    >
      {{ title }}
    </v-badge>
    <v-badge
      v-else-if="regularCount > 0"
      data-testid="regular-badge"
      :content="regularCount"
      :class="{ 'badge-pulse': pulseActive, 'regular-badge': true }"
      color="var(--bg-green-500)"
      :title="`${regularCount} regular ${label} pending`"
      offset-x="-10"
      offset-y="-10"
    >
      {{ title }}
    </v-badge>
    <template v-else>{{ title }}</template>
  </v-tab>
</template>

<script setup lang="ts">
  defineProps<{
    value: string;
    title: string;
    label: string;
    priorityCount: number;
    regularCount: number;
    pulseActive: boolean;
  }>();
</script>

<style scoped>
  .badge-pulse :deep(.v-badge__badge) {
    animation: badge-pop 0.35s ease-out;
  }

  .priority-badge :deep(.v-badge__badge),
  .regular-badge :deep(.v-badge__badge) {
    min-width: auto;
    height: 16px;
    overflow: hidden;
    color: var(--bg-white-500);
  }

  .order-combo-badge :deep(.v-badge__badge) {
    padding: 0;
    min-width: auto;
    height: 16px;
    overflow: hidden;
    background: transparent;
  }

  .order-combo-badge :deep(.v-badge__badge) .capsule-half {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 16px;
    height: 100%;
    padding: 0 6px;
    font-size: 0.75rem;
    line-height: 1;
    color: var(--bg-white-500);
  }

  .order-combo-badge :deep(.v-badge__badge) .capsule-half.priority {
    background-color: var(--bg-red-500);
  }

  .order-combo-badge :deep(.v-badge__badge) .capsule-half.regular {
    background-color: var(--bg-green-500);
  }

  @keyframes badge-pop {
    0% {
      transform: scale(1);
    }
    45% {
      transform: scale(1.2);
    }
    100% {
      transform: scale(1);
    }
  }
</style>
