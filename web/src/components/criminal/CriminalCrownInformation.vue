<template>
  <b-card v-if="isMounted" no-body>
    <div>
      <h3 class="mx-4 font-weight-normal">Crown Information</h3>
      <hr class="mx-3 bg-light" style="height: 5px" />
    </div>
    <b-card bg-variant="white" no-body class="mx-3 mb-5">
      <b-table
        :items="crownInformation"
        :fields="fields"
        thead-class="d-none"
        responsive="sm"
        borderless
        small
        striped
      >
        <template v-slot:cell(crownInfoValue)="data">
          <span v-if="data.item.crownInfoFieldName == 'Crown Assigned'">
            <b
              v-for="(assignee, index) in data.item.crownInfoValue.split('+')"
              v-bind:key="index"
              style="white-space: pre-line"
              >{{ assignee }} <br
            /></b>
          </span>
          <span v-if="data.item.crownInfoFieldName != 'Crown Assigned'">
            <b> {{ data.value }}</b>
          </span>
        </template>
      </b-table>
    </b-card>
  </b-card>
</template>

<script lang="ts">
  import { useCommonStore, useCriminalFileStore } from '@/stores';
  import { criminalCrownInformationInfoType } from '@/types/criminal';
  import { defineComponent, onMounted, ref } from 'vue';

  export default defineComponent({
    setup() {
      const commonStore = useCommonStore();
      const criminalFileStore = useCriminalFileStore();

      const crownInformation = ref<criminalCrownInformationInfoType[]>([]);
      const isMounted = ref(false);
      const fields = [
        {
          key: 'crownInfoFieldName',
          tdClass: 'border-top',
          label: 'Crown Info Field Name',
        },
        {
          key: 'crownInfoValue',
          tdClass: 'border-top',
          label: 'Crown Info Value',
        },
      ];

      const getCrownInfo = () => {
        const data = criminalFileStore.criminalFileInformation.detailsData;
        let assignedCrown = '';
        if (data.crown.length > 0) {
          for (const assignee of data.crown) {
            if (assignee.assigned) {
              commonStore.updateDisplayName({
                lastName: assignee.lastNm,
                givenName: assignee.givenNm,
              });
              assignedCrown += commonStore.displayName + '+';
            }
          }
          assignedCrown = assignedCrown.substr(0, assignedCrown.length - 1);
        }
        let crownInfo = {} as criminalCrownInformationInfoType;
        crownInfo.crownInfoFieldName = 'Crown Assigned';
        crownInfo.crownInfoValue = assignedCrown;
        crownInformation.value.push(crownInfo);
        crownInfo = {} as criminalCrownInformationInfoType;
        crownInfo.crownInfoFieldName = 'Crown Time Estimate';
        if (data.crownEstimateLenQty) {
          if (data.crownEstimateLenQty == 1) {
            crownInfo.crownInfoValue =
              data.crownEstimateLenQty +
              ' ' +
              data.crownEstimateLenDsc.replace('s', '');
          } else {
            crownInfo.crownInfoValue =
              data.crownEstimateLenQty + ' ' + data.crownEstimateLenDsc;
          }
        } else {
          crownInfo.crownInfoValue = '';
        }
        crownInformation.value.push(crownInfo);
        crownInfo = {} as criminalCrownInformationInfoType;
        crownInfo.crownInfoFieldName = 'Case Age';
        crownInfo.crownInfoValue = data.caseAgeDays
          ? data.caseAgeDays + ' Days'
          : '';
        crownInformation.value.push(crownInfo);
        crownInfo = {} as criminalCrownInformationInfoType;
        crownInfo.crownInfoFieldName = 'Approved By';
        if (data.approvedByAgencyCd) {
          crownInfo.crownInfoValue = data.approvedByPartNm
            ? data.approvedByAgencyCd + ' - ' + data.approvedByPartNm
            : data.approvedByAgencyCd + ' -';
        } else {
          crownInfo.crownInfoValue = '';
        }
        crownInformation.value.push(crownInfo);
        isMounted.value = true;
      };

      onMounted(() => {
        getCrownInfo();
      });

      return {
        isMounted,
        fields,
        crownInformation,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
