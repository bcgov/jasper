<template>
  <v-list-group value="quick-links" density="compact">
    <template v-slot:activator="{ props }">
      <v-list-item v-bind="props" color="primary" rounded="shaped">
        <template v-slot:prepend>
          <v-icon :icon="mdiLinkVariant"></v-icon>
        </template>
        <v-list-item-title>Quick links</v-list-item-title>
      </v-list-item>
    </template>

    <v-skeleton-loader
      v-if="quickLinksLoading"
      type="list-item-three-line"
      :loading="quickLinksLoading"
    ></v-skeleton-loader>
    <v-list v-else class="pl-1">
      <v-list-group
        v-for="parent in menuItems"
        :key="parent.id"
        :value="parent.id"
        density="compact"
        :subgroup="true"
      >
        <template v-slot:activator="{ props }">
          <v-list-item v-bind="props" density="compact">
            <v-list-item-title
              v-text="parent.name"
              class="text-subtitle-1"
              style="font-family: inherit"
            ></v-list-item-title>
          </v-list-item>
        </template>
        <v-list-item
          v-for="child in parent.children"
          :key="child.id"
          :value="child.id"
          density="compact"
          :slim="true"
          :tile="true"
          class="text-subtitle-2"
          rounded="shaped"
          @click="handleChildClick(child)"
          >{{ child.name }}</v-list-item
        >
      </v-list-group>
    </v-list>
  </v-list-group>
</template>

<script setup lang="ts">
  import { QuickLinkService } from '@/services/QuickLinkService';
  import { QuickLink } from '@/types';
  import { mdiLinkVariant } from '@mdi/js';
  import { computed, inject, onMounted, ref } from 'vue';

  const quickLinksLoading = ref(false);
  const quickLinks = ref<QuickLink[]>([]);
  const quickLinkService = inject<QuickLinkService>('quickLinkService');

  onMounted(() => {
    quickLinksLoading.value = true;
    quickLinkService
      ?.getQuickLinks()
      .then((links) => {
        quickLinks.value = links;
      })
      .catch((error) => {
        console.error('Error fetching quick links:', error);
      })
      .finally(() => {
        quickLinksLoading.value = false;
      });
  });

  const menuItems = computed<QuickLink[]>(() => {
    // Get all parent items (where isMenu is true or parentName is null/empty)
    const parents = quickLinks.value
      .filter((link) => link.isMenu === true || !link.parentName)
      .sort((a, b) => a.order - b.order);

    return parents.map((parent) => ({
      ...parent,
      children: quickLinks.value
        .filter(
          (link) => link.parentName === parent.name && link.isMenu !== true
        )
        .sort((a, b) => a.order - b.order),
    }));
  });

  const handleChildClick = (child: QuickLink) => {
    if (child.url) {
      window.open(child.url, '_blank');
    }
  }
</script>
