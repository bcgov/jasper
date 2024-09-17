<template>
  <header id="appHeader" class="app-header" style="overflow: hidden;">

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
   
    <div style="display: flex; flex-direction: row; background-color: grey;">
      <div style="width: 80%;">
        <ul>
          <li>
            <a class="nav-link" href="/Home">Dashboard</a>
          </li>
          <li>
            <a class="nav-link">Court Calendar</a>
          </li>
          <li>
            <a class="nav-link">Court List</a>
          </li>
          <li>
            <a class="nav-link">Court File Search</a>
          </li>
          <li>
            <a class="nav-link">DARS</a>
          </li>
        </ul>
      </div>
      <div style="width: 20%;">
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

<style lang="scss">
  @import "../styles/header";
</style>

<script lang="ts">
import { UserInfo } from '@/types/common';
import "@store/modules/CommonInformation";
import { Component, Vue } from 'vue-property-decorator';
import { namespace } from "vuex-class";
const commonState = namespace("CommonInformation");

@Component
export default class NavigationTopbar extends Vue {   

  @commonState.State
  public userInfo!: UserInfo;

    userName = '';
    userRole = '';
    userTitle = '';

    mounted(){
      this.getUserInfo();
    }

    public getUserInfo(): void{
      // TODO: Get user information so this can be populated properly
      const tempUser = this.userInfo;
      this.userRole = tempUser.role;
      this.userName = tempUser.userType;
      this.userTitle = this.userRole + ' ' + this.userName;
    }
  }
</script>