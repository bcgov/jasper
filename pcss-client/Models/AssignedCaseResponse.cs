namespace PCSSCommon.Models;

public class AssignedCaseResponse
{
    public bool Success { get; set; }
    public ICollection<Case> Data { get; set; } = [];
    public string Message { get; set; }
    public string Error { get; set; }
}
