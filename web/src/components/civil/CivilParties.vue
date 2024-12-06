<template>
  <b-card bg-variant="white" no-body>
    <b-row cols="2">
      <b-col class="mt-2" md="6" cols="6">
        <div>
          <h3 class="mx-4 font-weight-normal">
            {{ leftRole }} ({{ numberOfLeftParties }})
          </h3>
          <hr class="mx-3 bg-light" style="height: 5px" />
        </div>

        <b-card
          bg-variant="white"
          style="max-height: 400px; overflow-y: auto"
          no-body
          class="mx-3 mb-5"
        >
          <b-table
            :items="leftPartiesInfo"
            :fields="fields"
            :no-sort-reset="true"
            sort-icon-left
            borderless
            striped
            small
            responsive="sm"
          >
            <template
              v-for="(field, index) in fields"
              v-slot:[`head(${field.key})`]="data"
            >
              <b v-bind:key="index" :class="field.headerStyle">
                {{ data.label }}</b
              >
            </template>
            <template
              v-for="(field, index) in fields"
              v-slot:[`cell(${field.key})`]="data"
            >
              <span
                v-bind:key="index"
                :style="field.cellStyle"
                v-if="data.field.key != 'counsel'"
              >
                {{ data.value }}
              </span>
              <span
                v-bind:key="index"
                :style="field.cellStyle"
                v-if="data.field.key == 'counsel'"
              >
                <span
                  v-for="(counsel, counselIndex) in data.value"
                  v-bind:key="counselIndex"
                  style="white-space: pre-line"
                >
                  CEIS: {{ counsel }}
                </span>
              </span>
            </template>
          </b-table>
        </b-card>
      </b-col>
      <b-col class="mt-2" md="6" cols="6">
        <div>
          <h3 class="mx-4 font-weight-normal">
            {{ rightRole }} ({{ numberOfRightParties }})
          </h3>
          <hr class="mx-3 bg-light" style="height: 5px" />
        </div>

        <b-card
          bg-variant="white"
          style="max-height: 400px; overflow-y: auto"
          no-body
          class="mx-3 mb-5"
        >
          <b-table
            :items="rightPartiesInfo"
            :fields="fields"
            :no-sort-reset="true"
            borderless
            striped
            small
            sort-icon-left
            responsive="sm"
          >
            <template
              v-for="(field, index) in fields"
              v-slot:[`head(${field.key})`]="data"
            >
              <b v-bind:key="index" :class="field.headerStyle">
                {{ data.label }}</b
              >
            </template>
            <template
              v-for="(field, index) in fields"
              v-slot:[`cell(${field.key})`]="data"
            >
              <span
                v-bind:key="index"
                :style="field.cellStyle"
                v-if="data.field.key != 'counsel'"
              >
                {{ data.value }}
              </span>
              <span
                v-bind:key="index"
                :style="field.cellStyle"
                v-if="data.field.key == 'counsel'"
              >
                <span
                  v-for="(counsel, counselIndex) in data.value"
                  v-bind:key="counselIndex"
                  style="white-space: pre-line"
                >
                  CEIS: {{ counsel }}<br />
                </span>
              </span>
            </template>
          </b-table>
        </b-card>
      </b-col>
    </b-row>
  </b-card>
</template>

<script lang="ts">
  import { useCivilFileStore } from '@/stores';
  import { partiesInfoType } from '@/types/civil';
  import { defineComponent, onMounted, ref } from 'vue';

  export default defineComponent({
    setup() {
      const civilFileStore = useCivilFileStore();

      const isMounted = ref(false);
      const leftPartiesInfo = ref<partiesInfoType[]>([]);
      const rightPartiesInfo = ref<partiesInfoType[]>([]);
      const leftRole = ref('');
      const rightRole = ref('');
      const numberOfLeftParties = ref(0);
      const numberOfRightParties = ref(0);

      const fields = [
        {
          key: 'name',
          label: 'Name',
          sortable: true,
          tdClass: 'border-top',
          headerStyle: 'text-primary',
          cellStyle: 'font-weight: bold; font-size: 14px;',
        },
        {
          key: 'role',
          label: 'Role',
          sortable: false,
          tdClass: 'border-top',
          headerStyle: 'text',
          cellStyle: 'font-size: 14px;',
        },
        {
          key: 'counsel',
          label: 'Counsel',
          sortable: false,
          tdClass: 'border-top',
          headerStyle: 'text',
          cellStyle: 'font-size: 14px;',
        },
      ];

      onMounted(() => {
        getParties();
      });

      const getParties = () => {
        leftRole.value =
          civilFileStore.civilFileInformation.detailsData.leftRoleDsc;
        rightRole.value =
          civilFileStore.civilFileInformation.detailsData.rightRoleDsc;
        leftPartiesInfo.value = [
          ...civilFileStore.civilFileInformation.leftPartiesInfo,
        ];
        rightPartiesInfo.value = [
          ...civilFileStore.civilFileInformation.rightPartiesInfo,
        ];
        numberOfLeftParties.value = leftPartiesInfo.value.length;
        numberOfRightParties.value = rightPartiesInfo.value.length;
        isMounted.value = true;
      };

      return {
        isMounted,
        leftRole,
        numberOfLeftParties,
        leftPartiesInfo,
        fields,
        rightRole,
        numberOfRightParties,
        rightPartiesInfo,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
