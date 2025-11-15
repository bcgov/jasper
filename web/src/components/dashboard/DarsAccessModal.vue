<template>
  <v-dialog v-model="darsStore.isModalVisible" max-width="1000" persistent>
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
                label="Location"
                :loading="isLoadingLocations"
                placeholder="Select a location"
                :rules="[(v) => !!v || 'Location is required']"
                :disabled="isSearching"
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
  import { DarsService, type DarsLogNote } from '@/services/DarsService';
  import { LocationService } from '@/services/LocationService';
  import { useCommonStore } from '@/stores/CommonStore';
  import { useDarsStore } from '@/stores/DarsStore';
  import { useSnackbarStore } from '@/stores/SnackbarStore';
  import type { LocationInfo } from '@/types/courtlist';
  import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';
  import { mdiClose } from '@mdi/js';
  import { computed, inject, ref, watch } from 'vue';

  const snackbarStore = useSnackbarStore();
  const commonStore = useCommonStore();
  const darsStore = useDarsStore();
  const formRef = ref();
  const isSearching = ref(false);
  const searchResults = ref<DarsLogNote[]>([]);
  const isLoadingLocations = ref(false);
  const currentLocation = ref<LocationInfo | null>(null);

  const locations = computed<LocationInfo[]>(() => {
    return commonStore.courtRoomsAndLocations
      .filter((location) => location.active)
      .map((location) => ({
        name: location.name,
        shortName: location.shortName,
        code: location.code,
        locationId: location.locationId,
        active: location.active,
        courtRooms: location.courtRooms,
        agencyIdentifierCd: location.agencyIdentifierCd,
      }));
  });

  const searchParams = {
    get date() {
      return darsStore.searchDate;
    },
    set date(value: Date | null) {
      darsStore.searchDate = value;
    },
    get location() {
      return currentLocation.value;
    },
    set location(value: LocationInfo | null) {
      const newLocationId = value?.locationId || null;
      const currentLocationId = darsStore.searchLocationId;

      if (String(newLocationId) !== String(currentLocationId)) {
        currentLocation.value = value;
        darsStore.searchRoom = ''; // Clear room when location changes
        darsStore.searchLocationId = newLocationId;
      }
    },
    get room() {
      return darsStore.searchRoom;
    },
    set room(value: string) {
      darsStore.searchRoom = value;
    },
  };

  const darsService = inject<DarsService>('darsService');
  const locationsService = inject<LocationService>('locationService');

  const LOCATION_LOAD_ERROR =
    'Failed to load court locations. Please try again.';
  const NO_RECORDINGS_MESSAGE =
    'No audio recordings found for the specified criteria.';
  const SEARCH_ERROR_MESSAGE =
    'An error occurred while searching for recordings.';

  const preloadLocations = async () => {
    if (
      commonStore.courtRoomsAndLocations.length === 0 &&
      !isLoadingLocations.value
    ) {
      isLoadingLocations.value = true;
      try {
        const locationsData = await locationsService?.getLocations(true);
        if (locationsData?.length) {
          commonStore.setCourtRoomsAndLocations(locationsData);
        }
      } catch (error) {
        console.error('Failed to load locations:', error);
        snackbarStore.showSnackbar(
          LOCATION_LOAD_ERROR,
          'error',
          'Loading Error'
        );
      } finally {
        isLoadingLocations.value = false;
      }
    }
  };

  watch(
    () => darsStore.isModalVisible,
    (newValue) => {
      if (newValue) {
        preloadLocations();
        searchResults.value = [];
      }
    }
  );

  const updateCurrentLocation = () => {
    const locationId = darsStore.searchLocationId;
    const newLocation = locationId
      ? locations.value.find(
          (loc) => String(loc.locationId) === String(locationId)
        ) || null
      : null;

    if (
      String(newLocation?.locationId) !==
      String(currentLocation.value?.locationId)
    ) {
      currentLocation.value = newLocation;
    }
  };

  watch(() => darsStore.searchLocationId, updateCurrentLocation, {
    immediate: true,
  });

  watch(() => locations.value, updateCurrentLocation);

  const formatDateTime = (dateTime: string): string => {
    try {
      return formatDateToDDMMMYYYY(dateTime);
    } catch {
      return dateTime;
    }
  };

  const handleSearchResults = (results: DarsLogNote[]) => {
    if (!results?.length) {
      snackbarStore.showSnackbar(
        NO_RECORDINGS_MESSAGE,
        'warning',
        'No Results'
      );
      return [];
    }

    if (results.length === 1) {
      window.open(results[0].url, '_blank', 'noopener,noreferrer');
      snackbarStore.showSnackbar(
        'Opening audio recording in new tab.',
        'success',
        'Success'
      );
    }

    return results;
  };

  const handleSearch = async () => {
    const { valid } = await formRef.value.validate();
    if (!valid || !searchParams.date || !searchParams.location) {
      return;
    }

    isSearching.value = true;
    searchResults.value = [];

    try {
      const dateString = searchParams.date.toISOString().split('T')[0];
      const results = await darsService?.search(
        dateString,
        searchParams.location.agencyIdentifierCd,
        searchParams.room
      );

      searchResults.value = handleSearchResults(results || []);
    } catch (error: any) {
      console.error('Error searching DARS:', error);

      const isNotFound = error.response?.status === 404 || error.status === 404;
      const message = isNotFound
        ? NO_RECORDINGS_MESSAGE
        : `Error: ${error.message || SEARCH_ERROR_MESSAGE}`;
      const type = isNotFound ? 'warning' : 'error';
      const title = isNotFound ? 'No Results' : 'Search Failed';

      snackbarStore.showSnackbar(message, type, title);
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

  const close = () => {
    darsStore.closeModal();
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
