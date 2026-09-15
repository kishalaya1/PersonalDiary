using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalDiary.Models;
using PersonalDiary.Services;

namespace PersonalDiary.Controllers;

[Authorize]
public class DiaryController(IDiaryService diaryService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var entries = await diaryService.GetEntriesAsync(CurrentUserId, cancellationToken);
        return View(new DiaryIndexViewModel
        {
            Entries = entries,
            MaximumEdits = DiaryService.MaximumEdits
        });
    }

    public IActionResult Create()
    {
        return View(new DiaryEntryInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        DiaryEntryInputModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var created = await diaryService.CreateEntryAsync(
            CurrentUserId,
            model.EntryDate,
            model.Notes,
            cancellationToken);

        if (!created)
        {
            ModelState.AddModelError(nameof(model.EntryDate), "You already have an entry for this date.");
            return View(model);
        }

        TempData["StatusMessage"] = "Your diary entry was saved.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(DateOnly date, CancellationToken cancellationToken)
    {
        var entry = await diaryService.GetEntryAsync(CurrentUserId, date, cancellationToken);
        return entry is null ? NotFound() : View(entry);
    }

    public async Task<IActionResult> Edit(DateOnly date, CancellationToken cancellationToken)
    {
        var entry = await diaryService.GetEntryAsync(CurrentUserId, date, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        if (entry.EditCount >= DiaryService.MaximumEdits)
        {
            TempData["ErrorMessage"] = "This entry has reached the five-edit limit.";
            return RedirectToAction(nameof(Details), new { date = date.ToString("yyyy-MM-dd") });
        }

        return View(new DiaryEntryInputModel
        {
            EntryDate = entry.EntryDate,
            Notes = entry.Notes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        DiaryEntryInputModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await diaryService.UpdateEntryAsync(
            CurrentUserId,
            model.EntryDate,
            model.Notes,
            cancellationToken);

        switch (result)
        {
            case DiaryUpdateResult.NotFound:
                return NotFound();
            case DiaryUpdateResult.EditLimitReached:
                TempData["ErrorMessage"] = "This entry has reached the five-edit limit.";
                return RedirectToAction(nameof(Details), new { date = model.EntryDate.ToString("yyyy-MM-dd") });
            default:
                TempData["StatusMessage"] = "Your diary entry was updated.";
                return RedirectToAction(nameof(Details), new { date = model.EntryDate.ToString("yyyy-MM-dd") });
        }
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The current request has no authenticated user.");
}
