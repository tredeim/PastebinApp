using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PastebinApp.Api.Mapping;
using PastebinApp.Api.Models.Requests;
using PastebinApp.Api.Models.Responses;
using PastebinApp.Application.DTOs;
using PastebinApp.Application.Interfaces;

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

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var serviceResult = await _pasteService.CreatePasteAsync(dto, baseUrl, cancellationToken);
        var response = serviceResult.ToResponse();

        _logger.LogInformation("Paste created successfully: {Hash}", response.Hash);

        return CreatedAtAction(
            nameof(GetPaste),
            new { hash = response.Hash },
            response);
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
        var serviceResult = await _pasteService.GetPasteAsync(hash, cancellationToken);
        var response = serviceResult.ToResponse();
        return Ok(response);
    }
    
    [HttpDelete("{hash}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeletePaste(
        string hash,
        CancellationToken cancellationToken)
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
}