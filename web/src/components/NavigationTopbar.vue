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

    <div style="display: flex; flex-direction: row; background-color: grey">
      <div style="width: 80%">
        <ul>
          <li>
            <a class="nav-link" href="/Dashboard">Dashboard</a>
          </li>
          <li>
            <a class="nav-link">Court Calendar</a>
          </li>
          <li>
            <a class="nav-link" href="/court-list">Court List</a>
          </li>
          <li>
            <a class="nav-link" href="/court-file-search">Court File Search</a>
          </li>
          <li>
            <a class="nav-link">DARS</a>
          </li>
        </ul>
      </div>
      <div style="width: 20%">
        <ul>
          <li>
            <a class="nav-link">Settings</a>
          </li>
          <li>
            <a class="nav-link">Quick Links</a>
          </li>
        </ul>
      </div>
    </div>
    <h2>Welcome {{ userTitle }}</h2>
  </header>
</template>

<script lang="ts">
  import { defineComponent, onMounted } from 'vue';

  export default defineComponent({
    name: 'NavigationTopbar',
    setup() {
      const userTitle = '';

      // Lifecycle hook to fetch user information
      onMounted(() => {
        getUserInfo();
      });

      // Mock method to populate user information
      const getUserInfo = () => {
        console.log('User info loaded:');
      };

      return {
        userTitle,
      };
    },
  });
</script>

<style lang="scss">
  @import '../styles/header';
</style>
