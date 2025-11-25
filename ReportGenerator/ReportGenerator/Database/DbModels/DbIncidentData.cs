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
        public int Priority;
        public string Executor = "";
        public string DecisionTime = "";
        public int Status;
    }
}
