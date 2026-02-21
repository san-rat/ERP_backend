# Project Progress Summary

### 1) Repository & Docs Structure
- Created a clean repository structure for documentation and future services.
- Added `/docs` folder and stored the SRS PDF.
- Added a root `README.md` to describe the backend.

### 2) First Microservice: AuthService (Minimal)
- Created the first backend microservice: `src/AuthService`
- Implemented minimal endpoints:
  - `GET /health` (service health check)
  - `POST /login` (basic login endpoint for future integration)
- Enabled Swagger UI for API testing.

### 3) CI Pipeline (GitHub Actions)
A GitHub Actions workflow is set up to run on every push / pull request to `main` and `dev`:
- Verifies required repo structure (README + docs folder)
- Restores .NET dependencies
- Builds the backend solution (`ERP_Backend.slnx`)
- Builds the Docker image for AuthService (validates containerization)

This ensures the project always builds successfully and the Dockerfile remains valid.

---

## 📁 Current Project Structure

```text
ERP_backend/
├── .github/workflows/ # GitHub Actions CI pipeline
├── docs/              # Documentation (SRS, epics, research)
│   └── SRS/SRS.pdf
├── src/
│   └── AuthService/   # First microservice
│       ├── Program.cs
│       ├── AuthService.csproj
│       ├── Dockerfile
│       └── ...
├── ERP_Backend.slnx   # Solution file (contains services)
└── README.md
```

---

## ▶️ How To Run AuthService Locally (Step-by-Step)

### A) Run using .NET (recommended for development)
From the repository root:

1.  **Restore dependencies:**
    ```bash
    dotnet restore ERP_Backend.slnx
    ```

2.  **Run the AuthService:**
    ```bash
    dotnet run --project src/AuthService
    ```

3.  **Open Swagger UI:**
    - **Swagger:** `http://localhost:<PORT>/swagger`
    - **Health check:** `GET http://localhost:<PORT>/health`
    *(The port is shown in the terminal output when you run the service.)*

### B) Run using Docker (when Docker Desktop is available)
From the repository root:

1.  **Build the Docker image:**
    ```bash
    docker build -t authservice:local -f src/AuthService/Dockerfile src/AuthService
    ```

2.  **Run the container:**
    ```bash
    docker run --rm -p 8080:8080 authservice:local
    ```

3.  **Test:**
    - **Swagger:** `http://localhost:8080/swagger`
    - **Health check:** `GET http://localhost:8080/health`

---

## 🔌 API Endpoints (AuthService)

### 1) Health Check
- **Endpoint:** `GET /health`
- **Description:** Returns a simple OK response to confirm the service is running.

### 2) Login
- **Endpoint:** `POST /login`
- **Request body example:**
    ```json
    {
      "username": "admin",
      "password": "password"
    }
    ```
- **Description:** Success response includes a placeholder token (will be replaced with JWT later).

---

## 🔄 CI Workflow Summary (GitHub Actions)
The CI workflow runs automatically on:
- Push to `main` or `dev`
- Pull requests targeting `main` or `dev`

**CI steps:**
1.  **Checkout repository**
2.  **Verify structure** (README + docs)
3.  **Setup .NET** (v10.x)
4.  **Restore dependencies**
5.  **Build solution**
6.  **Docker build** AuthService

---

## 🚧 Next Planned Steps
- Add Azure Container Registry (ACR)
- Push Docker images to ACR via CD pipeline
- Deploy AuthService to Azure Container Apps
- Add API Gateway service (Gateway) and connect services
