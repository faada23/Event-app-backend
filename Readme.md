# Event API Project

This project provides an event management API built with ASP.NET Core and uses PostgreSQL as the database. It is containerized using Docker for easy setup and deployment.

## Prerequisites

Before you begin, ensure you have the following installed:

*   [Docker](https://docs.docker.com/get-docker/)
*   [Docker Compose](https://docs.docker.com/compose/install/) (Usually included with Docker Desktop)

## Configuration

1.  **Create a `.env` file:**
    In the root directory of the project, create a file named `.env`.

2.  **Add environment variables:**
    Copy the following content into the `.env` file and replace the placeholder values with your desired settings, especially `DB_PASSWORD`.

    ```dotenv
    # Database Configuration
    DB_PASSWORD=your_strong_password_here # Replace with your desired database password

    # Connection String (uses DB_PASSWORD from above)
    EventDbConnection="Host=db;Database=EventDB;Username=postgres;Password=${DB_PASSWORD};Port=5432"

    # Application Specific Secrets (replace with secure values)
    EventAppAP1=Your_Admin_Password_Or_Key # Replace if needed
    JwtSecretKey=Your_Super_Secret_JWT_Key_That_Is_At_Least_32_Characters_Long # Replace with a strong, unique key
    ```

    **Important:**
    *   Ensure the `DB_PASSWORD` value is set correctly as it's used by both the `db` service and the `Api` service via the connection string.
    *   Choose a strong and unique `JwtSecretKey` with at least 32 characters.

## Running the Application

1.  **Open a terminal** in the root directory of the project (where the `docker-compose.yml` file is located).

2.  **Build and start the services:**
    Run the following command:

    ```bash
    docker compose up -d --build
    ```

    *   `up`: Creates and starts the containers defined in `docker-compose.yml`.
    *   `-d`: Runs the containers in detached mode (in the background).
    *   `--build`: Forces Docker Compose to rebuild the images before starting the containers (useful for the first run or after code changes).

    Docker will download the necessary base images (PostgreSQL, .NET SDK, ASP.NET Runtime), build the API image, and start both the database and API containers. The API service will wait for the database service to be healthy before starting completely.

## Accessing the API

Once the containers are running, the API should be accessible at:

`http://localhost:8080`

You can use tools like `curl`, Postman, or your web browser to interact with the API endpoints (e.g., `http://localhost:8080/swagger` if Swagger UI is configured).

## Stopping the Application

To stop and remove the containers, networks, and volumes created by `docker compose up`, run the following command in the project's root directory:

```bash
docker compose down