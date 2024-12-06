<template>
  <b-card bg-variant="white" no-body v-if="isMounted">
    <div>
      <hr class="mx-3 bg-light" style="height: 5px" />
    </div>

    <b-card bg-variant="white" no-body class="mx-3">
      <b-table
        :items="SortedParticipants"
        :fields="fields"
        thead-class="d-none"
        borderless
        small
        responsive="sm"
      >
        <template v-slot:cell(name)="data">
          <b-button
            size="sm"
            :style="data.field.cellStyle"
            @click="
              OpenDetails(data);
              data.toggleDetails();
            "
            :variant="
              !data.item.countsDisable
                ? 'outline-primary border-white text-info'
                : 'text-muted'
            "
            :disabled="data.item.countsDisable"
          >
            <b-icon-caret-right-fill
              v-if="!data.item['_showDetails']"
            ></b-icon-caret-right-fill>
            <b-icon-caret-down-fill
              v-if="data.item['_showDetails']"
            ></b-icon-caret-down-fill>
            {{ data.value }}
          </b-button>
        </template>

        <template v-slot:cell(judge)="data">
          <b-button
            size="sm"
            :style="data.field.cellStyle"
            @click="OpenOrderMadeDetails(data)"
            :variant="
              !data.item.orderMadeDisable
                ? 'outline-primary border-white text-info'
                : 'text-muted'
            "
            :disabled="data.item.orderMadeDisable"
            class="mr-2"
          >
            Order Made Details
          </b-button>
          <b-button
            size="sm"
            :style="data.field.cellStyle"
            @click="OpenJudgeRecommendation(data)"
            :variant="
              !data.item.recommendationDisable
                ? 'outline-primary border-white text-info'
                : 'text-muted'
            "
            :disabled="data.item.recommendationDisable"
            class="mr-2"
          >
            Judge's Recommendations
          </b-button>
        </template>

        <template v-slot:cell(count)="data">
          <b-button size="sm" disabled variant="white">
            <b> Counts ({{ data.item.counts.length }}) </b>
          </b-button>
        </template>

        <template v-slot:row-details>
          <b-card no-body bg-border="dark">
            <criminal-sentence-details />
          </b-card>
        </template>
      </b-table>
    </b-card>

    <b-modal
      v-model="showRecommendation"
      id="bv-modal-recommendation"
      hide-footer
    >
      <template v-slot:modal-title>
        <h2 class="mb-0">Recommendations</h2>
      </template>
      <b-table
        :items="SortedJudgesRecommendation"
        :fields="orderMadeFields"
        borderless
        small
      >
        <template v-slot:cell(date)="data">
          <b-button
            @click="data.toggleDetails()"
            :style="data.field.cellStyle"
            variant="outline-primary border-white text-info"
            size="sm"
          >
            <b-icon-caret-right-fill
              v-if="!data.item['_showDetails']"
            ></b-icon-caret-right-fill>
            <b-icon-caret-down-fill
              v-if="data.item['_showDetails']"
            ></b-icon-caret-down-fill>
            {{ data.item.formattedDate }}
          </b-button>
        </template>
        <template v-slot:row-details="row">
          <li
            v-for="(recommendation, inx) in row.item.judgeRecommendation"
            :key="inx"
            class="mx-3"
          >
            {{ recommendation }}
          </li>
        </template>
      </b-table>
      <!-- <b-button class="mt-3" @click="$bvModal.hide('bv-modal-recommendation')"
        >Close</b-button
      > -->
    </b-modal>

    <b-modal v-model="showOrderMade" id="bv-modal-ordermade" hide-footer>
      <template v-slot:modal-title>
        <h2 class="mb-0">Order Made Details</h2>
      </template>
      <b-table
        :items="SortedOrderMade"
        :fields="orderMadeFields"
        borderless
        small
      >
        <template v-slot:cell(date)="data">
          <b-button
            @click="data.toggleDetails()"
            :style="data.field.cellStyle"
            variant="outline-primary border-white text-info"
            size="sm"
          >
            <b-icon-caret-right-fill
              v-if="!data.item['_showDetails']"
            ></b-icon-caret-right-fill>
            <b-icon-caret-down-fill
              v-if="data.item['_showDetails']"
            ></b-icon-caret-down-fill>
            {{ data.item.formattedDate }}
          </b-button>
        </template>
        <template v-slot:row-details="row">
          <li
            v-for="(order, inx) in row.item.orderMade"
            :key="inx"
            class="mx-3"
          >
            {{ order }}
          </li>
        </template>
      </b-table>
      <!-- <b-button class="mt-3" @click="$bvModal.hide('bv-modal-ordermade')"
        >Close</b-button
      > -->
    </b-modal>
  </b-card>
</template>

<script lang="ts">
  import CriminalSentenceDetails from '@/components/criminal/CriminalSentenceDetails.vue';
  import { beautifyDate } from '@/filters';
  import { useCriminalFileStore } from '@/stores';
  import {
    countInfoType,
    participantListInfoType,
    participantSentencesDetailsInfoType,
    participantSentencesInfoType,
  } from '@/types/criminal';
  import { criminalParticipantType } from '@/types/criminal/jsonTypes';
  import * as _ from 'underscore';
  import { computed, defineComponent, onMounted, ref } from 'vue';

  export default defineComponent({
    components: {
      CriminalSentenceDetails,
    },
    setup() {
      //      const commonStore = useCommonStore();
      const criminalFileStore = useCriminalFileStore();

      const participantSentences = ref<participantSentencesInfoType[]>([]);
      const participantList = ref<participantListInfoType[]>([]);
      const participantJson = ref<criminalParticipantType[]>([]);

      const isMounted = ref(false);
      const showRecommendation = ref(false);
      const showOrderMade = ref(false);
      const orderMadeClickedParticipant = ref(0);
      const judgeRecomClickedParticipant = ref(0);

      const fields = [
        {
          key: 'name',
          label: 'Name',
          tdClass: 'border-bottom',
          cellStyle: 'font-size:16px',
        },
        { key: 'count', label: 'Count', tdClass: 'border-bottom' },
        {
          key: 'judge',
          label: 'Judge',
          tdClass: 'border-bottom',
          cellStyle: 'font-size:16px',
        },
      ];

      const orderMadeFields = [
        {
          key: 'date',
          label: 'Date',
          tdClass: 'border-top',
          headerStyle: 'text-primary',
          cellStyle: 'font-size:16px',
        },
        {
          key: 'count',
          label: 'Count',
          tdClass: 'border-top',
          headerStyle: 'text',
          cellStyle: 'text',
        },
      ];

      const getParticipants = () => {
        const data = criminalFileStore.criminalFileInformation.detailsData;
        participantJson.value = data.participant;
        participantList.value =
          criminalFileStore.criminalFileInformation.participantList;
        ExtractParticipantSentencesInfo();
        isMounted.value = true;
      };

      onMounted(() => {
        getParticipants();
      });

      const ExtractParticipantSentencesInfo = () => {
        for (const partIndex in participantList) {
          const partInfo = {} as participantSentencesInfoType;
          partInfo.index = participantList[partIndex].index;
          partInfo.lastName = participantList[partIndex].lastName;
          partInfo.name = participantList[partIndex].name;
          partInfo.countsJson = participantList[partIndex].countsJson;

          partInfo.counts = [];

          const counts: countInfoType[] = [];

          partInfo.orderMade = [];
          partInfo.orderMadeDisable = true;
          partInfo.judgesRecommendation = [];
          partInfo.recommendationDisable = true;

          for (const cnt of mergeSentences(partInfo.countsJson)) {
            const countInfo = {} as countInfoType;

            countInfo.date = cnt.appearanceDate
              ? cnt.appearanceDate.split(' ')[0]
              : '';
            countInfo.formattedDate = beautifyDate(countInfo.date);

            countInfo.finding = cnt.finding ? cnt.finding : '';
            countInfo.findingDsc = cnt.findingDsc ? cnt.findingDsc : '';

            countInfo.chargeIssueCd = [];
            countInfo.chargeIssueDsc = [];
            countInfo.chargeIssueDscFull = [];
            countInfo.count = cnt.countNumber;

            for (const charge of cnt.charge) {
              countInfo.chargeIssueCd.push(
                charge.chargeTxt ? charge.chargeTxt : ''
              );
              countInfo.chargeIssueDsc.push(
                charge.chargeDscTxt
                  ? charge.chargeDscTxt.length > 10
                    ? charge.chargeDscTxt.substr(0, 10) + ' ...'
                    : charge.chargeDscTxt
                  : ''
              );
              countInfo.chargeIssueDscFull.push(
                charge.chargeDscTxt ? charge.chargeDscTxt : ''
              );
            }

            countInfo.sentenceDispositionType = [];
            countInfo.sentenceDsc = [];
            countInfo.term = [];
            countInfo.amount = [];
            countInfo.dueDateUntil = [];
            countInfo.effectiveDate = [];
            countInfo.orderMade = [];
            countInfo.judgeRecommendation = [];

            countInfo.lenCharge = cnt.charge.length;
            countInfo.len = Math.max(cnt.sentence.length, countInfo.lenCharge);

            for (const sentence of cnt.sentence) {
              countInfo.sentenceDispositionType.push(sentence.sntpCd);
              countInfo.sentenceDsc.push(
                sentence.sentenceTypeDesc ? sentence.sentenceTypeDesc : ''
              );
              countInfo.term.push(
                sentence.sentTermPeriodQty
                  ? sentence.sentTermPeriodQty +
                      ' ' +
                      sentence.sentTermCd.replace('-', '')
                  : ''
              );
              countInfo.amount.push(
                sentence.sentMonetaryAmt ? sentence.sentMonetaryAmt : ''
              );
              countInfo.dueDateUntil.push(
                sentence.sentDueTtpDt ? sentence.sentDueTtpDt.split(' ')[0] : ''
              );
              countInfo.effectiveDate.push(
                sentence.sentEffectiveDt
                  ? sentence.sentEffectiveDt.split(' ')[0]
                  : ''
              );
              if (sentence.judgesRecommendation)
                countInfo.judgeRecommendation.push(
                  sentence.judgesRecommendation
                );

              if (sentence.sentDetailTxt)
                countInfo.orderMade.push(sentence.sentDetailTxt);
            }

            if (cnt.sentence.length < countInfo.len) {
              for (
                let loop = 0;
                loop < countInfo.len - cnt.sentence.length;
                loop++
              ) {
                countInfo.sentenceDispositionType.push('');
                countInfo.sentenceDsc.push('');
                countInfo.term.push('');
                countInfo.amount.push('');
                countInfo.dueDateUntil.push('');
                countInfo.effectiveDate.push('');
              }
            } else if (countInfo.lenCharge < countInfo.len) {
              for (
                let loop = 0;
                loop < countInfo.len - countInfo.lenCharge;
                loop++
              ) {
                countInfo.chargeIssueCd.push('');
                countInfo.chargeIssueDsc.push('');
                countInfo.chargeIssueDscFull.push('');
              }
            }

            counts.push(countInfo);
            if (countInfo.orderMade.length > 0) {
              partInfo.orderMade.push(countInfo);
              partInfo.orderMadeDisable = false;
            }

            if (countInfo.judgeRecommendation.length > 0) {
              partInfo.judgesRecommendation.push(countInfo);
              partInfo.recommendationDisable = false;
            }
          }
          partInfo.counts = counts;
          partInfo.countsDisable = counts.length > 0 ? false : true;

          participantSentences.value.push(partInfo);
        }
        const participantInfo = {} as participantSentencesDetailsInfoType;
        participantInfo.selectedParticipant = 0;
        participantInfo.participantSentences = participantSentences.value;
        criminalFileStore.updateCriminalParticipantSentenceInformation(
          participantInfo
        );
      };

      const mergeSentences = (counts) => {
        const groupedCounts = _.groupBy(counts, function (count: any) {
          const orderedSentences = _.sortBy(count.sentence, 'sntpCd');
          return (
            count.appearanceDate +
            count.finding +
            _.pluck(orderedSentences, 'sntpCd') +
            _.pluck(orderedSentences, 'sentTermPeriodQty') +
            _.pluck(orderedSentences, 'sentTermCd') +
            _.pluck(orderedSentences, 'sentMonetaryAmt') +
            _.pluck(orderedSentences, 'sentDueTtpDt') +
            _.pluck(orderedSentences, 'sentEffectiveDt')
          );
        });

        return _.map(groupedCounts, function (countGroup) {
          const mergedCount = countGroup[0];
          mergedCount.countNumber = _.uniq(
            _.sortBy(_.pluck(countGroup, 'countNumber'))
          ).join(', ');
          mergedCount.charge = _.chain(countGroup)
            .sortBy(function (sort) {
              return sort.countNumber;
            })
            .map(function (group) {
              return (
                group.countNumber +
                '|' +
                group.sectionTxt +
                '|' +
                group.sectionDscTxt
              );
            })
            .uniq()
            .map(function (charge) {
              return {
                countNum: charge.split('|')[0],
                chargeTxt: charge.split('|')[1],
                chargeDscTxt: charge.split('|')[2],
              };
            })
            .value();
          return mergedCount;
        });
      };

      const SortedParticipants = computed(() => {
        return _.sortBy(participantSentences.value, (participant) => {
          return participant.lastName ? participant.lastName.toUpperCase() : '';
        });
      });

      const SortedOrderMade = computed(() => {
        return _.sortBy(
          participantSentences[orderMadeClickedParticipant.value].orderMade,
          'date'
        ).reverse();
      });

      const SortedJudgesRecommendation = computed(() => {
        return _.sortBy(
          participantSentences[judgeRecomClickedParticipant.value]
            .judgesRecommendation,
          'date'
        ).reverse();
      });

      const OpenDetails = (data) => {
        if (!data.detailsShowing) {
          const participantInfo =
            criminalFileStore.criminalParticipantSentenceInformation;
          participantInfo.selectedParticipant = data.item.index;
          criminalFileStore.updateCriminalParticipantSentenceInformation(
            participantInfo
          );
        }
      };

      const OpenOrderMadeDetails = (data) => {
        orderMadeClickedParticipant.value = data.item.index;
        showOrderMade.value = true;
      };

      const OpenJudgeRecommendation = (data) => {
        judgeRecomClickedParticipant.value = data.item.index;
        showRecommendation.value = true;
      };

      return {
        isMounted,
        fields,
        SortedParticipants,
        OpenDetails,
        OpenOrderMadeDetails,
        showRecommendation,
        OpenJudgeRecommendation,
        SortedJudgesRecommendation,
        orderMadeFields,
        showOrderMade,
        SortedOrderMade,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: black;
  }
</style>
