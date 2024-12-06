<template>
  <b-card v-if="isMounted && userInfo.userType != 'vc'" no-body>
    <div>
      <h3 class="mx-4 font-weight-normal">Case Notes and Comments</h3>
      <hr class="mx-3 bg-light" style="height: 5px" />
    </div>
    <b-card class="mb-5" v-if="!notesExist">
      <span class="text-muted ml-4"> No notes or comments. </span>
    </b-card>
    <b-card v-if="notesExist" bg-variant="white" no-body class="mx-3 mb-5">
      <b-table
        :items="civilNotes"
        :fields="fields"
        thead-class="d-none"
        responsive="sm"
        borderless
        small
        striped
      >
        <template v-slot:cell(notesFieldName)="data">
          <span>
            <b> {{ data.value }}</b>
          </span>
        </template>
      </b-table>
    </b-card>
  </b-card>
</template>

<script lang="ts">
  import { useCivilFileStore, useCommonStore } from '@/stores';
  import { civilNotesType } from '@/types/civil';
  import { defineComponent, onMounted, ref } from 'vue';

  export default defineComponent({
    setup() {
      const commonStore = useCommonStore();
      const civilFileStore = useCivilFileStore();

      const civilNotes = ref<civilNotesType[]>([]);
      const isMounted = ref(false);
      const notesExist = ref(false);

      const fields = [
        {
          key: 'notesFieldName',
          label: 'Notes Field Name',
          tdClass: 'border-top',
        },
        { key: 'notesValue', label: 'Notes Value', tdClass: 'border-top' },
      ];

      const getCivilNotes = () => {
        const data = civilFileStore.civilFileInformation.detailsData;

        let notesInfo = {} as civilNotesType;
        notesInfo.notesFieldName = 'Trial Remark';
        notesInfo.notesValue = data.trialRemarkTxt ? data.trialRemarkTxt : '';
        civilNotes.value.push(notesInfo);
        notesInfo = {} as civilNotesType;

        notesInfo.notesFieldName = 'Comment To Judge';
        notesInfo.notesValue = data.commentToJudgeTxt
          ? data.commentToJudgeTxt
          : '';
        civilNotes.value.push(notesInfo);
        notesInfo = {} as civilNotesType;

        notesInfo.notesFieldName = 'File Comment';
        notesInfo.notesValue = data.fileCommentText ? data.fileCommentText : '';
        civilNotes.value.push(notesInfo);
        if (
          data.trialRemarkTxt ||
          data.commentToJudgeTxt ||
          data.fileCommentText
        ) {
          notesExist.value = true;
        }

        isMounted.value = true;
      };

      onMounted(() => {
        getCivilNotes();
      });

      return {
        isMounted,
        userInfo: commonStore.userInfo,
        notesExist,
        civilNotes,
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
