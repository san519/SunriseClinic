# 🏥 Sunrise Clinic & Diagnostic Center
**Professional Hospital & Diagnostic Management System**  
Built with **ASP.NET Core MVC (.NET 8)**

---

## 📌 Overview
Sunrise Clinic & Diagnostic Center is a **full-featured hospital and diagnostic management system**
designed for real-world clinics and diagnostic centers.

The system provides **secure role-based dashboards** for:
- Admin
- Doctor
- Nurse
- Patient

This project is suitable for **production use** and **client delivery** (Upwork / Fiverr / Agency).

---

## ✨ Key Features

### 🔐 Authentication & Authorization
- Secure login system
- Role-based access control
- Separate dashboards for each user role

### 🧑‍💼 Admin Panel
- Manage doctors, nurses, and patients
- User account control
- Appointment monitoring
- System-wide management

### 👨‍⚕️ Doctor Panel
- Daily appointment list
- Assigned patient access
- Schedule management

### 👩‍⚕️ Nurse Panel
- Patient record management
- Diagnostic test report upload
- Prescription upload
- Appointment handling

### 🧑 Patient Panel
- Appointment booking
- Appointment status tracking
- Complaint submission

### 🌐 Public Website
- Hospital information pages
- Department and doctor listings
- Contact and complaint forms
- XML Sitemap support

---

## 🧾 Environment Variables

| Variable | Description |
|--------|------------|
| ConnectionStrings__DefaultConnection | SQL Server connection string |
| EmailSettings__SmtpServer | SMTP server |
| EmailSettings__Port | SMTP port |
| EmailSettings__SenderEmail | Sender email |
| EmailSettings__SenderPassword | Email app password |
| ASPNETCORE_ENVIRONMENT | Development / Production |

⚠️ Do not commit real credentials to GitHub.

---

## 🧰 Technology Stack
- ASP.NET Core MVC (.NET 8)
- Microsoft SQL Server
- Entity Framework Core
- Razor Views
- Bootstrap
- Cookie-based Authentication

---

## 🚀 Installation

```bash
git clone https://github.com/san519/SunriseClinic.git
cd SunriseClinic
dotnet restore
dotnet run
```

Configure `appsettings.json` before running.

---

## 👨‍💻 Author
**MD Nadim Mostaq Eman**  
Professional Web Developer  
ASP.NET Core Specialist

---

⭐ If you find this project useful, please give it a star on GitHub.
