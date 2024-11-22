<template>
  <header id="appHeader" class="app-header" style="overflow: hidden">
    <b-navbar toggleable="lg">
      <b-navbar-brand href="https://www2.gov.bc.ca">
        <img
          class="img-fluid d-none d-md-block"
          src="../../public/images/bcid-logo-rev-en.svg"
          width="177"
          height="44"
          alt="B.C. Government Logo"
        />
        <img
          class="img-fluid d-md-none"
          src="../../public/images/bcid-symbol-rev.svg"
          width="63"
          height="44"
          alt="B.C. Government Logo"
        />
      </b-navbar-brand>
    </b-navbar>
  </header>
</template>

<script lang="ts">
  import { defineComponent } from 'vue';

  export default defineComponent({});
</script>
