# AuthAPI

A secure authentication and role-based authorization system built with ASP.NET Core Web API, MongoDB, and JWT tokens.

## Overview

AuthAPI provides a robust authentication and authorization framework that allows for secure user management with role-based access control. The API uses MongoDB for data storage and JWT tokens for stateless authentication.

## Features

- User registration and authentication
- Role-based authorization (Admin and User roles)
- JWT token-based authentication
- Secure password handling
- Product management with role-based permissions

## Technologies Used

- **ASP.NET Core 6.0** - Web API framework
- **MongoDB** - NoSQL database for data storage
- **ASP.NET Core Identity with MongoDB** - User management and authentication
- **JWT (JSON Web Tokens)** - For secure API authentication
- **Swagger/OpenAPI** - API documentation and testing

## Project Structure

- **Controllers/** - API endpoints for Auth and Products
- **Models/** - Data models with MongoDB collection attributes
- **Services/** - Business logic implementation
- **Dtos/** - Data transfer objects for API requests/responses
- **Settings/** - Configuration classes for MongoDB and JWT

## Getting Started

### Prerequisites

- .NET 6.0 SDK or later
- MongoDB instance (local or cloud)

### Configuration

Update the `appsettings.json` file with your MongoDB connection string and JWT settings:

```json
{
  "MongoDbSettings": {
    "ConnectionString": "your_mongodb_connection_string",
    "DatabaseName": "your_database_name"
  },
  "JwtSettings": {
    "SecretKey": "your_secret_key_at_least_32_chars_long",
    "Issuer": "AuthApi",
    "Audience": "AuthApiClient",
    "ExpiryMinutes": 60
  }
}
```

### Running the API

```bash
# Clone the repository
git clone https://github.com/VSHAGEETHPRATHEEP06/AuthAPI.git
cd AuthAPI

# Build the application
dotnet build

# Run the application
dotnet run
```

The API will be available at `https://localhost:5001` and `http://localhost:5000`.

## API Endpoints

### Authentication

- `POST /api/auth/register` - Register a new user
- `POST /api/auth/login` - Authenticate a user and get JWT token
- `POST /api/auth/logout` - Invalidate current JWT token

### Products

- `GET /api/products` - Get all products (authenticated users)
- `GET /api/products/{id}` - Get product by ID (authenticated users)
- `POST /api/products` - Create a new product (admin only)
- `PUT /api/products/{id}` - Update a product (admin only)
- `DELETE /api/products/{id}` - Delete a product (admin only)

## Security Features

- Password hashing with ASP.NET Core Identity
- JWT token expiration
- Server-side token invalidation
- Role-based access control
- HTTPS support

