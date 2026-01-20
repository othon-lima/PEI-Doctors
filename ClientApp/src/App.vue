<script setup lang="ts">
import { ref } from 'vue'

const message = ref('PEI Doctors Monitor')
const status = ref('')

async function triggerScrape() {
  status.value = 'Scraping...'
  try {
    const response = await fetch('/api/doctors/scrape', {
      method: 'POST'
    })
    const data = await response.json()
    status.value = data.message
  } catch (error) {
    status.value = 'Error: ' + error
  }
}
</script>

<template>
  <div class="container">
    <h1>{{ message }}</h1>
    
    <div class="actions">
      <button @click="triggerScrape">Trigger New Scrape</button>
    </div>
    
    <div v-if="status" class="status">
      {{ status }}
    </div>
  </div>
</template>

<style scoped>
.container {
  max-width: 800px;
  margin: 0 auto;
  padding: 2rem;
  text-align: center;
  font-family: sans-serif;
}

.actions {
  margin: 2rem 0;
}

button {
  padding: 0.5rem 1rem;
  font-size: 1rem;
  cursor: pointer;
}

.status {
  margin-top: 1rem;
  padding: 1rem;
  background-color: #f0f0f0;
  border-radius: 4px;
}
</style>
