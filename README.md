---

📅 Sphere Schedule API

Backend API for the Sphere Schedule System built with .NET 10 Core Web API.


---

🚀 Overview

Sphere Schedule API is a RESTful backend service designed to manage:

📆 Events

👤 Users

⏰ Reminders

📂 Categories

🔔 Notifications


It powers the Sphere Schedule Mobile App, providing secure and scalable scheduling functionality.


---

🛠️ Built With

Framework: ASP.NET Core (.NET 10)

Language: C#

Database: SQL Server / MySQL

ORM: Entity Framework Core

Authentication: JWT (JSON Web Token)

Architecture: RESTful API

IDE: Visual Studio 2022 / Visual Studio Code



---

📂 Project Structure

SphereScheduleAPI/
│── Controllers/
│── Models/
│── DTOs/
│── Services/
│── Data/
│── Migrations/
│── Program.cs
│── appsettings.json


---

⚙️ Features

✔ Create, Update, Delete Events
✔ User Registration & Login (JWT Authentication)
✔ Event Reminders
✔ Weekly & Monthly Views
✔ Role-based Authorization (Admin/User)
✔ API Documentation (Swagger)


---

🔐 Authentication

This API uses JWT Authentication.

Register

POST /api/auth/register

Login

POST /api/auth/login

Include token in headers:

Authorization: Bearer {your_token}


---

📆 Event Endpoints

Method	Endpoint	Description

GET	/api/events	Get all events
GET	/api/events/{id}	Get event by ID
POST	/api/events	Create event
PUT	/api/events/{id}	Update event
DELETE	/api/events/{id}	Delete event



---

🗄️ Database Configuration

Update your appsettings.json:

"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=SphereScheduleDB;Trusted_Connection=True;"
}

Run migrations:

dotnet ef migrations add InitialCreate
dotnet ef database update


---

▶️ How To Run

1️⃣ Clone Repository

git clone https://github.com/yourusername/SphereScheduleAPI.git

2️⃣ Navigate to Project

cd SphereScheduleAPI

3️⃣ Run Project

dotnet run

API will run at:

https://localhost:5001

Swagger UI:

https://localhost:5001/swagger


---

🧪 Testing

You can test endpoints using:

Swagger UI

Postman

Insomnia



---

📱 Connected Client

This API is designed to work with:

Sphere Schedule Android App

Web Admin Dashboard



---

🧩 Future Improvements

Push Notifications

Calendar Sync (Google Calendar integration)

Email Reminders

Docker Deployment

CI/CD Pipeline



---

👨‍💻 Author

Faris Shikuku
Software Developer | Sphere Systems


---

📜 License

This project is licensed under the MIT License.


---
