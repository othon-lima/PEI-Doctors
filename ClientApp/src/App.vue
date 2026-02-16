<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { VueDatePicker } from '@vuepic/vue-datepicker'
import '@vuepic/vue-datepicker/dist/main.css'

interface Doctor {
  rg: string
  rl: string
  prl: string
  irc: boolean
  sid: number
  c: string
  atlr: string
  cv: string
  pdcv: string
  oa: string
}

const availableDates = ref<string[]>([])
const selectedDate = ref('')
const actualDate = ref('')
const pickerDate = ref<string | null>(null)
const doctors = ref<Doctor[]>([])
const loading = ref(false)
const status = ref('')
const searchQuery = ref('')
const selectedRegion = ref('')

const PEI_COUNTY_MAP: Record<string, string> = {
  'Charlottetown': 'Queens',
  'Stratford': 'Queens',
  'Hunter River': 'Queens',
  'Crapaud': 'Queens',
  'Cornwall': 'Queens',
  'Mount Herbert': 'Queens',
  'North Rustico': 'Queens',
  'Medical Education': 'Queens',
  'Montague': 'Kings',
  'Souris': 'Kings',
  'Murray River': 'Kings',
  'Summerside': 'Prince',
  'Alberton': 'Prince',
  "O'Leary": 'Prince',
  'Kensington': 'Prince',
  'Tignish': 'Prince',
  'Tyne Valley': 'Prince',
}

function getCounty(oa: string): string {
  if (!oa.includes('Prince Edward Island')) return 'Outside PEI'
  const m = oa.match(/(?:<br\/?>|<\/br>)\s*([^<,]+),\s*Prince Edward Island/)
  if (m) {
    const city = m[1].trim()
    if (PEI_COUNTY_MAP[city]) return PEI_COUNTY_MAP[city]
  }
  return 'Queens'
}
const isDark = ref(window.matchMedia('(prefers-color-scheme: dark)').matches)

window.matchMedia('(prefers-color-scheme: dark)')
  .addEventListener('change', e => isDark.value = e.matches)

const highlightedDates = computed(() => ({
  dates: availableDates.value.map(d => new Date(
    parseInt(d.substring(0, 4)),
    parseInt(d.substring(4, 6)) - 1,
    parseInt(d.substring(6, 8))
  ))
}))

const filteredDoctors = computed(() => {
  let result = doctors.value
  if (selectedRegion.value) {
    result = result.filter(d => getCounty(d.oa) === selectedRegion.value)
  }
  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    result = result.filter(d =>
      d.rl.toLowerCase().includes(q) ||
      d.prl.toLowerCase().includes(q) ||
      d.oa.toLowerCase().includes(q)
    )
  }
  return result
})

function onDatePicked(date: string | null) {
  if (!date) return
  selectedDate.value = date.replace(/-/g, '')
  loadDoctors()
}

function parseName(rl: string) {
  const match = rl.match(/^(.+?),\s*(.+?)\s*\((\w+)\)$/)
  if (!match) return { name: rl, regNumber: '' }
  return { name: `${match[2]} ${match[1]}`, regNumber: match[3] }
}

function parseSpecialty(prl: string) {
  return prl.replace(/<br\/?>/gi, ' — ')
}

function parseAddress(oa: string) {
  return oa
    .replace(/<br\/?>/gi, '\n')
    .replace(/<\/br>/gi, '')
    .replace(/<\/?b>/gi, '')
    .trim()
}

async function loadDates() {
  const response = await fetch('/api/doctors/dates')
  availableDates.value = await response.json()
  if (availableDates.value.length > 0) {
    selectedDate.value = availableDates.value[0]
    const d = availableDates.value[0]
    pickerDate.value = `${d.substring(0, 4)}-${d.substring(4, 6)}-${d.substring(6, 8)}`
    await loadDoctors()
  }
}

async function loadDoctors() {
  if (!selectedDate.value) return
  loading.value = true
  status.value = ''
  try {
    const response = await fetch(`/api/doctors/data/${selectedDate.value}`)
    const result = await response.json()
    actualDate.value = result.actualDate
    doctors.value = result.data.Records
    if (actualDate.value !== selectedDate.value) {
      const a = actualDate.value
      const formatted = `${a.substring(0, 4)}-${a.substring(4, 6)}-${a.substring(6, 8)}`
      status.value = `No data for selected date. Showing data from ${formatted}.`
    }
  } catch (error) {
    status.value = 'Error loading data: ' + error
    doctors.value = []
  } finally {
    loading.value = false
  }
}

async function triggerScrape() {
  status.value = 'Scraping...'
  try {
    const response = await fetch('/api/doctors/scrape', { method: 'POST' })
    const data = await response.json()
    status.value = data.message
    await loadDates()
  } catch (error) {
    status.value = 'Error: ' + error
  }
}

onMounted(loadDates)
</script>

<template>
  <div class="app">
    <header>
      <h1>PEI Doctors Monitor</h1>
      <div class="controls">
        <VueDatePicker
          v-model="pickerDate"
          :dark="isDark"
          :highlight="highlightedDates"
          :time-config="{ enableTimePicker: false }"
          model-type="yyyy-MM-dd"
          format="yyyy-MM-dd"
          auto-apply
          placeholder="Select a date"
          @update:model-value="onDatePicked"
        />
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Search doctors..."
          class="search-input"
        />
        <select v-model="selectedRegion" class="region-filter">
          <option value="">All Regions</option>
          <option value="Queens">Queens</option>
          <option value="Kings">Kings</option>
          <option value="Prince">Prince</option>
          <option value="Outside PEI">Outside PEI</option>
        </select>
        <button @click="triggerScrape" class="scrape-btn">Scrape Now</button>
      </div>
    </header>

    <div v-if="status" class="status-bar" :class="{ warning: actualDate !== selectedDate }">
      {{ status }}
    </div>

    <div v-if="loading" class="loading">Loading...</div>

    <div v-else-if="doctors.length" class="results">
      <p class="count">{{ filteredDoctors.length }} doctor{{ filteredDoctors.length !== 1 ? 's' : '' }} found</p>
      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Reg #</th>
            <th>Specialty</th>
            <th>Office Address</th>
            <th>Restricted</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="doc in filteredDoctors" :key="doc.rg" :class="{ restricted: doc.irc }">
            <td>{{ parseName(doc.rl).name }}</td>
            <td class="reg-num">{{ parseName(doc.rl).regNumber }}</td>
            <td>{{ parseSpecialty(doc.prl) }}</td>
            <td class="address">{{ parseAddress(doc.oa) }}</td>
            <td class="center">{{ doc.irc ? 'Yes' : '' }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-else-if="!loading" class="empty">
      No doctors data available.
    </div>
  </div>
</template>

<style scoped>
.app {
  max-width: 1200px;
  margin: 0 auto;
  padding: 1rem 2rem;
  text-align: left;
}

header {
  margin-bottom: 1rem;
}

header h1 {
  font-size: 1.8em;
  margin-bottom: 0.75rem;
}

.controls {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex-wrap: wrap;
}

.search-input {
  padding: 0.4rem 0.8rem;
  font-size: 0.95rem;
  border-radius: 6px;
  border: 1px solid #555;
  background: inherit;
  color: inherit;
  min-width: 200px;
}

.region-filter {
  padding: 0.4rem 0.8rem;
  font-size: 0.95rem;
  border-radius: 6px;
  border: 1px solid #555;
  background: inherit;
  color: inherit;
}

.region-filter option {
  background: #1a1a2e;
  color: #e0e0e0;
}

.scrape-btn {
  margin-left: auto;
}

.status-bar {
  padding: 0.5rem 1rem;
  background: #2a3a4a;
  border-radius: 6px;
  margin-bottom: 1rem;
  font-size: 0.9rem;
}

.status-bar.warning {
  background: #4a3a1a;
  border-left: 3px solid #e6a817;
}

.loading {
  text-align: center;
  padding: 3rem;
  font-size: 1.1rem;
  opacity: 0.7;
}

.count {
  margin: 0.5rem 0;
  font-size: 0.9rem;
  opacity: 0.7;
}

table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9rem;
}

thead th {
  text-align: left;
  padding: 0.6rem 0.8rem;
  border-bottom: 2px solid #444;
  white-space: nowrap;
}

tbody td {
  padding: 0.5rem 0.8rem;
  border-bottom: 1px solid #333;
  vertical-align: top;
}

tbody tr:hover {
  background: rgba(100, 108, 255, 0.08);
}

tr.restricted {
  background: rgba(255, 80, 80, 0.1);
}

.reg-num {
  font-family: monospace;
  white-space: nowrap;
}

.address {
  white-space: pre-line;
  max-width: 300px;
  font-size: 0.85rem;
}

.center {
  text-align: center;
}

.empty {
  text-align: center;
  padding: 3rem;
  opacity: 0.6;
}

@media (prefers-color-scheme: light) {
  .status-bar {
    background: #e8f0f8;
  }
  .status-bar.warning {
    background: #fef3cd;
    border-left-color: #d4a017;
  }
  thead th {
    border-bottom-color: #ccc;
  }
  tbody td {
    border-bottom-color: #eee;
  }
  .search-input,
  .region-filter {
    border-color: #ccc;
  }
  .region-filter option {
    background: #fff;
    color: #213547;
  }
}
</style>
