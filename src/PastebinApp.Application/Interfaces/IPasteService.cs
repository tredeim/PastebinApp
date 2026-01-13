using PastebinApp.Application.DTOs;

namespace PastebinApp.Application.Interfaces;

public interface IPasteService
{
    Task<CreatePasteResultDto> CreatePasteAsync(
        CreatePasteDto dto,
        string baseUrl,
        CancellationToken cancellationToken = default);

    Task<GetPasteResultDto> GetPasteAsync(
        string hash,
        CancellationToken cancellationToken = default);

    Task<bool> DeletePasteAsync(
        string hash,
        CancellationToken cancellationToken = default);
}