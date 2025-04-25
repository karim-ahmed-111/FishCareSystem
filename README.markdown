# FishCare API

FishCare API is a backend system for monitoring and managing aquaculture environments, developed as part of a project for Mansoura University (April 16-22, 2025). Built using ASP.NET Core 9, this API integrates with IoT devices, an AI prediction service, and a database to monitor water quality, generate alerts, and control devices in fish tanks. The system supports user authentication, role-based access control, and automated device control based on AI predictions.

## Table of Contents
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Setup Instructions](#setup-instructions)
- [Database Setup](#database-setup)
- [Running the API](#running-the-api)
- [API Endpoints](#api-endpoints)
- [Testing](#testing)
- [AI Integration](#ai-integration)
- [MQTT Integration](#mqtt-integration)
- [Deployment](#deployment)
- [Contributing](#contributing)
- [License](#license)

## Features
- **User Authentication**: Register, login, and refresh tokens using JWT-based authentication.
- **Role-Based Access Control**: Restrict access to certain operations (e.g., managers can create alerts and devices).
- **Alert Management**: Create, retrieve, and delete alerts for water quality issues in fish tanks.
- **Device Management**: Manage IoT devices (e.g., aerators, coolers) with real-time status updates via MQTT.
- **AI Predictions**: Integrate with an AI service to predict water quality issues and trigger device actions.
- **Forgot Password**: Users can request password reset links via email (using SendGrid).

## Tech Stack
- **Backend**: ASP.NET Core 9
- **Database**: SQL Server (via Entity Framework Core)
- **Authentication**: ASP.NET Core Identity with JWT
- **Email Service**: SendGrid (for forgot password emails)
- **IoT Communication**: MQTT (using `IMqttClientService` for device control)
- **AI Service**: FastAPI (Python) for water quality predictions
- **Logging**: Built-in ASP.NET Core logging
- **Testing**: Postman

## Prerequisites
- **.NET SDK**: Version 9.0 or later
- **SQL Server**: Local or cloud instance
- **SendGrid Account**: For email functionality (API key required)
- **MQTT Broker**: For IoT device communication (e.g., Mosquitto)
- **Python**: Version 3.8+ (for the AI service)
- **Postman**: For API testing
- **Git**: For cloning the repository

## Setup Instructions

### 1. Clone the Repository
```bash
git clone https://github.com/yourusername/fishcare-api.git
cd fishcare-api
```

### 2. Install .NET Dependencies
Restore the NuGet packages for the project:
```bash
dotnet restore
```

### 3. Configure `appsettings.json`
Update the `appsettings.json` file with your configuration:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=FishCare2025;Trusted_Connection=True;"
  },
  "Jwt": {
    "Key": "YourJwtSecretKeyHere", // Replace with a secure key
    "Issuer": "FishCareSystem",
    "Audience": "FishCareSystem",
    "AccessTokenExpireMinutes": "15",
    "RefreshTokenExpireDays": "7"
  },
  "AI": {
    "ServiceUrl": "http://localhost:8000"
  },
  "SendGrid": {
    "ApiKey": "YOUR_SENDGRID_API_KEY", // Replace with your SendGrid API key
    "SenderEmail": "noreply@fishcare.com",
    "SenderName": "FishCare System"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 4. Set Up the AI Service
The AI service is a FastAPI application that predicts water quality issues. Set it up as follows:

#### Install Python Dependencies
```bash
cd ai_service
pip install fastapi uvicorn pandas joblib
```

#### Run the AI Service
```bash
uvicorn ai_service:app --host localhost --port 8000
```

- The AI service will be available at `http://localhost:8000/predict`.
- It accepts POST requests with sensor data (e.g., `{"temperature": 31, "pH": 6.2, "oxygen": 4.8}`) and returns predictions.

## Database Setup

### 1. Create the Database
- Ensure SQL Server is running.
- Update the `DefaultConnection` string in `appsettings.json` with your SQL Server details.

### 2. Apply Migrations
Run the following commands to create the database and apply migrations:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. Seed Initial Data
- Add a tank to the `Tanks` table:
  ```sql
  INSERT INTO Tanks (Id, Name) VALUES (1, 'Tank1');
  ```
- Add devices to the `Devices` table:
  ```sql
  INSERT INTO Devices (TankId, Name, Type, Status)
  VALUES (1, 'Aerator_1', 'Aerator', 'Off'),
         (1, 'Cooler_1', 'Cooler', 'Off');
  ```

## Running the API
Run the API using:
```bash
dotnet run
```

- The API will be available at `https://localhost:7215`.
- Access the Swagger UI at `https://localhost:7215/swagger` for API documentation.

## API Endpoints

### Authentication
- **POST /api/auth/register**  
  Register a new user (assigns "Manager" role by default).  
  **Request Body**:
  ```json
  {
    "userName": "manager1",
    "email": "manager1@example.com",
    "password": "Manager@123",
    "firstName": "Manager",
    "lastName": "One"
  }
  ```
  **Response**:
  ```json
  {
    "success": true,
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "guid-value"
  }
  ```

- **POST /api/auth/login**  
  Log in and get a JWT token.  
  **Request Body**:
  ```json
  {
    "userName": "manager1",
    "password": "Manager@123"
  }
  ```
  **Response**:
  ```json
  {
    "success": true,
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "guid-value"
  }
  ```

- **POST /api/auth/forgot-password**  
  Request a password reset link (sent via email).  
  **Request Body**:
  ```json
  {
    "email": "manager1@example.com"
  }
  ```

- **POST /api/auth/reset-password**  
  Reset the user’s password using the token from the email.  
  **Request Body**:
  ```json
  {
    "token": "reset-token-from-email",
    "email": "manager1@example.com",
    "newPassword": "NewPass@123"
  }
  ```

### Alerts
- **GET /api/alerts**  
  Retrieve all alerts (requires authentication).  
  **Response**:
  ```json
  [
    {
      "id": 1,
      "tankId": 1,
      "message": "High temperature detected",
      "severity": "Critical",
      "createdAt": "2025-04-22T15:56:00Z"
    }
  ]
  ```

- **POST /api/alerts**  
  Create a new alert (requires "Manager" role).  
  **Request Body**:
  ```json
  {
    "tankId": 1,
    "message": "High temperature detected",
    "severity": "Critical"
  }
  ```

### Devices
- **GET /api/devices?tankId=1**  
  Retrieve devices for a specific tank (requires authentication).  
  **Response**:
  ```json
  [
    {
      "id": 1,
      "tankId": 1,
      "name": "Aerator_1",
      "type": "Aerator",
      "status": "Off"
    }
  ]
  ```

- **POST /api/devices**  
  Create a new device (requires "Manager" role).  
  **Request Body**:
  ```json
  {
    "tankId": 1,
    "name": "Aerator_1",
    "type": "Aerator",
    "status": "Off"
  }
  ```

- **PUT /api/devices/{id}**  
  Update a device’s status (requires "Manager" role).  
  **Request Body**:
  ```json
  {
    "status": "On"
  }
  ```

## Testing

### 1. Test Authentication
- Use Postman to register a user:
  ```http
  POST https://localhost:7215/api/auth/register
  Content-Type: application/json
  {
    "userName": "manager1",
    "email": "manager1@example.com",
    "password": "Manager@123",
    "firstName": "Manager",
    "lastName": "One"
  }
  ```
- Log in to get a token:
  ```http
  POST https://localhost:7215/api/auth/login
  Content-Type: application/json
  {
    "userName": "manager1",
    "password": "Manager@123"
  }
  ```

### 2. Test Alerts
- Create an alert:
  ```http
  POST https://localhost:7215/api/alerts
  Authorization: Bearer <your_access_token>
  Content-Type: application/json
  {
    "tankId": 1,
    "message": "High temperature detected",
    "severity": "Critical"
  }
  ```
- Retrieve alerts:
  ```http
  GET https://localhost:7215/api/alerts
  Authorization: Bearer <your_access_token>
  ```

### 3. Test Devices
- Create a device:
  ```http
  POST https://localhost:7215/api/devices
  Authorization: Bearer <your_access_token>
  Content-Type: application/json
  {
    "tankId": 1,
    "name": "Aerator_1",
    "type": "Aerator",
    "status": "Off"
  }
  ```
- Update device status:
  ```http
  PUT https://localhost:7215/api/devices/1
  Authorization: Bearer <your_access_token>
  Content-Type: application/json
  {
    "status": "On"
  }
  ```

## AI Integration
The API integrates with a FastAPI-based AI service for water quality predictions:
- **Endpoint**: `http://localhost:8000/predict`
- **Request**:
  ```json
  {
    "temperature": 31,
    "pH": 6.2,
    "oxygen": 4.8
  }
  ```
- **Response**:
  ```json
  {
    "is_abnormal": true,
    "action": {
      "device": "Cooler",
      "status": "On"
    }
  }
  ```

Currently, this integration is manual. Future updates will automate AI predictions to trigger alerts and device actions.

## MQTT Integration
The API uses MQTT to communicate with IoT devices:
- **Topic Format**: `fishcare/tank/{tankId}/device/{deviceId}`
- **Payload**: Device status (e.g., "On" or "Off").
- Ensure an MQTT broker (e.g., Mosquitto) is running and configured.

## Deployment
To deploy the API to Azure:
1. Publish the project:
   ```bash
   dotnet publish -c Release
   ```
2. Deploy to Azure App Service using the Azure CLI or Visual Studio.
3. Configure environment variables (e.g., connection strings, SendGrid API key) in Azure.
4. Set up a SQL Server database in Azure and update the connection string.

## Contributing
1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/your-feature`).
3. Commit your changes (`git commit -m "Add your feature"`).
4. Push to the branch (`git push origin feature/your-feature`).
5. Open a pull request.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.