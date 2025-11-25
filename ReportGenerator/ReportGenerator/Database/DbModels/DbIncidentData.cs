namespace ReportGenerator.Database.DbModels
{
    public class DbIncidentData
    {
        public int Id;
        public int NumerIncident;
        public string RegistrationTime = "";
        public string Service = "";
        public string ShortDescription = "";
        public string Applicant = "";
        public string Priority { get; set; }
        public string Executor = "";
        public string DecisionTime = "";
        public int Status;
        public string Content { get; set; } // Описание инцидента
        public string RealName { get; set; } // Фамилия заявителя
        public string FirstName { get; set; } // Имя заявителя
        public DateTime? SolvedDate { get; set; } // Время решения инцидента
    }
}
