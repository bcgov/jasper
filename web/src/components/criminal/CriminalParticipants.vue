<template>
  <b-card bg-variant="white" no-body>
    <div>
      <h3 class="mx-4 font-weight-normal">
        Participants ({{ numberOfParticipants }})
      </h3>
      <hr class="mx-3 bg-light" style="height: 5px" />
    </div>

    <b-card no-body class="mx-3 mb-5">
      <b-table
        :items="participantList"
        :fields="fields"
        :sort-by.sync="sortBy"
        :sort-desc.sync="sortDesc"
        :no-sort-reset="true"
        sort-icon-left
        borderless
        small
        responsive="sm"
      >
        <template
          v-for="(field, index) in fields"
          v-slot:[`head(${field.key})`]="data"
        >
          <b v-bind:key="index" :class="field.headerStyle"> {{ data.label }}</b>
        </template>

        <template
          v-for="(field, index) in fields"
          v-slot:[`cell(${field.key})`]="data"
        >
          <b-badge
            class="mt-1"
            :style="data.field.cellStyle"
            variant="white"
            v-bind:key="index"
          >
            {{ data.value }}
          </b-badge>
        </template>

        <template v-slot:cell(name)="data">
          <b-dropdown
            size="sm"
            style="height: 35px"
            no-caret
            variant="text-info"
          >
            <template v-slot:button-content>
              <b-button
                :variant="
                  data.item.charges.length > 0
                    ? 'outline-primary text-info'
                    : 'white'
                "
                :disabled="data.item.charges.length == 0"
                :style="data.field.cellStyle"
                size="sm"
              >
                {{ data.value }}
                <b-icon
                  v-if="data.item.charges.length > 0"
                  class="ml-1"
                  icon="caret-down-fill"
                  font-scale="1"
                ></b-icon>
              </b-button>
            </template>
            <b-dropdown-text variant="white text-danger"
              >Charges</b-dropdown-text
            >
            <b-dropdown-divider></b-dropdown-divider>
            <b-dropdown-item-button
              disabled
              v-for="(file, index) in data.item.charges"
              :key="index"
            >
              <b>{{ file.code }}</b> &mdash; {{ file.description }}
            </b-dropdown-item-button>
          </b-dropdown>
        </template>

        <template v-slot:cell(status)="data">
          <b-badge
            v-for="(field, index) in data.value"
            :key="index"
            class="mr-1 mt-2"
            :style="data.field.cellStyle"
            v-b-tooltip.hover
            :title="field.key"
          >
            {{ field.abbr }}
          </b-badge>
        </template>
      </b-table>
    </b-card>
  </b-card>
</template>

<script lang="ts">
  import { useCriminalFileStore } from '@/stores';
  import { participantListInfoType } from '@/types/criminal';
  import { defineComponent, onMounted, ref } from 'vue';

  export default defineComponent({
    setup() {
      const criminalFileStore = useCriminalFileStore();

      const participantList = ref<participantListInfoType[]>([]);

      const isMounted = ref(false);
      const numberOfParticipants = ref(0);
      const sortBy = ref('name');
      const sortDesc = ref(false);

      const fields = [
        {
          key: 'name',
          label: 'Name',
          sortable: true,
          tdClass: 'border-top',
          headerStyle: 'text-primary',
          cellStyle:
            'transform: translate(-10px,-4px); border:0px; font-size:16px',
        },
        {
          key: 'dob',
          label: 'D.O.B.',
          sortable: false,
          tdClass: 'border-top',
          headerStyle: 'text',
          cellStyle: 'font-weight:normal; font-size:16px',
        },
        {
          key: 'status',
          label: 'Status',
          sortable: false,
          tdClass: 'border-top',
          headerStyle: 'text',
          cellStyle: 'font-weight: normal; font-size: 14px;',
        },
        {
          key: 'counsel',
          label: 'Counsel',
          sortable: false,
          tdClass: 'border-top',
          headerStyle: 'text',
          cellStyle: 'font-weight:normal; font-size:16px',
        },
        {
          key: 'counselDesignationFiled',
          label: 'Counsel Designation Filed',
          sortable: false,
          tdClass: 'border-top',
          headerStyle: 'text',
          cellStyle: 'font-weight:normal; font-size:16px',
        },
      ];

      onMounted(() => {
        getParticipants();
      });

      const getParticipants = () => {
        participantList.value = [
          ...criminalFileStore.criminalFileInformation.participantList,
        ];
        numberOfParticipants.value = participantList.value.length;
        isMounted.value = true;
      };

      return {
        numberOfParticipants,
        fields,
        sortBy,
        participantList,
        sortDesc,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
