# PEI Doctors Monitor

A tool to monitor the [College of Physicians and Surgeons of Prince Edward Island (CPSPEI)](https://cpspei.alinityapp.com/Client/PublicDirectory/Registrants) public directory. It tracks changes in physician registrations by taking periodic snapshots and comparing them against a baseline.

## Features

- **Automated Scraping**: Fetches the latest registration data from the CPSPEI directory.
- **Change Detection**: Identifies additions, removals, and modifications in doctor records using `DiffPlex`.
- **Data Versioning**: Stores daily snapshots in the `data/` directory for historical tracking.
- **Single Page Application**: A Vue 3 frontend to interact with the monitor and view the baseline data.

## Tech Stack

- **Backend**: ASP.NET Core 8 Web API
- **Frontend**: Vue 3 + TypeScript + Vite (located in `ClientApp/`)
- **Libraries**: DiffPlex (for diffing), System.Text.Json (for serialization)

## Project Structure

- `Controllers/`: API endpoints for triggering scrapes and retrieving data.
- `Services/`: Contains `DoctorMonitorService.cs`, which handles the scraping, normalization, and diffing logic.
- `data/`: Stores JSON snapshots named by date (e.g., `20250707.json`) and the current `baseline.json`.
- `ClientApp/`: The Vue.js frontend application.

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (for frontend development)

### Running the Project

The easiest way to run the project is using Visual Studio or VS Code with the provided launch profiles.

#### Using Visual Studio

1. Open `PEI-Doctors.sln`.
2. Select the `https` or `http` launch profile.
3. Press `F5`. This will start the ASP.NET Core backend and automatically proxy to the Vite development server.

#### Manual Execution

1. **Frontend**:
   ```bash
   cd ClientApp
   npm install
   npm run dev
   ```
2. **Backend**:
   ```bash
   dotnet run
   ```

The backend will be available at `https://localhost:7196` (or similar) and will proxy frontend requests to `http://127.0.0.1:5173`.

## API Endpoints

- `POST /api/doctors/scrape`: Triggers a new scrape, saves a timestamped snapshot, and updates `baseline.json`.
- `GET /api/doctors/baseline`: Returns the current contents of `baseline.json`.

## Testing

The project includes a test suite in `PEI-Doctors.Tests`. To run the tests, use:

```bash
dotnet test
```

## Data Normalization

All JSON data is normalized using `System.Text.Json` with indentation to ensure consistent diffs. The `rg` (Registrant GUID) property is used as the primary key for identifying records.
