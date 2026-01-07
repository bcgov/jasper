namespace Scv.Models
{
    public class CaseResponse
    {
        /// <summary>
        /// List of cases related to reserved judgments and scheduled decisions.
        /// </summary>
        public List<CaseDto> ReservedJudgments { get; set; } = [];
        /// <summary>
        /// List of cases related to scheduled continuations.
        /// </summary>
        public List<CaseDto> ScheduledContinuations { get; set; } = [];
        /// <summary>
        /// List of other seized cases
        /// </summary>
        public List<CaseDto> Others { get; set; } = [];
        /// <summary>
        /// List of future assigned cases
        /// </summary>
        public List<CaseDto> FutureAssigned { get; set; } = [];
    }
}
