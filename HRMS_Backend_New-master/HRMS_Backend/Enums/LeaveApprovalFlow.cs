namespace HRMS_Backend.Enums
{
    /// <summary>
    /// مسار موافقات الإجازة حسب من يقدّم الطلب (يُحسب عند الإنشاء ويُخزَّن على الطلب).
    /// </summary>
    public enum LeaveApprovalFlow
    {
        /// <summary>موظف عادي: مدير قسم → ثم مدير إدارة فرعية (نهائي)</summary>
        RegularEmployee = 1,

        /// <summary>مدير قسم: مدير إدارة فرعية → ثم مدير إدارة عامة (نهائي)</summary>
        SectionManager = 2,

        /// <summary>مدير إدارة فرعية: مدير إدارة عامة (نهائي) فقط</summary>
        SubDepartmentManager = 3
    }
}
