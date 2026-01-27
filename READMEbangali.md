-# SunriseClinic
\ No newline at end of file
+# SunriseClinic
+
+SunriseClinic হলো একটি ASP.NET Core MVC ভিত্তিক ক্লিনিক ও ডায়াগনস্টিক ম্যানেজমেন্ট সিস্টেম। এই অ্যাপ্লিকেশনটি অ্যাডমিন, ডাক্তার, নার্স এবং পেশেন্ট ড্যাশবোর্ডের মাধ্যমে অ্যাপয়েন্টমেন্ট, রিপোর্ট, প্রেসক্রিপশন ও অন্যান্য পরিষেবা পরিচালনা করতে সাহায্য করে।
+
+## বৈশিষ্ট্যসমূহ (Features)
+
+- মাল্টি-রোল ড্যাশবোর্ড (Admin, Doctor, Nurse, Patient)
+- অ্যাপয়েন্টমেন্ট তৈরি ও ম্যানেজমেন্ট
+- রোগীর অভিযোগ (Complaint) ও বিভাগ (Department) ব্যবস্থাপনা
+- অথেন্টিকেশন ও সেশন ম্যানেজমেন্ট (Cookie + Session)
+- SQL Server ভিত্তিক ডেটাবেস লেয়ার (EF Core)
+
+## টেক স্ট্যাক
+
+- **.NET 8 (ASP.NET Core MVC)**
+- **Entity Framework Core 9**
+- **SQL Server**
+
+## প্রয়োজনীয়তা (Prerequisites)
+
+- .NET SDK 8
+- SQL Server (লোকাল বা রিমোট)
+
+## কনফিগারেশন
+
+1. `SunriseClinic/appsettings.json` ফাইলে আপনার SQL Server কানেকশন স্ট্রিং সেট করুন:
+
+   ```json
+   {
+     "ConnectionStrings": {
+       "DefaultConnection": "Server=YOUR_SERVER;Database=SunriseClinicDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
+     }
+   }
+   ```
+
+2. ইমেইল কনফিগারেশন প্রয়োজন হলে `EmailSettings` সেকশন আপডেট করুন। সংবেদনশীল তথ্য (যেমন পাসওয়ার্ড) Git রিপোজিটরিতে না রাখাই উত্তম।
+
+## রান করার ধাপ
+
+```bash
+# 1) ডিপেন্ডেন্সি রিস্টোর
+ dotnet restore
+
+# 2) প্রজেক্ট রান
+ dotnet run --project SunriseClinic/SunriseClinic.csproj
+```
+
+ডিফল্টভাবে অ্যাপটি `https://localhost:5001` এবং `http://localhost:5000` পোর্টে চলবে।
+
+## সাধারণ রুটসমূহ
+
+- `/Account/Login` - লগইন
+- `/Admin/Dashboard` - অ্যাডমিন ড্যাশবোর্ড
+- `/Doctor/Dashboard` - ডাক্তার ড্যাশবোর্ড
+- `/Nurse/Dashboard` - নার্স ড্যাশবোর্ড
+- `/Patient/Dashboard` - পেশেন্ট ড্যাশবোর্ড
+- `/Appointment/Create` - অ্যাপয়েন্টমেন্ট তৈরি
+
+## প্রজেক্ট স্ট্রাকচার (সংক্ষিপ্ত)
+
+```
+SunriseClinic/
+├─ Controllers/
+├─ Models/
+├─ Data/
+├─ Views/
+├─ wwwroot/
+└─ Program.cs
+```
+
+## নোট
+
+- ডেটাবেস কানেকশন উপলভ্য না থাকলে অ্যাপ স্টার্টআপে ওয়ার্নিং লগ দেখাবে, তবে অ্যাপ চলবে।
+- প্রোডাকশন ডেপ্লয়মেন্টের আগে সিক্রেটস ও কানেকশন স্ট্রিং সিকিউর স্টোরে রাখুন (যেমন: User Secrets, Azure Key Vault)।
