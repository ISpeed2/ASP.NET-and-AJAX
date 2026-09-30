using Asp.Versioning;
using BookStore.Api.Models;
using BookStore.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace BookStore.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[Route("api/v{version:apiVersion}/books")]
[Route("api/books")]
[Produces("application/json")]
public class BooksController : ControllerBase
{
    private readonly IBookRepository _repo;
    private readonly IFeatureManager _features;
    private readonly ILogger<BooksController> _logger;

    public BooksController(
        IBookRepository repo,
        IFeatureManager features,
        ILogger<BooksController> logger)
    {
        _repo = repo;
        _features = features;
        _logger = logger;
    }

    /// <summary>Получить список книг (v1).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BookV1Response>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var useFullName = await _features.IsEnabledAsync("UseFullNameField");

        var result = _repo.GetAll().Select(book => new
        {
            book.Id,
            book.Title,
            book.Author,
            FullName = useFullName ? book.Author : null,
            book.Price,
            book.Isbn
        });

        return Ok(result);
    }

    /// <summary>Получить книгу по id (v1).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BookV1Response), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<BookV1Response> GetById(int id)
    {
        var book = _repo.GetById(id);
        if (book is null)
        {
            _logger.LogInformation("Book {Id} not found", id);
            return NotFound(new { message = $"Book {id} not found" });
        }

        return Ok(ToResponse(book));
    }

    /// <summary>Создать книгу (v1).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BookV1Response), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<BookV1Response> Create([FromBody] CreateBookRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var created = _repo.Add(new Book
        {
            Title = request.Title,
            Author = request.Author,
            Price = request.Price,
            Isbn = request.Isbn
        });

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = created.Id },
            ToResponse(created));
    }

    /// <summary>Удалить книгу (v1).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id) => _repo.Delete(id) ? NoContent() : NotFound();

    private static BookV1Response ToResponse(Book book) => new()
    {
        Id = book.Id,
        Title = book.Title,
        Author = book.Author,
        Price = book.Price,
        Isbn = book.Isbn
    };
}
