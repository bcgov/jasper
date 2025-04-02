<template>
  <div>
    <v-dialog v-model="show" width="auto">
      <v-card>
        <v-card-title>Ban Details</v-card-title>
        <v-table>
          <thead>
            <tr>
              <th scope="col" class="text-left">TYPE</th>
              <th scope="col" class="text-left">ORDER DATE</th>
              <th scope="col" class="text-left">ACT</th>
              <th scope="col" class="text-left">SECT.</th>
              <th scope="col" class="text-left">SUB</th>
              <th scope="col" class="text-left">DESCRIPTION</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="ban in bans" :key="ban.banStatuteId">
              <td>{{ ban.banTypeDescription }}</td>
              <td>
                {{ formatDateToDDMMMYYYY(ban.banOrderedDate) }}
              </td>
              <td>{{ ban.banTypeAct }}</td>
              <td>{{ ban.banTypeSection }}</td>
              <td>{{ ban.banTypeSubSection }}</td>
              <td>{{ ban.banCommentText }}</td>
            </tr>
          </tbody>
        </v-table>
        <v-card-actions class="justify-start my-3 mx-1">
          <v-btn-secondary text="Close" @click="show = false" />
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
  import { formatDateToDDMMMYYYY } from '@/utils/utils';

  defineProps<{ bans: any }>();

  const show = defineModel<boolean>({ type: Boolean, required: true });
</script>
