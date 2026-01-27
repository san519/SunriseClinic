+# SunriseClinic
+
+SunriseClinic is an ASP.NET Core MVC application for managing a clinic or diagnostic center workflow. It provides role-based access for administrators, doctors, nurses, and patients, along with appointment scheduling and related operational pages.
+
+## Features
+
+- Authentication and role-based dashboards (admin, doctor, nurse, patient).
+- Appointment creation and management.
+- Department and doctor listings.
+- Complaint tracking and patient-facing pages.
+- XML sitemap endpoint.
+
+## Tech Stack
+
+- ASP.NET Core MVC (.NET 8)
+- Entity Framework Core (SQL Server provider)
+- Cookie authentication + server-side sessions
+
+## Getting Started
+
+### Prerequisites
+
+- .NET SDK 8.0+
+- SQL Server (local or remote instance)
+
+### Setup
+
+1. Restore dependencies:
+   ```bash
+   dotnet restore SunriseClinic/SunriseClinic.csproj
+   ```
+2. Configure the database connection string in `SunriseClinic/appsettings.json` (or use user secrets / environment variables for production).
+3. Run the application:
+   ```bash
+   dotnet run --project SunriseClinic/SunriseClinic.csproj
+   ```
+4. Open the app at `https://localhost:5001` or the URL shown in the console output.
+
+## Configuration Notes
+
+- `ConnectionStrings:DefaultConnection` controls the SQL Server connection.
+- `EmailSettings` are used for SMTP; do not commit real credentials in production.
+
+## Project Structure
+
+- `Controllers/` — MVC controllers for authentication, appointments, departments, and role dashboards.
+- `Data/` — EF Core `DbContext`.
+- `Models/` — Entity models.
+- `Views/` — Razor UI views.
+- `wwwroot/` — Static assets and uploads.
