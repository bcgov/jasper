<template>
  <v-dialog v-model="showDialogComputed">
    <v-card rounded="md" class="pa-3">
      <v-card-title
        ><div class="d-flex justify-space-between align-center w-100">
          <span>Print Court List</span>
          <v-btn icon @click="closeDialog">
            <v-icon :icon="mdiClose" size="32" />
          </v-btn>
        </div>
      </v-card-title>
      <v-card-text>
        <v-form ref="form" v-model="isFormValid">
          <v-row no-gutters>
            <v-col cols="12" class="mb-2">
              <label for="type">Type</label>
              <v-select
                id="type"
                :items="types"
                item-title="shortDesc"
                item-value="code"
                v-model="selectedType"
                placeholder="Select a Type"
                :rules="[(v) => !!v || 'Type is required.']"
              />
            </v-col>
            <v-col cols="12" v-if="showReportTypeComputed">
              <label for="reportType">Report type</label>
              <v-select
                id="reportType"
                :items="['Additions', 'Daily']"
                v-model="selectedReportType"
                placeholder="Select a Report Type"
                :rules="[(v) => !!v || 'Report type is required.']"
              />
            </v-col>
            <v-col cols="12" v-if="showAdditionsComputed">
              <label for="additions">Additions</label>
              <v-select
                id="additions"
                :items="additionsOptions"
                item-title="text"
                item-value="value"
                v-model="selectedAdditions"
                :rules="[(v) => !!v || 'Additions is required.']"
              />
            </v-col>
          </v-row>
        </v-form>
      </v-card-text>
      <v-card-actions class="d-flex justify-center">
        <v-btn-tertiary type="submit" @click="generateReport"
          >Print</v-btn-tertiary
        >
        <v-btn-secondary @click="closeDialog">Close</v-btn-secondary>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
<style scoped>
  .v-dialog {
    max-width: 500px;
  }

  .v-dialog .v-card {
    border-radius: 1rem;
  }
</style>
<script setup lang="ts">
  import { DivisionEnum, LookupCode } from '@/types/common';
  import { mdiClose } from '@mdi/js';
  import { computed, ref } from 'vue';
  import { VForm } from 'vuetify/lib/components/index.mjs';

  const isFormValid = ref(false);
  const selectedType = ref(null);
  const selectedReportType = ref('Daily');
  const selectedAdditions = ref(null);
  const form = ref<VForm | null>(null);

  const additionsOptions = [
    { text: 'Yes', value: 'Y' },
    { text: 'No', value: 'N' },
  ];

  const props = defineProps<{
    showDialog: boolean;
    types: LookupCode[];
    onGenerateClicked: (
      type: string,
      reportType: string,
      additions: string
    ) => void;
  }>();

  const emit = defineEmits<(e: 'update:showDialog', value: boolean) => void>();

  const showDialogComputed = computed({
    get: () => props.showDialog,
    set: (value) => emit('update:showDialog', value),
  });

  const showReportTypeComputed = computed(() => {
    const c = props.types.find((c) => c.code === selectedType.value);
    return selectedType.value !== null && c!.longDesc == DivisionEnum.CRIMINAL;
  });

  const showAdditionsComputed = computed(() => {
    const c = props.types.find((c) => c.code === selectedType.value);
    return selectedType.value !== null && c!.longDesc == DivisionEnum.CIVIL;
  });

  const closeDialog = () => {
    selectedReportType.value = 'Daily';
    selectedAdditions.value = null;
    selectedType.value = null;
    emit('update:showDialog', false);
  };

  const generateReport = async () => {
    if (!form.value) {
      return;
    }

    const { valid: isValid } = await form.value.validate();

    if (!isValid) {
      return;
    }

    props.onGenerateClicked(
      selectedType.value!,
      selectedReportType.value,
      selectedAdditions.value!
    );
    closeDialog();
  };
</script>
