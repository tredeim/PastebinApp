using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PastebinApp.Api.Mapping;
using PastebinApp.Api.Models.Requests;
using PastebinApp.Api.Models.Responses;
using PastebinApp.Application.DTOs;
using PastebinApp.Application.Interfaces;
using PastebinApp.Domain.Exceptions;

namespace PastebinApp.Api.Controllers;

[ApiController]
[Route("api/pastes")]
[Produces("application/json")]
public class PasteController : ControllerBase
{
    private readonly IPasteService _pasteService;
    private readonly IValidator<CreatePasteDto> _validator;
    private readonly ILogger<PasteController> _logger;

    public PasteController(
        IPasteService pasteService,
        IValidator<CreatePasteDto> validator,
        ILogger<PasteController> logger)
    {
        _pasteService = pasteService;
        _validator = validator;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreatePasteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePaste(
        [FromBody] CreatePasteRequest request,
        CancellationToken cancellationToken)
    {
        var dto = request.ToDto();
        
        var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => new ValidationError
            {
                Field = e.PropertyName,
                Message = e.ErrorMessage
            });

            return BadRequest(new ValidationErrorResponse
            {
                Errors = errors
            });
        }

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            
            var serviceResult = await _pasteService.CreatePasteAsync(dto, baseUrl, cancellationToken);
            
            var response = serviceResult.ToResponse();

            _logger.LogInformation("Paste created successfully: {Hash}", response.Hash);
            
            return CreatedAtAction(
                nameof(GetPaste),
                new { hash = response.Hash },
                response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating paste");
            return StatusCode(500, new ErrorResponse
            {
                Error = "Failed to create paste",
                Details = ex.Message
            });
        }
    }
    
    [HttpGet("{hash}")]
    [ProducesResponseType(typeof(GetPasteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status410Gone)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaste(
        string hash,
        CancellationToken cancellationToken)
    {
        try
        {
            var serviceResult = await _pasteService.GetPasteAsync(hash, cancellationToken);
            
            var response = serviceResult.ToResponse();

            return Ok(response);
        }
        catch (PasteNotFoundException ex)
        {
            _logger.LogWarning("Paste not found: {Hash}", hash);
            return NotFound(new ErrorResponse
            {
                Error = ex.Message
            });
        }
        catch (PasteExpiredException ex)
        {
            _logger.LogInformation("Paste expired: {Hash}", hash);
            return StatusCode(410, new ErrorResponse
            {
                Error = "Paste has expired",
                Details = new
                {
                    hash = ex.Hash,
                    expiredAt = ex.ExpiredAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paste: {Hash}", hash);
            return StatusCode(500, new ErrorResponse
            {
                Error = "Failed to get paste",
                Details = ex.Message
            });
        }
    }
    
    [HttpDelete("{hash}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeletePaste(
        string hash,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _pasteService.DeletePasteAsync(hash, cancellationToken);

            if (!deleted)
            {
                return NotFound(new ErrorResponse
                {
                    Error = $"Paste '{hash}' not found"
                });
            }

            _logger.LogInformation("Paste deleted: {Hash}", hash);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting paste: {Hash}", hash);
            return StatusCode(500, new ErrorResponse
            {
                Error = "Failed to delete paste",
                Details = ex.Message
            });
        }
    }
}