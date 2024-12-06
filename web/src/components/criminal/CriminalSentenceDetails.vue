<template>
  <b-card v-if="isMounted" no-body>
    <b-table-simple small responsive borderless>
      <b-thead>
        <b-tr>
          <b-th
            v-for="(head, index) in fields"
            v-bind:key="index"
            variant="info"
            :class="head.headerStyle"
          >
            <b-icon-caret-up-fill
              v-if="
                (index < 2 || index == 3) &&
                dateSortDir == 'asc' &&
                sortBy == head.key
              "
              @click="sortClick(head)"
            ></b-icon-caret-up-fill>
            <b-icon-caret-down-fill
              v-else-if="
                (index < 2 || index == 3) &&
                dateSortDir == 'desc' &&
                sortBy == head.key
              "
              @click="sortClick(head)"
            ></b-icon-caret-down-fill>
            <b-icon-caret-up
              v-else-if="(index < 2 || index == 3) && sortBy != head.key"
              @click="sortClick(head)"
            ></b-icon-caret-up>
            {{ head.key }}
          </b-th>
        </b-tr>
      </b-thead>

      <b-tbody
        v-for="(counts, inx) in SortedParticipantSentencesCounts"
        v-bind:key="inx"
      >
        <b-tr
          v-for="(sentence, index) in counts.sentenceDispositionType"
          v-bind:key="index"
          :style="getRowStyle(index)"
        >
          <b-td :rowspan="counts.len" v-if="index == 0">{{
            beautifyDate(counts.date)
          }}</b-td>
          <b-td :rowspan="counts.len" v-if="index == 0">{{
            counts.count
          }}</b-td>

          <b-td>
            <b> {{ counts.chargeIssueCd[index] }} </b>
            <span v-if="counts.chargeIssueCd[index]">
              &mdash;
              <b-badge
                variant="secondary"
                v-b-tooltip.hover.right
                :title="counts.chargeIssueDscFull[index]"
              >
                {{ counts.chargeIssueDsc[index] }}
              </b-badge>
            </span>
          </b-td>

          <b-td :rowspan="counts.len" v-if="index == 0">
            <b-badge
              v-if="counts.finding"
              variant="secondary"
              v-b-tooltip.hover.right
              :title="counts.findingDsc"
            >
              {{ counts.finding }}
            </b-badge>
          </b-td>

          <b-td>
            <b-badge
              v-if="sentence"
              variant="secondary"
              v-b-tooltip.hover.right
              :title="counts.sentenceDsc[index]"
            >
              {{ sentence }}
            </b-badge>
          </b-td>

          <b-td> {{ counts.term[index] }} </b-td>
          <b-td> {{ counts.amount[index] }} </b-td>
          <b-td> {{ beautifyDate(counts.dueDateUntil[index]) }} </b-td>
          <b-td> {{ beautifyDate(counts.effectiveDate[index]) }} </b-td>
        </b-tr>
      </b-tbody>
    </b-table-simple>
  </b-card>
</template>

<script lang="ts">
  import { beautifyDate } from '@/filters';
  import { useCriminalFileStore } from '@/stores';
  import { participantSentencesInfoType } from '@/types/criminal';
  import * as _ from 'underscore';
  import { computed, defineComponent, onMounted, ref } from 'vue';

  export default defineComponent({
    setup() {
      const criminalFileStore = useCriminalFileStore();

      const sortBy = ref('date');
      const dateSortDir = ref('desc');
      const isMounted = ref(false);
      const selectedParticipant = ref(0);
      const participantSentences = ref<participantSentencesInfoType[]>([]);

      const baseField = {
        tdClass: 'border-top',
        headerStyle: 'text',
      };

      const fields = [
        {
          ...baseField,
          key: 'date',
          label: 'Date',
          sortable: true,
          headerStyle: 'text-primary',
        },
        {
          ...baseField,
          key: 'count',
          label: 'Count',
          sortable: true,
          headerStyle: 'text-primary',
        },
        {
          ...baseField,
          key: 'chargeIssue',
          label: 'Charge/Issue',
          sortable: false,
        },
        {
          ...baseField,
          key: 'finding',
          label: 'Finding',
          sortable: true,
          headerStyle: 'text-danger',
        },
        {
          ...baseField,
          key: 'sentenceDispositionType',
          label: 'Sentence/ Disposition Type',
          sortable: false,
        },
        {
          ...baseField,
          key: 'term',
          label: 'Term',
          sortable: false,
        },
        {
          ...baseField,
          key: 'amount',
          label: 'Amount',
          sortable: false,
        },
        {
          ...baseField,
          key: 'dueDateUntil',
          label: 'Due Date/ Until',
          sortable: false,
        },
        {
          ...baseField,
          key: 'effectiveDate',
          label: 'Effective Date',
          sortable: false,
        },
      ];

      const getParticipants = () => {
        participantSentences.value =
          criminalFileStore.criminalParticipantSentenceInformation.participantSentences;
        selectedParticipant.value =
          criminalFileStore.criminalParticipantSentenceInformation.selectedParticipant;
        isMounted.value = true;
      };

      onMounted(() => {
        getParticipants();
      });

      const SortedParticipantSentencesCounts = computed(() => {
        if (dateSortDir.value == 'desc')
          return _.sortBy(
            participantSentences[selectedParticipant.value].counts,
            sortBy
          ).reverse();
        else
          return _.sortBy(
            participantSentences[selectedParticipant.value].counts,
            sortBy
          );
      });

      // const NumberOfCounts = () => {
      //   return participantSentences[selectedParticipant.value].counts.length;
      // };

      const getRowStyle = (index) => {
        if (index == 0) return 'border-top : 1px solid #999;';
        return '';
      };

      const sortClick = (data) => {
        sortBy.value = data.key;
        if (dateSortDir.value == 'desc') dateSortDir.value = 'asc';
        else dateSortDir.value = 'desc';
      };

      return {
        isMounted,
        fields,
        dateSortDir,
        sortBy,
        sortClick,
        beautifyDate,
        SortedParticipantSentencesCounts,
        getRowStyle,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
