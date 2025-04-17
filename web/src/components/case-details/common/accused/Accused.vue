<template>
  <v-card variant="text" class="my-3 pb-5">
    <v-row>
      <v-col cols="12">
        <v-chip
          rounded="lg"
          color="#d9e5f4"
          variant="flat"
          class="w-100 justify-center align-center"
        >
          {{ accused.lastNm?.toUpperCase() }},
          {{ accused.givenNm?.toUpperCase() }}
        </v-chip>
      </v-col>
    </v-row>

    <v-row class="mx-1 mt-2">
      <v-col cols="6" class="data-label">Ban</v-col>
      <v-col>
        <div v-for="(bans, banType, index) in groupedBans" :key="banType">
          <span style="color: #e30e0e">
            {{ banType }} ({{ (bans ?? []).length }})
          </span>
          <v-icon
            v-if="index === 0"
            class="ml-1"
            color="primary"
            :icon="mdiInformationSlabCircleOutline"
            @click="showBanModal = true"
          />
        </div>
      </v-col>
    </v-row>
    <v-row class="mx-1 mt-2">
      <v-col cols="6" class="data-label">DOB</v-col>
      <v-col>{{ formatDateToDDMMMYYYY(accused.birthDt) }}</v-col>
    </v-row>
    <v-row class="mx-1 mt-2">
      <v-col cols="6" class="data-label">Counsel</v-col>
      <v-col>{{ counselName }}</v-col>
    </v-row>
    <v-row class="mx-1 mt-2">
      <v-col cols="6" class="data-label">Counsel Desig. Filed</v-col>
      <v-col>{{ accused.designatedCounselYN }}</v-col>
    </v-row>
    <!-- Comment these fields until we have more information on them -->
    <!-- <v-row class="mx-1 mt-2">
      <v-col cols="6" class="data-label">Bail status</v-col>
      <v-col></v-col>
    </v-row>
    <v-row class="mx-1 mt-2">
      <v-col cols="6" class="data-label">Plea</v-col>
      <v-col></v-col>
    </v-row> -->
    <v-row class="mx-1 mt-2">
      <v-col cols="6" class="data-label">Appearances</v-col>
      <v-col>{{ appearances.length }}</v-col>
    </v-row>
  </v-card>
  <Bans v-if="showBanModal" :bans="accused.ban" v-model="showBanModal" />
</template>

<script setup lang="ts">
  import { criminalParticipantType } from '@/types/criminal/jsonTypes';
  import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';
  import { mdiInformationSlabCircleOutline } from '@mdi/js';
  import { computed, defineProps, ref } from 'vue';
  import Bans from './Bans.vue';

  const props = defineProps<{
    accused: criminalParticipantType;
    appearances: any;
  }>();
  const showBanModal = ref(false);
  const counselName = computed(() =>
    props.accused.counselLastNm && props.accused.counselGivenNm
      ? `${props.accused.counselLastNm.toUpperCase()}, ${props.accused.counselGivenNm}`
      : ''
  );
  const groupedBans = computed(() =>
    props.accused.ban.reduce(
      (acc, ban) => {
        const { banTypeDescription } = ban;
        acc[banTypeDescription] = acc[banTypeDescription] || [];
        acc[banTypeDescription].push(ban);
        return acc;
      },
      {} as Record<string, typeof props.accused.ban>
    )
  );
</script>
