<template>
  <!-- todo: Extract this out to more generic location -->
  <v-overlay :opacity="0.333" v-model="isLoading" />

  <v-card style="min-height: 40px" v-if="errorCode > 0 && errorCode == 403">
    <span> You are not authorized to access this page. </span>
  </v-card>
  <!------------------------------------------------------->
  <banner title="Court list" color="#183a4a" bgColor="#b4e6ff">
    <template #left v-if="bannerDate"
      >:
      <v-icon :icon="mdiChevronLeft" @click="AddDay(-1)" />
      <b>{{ shortHandDate }}</b>
      <v-icon :icon="mdiChevronRight" @click="AddDay(1)" />
    </template>
    <template #right>
      <v-btn @click="showDropdown = !showDropdown">
        Search by Courtroom & Date
      </v-btn>
    </template>
  </banner>

  <v-expand-transition>
    <v-card v-show="showDropdown" height="100" elevation="16">
      <court-list-search
        v-model:date="selectedDate"
        v-model:isSearching="searchingRequest"
        v-model:showDropdown="showDropdown"
        v-model:bannerDate="bannerDate"
        @courtListSearch="PopulateCards"
      />
    </v-card>
  </v-expand-transition>
  <v-skeleton-loader type="table" :loading="searchingRequest">
    <div />
  </v-skeleton-loader>
</template>

<script setup lang="ts">
  import { HttpService } from '@/services/HttpService';
  import { mdiChevronLeft, mdiChevronRight } from '@mdi/js';
  import { computed, inject, ref } from 'vue';

  const errorCode = ref(0);
  const searchingRequest = ref(false);
  const isLoading = ref(false);
  const selectedDate = ref(new Date());
  const bannerDate = ref<Date | null>(null);
  const showDropdown = ref(false);

  const shortHandDate = computed(() =>
    bannerDate.value
      ? bannerDate.value.toLocaleDateString('en-US', {
          weekday: 'long',
          day: '2-digit',
          month: 'long',
          year: 'numeric',
        })
      : ''
  );

  const httpService = inject<HttpService>('httpService');
  if (!httpService) {
    throw new Error('Service is undefined.');
  }

  const PopulateCards = (data: any) => {
    if (!data) {
      return;
    }
    // todo: map cards from retrieved data
  };

  const AddDay = (days: number) => {
    if (bannerDate.value) {
      bannerDate.value = new Date(
        bannerDate.value.setDate(bannerDate.value.getDate() + days)
      );
      selectedDate.value = bannerDate.value;
    }
  };
</script>
