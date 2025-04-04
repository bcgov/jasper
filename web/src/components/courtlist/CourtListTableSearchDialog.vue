<template>
  <v-dialog v-model="showDialog" @update:modelValue="closeDialog">
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
            <v-col cols="12">
              <label for="reportType">Report type</label>
              <v-select
                id="reportType"
                :items="['Additions', 'Daily']"
                v-model="selectedReportType"
                placeholder="Select a Report Type"
                :rules="[(v) => !!v || 'Report type is required.']"
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
<script setup lang="ts">
  import { mdiClose } from '@mdi/js';
  import { ref } from 'vue';
  import { VForm } from 'vuetify/components';

  const showDialog = defineModel<boolean>('showDialog');
  const isFormValid = ref(false);

  const selectedReportType = ref('Daily');
  const form = ref<VForm | null>(null);

  const props = defineProps({
    onGenerate: { type: Function, default: () => {} },
  });

  const closeDialog = () => {
    selectedReportType.value = 'Daily';
    showDialog.value = false;
  };

  const generateReport = async () => {
    if (!form.value) {
      return;
    }

    const { valid: isValid } = await form.value.validate();
    if (!isValid) {
      return;
    }

    props.onGenerate(selectedReportType.value);
    closeDialog();
  };
</script>
<style scoped>
  .v-dialog {
    max-width: 500px;
  }

  .v-dialog .v-card {
    border-radius: 1rem;
  }
</style>
