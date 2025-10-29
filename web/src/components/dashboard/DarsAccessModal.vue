<template>
  <v-dialog v-model="isOpen" max-width="1000" persistent>
    <v-card>
      <v-card-title class="d-flex justify-space-between align-center">
        <span>DARS Access</span>
        <v-btn :icon="mdiClose" variant="text" @click="close"></v-btn>
      </v-card-title>

      <v-card-text>
        <v-form ref="formRef" @submit.prevent="handleSearch">
          <v-row>
            <v-col cols="4">
              <v-date-input
                v-model="searchParams.date"
                label="Date"
                prepend-icon=""
                prepend-inner-icon="$calendar"
                :rules="[(v) => !!v || 'Date is required']"
                :disabled="isSearching"
                required
              />
            </v-col>

            <v-col cols="4">
              <v-select
                v-model="searchParams.location"
                :items="locations"
                return-object
                item-title="shortName"
                item-value="locationId"
                label="Location"
                :loading="!isLocationDataLoaded"
                placeholder="Select a location"
                :rules="[(v) => !!v || 'Location is required']"
                :disabled="isSearching"
                @update:modelValue="handleLocationChange"
                required
              />
            </v-col>

            <v-col cols="4">
              <v-select
                v-model="searchParams.room"
                :items="
                  searchParams.location ? searchParams.location.courtRooms : []
                "
                item-title="room"
                item-value="room"
                label="Room"
                :disabled="!searchParams.location || isSearching"
                placeholder="Select a room"
                required
              />
            </v-col>
          </v-row>
          <v-row>
            <v-col cols="4">
              <action-buttons
                :showSearch="!isSearching"
                size="large"
                @reset="resetForm"
              />
            </v-col>
          </v-row>
        </v-form>

        <v-row v-if="isSearching" class="mt-4">
          <v-col cols="12" class="text-center">
            <v-progress-circular
              indeterminate
              color="primary"
              size="64"
            ></v-progress-circular>
            <p class="mt-2">Searching for audio recordings...</p>
          </v-col>
        </v-row>

        <v-row v-else-if="searchResults.length > 0" class="mt-4">
          <v-col cols="12">
            <v-alert
              v-if="searchResults.length > 1"
              type="info"
              border="start"
              class="mb-3"
            >
              Multiple audio recordings were found. Please select the one you
              would like to hear.
            </v-alert>
            <v-list
              lines="two"
              class="results-list"
              :max-height="searchResults.length > 10 ? '400px' : 'auto'"
            >
              <v-list-item
                v-for="(result, index) in searchResults"
                :key="index"
                :href="result.url"
                target="_blank"
                rel="noopener noreferrer"
                class="result-item"
              >
                <v-list-item-title class="text-primary">
                  {{
                    [
                      formatDateTime(result.date),
                      result.locationNm,
                      result.courtRoomCd,
                    ]
                      .filter((x) => x)
                      .join(' - ')
                  }}
                </v-list-item-title>
                <v-list-item-subtitle>
                  {{ result.fileName }}
                </v-list-item-subtitle>
              </v-list-item>
            </v-list>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
  import { LocationService } from '@/services';
  import { DarsService, type DarsLogNote } from '@/services/DarsService';
  import { useDarsStore } from '@/stores/DarsStore';
  import { useSnackbarStore } from '@/stores/SnackbarStore';
  import type { LocationInfo } from '@/types/courtlist';
  import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';
  import { mdiClose } from '@mdi/js';
  import { inject, onMounted, ref, watch } from 'vue';

  const props = defineProps<{
    modelValue: boolean;
    prefillDate?: Date | null;
    prefillLocationId?: number | null;
    prefillRoom?: string;
  }>();

  const emit = defineEmits<{
    'update:modelValue': [value: boolean];
  }>();

  const isOpen = ref(props.modelValue);
  const formRef = ref();
  const isSearching = ref(false);
  const isLocationDataLoaded = ref(false);
  const locations = ref<LocationInfo[]>([]);
  const searchResults = ref<DarsLogNote[]>([]);
  const snackbarStore = useSnackbarStore();
  const darsStore = useDarsStore();

  const searchParams = {
    get date() {
      return darsStore.searchDate;
    },
    set date(value: Date | null) {
      darsStore.searchDate = value;
    },
    get location() {
      return darsStore.searchLocation;
    },
    set location(value: LocationInfo | null) {
      darsStore.searchLocation = value;
    },
    get room() {
      return darsStore.searchRoom;
    },
    set room(value: string) {
      darsStore.searchRoom = value;
    },
  };

  const darsService = inject<DarsService>('darsService');
  const locationService = inject<LocationService>('locationService');

  watch(
    () => props.modelValue,
    (newValue) => {
      isOpen.value = newValue;
      if (newValue) {
        if (
          props.prefillDate !== undefined ||
          props.prefillLocationId !== undefined ||
          props.prefillRoom !== undefined
        ) {
          applyPrefillData();
          // TODO: if desired, kick-off search automatically here.
        }
        // Always clear search results when opening
        searchResults.value = [];
      }
    }
  );

  watch(isOpen, (newValue) => {
    if (!newValue) {
      searchResults.value = [];
      emit('update:modelValue', false);
    }
  });

  onMounted(async () => {
    await loadLocations();
  });

  const loadLocations = async () => {
    try {
      isLocationDataLoaded.value = false;
      locations.value = (await locationService?.getLocations(true)) || [];
      isLocationDataLoaded.value = true;
    } catch (error) {
      console.error('Error loading locations:', error);
      snackbarStore.showSnackbar(
        'Failed to load locations. Please try again.',
        'error',
        'Error'
      );
      isLocationDataLoaded.value = true;
    }
  };

  const handleLocationChange = () => {
    // Reset room when location changes
    darsStore.searchRoom = '';
  };

  const formatDateTime = (dateTime: string): string => {
    try {
      return formatDateToDDMMMYYYY(dateTime);
    } catch {
      return dateTime;
    }
  };

  const handleSearch = async () => {
    // Validate form
    const { valid } = await formRef.value.validate();
    if (!valid) {
      return;
    }

    if (!searchParams.date || !searchParams.location) {
      return;
    }

    isSearching.value = true;
    searchResults.value = [];

    try {
      const dateString = searchParams.date.toISOString().split('T')[0];
      const results = await darsService?.search(
        dateString,
        Number.parseInt(searchParams.location.locationId, 10),
        searchParams.room
      );

      if (!results || results.length === 0) {
        snackbarStore.showSnackbar(
          'No audio recordings found for the specified criteria.',
          'warning',
          'No Results'
        );
        searchResults.value = [];
      } else if (results.length === 1) {
        window.open(results[0].url, '_blank', 'noopener,noreferrer');
        snackbarStore.showSnackbar(
          'Opening audio recording in new tab.',
          'success',
          'Success'
        );
        // Still show the result in the list for reference
        searchResults.value = results;
      } else {
        // Multiple results - show in list
        searchResults.value = results;
      }
    } catch (error: any) {
      console.error('Error searching DARS:', error);

      // Handle 404 specifically
      if (error.response?.status === 404 || error.status === 404) {
        snackbarStore.showSnackbar(
          'No audio recordings found for the specified criteria.',
          'warning',
          'No Results'
        );
      } else {
        const errorMessage =
          error.message || 'An error occurred while searching for recordings.';
        snackbarStore.showSnackbar(
          `Error: ${errorMessage}`,
          'error',
          'Search Failed'
        );
      }
      searchResults.value = [];
    } finally {
      isSearching.value = false;
    }
  };

  const resetForm = () => {
    darsStore.resetSearchCriteria();
    searchResults.value = [];
    formRef.value?.resetValidation();
  };

  const applyPrefillData = () => {
    // Only apply prefill data if explicitly provided
    // Otherwise, keep the existing search criteria
    if (props.prefillDate !== undefined && props.prefillDate !== null) {
      darsStore.searchDate = props.prefillDate;
    }

    if (
      props.prefillLocationId !== undefined &&
      props.prefillLocationId !== null
    ) {
      // Find the matching location from the loaded locations (in order to populate list of rooms)
      const matchingLocation = locations.value.find(
        (loc) => Number.parseInt(loc.locationId, 10) === props.prefillLocationId
      );
      if (matchingLocation) {
        darsStore.searchLocation = matchingLocation;
      }
    }

    if (
      props.prefillRoom !== undefined &&
      props.prefillRoom !== null &&
      props.prefillRoom !== ''
    ) {
      darsStore.searchRoom = props.prefillRoom;
    }
  };

  const close = () => {
    isOpen.value = false;
    emit('update:modelValue', false);
  };
</script>

<style scoped>
  .results-list {
    overflow-y: auto;
    border: 1px solid rgba(0, 0, 0, 0.12);
    border-radius: 4px;
  }

  .result-item {
    border-bottom: 1px solid rgba(0, 0, 0, 0.08);
  }

  .result-item:last-child {
    border-bottom: none;
  }

  .result-item:hover {
    background-color: rgba(0, 0, 0, 0.04);
  }

  :deep(.v-list-item-title) {
    font-weight: 500;
  }
</style>
