<template>
    <div class="container">
      <div class="col-10">
        <base-card>
          <template v-slot:title><i class="bi bi-gear me-1"></i>User Profile </template>
          <template v-slot:body>
            <div v-if="profile" class="profile">
              <div class="row">ID: {{ profile.id }}</div>
              <div class="row">Principal ID: {{ profile.principalId }}</div>
              <div class="row">Created: {{ profile.createdAt }}</div>
            </div>
            <div v-if="profile" class="claims">
              <span class="title">User Claims:</span>
              <ul>
                <li v-for="(value, name) in profile.claims">
                  {{ name }}: {{ value }}
                </li>
              </ul>
            </div>
          </template>
        </base-card>
      </div>
    </div>
  </template>
  
<script>
  import { ref } from "vue";
  import { useUserStore } from '../stores/userStore.js'
  import { minimalApiClient } from '../services/minimalApiClient.js'
  
  export default {
    components: { 
    },
    async setup() {

        const userStore = useUserStore();
  
        const profile = ref([]);
        profile.value = {
        };
  
        const profileResponse = await minimalApiClient.getCurrentUser(userStore.id_token);
        profile.value = profileResponse;
    
        return {
            profile
        };
    }
  };
</script>
  
  <style>
  .card {
    background-color: #555 !important;
  }
  .profile {
    margin-top: 20px;
  }
  
  .claims {
    margin-top: 20px;
    text-align: left;
  }
  
  .claims .title{
    font-weight: bold;
    text-align: left;
  }
  
  </style>
