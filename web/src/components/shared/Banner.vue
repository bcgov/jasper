<template>
  <v-banner :style="{ backgroundColor: bgColor, color: color }">
    <v-row class="my-1">
      <v-col>
        <h3>{{ title }}</h3>
      </v-col>
    </v-row>
  </v-banner>
</template>

<script setup>
  const props = defineProps({
    title: {
      type: String,
      default: true,
    },
    color: {
      type: String,
      required: true,
    },
    bgColor: {
      type: String,
      required: true,
    },
  });
</script>
