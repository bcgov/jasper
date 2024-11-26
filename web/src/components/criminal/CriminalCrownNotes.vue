<template>
  <b-card bg-variant="white" no-body>
    <div>
      <h3 class="mx-4 font-weight-normal">Crown Notes to JCM</h3>
      <hr class="mx-3 bg-light" style="height: 5px" />
    </div>
    <b-card v-if="!(crownNotes.length > 0)" no-body>
      <span class="text-muted ml-4 mb-5"> No crown notes to JCM. </span>
    </b-card>

    <b-card
      bg-variant="white"
      v-if="isMounted && crownNotes.length > 0"
      no-body
      class="mx-3 mb-5"
    >
      <b-table
        borderless
        :items="crownNotes"
        :fields="fields"
        thead-class="d-none"
        responsive="sm"
        striped
      >
        <template
          v-for="(field, index) in fields"
          v-slot:[`cell(${field.key})`]="data"
        >
          <span v-bind:key="index" style="white-space: pre">
            {{ data.value }}</span
          >
        </template>
      </b-table>
    </b-card>
  </b-card>
</template>

<script lang="ts">
  import { useCriminalFileStore } from '@/stores';
  import { criminalCrownNotesInfoType } from '@/types/criminal';
  import { defineComponent, onMounted, ref } from 'vue';

  export default defineComponent({
    setup() {
      const criminalFileStore = useCriminalFileStore();

      const crownNotes = ref<criminalCrownNotesInfoType[]>([]);
      const isMounted = ref(false);

      const fields = [{ key: 'crownNotes', label: 'Crown Notes' }];

      const getCrownNotes = () => {
        const data = criminalFileStore.criminalFileInformation.detailsData;

        if (data.trialRemark.length > 0) {
          for (const note of data.trialRemark) {
            const crownNote = {} as criminalCrownNotesInfoType;
            crownNote.crownNotes = note.commentTxt;
            crownNotes.value.push(crownNote);
          }
        }
        isMounted.value = true;
      };

      onMounted(() => {
        getCrownNotes();
      });

      return {
        isMounted,
        crownNotes,
        fields,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
