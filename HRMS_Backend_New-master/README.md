نظام إدارة الموارد البشرية (HRMS Backend API)

---

 وصف المشروع
هذا المشروع عبارة عن نظام Backend لإدارة الموارد البشرية،  
يهدف إلى تنظيم:
- الموظفين
- المستخدمين
- الأدوار والصلاحيات
- طلبات الإجازات
- (تسلسل الموافق (موظف → مدير
- الإشعارات
- رصيد الإجازات
- العطل الرسمية

تم بناء النظام باستخدام:
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Authentication
- Role Based Access Control
- Permission Based Authorization

---

 الأدوار (Roles)

1. SuperAdmin
- إضافة مستخدمين (Users)
- إضافة موظفين (Employees)
- تحديد المدراء
- التحكم الكامل في النظام

2. Manager (مدير قسم)
- مشاهدة طلبات الإجازة للموظفين التابعين له
- الموافقة على الإجازة
- رفض الإجازة مع كتابة سبب الرفض
- خصم رصيد الإجازات عند الموافقة

3. Employee
- تسجيل الدخول
- تقديم طلب إجازة
- مشاهدة طلباته السابقة
- مشاهدة حالة الطلب
- مشاهدة رصيد الإجازات
- استلام الإشعارات

---

الصلاحيات (Permissions)

:( الصلاحية (الوصف 

( SubmitLeave ) تقديم طلب إجازة -
( ApproveLeave ) الموافقة / الرفض على الإجازة -
( ViewLeaves ) عرض طلبات الإجازات -
( ManageEmployees ) إدارة الموظفين -
( ManageUsers)  إدارة المستخدمين -

---

 تسلسل الإجازات (Leave Flow)

1.  الموظف يقوم بتقديم طلب إجازة  
2.  الحالة = قيد_الانتظار  
3.  المدير يستلم الطلب  
4. المدير يقرر:
   -  موافقة  
   - تخصم الأيام من رصيد الإجازات  
   - الحالة = موافق_المدير
   -  رفض  
   - يتم تسجيل سبب الرفض  
   - الحالة = مرفوض
5.  يتم إرسال إشعار للموظف بالحالة الجديدة

---

 حالات الطلب (LeaveStatus Enum)

- قيد_الانتظار
- موافق_المدير
- مرفوض

---

 نظام الإشعارات (Notifications)

يتم إنشاء إشعار تلقائي في الحالات التالية:
- عند تقديم طلب إجازة
- عند موافقة المدير على الطلب
- عند رفض المدير للطلب

كل موظف يستطيع:
- مشاهدة إشعاراته
- تعليم الإشعار كمقروء

---

 رصيد الإجازات (AnnualLeaveBalance)

- كل موظف يملك رصيد إجازات سنوي
- يتم خصم الرصيد تلقائيًا عند الموافقة على الإجازة
- لا يسمح بالموافقة إذا كان الرصيد غير كافي

---

 العطل الرسمية (Holidays)

- يوجد جدول خاص بالعطل الرسمية
- أيام العطل لا تُحسب ضمن مدة الإجازة
- يتم استبعادها عند حساب TotalDays

---

 المصادقة (Authentication)

- النظام يستخدم JWT Token
- تسجيل الدخول يعيد Token
- يتم تمرير التوكن في Swagger عبر:
---

 العلاقات في النظام

User  
↕  
Employee  
↕  
Manager (EmployeeId)  

Employee  
↕  
LeaveRequests  

LeaveRequests  
↕  
LeaveTypes  

Employee  
↕  
Notifications  

---

 التقنيات المستخدمة

- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Authentication
- Swagger
- DTO Pattern
- Repository Pattern
- Enum Status Handling
- Permission Based Authorization
- Role Based Authorization

---

 ملاحظات تقنية

- لا يمكن إضافة موظف بدون UserId
- لا يمكن ربط موظف كمدير إلا بعد إنشائه
- لا يمكن الموافقة على الإجازة بدون رصيد كافي
- لا يمكن التعامل مع الطلب أكثر من مرة
- لا يمكن تقديم طلب إجازة بدون صلاحية SubmitLeave
- لا يمكن الموافقة/الرفض بدون صلاحية ApproveLeave

---

    الهدف من المشروع
تطبيق عملي لمفاهيم:
- Authentication & Authorization
- Database Relationships
- Business Logic
- Clean Architecture
- HR Systems Flow
- Real World Scenarios