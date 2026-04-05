# Lessons Learned - Arhitectura Proiectului Lab06

## De ce folosim Repository Pattern?

Repository Pattern ne ajută să separăm accesul la date de restul aplicației. În loc să lucrăm direct cu Entity Framework în fiecare loc, folosim un strat intermediar care se ocupă de operațiile CRUD. Asta face codul mai ușor de întreținut, testat și schimbat – de exemplu, dacă vrem să trecem la alt ORM. De asemenea, respectă principiul Single Responsibility: repository-ul se ocupă doar de date, nu de logica de business.

## Ce s-ar întâmpla dacă apelăm _context direct din controller?

Dacă apelăm direct `AppDbContext` din controller, încălcăm principiile de separare a preocupărilor. Controller-ul devine strâns legat de EF, ceea ce îl face greu de testat (nu poți să mock-uiești context-ul ușor), greu de întreținut și vulnerabil la schimbări în baza de date. Logica de business se amestecă cu cea de prezentare, încălcând MVC. Poate duce la cuplare strânsă și probleme de performanță dacă nu gestionăm bine context-ul.

## De ce avem un Service Layer separat?

Service Layer-ul conține logica de business a aplicației. E separat pentru a ține controller-ul subțire și focalizat doar pe gestionarea cererilor HTTP. Service-ul coordonează operațiile între repository-uri, validează datele și aplică reguli de business. Asta face codul mai modular, ușor de testat unitar și reutilizabil.

## Ce logică ar ajunge în controller fără el?

Fără Service Layer, toată logica de business – validări, transformări de date, reguli complexe – ar ajunge în controller. Controller-ul ar deveni umflat, greu de citit și testat. De exemplu, în loc să apeleze simplu `articleService.CreateArticle(viewModel)`, controller-ul ar trebui să valideze manual datele, să mapeze view model-ul la entitate, să salveze în repository și să gestioneze erorile. Asta încalcă Single Responsibility și face codul mai predispus la bug-uri.

## De ce folosim interfețe (IArticleRepository, IArticleService)?

Interfețele permit Dependency Injection, făcând codul mai flexibil și testabil. În loc să instanțiem direct clasele concrete, injectăm interfețele. Asta ne permite să mock-uim dependențele în teste unitare, să schimbăm implementările fără să modificăm codul dependent și să folosim container-ul DI din ASP.NET Core pentru gestionarea lifecycle-ului obiectelor. De asemenea, promovează Inversion of Control.

## Dați un exemplu concret din cod.

Un exemplu concret e folosirea interfețelor `IArticleRepository` și `IArticleService` pentru Dependency Injection. Iată interfața repository-ului:

```csharp
public interface IArticleRepository : IRepository<Article>
{
    Task<List<Article>> GetAllWithDetailsAsync();
    Task<Article?> GetByIdWithDetailsAsync(int id);
    Task<List<Article>> GetByCategoryAsync(int categoryId);
    Task<int> CountAsync();
    Task<List<Article>> GetPagedAsync(int page, int pageSize);
}
```

Implementarea în `ArticleRepository`:

```csharp
public class ArticleRepository : Repository<Article>, IArticleRepository
{
    public ArticleRepository(AppDbContext context) : base(context) { }

    public async Task<List<Article>> GetAllWithDetailsAsync()
    {
        return await _context.Articles
            .Include(a => a.Category)
            .Include(a => a.User)
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync();
    }
    // ... alte metode
}
```

În `ArticleService`, injectăm `IUnitOfWork`:

```csharp
public class ArticleService : IArticleService
{
    private readonly IUnitOfWork _unitOfWork;

    public ArticleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task AddAsync(Article article)
    {
        article.PublishedAt = DateTime.Now; // logică de business
        await _unitOfWork.ArticleRepository.AddAsync(article);
        await _unitOfWork.SaveChangesAsync();
    }
    // ... alte metode
}
```

În `ArticlesController`, injectăm serviciile:

```csharp
public class ArticlesController : Controller
{
    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;

    public ArticlesController(
        IArticleService articleService,
        ICategoryService categoryService
    )
    {
        _articleService = articleService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Create()
    {
        var categories = await _categoryService.GetAllAsync();
        ViewData["CategoryId"] = new SelectList(categories, "Id", "Name");
        return View();
    }
    // ... alte acțiuni
}
```

Asta permite teste ușoare cu mock-uri și separare clară a responsabilităților.

## Cum vă ajută această structură dacă adăugați:

### -un API REST

Structura ajută mult pentru un API REST, deoarece logica de business și accesul la date sunt deja separate. Poți crea noi controller-e API care folosesc aceleași servicii, fără să duplicați codul. De exemplu, endpoint-ul `/api/articles` poate apela direct `articleService.GetAllArticles()`, reutilizând validările existente. Asta accelerează dezvoltarea și menține consistența între MVC și API.

### -sau o aplicație mobilă pe același proiect?

Pentru o aplicație mobilă, expui API-ul REST ca backend, iar Service Layer-ul și Repository-ul rămân neschimbate pe server. App-ul mobil consumă date prin API, cu logica de business centralizată. Dacă proiectul include MAUI, poți reutiliza Service Layer-ul pentru logică comună. De exemplu, validările din `ArticleService` se aplică pentru web și mobile, evitând duplicarea.

## Vizioneaza AICI demo-ul

[Vizioneaza demo-ul pe YouTube](https://youtu.be/KLxxIs5rc6U)