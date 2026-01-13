using PastebinApp.Api.Models.Requests;
using PastebinApp.Api.Models.Responses;
using PastebinApp.Application.DTOs;

namespace PastebinApp.Api.Mapping;

public static class PasteMappingExtensions
{
    public static CreatePasteDto ToDto(this CreatePasteRequest request)
    {
        return new CreatePasteDto
        {
            Content = request.Content,
            ExpirationHours = request.ExpirationHours,
            Language = request.Language,
            Title = request.Title
        };
    }
    
    public static CreatePasteResponse ToResponse(this CreatePasteResultDto dto)
    {
        return new CreatePasteResponse
        {
            Hash = dto.Hash,
            Url = dto.Url,
            CreatedAt = dto.CreatedAt,
            ExpiresAt = dto.ExpiresAt,
            ExpiresInSeconds = dto.ExpiresInSeconds
        };
    }

    public static GetPasteResponse ToResponse(this GetPasteResultDto dto)
    {
        return new GetPasteResponse
        {
            Hash = dto.Hash,
            Content = dto.Content,
            Title = dto.Title,
            Language = dto.Language,
            ContentSizeBytes = dto.ContentSizeBytes,
            CreatedAt = dto.CreatedAt,
            ExpiresAt = dto.ExpiresAt,
            ViewCount = dto.ViewCount,
            ExpiresInSeconds = dto.ExpiresInSeconds,
            IsExpired = dto.IsExpired
        };
    }
}