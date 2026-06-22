# SPEAK Backend Service

The **SPEAK** backend is a comprehensive RESTful API built with **.NET 9** following the principles of **Onion Architecture**. It serves as the core system for the SPEAK solution, handling user authentication, doctor verification, diagnostic records, and voice-processing integration with an external AI pipeline.

## 🏗 Architecture Overview

The application is structured using **Onion Architecture** to ensure separation of concerns, testability, and scalability. The layers depend strictly on the layers closer to the center.

### Layers

1. **Domain Layer (`Core/SPEAK.Domain`)**
   - The heart of the application.
   - Contains core business entities (e.g., `ApplicationUser`, `Message`, `DiagnosticRecord`).
   - Contains Enum definitions and business rules with zero dependencies on external frameworks or databases.

2. **Abstraction Layer (`Core/SPEAK.Abstraction`)**
   - Defines the interfaces (Contracts) for Repositories and Services.
   - Ensures that the core remains decoupled from data access implementation.
   - Examples: `IAuthenticationServices`, `IDoctorRepository`, `IVoiceProcessingService`.

3. **Service Layer (`Core/SPEAK.Service`)**
   - Contains the business logic implementations of the abstraction interfaces.
   - Integrates different repositories and coordinates actions.
   - Handles external API calls for AI-based voice processing (`/segment`, `/analyze`).

4. **Infrastructure Layer (`Infrastructure/SPEAK.Persistence` & `SPEAK.Infrastructure`)**
   - Implementation of data access using **Entity Framework Core**.
   - Contains DbContext (`UserIdentityDbContext`), Entity configurations, and Migrations.
   - Implements the repository interfaces defined in the Abstraction layer.

5. **Presentation / Web Layer (`SPEAK.Web` & `Infrastructure/SPEAK.Presentation`)**
   - The entry point for client applications (Mobile/Web).
   - Contains API Controllers (`AuthenticationController`, `MergeVoicesController`, etc.).
   - Responsible for routing, validating requests, formatting responses, and Authentication (JWT).

6. **Dashboard Layer (`SPEAK.Dashboard`)**
   - An MVC-based Admin dashboard for managing users, doctors' verification, and platform monitoring.
   - Requires an `Admin` role to access.

## 🚀 Technologies & Features

- **Framework:** .NET 9.0
- **Architecture:** Onion Architecture / Clean Architecture
- **Authentication & Authorization:** ASP.NET Core Identity + JWT Bearer Tokens.
- **ORM:** Entity Framework Core (SQL Server)
- **API Documentation:** Swagger / OpenAPI
- **Real-Time Communication:** SignalR (for Chat functionality)
- **Audio Processing:** NAudio for WAV merging and manipulation.

## 🛠 Features

- **Authentication System:** Login, Registration, OTP Verification, Forget Password, and Google OAuth Integration.
- **Doctor Verification System:** Secure endpoints for doctors to upload their Syndicate Card and National ID for approval.
- **AI Integration (Voice Processing):** 
  - Gathers uploaded `.wav` voice chunks from the client.
  - Merges them into a single file and dispatches them to a Python/AI microservice for stuttering segmentation and SSI calculation.
- **User Dashboard:** MVC web interface for Admins to enable/disable users and approve doctors.
- **Stateless Architecture:** Highly scalable using JWTs for API access.

## ⚙️ Setup and Installation

### Prerequisites
- .NET 9 SDK
- SQL Server (Local or Docker)
- Ensure the external AI service is running and accessible (URLs configured in `appsettings.json`).

### Configuration
1. Clone the repository.
2. Open `appsettings.json` in `SPEAK.Web` and `SPEAK.Dashboard`.
3. Update the `ConnectionStrings:DefaultConnection` to point to your local SQL Server database.
4. Set the proper AI integration URLs:
   ```json
   "AI": {
     "SegmentUrl": "http://localhost:8000/segment",
     "AnalyzeUrl": "http://localhost:8000/analyze"
   }
   ```

### Running Migrations
Navigate to the Web API project and run the Entity Framework migrations to seed the database:

```bash
cd SPEAK.Web
dotnet ef database update --project ../Infrastructure/SPEAK.Persistence
```

### Running the Project
You can start the API and the Dashboard using the .NET CLI or Visual Studio.

**To run the API:**
```bash
cd SPEAK.Web
dotnet run
```

**To run the Dashboard:**
```bash
cd SPEAK.Dashboard
dotnet run
```

## 🔐 Authentication
The API utilizes **JWT (JSON Web Tokens)**.
To access secure endpoints, include the token in the Authorization header:
`Authorization: Bearer {your_jwt_token}`
