using Doario.Data.Models.Mail;
using Doario.Data.Repositories;
using Doario.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Doario.Web.Controllers;

/// <summary>
/// Handles token-based staff actions triggered from email buttons.
/// No login required — authenticated via StaffAccessToken in the URL.
/// Tokens expire 30 days after assignment.
/// </summary>
[ApiController]
[Route("api/staff-action")]
public class StaffActionController : ControllerBase
{
    private readonly IAssignmentRepository _assignments;
    private readonly IDocumentRepository _documents;
    private readonly IDocumentExtractionResultRepository _extractionResults;
    private readonly IStaffRepository _staff;
    private readonly SharePointService _sharePoint;
    private readonly ILogger<StaffActionController> _logger;
    private readonly IConfiguration _config;

    public StaffActionController(
        IAssignmentRepository assignments,
        IDocumentRepository documents,
        IDocumentExtractionResultRepository extractionResults,
        IStaffRepository staff,
        SharePointService sharePoint,
        ILogger<StaffActionController> logger,
        IConfiguration config)
    {
        _assignments = assignments;
        _documents = documents;
        _extractionResults = extractionResults;
        _staff = staff;
        _sharePoint = sharePoint;
        _logger = logger;
        _config = config;
    }

    // ── Validate token helper ─────────────────────────────────────────────────

    private async Task<(Doario.Data.Models.Mail.DocumentAssignment Assignment, IActionResult Error)>
        ValidateTokenAsync(Guid documentId, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (null, BadRequest(new { error = "Missing token." }));

        var assignment = await _assignments.GetByDocumentAndTokenAsync(documentId, token);

        if (assignment is null)
            return (null, Content(BuildErrorPage("Invalid or expired link."), "text/html"));

        if (assignment.StaffAccessTokenExpiresAt < DateTime.UtcNow)
            return (null, Content(BuildErrorPage("This link has expired."), "text/html"));

        return (assignment, null);
    }

    // ── Mark as Actioned ──────────────────────────────────────────────────────

    [HttpPost("action/{documentId}/{token}")]
    [HttpGet("action/{documentId}/{token}")]
    public async Task<IActionResult> MarkActioned(Guid documentId, string token)
    {
        var (assignment, error) = await ValidateTokenAsync(documentId, token);
        if (error is not null) return error;

        await _documents.UpdateStatusAsync(documentId, 4); // 4 = Actioned

        _logger.LogInformation(
            "StaffAction: Document {DocId} marked as actioned by token.", documentId);

        return Content(BuildPopupPage(
            "✅ Marked as Actioned",
            "This document has been marked as actioned.",
            "#16a34a",
            showClose: true), "text/html");
    }

    // ── Get Extraction Fields ─────────────────────────────────────────────────

    [HttpGet("extraction/{documentId}/{token}")]
    public async Task<IActionResult> GetExtraction(Guid documentId, string token)
    {
        var (assignment, error) = await ValidateTokenAsync(documentId, token);
        if (error is not null) return error;

        var document = await _documents.GetByIdWithTenantAsync(documentId, assignment.TenantId);
        if (document is null)
            return NotFound(new { error = "Document not found." });

        var fields = await _extractionResults.GetByDocumentAsync(documentId, assignment.TenantId);

        return Ok(new
        {
            documentId = document.DocumentId,
            fileName = document.OriginalFileName,
            sharePointUrl = document.SharePointUrl,
            aiSummary = document.AiSummary,
            uploadedAt = document.UploadedAt,
            fields = fields.Select(f => new
            {
                f.DocumentExtractionResultId,
                f.FieldName,
                f.FieldValue,
                f.IsConfirmed,
                f.CorrectedValue,
                f.PageNumber,
                f.BoundingBox,
            }).ToList()
        });
    }

    // ── Confirm / Correct Extraction Field ────────────────────────────────────

    [HttpPost("extraction/{documentId}/{token}/confirm")]
    public async Task<IActionResult> ConfirmField(
        Guid documentId,
        string token,
        [FromBody] ConfirmFieldRequest request)
    {
        var (assignment, error) = await ValidateTokenAsync(documentId, token);
        if (error is not null) return error;

        await _extractionResults.UpdateConfirmationAsync(
            request.DocumentExtractionResultId,
            request.IsConfirmed,
            request.CorrectedValue ?? string.Empty);

        return Ok(new { success = true });
    }

    // ── Verify Extraction redirect ────────────────────────────────────────────

    [HttpGet("verify/{documentId}/{token}")]
    public async Task<IActionResult> VerifyExtraction(Guid documentId, string token)
    {
        var (assignment, error) = await ValidateTokenAsync(documentId, token);
        if (error is not null) return error;

        var baseUrl = _config["Doario:BaseUrl"] ?? "https://doario.com";
        return Redirect($"{baseUrl}/verify-extraction/{documentId}/{token}");
    }

    // ── PDF Proxy — stream PDF from SharePoint to browser ────────────────────

    [HttpGet("pdf/{documentId}/{token}")]
    public async Task<IActionResult> GetPdf(Guid documentId, string token)
    {
        var (assignment, error) = await ValidateTokenAsync(documentId, token);
        if (error is not null) return error;

        var document = await _documents.GetByIdWithTenantAsync(documentId, assignment.TenantId);
        if (document is null)
            return NotFound(new { error = "Document not found." });

        try
        {
            var (bytes, contentType) = await _sharePoint.DownloadFileAsync(
                assignment.TenantId, document.SharePointUrl);

            Response.Headers.Append("Content-Disposition",
                $"inline; filename=\"{document.OriginalFileName}\"");

            return File(bytes, contentType ?? "application/pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StaffAction: Failed to proxy PDF for Document {DocId}.", documentId);
            return StatusCode(500, new { error = "Failed to load document." });
        }
    }

    // ── Add Note ──────────────────────────────────────────────────────────────

    [HttpGet("note/{documentId}/{token}")]
    public async Task<IActionResult> AddNote(Guid documentId, string token)
    {
        var (assignment, error) = await ValidateTokenAsync(documentId, token);
        if (error is not null) return error;

        var document = await _documents.GetByIdWithTenantAsync(documentId, assignment.TenantId);
        if (document is null)
            return Content(BuildErrorPage("Document not found."), "text/html");

        return Content(BuildNotePage(documentId, token, document.OriginalFileName, assignment.Note), "text/html");
    }

    [HttpPost("note/{documentId}/{token}")]
    public async Task<IActionResult> SubmitNote(
        Guid documentId,
        string token,
        [FromForm] string note)
    {
        var (assignment, error) = await ValidateTokenAsync(documentId, token);
        if (error is not null) return error;

        if (string.IsNullOrWhiteSpace(note))
            return Content(BuildErrorPage("Note cannot be empty."), "text/html");

        var trimmed = note.Trim();
        if (trimmed.Length > 1000) trimmed = trimmed[..1000];

        await _assignments.UpdateNoteAsync(assignment.DocumentAssignmentId, trimmed);

        _logger.LogInformation(
            "StaffAction: Note added to Document {DocId} by token.", documentId);

        return Content(BuildPopupPage(
            "💬 Note Saved",
            "Your note has been saved and will be visible to your admin.",
            "#7c3aed",
            showClose: true), "text/html");
    }

    // ── Forward ───────────────────────────────────────────────────────────────

    [HttpGet("forward/{documentId}/{token}")]
    public async Task<IActionResult> Forward(Guid documentId, string token)
    {
        var (assignment, error) = await ValidateTokenAsync(documentId, token);
        if (error is not null) return error;

        var document = await _documents.GetByIdWithTenantAsync(documentId, assignment.TenantId);
        if (document is null)
            return Content(BuildErrorPage("Document not found."), "text/html");

        var staffList = await _staff.GetAllForTenantAsync(assignment.TenantId);
        var activeStaff = staffList.Where(s => s.IsActive).ToList();

        return Content(BuildForwardPage(documentId, token, document.OriginalFileName, activeStaff), "text/html");
    }

    [HttpPost("forward/{documentId}/{token}")]
    public async Task<IActionResult> SubmitForward(
        Guid documentId,
        string token,
        [FromForm] string toEmail,
        [FromForm] string message)
    {
        var (assignment, error) = await ValidateTokenAsync(documentId, token);
        if (error is not null) return error;

        if (string.IsNullOrWhiteSpace(toEmail))
            return Content(BuildErrorPage("Email address is required."), "text/html");

        _logger.LogInformation(
            "StaffAction: Document {DocId} forwarded to {Email} by token.", documentId, toEmail);

        return Content(BuildPopupPage(
            "↗ Document Forwarded",
            $"The document has been forwarded to {Enc(toEmail)}.",
            "#2563eb",
            showClose: true), "text/html");
    }

    // ── HTML builders ─────────────────────────────────────────────────────────

    private static string PopupShell(string title, string bodyContent) => $@"<!DOCTYPE html>
<html><head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width,initial-scale=1"">
<title>{Enc(title)}</title>
<style>
  * {{ box-sizing: border-box; margin: 0; padding: 0; }}
  body {{
    font-family: 'Segoe UI', Arial, sans-serif;
    background: rgba(0,0,0,0.35);
    display: flex;
    align-items: center;
    justify-content: center;
    min-height: 100vh;
    padding: 16px;
  }}
  .card {{
    background: #fff;
    border-radius: 14px;
    padding: 32px 28px 24px;
    width: 100%;
    max-width: 420px;
    box-shadow: 0 8px 40px rgba(0,0,0,0.18);
    animation: pop 0.18s ease;
  }}
  @keyframes pop {{
    from {{ transform: scale(0.94); opacity: 0; }}
    to   {{ transform: scale(1);    opacity: 1; }}
  }}
  .logo {{
    font-size: 13px;
    font-weight: 800;
    color: #0f2d4a;
    letter-spacing: -0.3px;
    margin-bottom: 20px;
    opacity: 0.5;
  }}
  .logo span {{ color: #0d9488; }}
  h1 {{ font-size: 17px; font-weight: 700; color: #0f2d4a; margin-bottom: 6px; }}
  .sub {{ font-size: 13px; color: #6b7280; margin-bottom: 20px; }}
  label {{ font-size: 12px; font-weight: 600; color: #374151; display: block; margin-bottom: 5px; }}
  input[type=email], input[type=text], textarea, select {{
    width: 100%;
    padding: 9px 12px;
    border: 1px solid #d1d5db;
    border-radius: 8px;
    font-size: 14px;
    font-family: inherit;
    outline: none;
    transition: border 0.15s;
    margin-bottom: 14px;
  }}
  input:focus, textarea:focus, select:focus {{ border-color: #0d9488; }}
  textarea {{ resize: vertical; min-height: 90px; }}
  .btn-primary {{
    width: 100%;
    padding: 11px;
    background: #0f2d4a;
    color: #fff;
    border: none;
    border-radius: 8px;
    font-size: 14px;
    font-weight: 700;
    cursor: pointer;
    font-family: inherit;
    transition: background 0.15s;
  }}
  .btn-primary:hover {{ background: #1a3f66; }}
  .btn-close {{
    width: 100%;
    margin-top: 10px;
    padding: 9px;
    background: none;
    color: #9ca3af;
    border: 1px solid #e5e7eb;
    border-radius: 8px;
    font-size: 13px;
    cursor: pointer;
    font-family: inherit;
  }}
  .btn-close:hover {{ color: #374151; border-color: #d1d5db; }}
  .success-icon {{ font-size: 44px; text-align: center; margin-bottom: 14px; }}
  .divider {{ height: 1px; background: #f3f4f6; margin: 16px 0; }}
  .existing-note {{
    background: #f9fafb;
    border: 1px solid #e5e7eb;
    border-radius: 8px;
    padding: 10px 12px;
    font-size: 13px;
    color: #374151;
    margin-bottom: 14px;
    line-height: 1.5;
  }}
  .existing-note-label {{
    font-size: 11px;
    font-weight: 700;
    color: #9ca3af;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    margin-bottom: 4px;
  }}
</style>
</head>
<body>
<div class=""card"">
  <div class=""logo"">Do<span>a</span>rio</div>
  {bodyContent}
</div>
<script>
  function closeWindow() {{
    window.close();
    setTimeout(() => document.body.innerHTML = '<div style=""text-align:center;padding:40px;font-family:Segoe UI,sans-serif;color:#6b7280"">You can close this tab.</div>', 300);
  }}
</script>
</body></html>";

    private static string BuildPopupPage(string title, string message, string color, bool showClose = true)
    {
        var emoji = title.Split(' ')[0];
        var titleText = title.Contains(' ') ? title[(title.IndexOf(' ') + 1)..] : title;
        var body = $@"
<div class=""success-icon"">{emoji}</div>
<h1 style=""color:{color}"">{Enc(titleText)}</h1>
<p class=""sub"">{Enc(message)}</p>
{(showClose ? @"<button class=""btn-close"" onclick=""closeWindow()"">Close this window</button>" : "")}";
        return PopupShell(title, body);
    }

    private static string BuildNotePage(Guid documentId, string token, string fileName, string existingNote)
    {
        var existingSection = !string.IsNullOrWhiteSpace(existingNote)
            ? $@"<div class=""existing-note-label"">Current note</div>
                 <div class=""existing-note"">{Enc(existingNote)}</div>
                 <div class=""divider""></div>"
            : string.Empty;

        var body = $@"
<h1>💬 Add Note</h1>
<p class=""sub"" style=""margin-bottom:16px"">{Enc(fileName)}</p>
{existingSection}
<form method=""post"" action=""/api/staff-action/note/{documentId}/{token}"">
  <label>Your note</label>
  <textarea name=""note"" placeholder=""Enter your note here..."" maxlength=""1000"" autofocus></textarea>
  <button type=""submit"" class=""btn-primary"">Save Note</button>
</form>
<button class=""btn-close"" onclick=""closeWindow()"">Cancel</button>";
        return PopupShell("Add Note", body);
    }

    private static string BuildForwardPage(
        Guid documentId,
        string token,
        string fileName,
        List<Doario.Data.Models.Mail.ImportedStaff> staffList)
    {
        var staffOptions = staffList.Any()
            ? "<option value=\"\">— Select a colleague —</option>\n" +
              string.Join("\n", staffList.Select(s =>
                $"<option value=\"{Enc(s.Email)}\">{Enc(s.FirstName)} {Enc(s.LastName)} &lt;{Enc(s.Email)}&gt;</option>"))
            : "<option value=\"\">No staff found</option>";

        var body = $@"
<h1>↗ Forward Document</h1>
<p class=""sub"" style=""margin-bottom:16px"">{Enc(fileName)}</p>
<form method=""post"" action=""/api/staff-action/forward/{documentId}/{token}"">
  <label>Forward to</label>
  <select name=""toEmail"" onchange=""if(this.value) document.getElementById('customEmail').style.display='none'; else document.getElementById('customEmail').style.display='block';"">
    {staffOptions}
  </select>
  <div id=""customEmail"" style=""display:none"">
    <label>Or enter email manually</label>
    <input type=""email"" name=""toEmailManual"" placeholder=""colleague@company.com"" />
  </div>
  <label>Message (optional)</label>
  <textarea name=""message"" placeholder=""Add a message...""></textarea>
  <button type=""submit"" class=""btn-primary"">Forward Document</button>
</form>
<button class=""btn-close"" onclick=""closeWindow()"">Cancel</button>
<script>
  document.querySelector('form').addEventListener('submit', function(e) {{
    var sel = document.querySelector('select[name=toEmail]');
    var manual = document.querySelector('input[name=toEmailManual]');
    if (!sel.value && manual && manual.value) {{
      sel.name = '_toEmail';
      manual.name = 'toEmail';
    }}
  }});
</script>";
        return PopupShell("Forward Document", body);
    }

    private static string BuildErrorPage(string message) => $@"<!DOCTYPE html>
<html><head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1"">
<title>Error</title>
<style>
  body {{ font-family: 'Segoe UI',Arial,sans-serif; background: rgba(0,0,0,0.35);
         display:flex; align-items:center; justify-content:center; min-height:100vh; padding:16px; }}
  .card {{ background:#fff; border-radius:14px; padding:32px 28px; max-width:380px; width:100%;
           box-shadow:0 8px 40px rgba(0,0,0,0.18); text-align:center; }}
  h2 {{ color:#dc2626; font-size:16px; margin:12px 0 8px; }}
  p {{ color:#6b7280; font-size:13px; }}
  button {{ margin-top:16px; padding:9px 20px; background:none; border:1px solid #e5e7eb;
            border-radius:8px; font-size:13px; cursor:pointer; }}
</style></head>
<body><div class=""card"">
  <div style=""font-size:36px"">⚠️</div>
  <h2>Something went wrong</h2>
  <p>{Enc(message)}</p>
  <button onclick=""window.close()"">Close</button>
</div></body></html>";

    private static string Enc(string value) =>
        string.IsNullOrEmpty(value) ? string.Empty : System.Net.WebUtility.HtmlEncode(value);
}

// ── Request models ────────────────────────────────────────────────────────────

public class ConfirmFieldRequest
{
    public Guid DocumentExtractionResultId { get; set; }
    public bool IsConfirmed { get; set; }
    public string CorrectedValue { get; set; }
}