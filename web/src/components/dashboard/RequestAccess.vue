<template>
  <b-card bg-variant="white" class="home">
    <b-card
      style="width: 50%; margin: 2rem auto 0 auto"
      bg-variant="light"
      class="text-center"
      body-class="access-body"
    >
      <b-row align-h="center">
        <img
          class="img-fluid ml-5 mr-2"
          src="../../../public/images/bcid-logo-en.svg"
          width="177"
          height="44"
          alt="B.C. Government Logo"
        />
      </b-row>
      <b-row align-h="center">
        <h2 class="mt-4">Request Access To JASPER:</h2>
      </b-row>
      <b-row class="mx-auto my-0 p-0">
        <b-form-group class="mx-auto my-4" style="width: 20rem">
          <label class="h6 m-0 p-0" for="email"
            >Email:<span class="text-danger">*</span></label
          >
          <b-form-input
            id="email"
            size="sm"
            v-model="selectedEmail"
            placeholder="Enter Your Email"
            style="text-align: center"
            :disabled="isSubmitted || isUserInvalid || isUserDisabled"
            v-b-tooltip.hover
            title="This email is associated to the account you logged in with. You should not change this email unless instructed."
          >
          </b-form-input>
        </b-form-group>
      </b-row>
      <b-button
        variant="primary"
        class="py-2 px-5"
        @click="requestAccess()"
        :disabled="isSubmitted || isUserInvalid || isUserDisabled"
      >
        {{ isSubmitted ? 'Submitted' : 'Submit Your Request' }}</b-button
      >
      <b-row class="mx-auto my-4 p-0">
        <b-badge
          v-if="isSubmitted"
          class="px-5 mx-auto shared-badge bg-success"
        >
          Your request has been submitted!<br />
          We will get back to you soon.
        </b-badge>
        <b-badge
          v-if="isUserDisabled"
          class="px-5 mx-auto shared-badge bg-warning"
        >
          Your user has been disabled.<br />
          Please contact the JASPER admin if you require access.
        </b-badge>
        <b-badge
          v-if="isUserInvalid"
          class="px-5 mx-auto shared-badge bg-warning"
        >
          Warning, you do not have valid access to JASPER.<br />
          Please contact the JASPER admin to correct your account.
        </b-badge>
      </b-row>
    </b-card>
  </b-card>
</template>

<script lang="ts">
  import { UserService } from '@/services/UserService';
  import { useCommonStore } from '@/stores';
  import { useSnackbarStore } from '@/stores/SnackbarStore';
  import { CustomAPIError, isPositiveInteger } from '@/utils/utils';
  import axios, { AxiosError } from 'axios';
  import _ from 'underscore';
  import { inject, onMounted, ref } from 'vue';

  export default {
    name: 'AccessRequest',
    setup() {
      const commonStore = useCommonStore();
      const snackBarStore = useSnackbarStore();
      const selectedEmail = ref<string>(commonStore.userInfo?.email ?? '');
      const isUserDisabled = ref(false);
      const isUserInvalid = ref(false);
      const isSubmitted = ref(false);
      const userService = inject<UserService>('userService');

      const requestAccess = async (): Promise<void> => {
        if (!_.isEmpty(selectedEmail.value)) {
          try {
            const accessRequest = await userService?.requestAccess(
              selectedEmail.value
            );
            if (accessRequest?.email === selectedEmail.value) {
              isSubmitted.value = true;
            } else {
              throw Error();
            }
          } catch (error) {
            if (
              error instanceof CustomAPIError &&
              axios.isAxiosError(
                (error as CustomAPIError<AxiosError>).originalError
              )
            ) {
              snackBarStore.showSnackbar(
                `${(error as CustomAPIError<AxiosError<[]>>)?.originalError?.response?.data?.join(' ') ?? ''}`,
                '#b84157',
                'Unable to submit access request'
              );
            }
          }
        }
      };

      const checkForEmail = async (): Promise<void> => {
        if (!_.isEmpty(selectedEmail.value)) {
          const user = await userService?.getByEmail(selectedEmail.value);
          if (user?.email === selectedEmail.value) {
            if (!user?.isActive && isPositiveInteger(user?.roles?.length)) {
              isUserDisabled.value = true;
            } else if (user?.isPendingRegistration) {
              isSubmitted.value = true;
            } else {
              isUserInvalid.value = true;
            }
          }
        }
      };

      onMounted(async () => {
        await checkForEmail();
      });

      return {
        selectedEmail,
        isSubmitted,
        userService,
        snackBarStore,
        isUserDisabled,
        isUserInvalid,
        requestAccess,
        checkForEmail,
      };
    },
  };
</script>

<style>
  .card {
    border: white;
  }

  .btn.disabled {
    cursor: default;
  }

  .shared-badge {
    font-size: 16px;
    font-weight: unset;
    white-space: wrap;
  }

  .access-body {
    background-color: var(--bg-blue-100);
  }
</style>
