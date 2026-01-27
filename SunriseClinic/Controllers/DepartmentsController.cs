using Microsoft.AspNetCore.Mvc;
using SunriseClinic.Models;
using System.Collections.Generic;

namespace SunriseClinic.Controllers
{
    public class DepartmentsController : Controller
    {
        // GET: /Departments
        public IActionResult Index()
        {
            ViewData["Title"] = "Our Medical Departments";

            var departments = new List<Department>
            {
                new Department
                {
                    DepartmentId = 1,
                    DepartmentName = "Chest & TB Diseases",
                    Description = "Our Chest & TB Diseases Department specializes in diagnosis and treatment of pulmonary diseases including tuberculosis, asthma, COPD, pneumonia, and other respiratory disorders. We provide comprehensive care with modern diagnostic facilities.",
                    HeadDoctorName = "Dr. Md. Khalilur Rahman",
                    DoctorCount = 3
                },
                new Department
                {
                    DepartmentId = 2,
                    DepartmentName = "Gynecology & Obstetrics",
                    Description = "Comprehensive women's health services including prenatal care, delivery, gynecological surgeries, fertility treatment, laparoscopic surgeries, and menopausal care. We provide both medical and surgical gynecological treatments.",
                    HeadDoctorName = "Dr. Farjana Mahjabin (Ovi)",
                    DoctorCount = 4
                },
                new Department
                {
                    DepartmentId = 3,
                    DepartmentName = "Administration & Management",
                    Description = "Our Administration Department ensures smooth operation of the clinic with efficient management systems. We handle patient registration, billing, appointment scheduling, and overall clinic administration.",
                    HeadDoctorName = "Monirujjaman Mishuk",
                    DoctorCount = 5
                },
                new Department
                {
                    DepartmentId = 4,
                    DepartmentName = "Laparoscopic Surgery",
                    Description = "Advanced minimally invasive surgical procedures for various conditions. Our laparoscopic surgery department specializes in gynecological and general surgical procedures with faster recovery and minimal scarring.",
                    HeadDoctorName = "Dr. Shamima Khatun Poly",
                    DoctorCount = 2
                },
                new Department
                {
                    DepartmentId = 5,
                    DepartmentName = "Diagnostic Center",
                    Description = "Our Diagnostic Center is equipped with modern technology for accurate diagnosis including USG, X-Ray, ECG, Pathology, Biochemistry, Serology, and Hormone tests. We provide comprehensive diagnostic services under one roof.",
                    HeadDoctorName = "Dr. Lisa Davis, MD (Radiology)",
                    DoctorCount = 8
                }
            };

            return View(departments);
        }

        // GET: /Departments/ChestAndTBDiseases
        public IActionResult ChestAndTBDiseases()
        {
            ViewData["Title"] = "Chest & TB Diseases Department";
            var department = new Department
            {
                DepartmentName = "Chest & TB Diseases",
                Description = "The Chest & TB Diseases Department at Sunrise Clinic provides specialized care for pulmonary and respiratory disorders. Our department is led by Dr. Md. Khalilur Rahman who has 30+ years of experience and international training in Thailand and Tanzania. We offer comprehensive diagnosis and treatment for tuberculosis, asthma, COPD, pneumonia, bronchitis, and other respiratory conditions.",
                HeadDoctorName = "Dr. Md. Khalilur Rahman",
                DoctorCount = 3
            };
            return View("DepartmentDetail", department);
        }

        // GET: /Departments/GynecologyObstetrics
        public IActionResult GynecologyObstetrics()
        {
            ViewData["Title"] = "Gynecology & Obstetrics Department";
            var department = new Department
            {
                DepartmentName = "Gynecology & Obstetrics",
                Description = "Our Gynecology & Obstetrics Department provides complete women's healthcare services. Led by Dr. Farjana Mahjabin (Ovi) with 20+ years of experience, we offer prenatal care, delivery services, gynecological surgeries, fertility treatments, family planning, and menopausal care. We focus on providing compassionate care in a supportive environment.",
                HeadDoctorName = "Dr. Farjana Mahjabin (Ovi)",
                DoctorCount = 4
            };
            return View("DepartmentDetail", department);
        }

        // GET: /Departments/LaparoscopicSurgery
        public IActionResult LaparoscopicSurgery()
        {
            ViewData["Title"] = "Laparoscopic Surgery Department";
            var department = new Department
            {
                DepartmentName = "Laparoscopic Surgery",
                Description = "The Laparoscopic Surgery Department specializes in minimally invasive surgical procedures. Under the leadership of Dr. Shamima Khatun Poly (MBBS, BCS, FCPS), we perform advanced laparoscopic surgeries for gynecological and general surgical conditions. Our procedures ensure faster recovery, minimal pain, and reduced hospital stay compared to traditional surgery.",
                HeadDoctorName = "Dr. Shamima Khatun Poly",
                DoctorCount = 2
            };
            return View("DepartmentDetail", department);
        }

        // GET: /Departments/DiagnosticCenter
        public IActionResult DiagnosticCenter()
        {
            ViewData["Title"] = "Diagnostic Center";
            var department = new Department
            {
                DepartmentName = "Diagnostic Center",
                Description = "Our Diagnostic Center is a state-of-the-art facility equipped with modern medical technology. We provide comprehensive diagnostic services including Radiology & Imaging (USG, X-Ray, ECG), Pathology (Hematology, Biochemistry, Serology), Hormone Testing, and Special Tests. With 12 specialists and advanced equipment, we ensure accurate and timely diagnosis.",
                HeadDoctorName = "Dr. Lisa Davis, MD (Radiology)",
                DoctorCount = 12
            };
            return View("DepartmentDetail", department);
        }

        // GET: /Departments/Administration
        public IActionResult Administration()
        {
            ViewData["Title"] = "Administration & Management";
            var department = new Department
            {
                DepartmentName = "Administration & Management",
                Description = "The Administration Department ensures efficient management and smooth operation of Sunrise Clinic. Led by Monirujjaman Mishuk (MBA, Healthcare Management), our team handles patient registration, appointment scheduling, billing, insurance processing, clinic management, and overall administrative functions to provide a seamless healthcare experience.",
                HeadDoctorName = "Monirujjaman Mishuk",
                DoctorCount = 5
            };
            return View("DepartmentDetail", department);
        }
    }
}