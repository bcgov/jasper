<template>
  <v-skeleton-loader
    v-if="isLoading"
    :loading="isLoading"
    type="table"
  ></v-skeleton-loader>
  <div class="my-4 mx-2" v-else>
    <v-expansion-panels
      class="mb-3"
      bg-color="var(--bg-gray-500)"
      :flat="true"
      multiple
    >
      <!-- Pending Orders Panel -->
      <v-expansion-panel>
        <v-expansion-panel-title>
          <h5 class="m-0">
            For signing ({{
              pendingOrders.length > 0 ? `(${pendingOrders.length})` : ''
            }})
          </h5>
        </v-expansion-panel-title>
        <v-expansion-panel-text> </v-expansion-panel-text>
      </v-expansion-panel>
      <v-expansion-panel collapsed>
        <v-expansion-panel-title>
          <h5 class="m-0">
            Completed ({{
              completedOrders.length > 0 ? `(${completedOrders.length})` : ''
            }})
          </h5>
        </v-expansion-panel-title>
        <v-expansion-panel-text> </v-expansion-panel-text>
      </v-expansion-panel>
    </v-expansion-panels>
  </div>
</template>
<script lang="ts" setup>
  const isLoading = ref(true);
</script>
