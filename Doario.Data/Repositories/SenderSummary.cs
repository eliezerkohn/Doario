using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doario.Data.Repositories;

/// <summary>
/// Lightweight projection — one row per unique sender seen at this tenant.
/// Not a DB entity. Used for the sender search dropdown.
/// </summary>
public class SenderSummary
{
    public string DisplayName { get; set; }
    public string Email { get; set; }
    public int DocumentCount { get; set; }
}
