<template>
  <v-dialog v-model="show" persistent max-width="700">
    <v-card class="pa-4">
      <div v-if="loading" class="text-center py-6">
        <v-progress-circular indeterminate />
      </div>

      <v-alert v-else-if="error" type="error" variant="tonal">
        {{ error }}
      </v-alert>

      <template v-else-if="location">
        <v-card-title class="d-flex align-center px-0">
          <div class="d-flex align-center">
            <a
              v-if="locationUrl"
              class="mr-1 inherit-color d-inline-flex"
              :href="locationUrl"
              target="_blank"
              aria-label="Open location page"
            >
              <v-icon :icon="mdiOpenInNew" size="24" />
            </a>
            <h3 class="mb-0 font-weight-bold">{{ location.name }}</h3>
          </div>
          <v-spacer />
          <v-btn
            icon
            variant="text"
            density="comfortable"
            @click="show = false"
            aria-label="Close dialog"
          >
            <v-icon :icon="mdiClose" />
          </v-btn>
        </v-card-title>
        <v-card-text class="pa-0">
          <div class="mb-2">
            <div class="d-flex">
              <h5 class="font-weight-bold">Courthouse</h5>
              <v-chip
                class="ml-2"
                :color="location.isStaffed ? 'success' : 'warning'"
                :prepend-icon="mdiAccountCircleOutline"
                variant="outlined"
              >
                {{ location.isStaffed ? 'Staffed' : 'Unstaffed' }}
              </v-chip>
            </div>
            <p v-if="location.address1">
              {{ location.address1 }}
            </p>
            <p v-if="location.address2">
              {{ location.address2 }}
            </p>
            <p v-if="location.city">{{ location.city }}</p>
          </div>
          <v-divider class="border-opacity-100 my-2" />
          <div class="mb-2">
            <h5 class="font-weight-bold">Judicial Case Manager</h5>
            <div
              v-for="(p, i) in location.jcmPhones"
              :key="`jcm-phone-${i}`"
              class="d-flex align-start mb-1"
            >
              <v-icon :icon="mdiPhone" size="x-small" class="mr-1 mt-1" />
              <div>
                <a class="inherit-color" :href="`tel:${p.phone}`">{{
                  p.phone
                }}</a>
                <p v-if="p.notes" class="text-caption text-medium-emphasis">
                  {{ p.notes }}
                </p>
              </div>
            </div>
            <div
              v-for="(e, i) in location.emails"
              :key="`jcm-email-${i}`"
              class="d-flex align-start mb-1"
            >
              <v-icon :icon="mdiEmail" size="x-small" class="mr-1 mt-1" />
              <div>
                <a :href="`mailto:${e.email}`" class="inherit-color">{{
                  e.email
                }}</a>
                <p v-if="e.notes" class="text-caption text-medium-emphasis">
                  {{ e.notes }}
                </p>
              </div>
            </div>
          </div>

          <template v-if="location.iarSchedule || location.fxdSchedule">
            <div class="mb-2">
              <h5 class="font-weight-bold">JCM Schedule</h5>
              <div v-if="location.iarSchedule">
                <p class="font-weight-bold">IAR</p>
                <p class="schedule-value">{{ location.iarSchedule }}</p>
              </div>
              <div v-if="location.fxdSchedule">
                <p class="font-weight-bold">FXD</p>
                <p class="schedule-value">{{ location.fxdSchedule }}</p>
              </div>
            </div>
          </template>

          <template
            v-if="
              location.adultProbationOffice || location.youthProbationOffice
            "
          >
            <v-divider class="border-opacity-100 my-2" />
            <div class="mb-2">
              <h5 class="font-weight-bold">Nearest probation offices</h5>

              <div v-if="location.adultProbationOffice" class="mb-1">
                <span class="font-weight-bold">
                  {{ location.adultProbationOffice.name }}
                </span>
                <p v-if="adultProbationAddress">{{ adultProbationAddress }}</p>
                <p
                  v-for="(p, i) in location.adultProbationOffice.phones"
                  :key="`adult-phone-${i}`"
                >
                  <v-icon :icon="mdiPhone" size="x-small" class="mr-1" />
                  <a class="inherit-color" :href="`tel:${p.phone}`">{{
                    p.phone
                  }}</a>
                  <span v-if="p.notes" class="text-caption text-medium-emphasis"
                    >({{ p.notes }})</span
                  >
                </p>
                <p v-if="location.adultProbationOffice.tollFreePhone">
                  <v-icon :icon="mdiPhone" size="x-small" class="mr-1" />
                  <a
                    class="inherit-color"
                    :href="`tel:${location.adultProbationOffice.tollFreePhone.phone}`"
                    >{{ location.adultProbationOffice.tollFreePhone.phone }}</a
                  >
                  (Toll Free)
                </p>
              </div>

              <div v-if="location.youthProbationOffice">
                <span class="font-weight-bold">
                  {{ location.youthProbationOffice.name }}
                </span>
                <p v-if="youthProbationAddress">{{ youthProbationAddress }}</p>
                <p v-if="location.youthProbationOffice.phone">
                  <v-icon :icon="mdiPhone" size="x-small" class="mr-1" />
                  <a
                    class="inherit-color"
                    :href="`tel:${location.youthProbationOffice.phone.phone}`"
                    >{{ location.youthProbationOffice.phone.phone }}</a
                  >
                  <span
                    v-if="location.youthProbationOffice.phone.notes"
                    class="text-caption text-medium-emphasis"
                    >({{ location.youthProbationOffice.phone.notes }})</span
                  >
                </p>
              </div>
            </div>
          </template>
        </v-card-text>
      </template>

      <p v-else>No details available for this location.</p>

      <v-card-actions class="justify-end px-0 pt-4">
        <v-btn-secondary text="Close" @click="show = false" />
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
  import { CourtLocationService } from '@/services';
  import { CourtLocation } from '@/types/CourtLocation';
  import {
    mdiAccountCircleOutline,
    mdiClose,
    mdiEmail,
    mdiOpenInNew,
    mdiPhone,
  } from '@mdi/js';
  import { computed, inject, ref, watch } from 'vue';

  const props = defineProps<{ agencyIdCode?: string; locationUrl?: string }>();

  const show = defineModel<boolean>({ type: Boolean, required: true });

  const courtLocationService = inject<CourtLocationService>(
    'courtLocationService'
  );

  const location = ref<CourtLocation | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);

  const joinParts = (...parts: (string | undefined)[]): string =>
    parts.filter(Boolean).join(', ');

  const adultProbationAddress = computed(() =>
    joinParts(
      location.value?.adultProbationOffice?.address1,
      location.value?.adultProbationOffice?.address2
    )
  );

  const youthProbationAddress = computed(() =>
    joinParts(
      location.value?.youthProbationOffice?.address1,
      location.value?.youthProbationOffice?.address2,
      location.value?.youthProbationOffice?.city
    )
  );

  const loadLocation = async (): Promise<void> => {
    if (!props.agencyIdCode) {
      error.value = 'No location code available.';
      return;
    }

    loading.value = true;
    error.value = null;
    try {
      location.value =
        (await courtLocationService?.getCourtLocationByCode(
          props.agencyIdCode
        )) ?? null;
    } catch {
      error.value = 'Unable to load location details.';
    } finally {
      loading.value = false;
    }
  };

  watch(show, (isOpen) => {
    if (isOpen) {
      loadLocation();
    }
  });
</script>

<style scoped>
  a {
    text-decoration: underline;
  }

  .inherit-color,
  .inherit-color:hover {
    color: inherit;
  }

  .schedule-value {
    white-space: pre-line;
  }

  p {
    margin-bottom: 0;
  }
</style>
