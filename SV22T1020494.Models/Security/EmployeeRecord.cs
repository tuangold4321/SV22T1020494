namespace SV22T1020494.Models.Security
{
    public class EmployeeRecord
    {
        public int EmployeeID { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Photo { get; set; } = "";
        public string RoleNames { get; set; } = "";
        public string Password { get; set; } = "";
        public bool IsWorking { get; set; } = true;
    }
}
