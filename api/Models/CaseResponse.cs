﻿
using System.Collections.Generic;

namespace Scv.Api.Models;

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
}
