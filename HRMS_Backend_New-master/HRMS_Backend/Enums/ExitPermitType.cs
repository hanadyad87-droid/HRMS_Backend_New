namespace HRMS_Backend.Enums
{
    // أنواع أذونات الخروج
    public enum ExitPermitType
    {
        خروج_عاجل,
        خروج_شخصي,
        خروج_طبي
    }

    // أنواع تعديل البيانات
    public enum DataUpdateField
    {
        الاسم_الكامل,
        الإدارة,
        المسمى_الوظيفي,
        الرقم_الوطني,
        رقم_الهاتف_الأول,
        رقم_الهاتف_الثاني
    }
}
